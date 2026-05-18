using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 배틀 씬의 모든 UI를 조율하는 컨트롤러.
    ///
    /// 손패 갱신 시점:
    ///   - BattleStarted   → 전체 갱신 (첫 5장 드로우)
    ///   - EnemyIntentSelected → 새 플레이어 턴 시작 → 손패 갱신 (턴 드로우)
    ///   - 카드 사용 성공 시 → 사용 카드 애니메이션 후 현재 런타임 손패와 동기화
    ///
    /// HUD / 적 영역 갱신 시점:
    ///   - StateChanged 마다 (HP, 방어막, 에너지, 의도)
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class BattleUIManager : MonoBehaviour
    {
        [Header("핵심 참조")]
        [SerializeField] private BattleManager battleManager;

        [Header("뷰 참조")]
        [SerializeField] private BattleHudView   playerHud;
        [SerializeField] private BattleEnemyView enemyView;
        [SerializeField] private BattleHandView  handView;

        [Header("버튼")]
        [SerializeField] private Button endTurnButton;

        [Header("결과 패널")]
        [SerializeField] private GameObject      resultPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button          resultRestartButton;

        [Header("전투 보상 UI")]
        [Tooltip("설정 시 승리 결과 패널을 표시하지 않고 보상 UI에 위임합니다.")]
        [SerializeField] private BattleRewardUIController rewardUI;

        [Header("페이드")]
        [SerializeField] private CanvasGroup fadeMask;
        [SerializeField] private float       fadeInDuration = 0.5f;

        [Header("턴 알림 텍스트")]
        [Tooltip("화면 중앙에 표시되는 '나의 턴!' 등 알림 텍스트")]
        [SerializeField] private TextMeshProUGUI turnAnnouncementText;
        [SerializeField] private float           announceDuration = 1f;

        [Header("카드 사용 연출 기준점")]
        [SerializeField] private RectTransform cardPlayTarget;

        [Header("타게팅 화살표")]
        [SerializeField] private BattleTargetArrow targetArrow;

        [Header("상단 전투 상태")]
        [Tooltip("TopBar에 표시할 현재 전투 상태 텍스트. 비워두면 기존 Title 텍스트를 재사용합니다.")]
        [SerializeField] private TextMeshProUGUI battleStatusText;

        // ── 내부 상태 ──────────────────────────────────────────────
        private BattleCardView pendingCardView;
        private CardUseMode    pendingUseMode;
        private int            pendingStartedFrame = -1;
        private Canvas         rootCanvas;
        private bool           playerUsedCardThisTurn;

        private enum CardUseMode
        {
            None,
            SingleTarget,
            PlayArea
        }
        private bool           isCardAnimating;   // 카드 사용 애니메이션 진행 중 여부

        // ── 드래그 앤 드롭 카드 플레이 상태 ────────────────────────
        private bool           isDragging;
        private BattleCardView draggedCardView;
        private CardUseMode    dragUseMode;
        private bool           dragTargetArrowShown;
        private Vector3        dragStartCardPosition;


        // ── 라이프사이클 ───────────────────────────────────────────

        private void Awake()
        {
            if (battleManager == null)
                battleManager = Object.FindFirstObjectByType<BattleManager>();

            if (battleManager == null)
            {
                Debug.LogError("[BattleUIManager] BattleManager를 찾을 수 없습니다.");
                return;
            }

            rootCanvas = GetComponent<Canvas>()?.rootCanvas;
            ConfigureTopBattleStatusBar();

            SubscribeEvents();
            SetupButtons();
        }

        private void Start()
        {
            if (resultPanel != null) resultPanel.SetActive(false);

            if (fadeMask != null)
            {
                fadeMask.alpha = 1f;
                fadeMask.DOFade(0f, fadeInDuration).SetEase(Ease.OutQuad);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (handView != null)
            {
                handView.CardSelected -= OnHandCardSelected;
                handView.CardBeginDrag -= OnCardBeginDrag;
                handView.CardDrag -= OnCardDrag;
                handView.CardEndDrag -= OnCardEndDrag;
            }
        }

        // ── 매 프레임: 타게팅 입력 처리 ──────────────────────────

        private void Update()
        {
            if (pendingCardView == null) return;
            if (isCardAnimating) return;

            if (Input.GetMouseButtonDown(1))
            {
                CancelTargeting();
                return;
            }

            if (Input.GetMouseButtonDown(0) && Time.frameCount > pendingStartedFrame)
            {
                if (pendingUseMode == CardUseMode.SingleTarget)
                {
                    if (IsPointerOverEnemy())
                    {
                        TryPlaySelectedCard();
                    }
                }
                else if (pendingUseMode == CardUseMode.PlayArea)
                {
                    if (handView == null || handView.IsScreenPointAboveHand(Input.mousePosition, rootCanvas))
                    {
                        TryPlaySelectedCard();
                    }
                }
            }
        }

        // ── 이벤트 구독 ────────────────────────────────────────────

        private void SubscribeEvents()
        {
            battleManager.BattleStarted              += OnBattleStarted;
            battleManager.StateChanged               += OnStateChanged;
            battleManager.EnemyIntentSelected        += OnEnemyIntentSelected;
            battleManager.TurnStartStatusResolved    += OnTurnStartStatusResolved;
            battleManager.BattleEnded                += OnBattleEnded;
            battleManager.EnemyActionExecuting       += OnEnemyActionExecuting;
            battleManager.EnemyStatusEffectApplied   += OnEnemyStatusEffectApplied;
        }

        private void UnsubscribeEvents()
        {
            if (battleManager == null) return;
            battleManager.BattleStarted              -= OnBattleStarted;
            battleManager.StateChanged               -= OnStateChanged;
            battleManager.EnemyIntentSelected        -= OnEnemyIntentSelected;
            battleManager.TurnStartStatusResolved    -= OnTurnStartStatusResolved;
            battleManager.BattleEnded                -= OnBattleEnded;
            battleManager.EnemyActionExecuting       -= OnEnemyActionExecuting;
            battleManager.EnemyStatusEffectApplied   -= OnEnemyStatusEffectApplied;
        }

        // ── 이벤트 핸들러 ──────────────────────────────────────────

        /// <summary>전투 시작 — 손패 포함 전체 갱신.</summary>
        private void OnBattleStarted(BattleManager manager)
        {
            isCardAnimating = false;
            pendingCardView = null;
            pendingUseMode = CardUseMode.None;
            pendingStartedFrame = -1;
            playerUsedCardThisTurn = false;
            targetArrow?.Hide();

            if (resultPanel != null) resultPanel.SetActive(false);
            if (endTurnButton != null) endTurnButton.gameObject.SetActive(true);

            // 직업 데이터에서 플레이어 스프라이트를 HUD에 코드-side 주입
            // (BattleManager.ActiveJob: GameDataManager 없으면 defaultJob(전사) 사용)
            if (playerHud != null && manager.ActiveJob != null)
            {
                playerHud.SetPlayerSprite(manager.ActiveJob.previewSprite);
                playerHud.SetPlayerFaceSprite(GetPlayerFaceSprite(manager.ActiveJob));
            }

            enemyView?.ResetForBattle(manager.Enemy);
            RefreshHudAndButtons(manager);
            RefreshHand(manager);   // 첫 5장 드로우
            SetBattleStatusText("전투를 시작합니다.");
        }

        private static Sprite GetPlayerFaceSprite(JobClassInfo job)
        {
            if (job == null)
            {
                return null;
            }

            return job.battleFaceSprite != null ? job.battleFaceSprite : job.previewSprite;
        }

        /// <summary>
        /// 상태 변경 — HUD와 버튼만 갱신.
        /// 손패는 여기서 건드리지 않는다 (카드 애니메이션과 충돌 방지).
        /// </summary>
        private void OnStateChanged(BattleManager manager)
        {
            RefreshHudAndButtons(manager);
        }

        /// <summary>
        /// 적 의도 선택 완료 = 새 플레이어 턴 시작 신호.
        /// 새 턴 드로우 후 손패를 갱신한다.
        /// </summary>
        private void OnEnemyIntentSelected(BattleManager manager, EnemyAction intent)
        {
            playerUsedCardThisTurn = false;
            enemyView?.Refresh(manager.Enemy);
            SetBattleStatusText(DescribeEnemyIntent(manager, intent));

            if (manager.PlayerTurnCount > 1)
            {
                ShowTurnAnnouncement("나의 턴!");
                RefreshHand(manager);
            }
        }

        private void OnEnemyActionExecuting(BattleManager manager, EnemyAction action)
        {
            enemyView?.PlayActionAnimation(action.actionType);
            SetBattleStatusText(DescribeEnemyActionExecuting(manager, action));
        }

        private void OnEnemyStatusEffectApplied(BattleManager manager, StatusEffectType type, StatusEffectData data)
        {
            Color color = data != null ? data.displayColor : GetStatusFallbackColor(type);
            enemyView?.PlayStatusAppliedAnim(color);
        }

        private static Color GetStatusFallbackColor(StatusEffectType type) => type switch
        {
            StatusEffectType.Poison       => new Color(0.35f, 0.75f, 0.25f),
            StatusEffectType.Burn         => new Color(1f,    0.35f, 0.08f),
            StatusEffectType.Weak         => new Color(0.60f, 0.60f, 0.90f),
            StatusEffectType.Vulnerable   => new Color(0.98f, 0.44f, 0.09f),
            StatusEffectType.Strength     => new Color(0.95f, 0.30f, 0.27f),
            StatusEffectType.Regeneration => new Color(0.30f, 0.69f, 0.31f),
            StatusEffectType.Dodge        => new Color(0.20f, 0.75f, 0.95f),
            _                             => Color.gray,
        };

        private void OnTurnStartStatusResolved(BattleManager manager,
            BattleCombatantState combatant, BattleStatusTurnResult result)
        {
            if ((result.PoisonDamage > 0 || result.BurnDamage > 0) && combatant == manager.Player?.Combatant)
                playerHud?.PlayDamageFlash();

            RefreshHudAndButtons(manager);
        }

        private void OnBattleEnded(BattleManager manager, BattlePhase phase)
        {
            handView?.SetInteractable(false);
            targetArrow?.Hide();
            pendingCardView?.EndPointerFollow(restoreToHand: true);
            pendingCardView = null;
            pendingUseMode = CardUseMode.None;
            pendingStartedFrame = -1;

            if (endTurnButton != null) endTurnButton.gameObject.SetActive(false);

            // 승리 시 보상 UI가 있으면 결과 패널 표시 건너뜀
            bool showResult = !(phase == BattlePhase.Won && rewardUI != null);
            if (resultPanel != null && showResult)
            {
                resultPanel.SetActive(true);
                if (resultText != null)
                    resultText.text = phase == BattlePhase.Won ? "승리!" : "패배...";

                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }
        }

        // ── 버튼 설정 ──────────────────────────────────────────────

        private void SetupButtons()
        {
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(OnEndTurnClicked);

            if (resultRestartButton != null)
                resultRestartButton.onClick.AddListener(OnRestartClicked);

            if (handView != null)
            {
                handView.CardSelected += OnHandCardSelected;
                handView.CardBeginDrag += OnCardBeginDrag;
                handView.CardDrag += OnCardDrag;
                handView.CardEndDrag += OnCardEndDrag;
            }
        }

        // ── 버튼 핸들러 ────────────────────────────────────────────

        public void OnEndTurnClicked()
        {
            if (battleManager == null || battleManager.Phase != BattlePhase.PlayerTurn) return;

            CancelTargeting();
            handView?.SetInteractable(false);
            SetBattleStatusText("턴을 종료했습니다. 몬스터가 행동합니다.");
            battleManager.EndPlayerTurn();
        }

        private void OnRestartClicked()
        {
            DOTween.KillAll();
            isCardAnimating = false;
            battleManager.StartBattle();
        }

        // ── 손패 카드 선택 ─────────────────────────────────────────

        private void OnHandCardSelected(BattleCardView cardView)
        {
            if (cardView == null)
            {
                CancelTargeting();
                return;
            }

            if (isCardAnimating) return;

            if (pendingCardView != null && pendingCardView != cardView)
            {
                CancelTargeting();
            }

            pendingCardView = cardView;
            pendingUseMode = GetCardUseMode(cardView.RuntimeCard);
            pendingStartedFrame = Time.frameCount;

            if (pendingUseMode == CardUseMode.SingleTarget)
            {
                cardView.EndPointerFollow(restoreToHand: true);
                if (targetArrow != null)
                {
                    targetArrow.Show(cardView.transform.position);
                }
            }
            else
            {
                targetArrow?.Hide();
                cardView.BeginPointerFollow(rootCanvas);
            }
        }

        // ── 드래그 앤 드롭 카드 플레이 ─────────────────────────────

        private void OnCardBeginDrag(BattleCardView cv, UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (battleManager == null || battleManager.Phase != BattlePhase.PlayerTurn) return;
            if (isCardAnimating) return;

            // 기존 클릭-선택 방식 취소 (드래그와 충돌 방지)
            if (pendingCardView != null)
            {
                CancelTargeting();
            }

            isDragging = true;
            draggedCardView = cv;
            dragUseMode = GetCardUseMode(cv.RuntimeCard);
            dragTargetArrowShown = false;
            dragStartCardPosition = cv.transform.position;

            // 카드가 마우스 포인터를 따라다니도록 설정
            cv.BeginPointerFollow(rootCanvas);
        }

        private void OnCardDrag(BattleCardView cv, UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!isDragging || draggedCardView != cv) return;

            if (dragUseMode == CardUseMode.SingleTarget)
            {
                bool isAboveHand = handView == null || handView.IsScreenPointAboveHand(Input.mousePosition, rootCanvas);

                if (isAboveHand)
                {
                    if (!dragTargetArrowShown)
                    {
                        dragTargetArrowShown = true;
                        if (targetArrow != null)
                        {
                            targetArrow.Show(dragStartCardPosition);
                        }
                    }
                }
                else
                {
                    if (dragTargetArrowShown)
                    {
                        dragTargetArrowShown = false;
                        targetArrow?.Hide();
                    }
                }
            }
        }

        private void OnCardEndDrag(BattleCardView cv, UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!isDragging || draggedCardView != cv) return;

            isDragging = false;
            draggedCardView = null;

            if (dragTargetArrowShown)
            {
                dragTargetArrowShown = false;
                targetArrow?.Hide();
            }

            bool isAboveHand = handView == null || handView.IsScreenPointAboveHand(Input.mousePosition, rootCanvas);

            if (!isAboveHand)
            {
                // 손패 영역 내에서 놓은 경우 사용 취소하고 원래 자리로 복원
                cv.EndPointerFollow(restoreToHand: true);
                cv.SetSelected(false);
                handView?.ClearSelection();
                return;
            }

            // 손패 위 영역에서 드래그 해제됨 -> 사용 시도
            if (dragUseMode == CardUseMode.SingleTarget)
            {
                if (IsPointerOverEnemy())
                {
                    // 적 위에서 놓았으므로 카드 사용 실행
                    pendingCardView = cv;
                    pendingUseMode = dragUseMode;
                    pendingStartedFrame = Time.frameCount;

                    TryPlaySelectedCard();
                }
                else
                {
                    // 적 위가 아니면 사용 취소하고 원래 자리로 복원
                    cv.EndPointerFollow(restoreToHand: true);
                    cv.SetSelected(false);
                    handView?.ClearSelection();
                }
            }
            else if (dragUseMode == CardUseMode.PlayArea)
            {
                // 영역형 카드는 손패 영역 위에서 놓으면 무조건 사용 실행
                pendingCardView = cv;
                pendingUseMode = dragUseMode;
                pendingStartedFrame = Time.frameCount;

                TryPlaySelectedCard();
            }
        }


        private CardUseMode GetCardUseMode(BattleRuntimeCard card)
        {
            return RequiresSingleEnemyTarget(card) ? CardUseMode.SingleTarget : CardUseMode.PlayArea;
        }

        private static bool RequiresSingleEnemyTarget(BattleRuntimeCard card)
        {
            if (card == null || card.Data == null)
            {
                return false;
            }

            CardEffectType effectType = card.Data.effectType;
            return effectType == CardEffectType.BasicAttack
                || effectType == CardEffectType.ShieldBash
                || effectType == CardEffectType.DoubleStrike
                || effectType == CardEffectType.BerserkerAttack
                || effectType == CardEffectType.AttackAndDefend
                || effectType == CardEffectType.AttackAndApplyStatus
                || effectType == CardEffectType.AttackAndGainBlockEqualDamage
                || effectType == CardEffectType.ConsumeBlockToDealDamage
                || effectType == CardEffectType.DamageAndApplyStatus
                || effectType == CardEffectType.GrantEnemyStrengthAndRetaliateNext
                || effectType == CardEffectType.MultiHitAttack
                || effectType == CardEffectType.MultiHitAndGainStrength
                || effectType == CardEffectType.FreezeEnemyNextAction
                || effectType == CardEffectType.PlayHandRandomly
                || effectType == CardEffectType.ApplyStatusToEnemy
                || effectType == CardEffectType.ConsumeAllEnergyAndAttack
                || effectType == CardEffectType.AttackAndGainDodge
                || effectType == CardEffectType.MultiHitWithCritFromDodge
                || effectType == CardEffectType.PoisonAndDetonateAllPoison
                || effectType == CardEffectType.AttackAndShuffleBackToDeck;
        }

        private bool IsPointerOverEnemy()
        {
            RectTransform enemyRect = enemyView != null
                ? enemyView.GetComponent<RectTransform>()
                : null;

            if (enemyRect == null)
            {
                return true;
            }

            Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            return RectTransformUtility.RectangleContainsScreenPoint(
                enemyRect, Input.mousePosition, cam);
        }


        private void CancelTargeting()
        {
            targetArrow?.Hide();
            pendingCardView?.EndPointerFollow(restoreToHand: true);
            pendingCardView?.SetSelected(false);
            pendingCardView = null;
            pendingUseMode = CardUseMode.None;
            pendingStartedFrame = -1;
            handView?.ClearSelection();
        }

        // ── 카드 사용 ──────────────────────────────────────────────

        private void TryPlaySelectedCard()
        {
            if (pendingCardView == null || battleManager == null) return;
            if (isCardAnimating) return;

            BattleRuntimeCard card = pendingCardView.RuntimeCard;
            BattleCardView played = pendingCardView;
            CardUseMode useMode = pendingUseMode;

            pendingCardView = null;
            pendingUseMode = CardUseMode.None;
            pendingStartedFrame = -1;
            targetArrow?.Hide();

            Vector2 cardUseScreenPoint = Input.mousePosition;

            // ── 사전 검증 (효과 미적용) ──────────────────────────────
            // EndPointerFollow 전에 먼저 검증한다.
            // 실패 시 restoreToHand: true 로 원위치 복귀 — ReattachCardView 불필요.
            if (!battleManager.CanPlayCard(card))
            {
                played.EndPointerFollow(restoreToHand: true);
                played.SetSelected(false);
                handView?.ClearSelection();
                if (battleManager.Player != null && !battleManager.Player.CanPayEnergy(card))
                    ShakeCard(played);
                return;
            }

            played.EndPointerFollow(restoreToHand: false);

            handView?.DetachCardView(played);
            isCardAnimating = true;
            handView?.SetInteractable(false);

            bool cardEffectResolved = false;
            bool cardFlyCompleted = false;

            void CompleteCardUseIfReady()
            {
                if (!cardEffectResolved || !cardFlyCompleted)
                    return;

                isCardAnimating = false;

                if (battleManager.Phase == BattlePhase.PlayerTurn)
                {
                    handView?.SetInteractable(true);

                    // 카드 효과가 방어/피해/무료 카드 트리거를 통해 간접 드로우를 만들 수 있으므로
                    // effectType 추정 대신 현재 런타임 손패를 항상 동기화한다.
                    RefreshHand(battleManager);
                }
            }

            void ResolveCardEffect()
            {
                if (cardEffectResolved)
                    return;

                battleManager.SetTriggeredDamageDeferred(true);
                BattleStateSnapshot before = CaptureBattleState();
                BattleCardPlayResult result = battleManager.PlayCard(card);
                BattleStateSnapshot after = CaptureBattleState();
                List<BattleTriggeredDamageRequest> triggeredDamageRequests =
                    battleManager.ConsumePendingTriggeredDamageRequests();

                if (!result.Success)
                {
                    battleManager.SetTriggeredDamageDeferred(false);
                    Debug.LogWarning("[BattleUIManager] 카드 효과 발동 실패: " + result.FailureReason);
                }
                else
                {
                    playerUsedCardThisTurn = true;
                    SetBattleStatusText(DescribeCardPlay(card, before, after, triggeredDamageRequests));
                }

                if (triggeredDamageRequests.Count > 0 && playerHud != null)
                {
                    ResolveTriggeredDamageRequests(triggeredDamageRequests, 0);
                    return;
                }

                foreach (BattleTriggeredDamageRequest request in triggeredDamageRequests)
                {
                    battleManager.ResolveTriggeredDamage(request);
                }

                battleManager.SetTriggeredDamageDeferred(false);
                FinishCardEffectResolution();
            }

            void ResolveTriggeredDamageRequests(List<BattleTriggeredDamageRequest> requests, int index)
            {
                // 전투가 끝났으면(적 사망 등) 남은 요청을 모두 건너뜀
                if (battleManager.Phase != BattlePhase.PlayerTurn)
                {
                    battleManager.SetTriggeredDamageDeferred(false);
                    FinishCardEffectResolution();
                    return;
                }

                if (index >= requests.Count)
                {
                    List<BattleTriggeredDamageRequest> chainedRequests =
                        battleManager.ConsumePendingTriggeredDamageRequests();
                    if (chainedRequests.Count > 0)
                    {
                        requests.AddRange(chainedRequests);
                        ResolveTriggeredDamageRequests(requests, index);
                        return;
                    }

                    battleManager.SetTriggeredDamageDeferred(false);
                    FinishCardEffectResolution();
                    return;
                }

                BattleTriggeredDamageRequest request = requests[index];

                // 다단 히트는 전용 서브히트 애니메이션, 연쇄 피해(가시 방벽 등)는 반격 애니메이션
                if (request.IsMultiHit && playerHud != null)
                {
                    playerHud.PlayMultiHitSubAnim(
                        () => battleManager.ResolveTriggeredDamage(request),
                        () => ResolveTriggeredDamageRequests(requests, index + 1));
                }
                else
                {
                    playerHud.PlayTriggeredAttackAnim(
                        () => battleManager.ResolveTriggeredDamage(request),
                        () => ResolveTriggeredDamageRequests(requests, index + 1));
                }
            }

            void FinishCardEffectResolution()
            {
                cardEffectResolved = true;
                CompleteCardUseIfReady();
            }

            // ── 플레이어 애니메이션 → 피크에서 카드 효과 발동 ──────
            if (playerHud != null)
                playerHud.PlayCardUsedAnimation(card.Data, ResolveCardEffect);
            else
                ResolveCardEffect();

            // 카드 퇴장 애니메이션 동시 시작 → 효과 처리까지 끝난 뒤 손패 갱신
            played.PlayCardUseAnimation(cardUseScreenPoint, card.IsExhaust, () =>
            {
                cardFlyCompleted = true;
                CompleteCardUseIfReady();
            });
        }

        private Vector3 GetCardPlayTargetWorld()
        {
            if (cardPlayTarget != null)
                return cardPlayTarget.position;

            Camera cam = Camera.main;
            if (cam != null)
                return cam.ScreenToWorldPoint(
                    new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 10f));

            return Vector3.zero;
        }

        private void ConfigureTopBattleStatusBar()
        {
            Transform topBar = null;
            if (battleStatusText == null)
            {
                topBar = FindChildByName(transform, "TopBar");
                if (topBar != null)
                {
                    TextMeshProUGUI[] topTexts = topBar.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (TextMeshProUGUI tmp in topTexts)
                    {
                        if (tmp != null && tmp.gameObject.name == "Title")
                        {
                            battleStatusText = tmp;
                            break;
                        }
                    }

                    if (battleStatusText == null)
                    {
                        GameObject statusGo = new GameObject("BattleStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
                        statusGo.transform.SetParent(topBar, false);
                        battleStatusText = statusGo.GetComponent<TextMeshProUGUI>();
                    }

                    foreach (TextMeshProUGUI tmp in topTexts)
                    {
                        if (tmp == null || tmp == battleStatusText)
                        {
                            continue;
                        }

                        if (tmp.gameObject.name == "BattleLabel")
                        {
                            tmp.gameObject.SetActive(false);
                        }
                    }
                }
            }
            else
            {
                topBar = battleStatusText.transform.parent;
            }

            if (battleStatusText == null)
            {
                return;
            }

            battleStatusText.gameObject.name = "BattleStatusText";
            battleStatusText.text = string.Empty;
            battleStatusText.alignment = TextAlignmentOptions.Center;
            battleStatusText.fontSize = 20f;
            battleStatusText.fontStyle = FontStyles.Bold;
            battleStatusText.color = new Color(0.88f, 0.94f, 1f);
            battleStatusText.textWrappingMode = TextWrappingModes.NoWrap;
            battleStatusText.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform rt = battleStatusText.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(24f, 0f);
            rt.offsetMax = new Vector2(-204f, 0f);
            rt.anchoredPosition = Vector2.zero;

            playerHud?.MoveTurnTextToTopBar(topBar);
        }

        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root)
            {
                if (child.name == childName)
                {
                    return child;
                }

                Transform found = FindChildByName(child, childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void SetBattleStatusText(string message)
        {
            if (battleStatusText == null)
            {
                return;
            }

            battleStatusText.text = string.IsNullOrWhiteSpace(message)
                ? "전투 상황을 확인하세요."
                : message;
        }

        private string DescribeEnemyIntent(BattleManager manager, EnemyAction intent)
        {
            if (intent == null)
            {
                return "몬스터의 다음 행동을 살피는 중입니다.";
            }

            string enemyName = manager?.Enemy?.Data?.enemyName;
            if (string.IsNullOrWhiteSpace(enemyName))
            {
                enemyName = "몬스터";
            }

            string statusName = intent.statusEffect != null ? intent.statusEffect.effectName : "상태이상";
            int stacks = Mathf.Max(1, intent.statusEffectStacks);

            return intent.actionType switch
            {
                EnemyActionType.Attack => $"{enemyName}은(는) 다음 턴에 플레이어에게 {Mathf.Max(0, intent.value)} 데미지를 주려 합니다.",
                EnemyActionType.Defend => $"{enemyName}은(는) 다음 턴에 방어도 {Mathf.Max(0, intent.value)}을 얻어 버티려 합니다.",
                EnemyActionType.Buff => $"{enemyName}은(는) 다음 턴에 자신에게 {statusName} {stacks} 효과를 얻으려 합니다.",
                EnemyActionType.DebuffPlayer => $"{enemyName}은(는) 다음 턴에 플레이어에게 {statusName} {stacks} 효과를 부여하려 합니다.",
                EnemyActionType.HealSelf => $"{enemyName}은(는) 다음 턴에 HP {Mathf.Max(0, intent.value)}을 회복하려 합니다.",
                _ => $"{enemyName}의 다음 행동을 알 수 없습니다.",
            };
        }

        private string DescribeEnemyActionExecuting(BattleManager manager, EnemyAction action)
        {
            if (action == null)
            {
                return "몬스터가 행동합니다.";
            }

            string enemyName = manager?.Enemy?.Data?.enemyName;
            if (string.IsNullOrWhiteSpace(enemyName))
            {
                enemyName = "몬스터";
            }

            return $"{enemyName}이(가) {action.GetIntentDescription()} 행동을 실행합니다.";
        }

        private BattleStateSnapshot CaptureBattleState()
        {
            return new BattleStateSnapshot(
                battleManager?.Enemy?.Combatant?.CurrentHp ?? 0,
                battleManager?.Enemy?.Combatant?.Block ?? 0,
                battleManager?.Player?.Combatant?.Block ?? 0,
                battleManager?.Player?.CurrentEnergy ?? 0,
                battleManager?.Player?.CardPiles?.Hand?.Count ?? 0);
        }

        private string DescribeCardPlay(
            BattleRuntimeCard card,
            BattleStateSnapshot before,
            BattleStateSnapshot after,
            List<BattleTriggeredDamageRequest> triggeredDamageRequests)
        {
            CardData data = card?.Data;
            string cardName = !string.IsNullOrWhiteSpace(data?.cardName) ? data.cardName : "카드";
            int damageDealt = Mathf.Max(0, before.EnemyHp - after.EnemyHp);
            int blockGained = Mathf.Max(0, after.PlayerBlock - before.PlayerBlock);
            int energyGained = Mathf.Max(0, after.PlayerEnergy - before.PlayerEnergy);
            int cardsDrawn = Mathf.Max(0, after.HandCount - before.HandCount + 1);

            if (damageDealt <= 0)
            {
                damageDealt = EstimateDeferredDamage(data, triggeredDamageRequests);
            }

            List<string> effects = new List<string>();
            if (damageDealt > 0)
            {
                effects.Add($"{damageDealt} 데미지를 가함");
            }

            if (blockGained > 0)
            {
                effects.Add($"방어도 {blockGained}을 얻음");
            }

            if (energyGained > 0)
            {
                effects.Add($"에너지 {energyGained}을 얻음");
            }

            if (cardsDrawn > 0)
            {
                effects.Add($"카드 {cardsDrawn}장을 뽑음");
            }

            AddKnownCardEffectDescription(data, effects);

            if (effects.Count == 0)
            {
                effects.Add("효과를 발동함");
            }

            return $"{cardName} 카드를 사용하여 {JoinKoreanEffects(effects)}.";
        }

        private static int EstimateDeferredDamage(CardData data, List<BattleTriggeredDamageRequest> requests)
        {
            if (requests != null && requests.Count > 0)
            {
                int total = 0;
                foreach (BattleTriggeredDamageRequest request in requests)
                {
                    total += Mathf.Max(0, request.BaseDamage);
                }

                if (total > 0)
                {
                    return total;
                }
            }

            if (data == null)
            {
                return 0;
            }

            return data.effectType switch
            {
                CardEffectType.DoubleStrike => Mathf.Max(0, data.effectValue * 2),
                CardEffectType.MultiHitAttack => Mathf.Max(0, data.effectValue * Mathf.Max(1, data.secondaryValue)),
                CardEffectType.MultiHitAndGainStrength => Mathf.Max(0, data.effectValue * 3),
                CardEffectType.MultiHitWithCritFromDodge => Mathf.Max(0, data.effectValue * 3),
                _ => 0,
            };
        }

        private static void AddKnownCardEffectDescription(CardData data, List<string> effects)
        {
            if (data == null || effects == null)
            {
                return;
            }

            string statusName = data.statusEffect != null ? data.statusEffect.effectName : "상태이상";
            int statusStacks = Mathf.Max(1, data.secondaryValue > 0 ? data.secondaryValue : data.effectValue);

            switch (data.effectType)
            {
                case CardEffectType.Rage:
                    effects.Add($"이번 턴 공격 보너스 {Mathf.Max(0, data.effectValue)}을 얻음");
                    break;
                case CardEffectType.Taunt:
                    effects.Add("적에게 약화 효과를 부여함");
                    break;
                case CardEffectType.ApplyStatusToEnemy:
                case CardEffectType.AttackAndApplyStatus:
                case CardEffectType.DamageAndApplyStatus:
                    effects.Add($"적에게 {statusName} {statusStacks} 효과를 부여함");
                    break;
                case CardEffectType.ApplyStatusToPlayer:
                    effects.Add($"{statusName} {Mathf.Max(1, data.effectValue)} 효과를 얻음");
                    break;
                case CardEffectType.GainStrength:
                    effects.Add($"힘 {Mathf.Max(0, data.effectValue)} 효과를 얻음");
                    break;
                case CardEffectType.GainStrengthEqualCurrentBlock:
                    effects.Add("현재 방어도만큼 힘을 얻음");
                    break;
                case CardEffectType.DealDamageWhenBlockGained:
                    effects.Add($"이번 턴 방어할 때마다 {Mathf.Max(0, data.effectValue)} 데미지 효과를 얻음");
                    break;
                case CardEffectType.GainBlockWhenDamageDealt:
                    effects.Add($"이번 턴 피해를 줄 때마다 방어도 {Mathf.Max(0, data.effectValue)}을 얻는 효과를 얻음");
                    break;
                case CardEffectType.DrawCardWhenBlockGained:
                    effects.Add($"이번 턴 방어할 때마다 카드 {Mathf.Max(0, data.effectValue)}장을 뽑는 효과를 얻음");
                    break;
                case CardEffectType.GrantEnemyStrengthAndRetaliateNext:
                    effects.Add("다음에 받는 피해만큼 힘을 얻는 효과를 준비함");
                    break;
                case CardEffectType.BlockAndNextTurnEnergy:
                    effects.Add($"다음 턴 에너지 {Mathf.Max(0, data.secondaryValue)}을 예약함");
                    break;
                case CardEffectType.ApplyPoisonWhenDamageDealt:
                    effects.Add($"이번 턴 피해를 줄 때마다 독 {Mathf.Max(0, data.effectValue)}을 부여하는 효과를 얻음");
                    break;
                case CardEffectType.FreezeEnemyNextAction:
                    effects.Add("적의 다음 행동을 봉인함");
                    break;
                case CardEffectType.MakeFirstAttackFreeThisTurn:
                    effects.Add("이번 턴 첫 공격 카드 비용을 0으로 만듦");
                    break;
                case CardEffectType.PlayHandRandomly:
                    effects.Add("손패의 다른 카드를 무작위로 사용함");
                    break;
                case CardEffectType.AttackAndGainDodge:
                    effects.Add($"회피 {Mathf.Max(0, data.secondaryValue)} 효과를 얻음");
                    break;
                case CardEffectType.GainDodgeWhenPlayingFreeCards:
                    effects.Add($"비용 0 카드 사용 시 회피 {Mathf.Max(0, data.effectValue)}을 얻는 효과를 얻음");
                    break;
                case CardEffectType.MultiplyDodgeStacks:
                    effects.Add($"회피 스택을 {Mathf.Max(2, data.effectValue)}배로 증폭함");
                    break;
                case CardEffectType.DrawCardWhenPlayingFreeCards:
                    effects.Add($"비용 0 카드 사용 시 카드 {Mathf.Max(0, data.effectValue)}장을 뽑는 효과를 얻음");
                    break;
                case CardEffectType.DrawCardsGainDodgeOnFreeDraw:
                    effects.Add($"비용 0 카드를 뽑을 때마다 회피 {Mathf.Max(0, data.secondaryValue)}을 얻는 효과를 얻음");
                    break;
            }
        }

        private static string JoinKoreanEffects(List<string> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                return "효과를 발동함";
            }

            if (effects.Count == 1)
            {
                return effects[0];
            }

            if (effects.Count == 2)
            {
                return $"{effects[0]} 및 {effects[1]}";
            }

            return $"{effects[0]}, {effects[1]} 외 {effects.Count - 2}개 효과";
        }

        private readonly struct BattleStateSnapshot
        {
            public BattleStateSnapshot(int enemyHp, int enemyBlock, int playerBlock, int playerEnergy, int handCount)
            {
                EnemyHp = enemyHp;
                EnemyBlock = enemyBlock;
                PlayerBlock = playerBlock;
                PlayerEnergy = playerEnergy;
                HandCount = handCount;
            }

            public int EnemyHp { get; }
            public int EnemyBlock { get; }
            public int PlayerBlock { get; }
            public int PlayerEnergy { get; }
            public int HandCount { get; }
        }

        private void ShowTurnAnnouncement(string message)
        {
            if (turnAnnouncementText == null) return;

            DOTween.Kill(turnAnnouncementText);
            turnAnnouncementText.text  = message;
            turnAnnouncementText.alpha = 0f;
            turnAnnouncementText.transform.localScale = Vector3.one * 0.6f;
            turnAnnouncementText.gameObject.SetActive(true);

            DOTween.Sequence()
                .Append(turnAnnouncementText.DOFade(1f, 0.15f))
                .Join(turnAnnouncementText.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
                .AppendInterval(announceDuration - 0.35f)
                .Append(turnAnnouncementText.DOFade(0f, 0.2f))
                .OnComplete(() => turnAnnouncementText.gameObject.SetActive(false));
        }

        private static void ShakeCard(BattleCardView cv)
        {
            if (cv == null) return;
            RectTransform rt = cv.GetComponent<RectTransform>();
            if (rt != null)
                rt.DOShakePosition(0.25f, 10f, 15, 90f, false, true);
        }

        // ── UI 갱신 ────────────────────────────────────────────────

        /// <summary>HUD, 적, 버튼만 갱신한다. 손패는 건드리지 않는다.</summary>
        private void RefreshHudAndButtons(BattleManager manager)
        {
            playerHud?.Refresh(manager.Player, manager.PlayerTurnCount);
            enemyView?.Refresh(manager.Enemy);

            bool isPlayerTurn = manager.Phase == BattlePhase.PlayerTurn;
            if (endTurnButton != null) endTurnButton.interactable = isPlayerTurn;

            // 카드 애니메이션 중에는 상호작용 잠금 유지
            if (!isCardAnimating)
                handView?.SetInteractable(isPlayerTurn);
        }

        /// <summary>손패를 완전히 재구성한다.</summary>
        private void RefreshHand(BattleManager manager)
        {
            if (handView == null || manager.Player == null) return;
            bool isPlayerTurn = manager.Phase == BattlePhase.PlayerTurn;
            handView.RefreshHand(manager.Player.CardPiles.Hand, isPlayerTurn);
        }
    }
}

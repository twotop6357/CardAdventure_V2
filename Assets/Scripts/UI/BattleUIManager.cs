using DG.Tweening;
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
    ///   - 카드 사용 성공 시 → 해당 카드 뷰만 애니메이션 후 제거
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

        [Header("페이드")]
        [SerializeField] private CanvasGroup fadeMask;
        [SerializeField] private float       fadeInDuration = 0.5f;

        [Header("카드 사용 연출 기준점")]
        [SerializeField] private RectTransform cardPlayTarget;

        [Header("타게팅 화살표")]
        [SerializeField] private BattleTargetArrow targetArrow;

        // ── 내부 상태 ──────────────────────────────────────────────
        private BattleCardView pendingCardView;
        private CardUseMode    pendingUseMode;
        private int            pendingStartedFrame = -1;
        private Canvas         rootCanvas;

        private enum CardUseMode
        {
            None,
            SingleTarget,
            PlayArea
        }
        private bool           isCardAnimating;   // 카드 사용 애니메이션 진행 중 여부

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
            battleManager.BattleStarted           += OnBattleStarted;
            battleManager.StateChanged            += OnStateChanged;
            battleManager.EnemyIntentSelected     += OnEnemyIntentSelected;
            battleManager.TurnStartStatusResolved += OnTurnStartStatusResolved;
            battleManager.BattleEnded             += OnBattleEnded;
        }

        private void UnsubscribeEvents()
        {
            if (battleManager == null) return;
            battleManager.BattleStarted           -= OnBattleStarted;
            battleManager.StateChanged            -= OnStateChanged;
            battleManager.EnemyIntentSelected     -= OnEnemyIntentSelected;
            battleManager.TurnStartStatusResolved -= OnTurnStartStatusResolved;
            battleManager.BattleEnded             -= OnBattleEnded;
        }

        // ── 이벤트 핸들러 ──────────────────────────────────────────

        /// <summary>전투 시작 — 손패 포함 전체 갱신.</summary>
        private void OnBattleStarted(BattleManager manager)
        {
            isCardAnimating = false;
            pendingCardView = null;
            pendingUseMode = CardUseMode.None;
            pendingStartedFrame = -1;
            targetArrow?.Hide();

            if (resultPanel != null) resultPanel.SetActive(false);
            if (endTurnButton != null) endTurnButton.gameObject.SetActive(true);

            // 직업 데이터에서 플레이어 스프라이트를 HUD에 코드-side 주입
            // (BattleManager.ActiveJob: GameDataManager 없으면 defaultJob(전사) 사용)
            if (playerHud != null && manager.ActiveJob != null)
                playerHud.SetPlayerSprite(manager.ActiveJob.previewSprite);

            enemyView?.ResetForBattle(manager.Enemy);
            RefreshHudAndButtons(manager);
            RefreshHand(manager);   // 첫 5장 드로우
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
            enemyView?.Refresh(manager.Enemy);

            // 첫 턴(BattleStarted에서 이미 RefreshHand 호출)은 중복 방지
            if (manager.PlayerTurnCount > 1)
            {
                RefreshHand(manager);
            }
        }

        private void OnTurnStartStatusResolved(BattleManager manager,
            BattleCombatantState combatant, BattleStatusTurnResult result)
        {
            if (result.PoisonDamage > 0 && combatant == manager.Player?.Combatant)
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

            if (resultPanel != null)
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
                handView.CardSelected += OnHandCardSelected;
        }

        // ── 버튼 핸들러 ────────────────────────────────────────────

        public void OnEndTurnClicked()
        {
            if (battleManager == null || battleManager.Phase != BattlePhase.PlayerTurn) return;

            CancelTargeting();
            handView?.SetInteractable(false);
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
                || effectType == CardEffectType.ApplyStatusToEnemy;
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

            if (useMode == CardUseMode.PlayArea)
            {
                played.EndPointerFollow(restoreToHand: false);
            }

            handView?.DetachCardView(played);

            BattleCardPlayResult result = battleManager.PlayCard(card);

            if (!result.Success)
            {
                handView?.ReattachCardView(played, card);

                if (result.FailureReason == BattleCardPlayFailureReason.NotEnoughEnergy)
                    ShakeCard(played);

                Debug.Log("[BattleUIManager] 카드 사용 실패: " + result.FailureReason);
                return;
            }

            isCardAnimating = true;
            handView?.SetInteractable(false);

            Vector3 targetWorld = GetCardPlayTargetWorld();
            played.PlayCardAnimation(targetWorld, () =>
            {
                isCardAnimating = false;

                if (battleManager.Phase == BattlePhase.PlayerTurn)
                    handView?.SetInteractable(true);
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

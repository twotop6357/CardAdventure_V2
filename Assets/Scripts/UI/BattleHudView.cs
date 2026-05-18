using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 플레이어 HP 바, 방어막, 에너지, 상태이상을 표시하는 HUD 컴포넌트.
    /// BattleUIManager.StateChanged 마다 Refresh() 호출로 갱신한다.
    /// </summary>
    public class BattleHudView : MonoBehaviour
    {
        [Header("HP")]
        [SerializeField] private Slider    hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("방어막")]
        [SerializeField] private GameObject   blockPanel;
        [SerializeField] private TextMeshProUGUI blockText;

        [Header("에너지")]
        [SerializeField] private TextMeshProUGUI energyText;

        [Header("상태이상 컨테이너")]
        [Tooltip("상태이상 아이콘을 담을 부모 RectTransform (Horizontal Layout Group 권장)")]
        [SerializeField] private RectTransform statusContainer;
        [SerializeField] private BattleStatusIconView statusIconPrefab;

        [Header("초상화")]
        [Tooltip("배틀 HUD에 표시할 플레이어 직업 초상화 Image. 비어 있으면 PlayerFaceImage 이름으로 자동 탐색한다.")]
        [SerializeField] private Image playerFaceImage;

        [Header("턴/페이즈 표시")]
        [SerializeField] private TextMeshProUGUI turnText;

        [Header("턴 공격 보너스 표시")]
        [Tooltip("분노 등 이번 턴 일시적 공격 보너스를 표시할 때 아이콘 기준으로 사용할 상태 에셋 (Status_Strength.asset 할당)")]
        [SerializeField] private StatusEffectData turnStrengthStatusData;

        [Header("상태이상 아이콘 라이브러리")]
        [Tooltip("Data 없이 생성된 상태이상(힘·회피·독 등)에 아이콘/설명을 제공하기 위한 전체 목록")]
        [SerializeField] private StatusEffectData[] statusDataLibrary;

        [Header("피격 연출")]
        [SerializeField] private float shakeDuration  = 0.3f;
        [SerializeField] private float shakeStrength  = 18f;
        [SerializeField] private int   shakeVibrato   = 20;
        [SerializeField] private float flashDuration  = 0.15f;
        [Tooltip("playerImage 없을 때 폴백으로 사용할 전체 화면 오버레이 Image")]
        [SerializeField] private Graphic damageFlash;

        // 코드-side 주입 (BattleUIManager가 직업 데이터를 통해 설정)
        private Image   playerImage;
        private Vector2 playerRestAnchoredPos;
        private bool    playerRestPosCached;
        private const float SuspiciousAvatarOffset = 800f;

        // ── 공개 메서드 ────────────────────────────────────────────

        /// <summary>플레이어 상태를 HUD에 반영한다.</summary>
        public void Refresh(BattlePlayerState player, int turnCount)
        {
            if (player == null) return;

            BattleCombatantState c = player.Combatant;

            // HP
            if (hpSlider != null)
            {
                hpSlider.maxValue = c.MaxHp;
                hpSlider.value    = c.CurrentHp;
            }
            if (hpText != null)
            {
                hpText.text = $"{c.CurrentHp} / {c.MaxHp}";
            }

            // 방어막
            bool hasBlock = c.Block > 0;
            if (blockPanel  != null) blockPanel.SetActive(hasBlock);
            if (blockText   != null) blockText.text = c.Block.ToString();

            // 에너지
            if (energyText != null)
            {
                energyText.text = $"{player.CurrentEnergy} / {player.MaxEnergy}";
            }

            // 턴
            if (turnText != null)
            {
                turnText.text = $"턴 {turnCount}";
            }

            // 상태이상 + 턴 공격 보너스 (분노 등)
            // 첫 공격 전에는 AttackBonusGainedPerAttack을, 이후엔 누적된 TurnAttackDamageBonus를 표시
            int displayBonus = player.TurnAttackDamageBonus > 0
                ? player.TurnAttackDamageBonus
                : player.AttackBonusGainedPerAttack;
            RefreshStatusIcons(c, displayBonus);
        }

        public void MoveTurnTextToTopBar(Transform topBar)
        {
            if (turnText == null || topBar == null)
            {
                return;
            }

            turnText.transform.SetParent(topBar, false);
            turnText.gameObject.name = "TopTurnText";
            turnText.alignment = TextAlignmentOptions.Right;
            turnText.fontSize = 18f;
            turnText.fontStyle = FontStyles.Bold;
            turnText.color = new Color(0.95f, 0.88f, 0.45f);
            turnText.textWrappingMode = TextWrappingModes.NoWrap;
            turnText.overflowMode = TextOverflowModes.Ellipsis;

            RectTransform rt = turnText.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(160f, 0f);
            rt.anchoredPosition = new Vector2(-24f, 0f);
        }

        /// <summary>
        /// 직업 데이터에서 가져온 플레이어 스프라이트를 PlayerAvatar Image에 적용한다.
        /// BattleUIManager가 전투 시작 시 코드-side로 호출한다.
        /// </summary>
        public void SetPlayerSprite(Sprite sprite)
        {
            // 이미 playerImage 참조가 있으면 스프라이트만 교체
            if (playerImage != null)
            {
                playerImage.sprite  = sprite;
                playerImage.enabled = sprite != null;
                return;
            }

            // 씬에 있는 "PlayerAvatar" GameObject를 찾아 Image 컴포넌트를 캐싱
            GameObject avatarGo = GameObject.Find("PlayerAvatar");
            if (avatarGo != null)
                playerImage = avatarGo.GetComponent<Image>();

            if (playerImage != null)
            {
                playerImage.sprite  = sprite;
                playerImage.enabled = sprite != null;
                if (!playerRestPosCached)
                {
                    playerRestAnchoredPos = playerImage.rectTransform.anchoredPosition;
                    if (IsSuspiciousAvatarPosition(playerRestAnchoredPos))
                    {
                        playerRestAnchoredPos = Vector2.zero;
                        playerImage.rectTransform.anchoredPosition = playerRestAnchoredPos;
                    }

                    playerRestPosCached = true;
                }
            }
            else
            {
                Debug.LogWarning("[BattleHudView] 'PlayerAvatar' Image를 찾을 수 없습니다. " +
                                 "씬에 'PlayerAvatar' 이름의 Image 오브젝트가 있는지 확인하세요.");
            }
        }

        /// <summary>
        /// 현재 직업에 맞는 배틀 HUD 초상화를 PlayerFaceImage에 적용한다.
        /// </summary>
        public void SetPlayerFaceSprite(Sprite sprite)
        {
            if (playerFaceImage == null)
            {
                GameObject faceGo = GameObject.Find("PlayerFaceImage");
                if (faceGo != null)
                    playerFaceImage = faceGo.GetComponent<Image>();
            }

            if (playerFaceImage == null)
            {
                Debug.LogWarning("[BattleHudView] 'PlayerFaceImage' Image를 찾을 수 없습니다. " +
                                 "배틀 씬 HUD에 PlayerFaceImage 이름의 Image 오브젝트가 있는지 확인하세요.");
                return;
            }

            playerFaceImage.sprite = sprite;
            playerFaceImage.color = Color.white;
            playerFaceImage.preserveAspect = true;
            playerFaceImage.enabled = sprite != null;
        }

        /// <summary>피격 연출 (플레이어가 데미지를 받을 때 호출).</summary>
        public void PlayDamageFlash()
        {
            if (playerImage != null)
            {
                // 플레이어 이미지가 지정된 경우: 적 피격과 동일하게 흔들림 + 붉은 플래시 → 흰색 복귀
                DOTween.Kill(playerImage.rectTransform);
                DOTween.Kill(playerImage);

                playerImage.rectTransform
                    .DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true)
                    .SetEase(Ease.OutQuad);

                DOTween.Sequence()
                    .Append(playerImage.DOColor(Color.red,   flashDuration))
                    .Append(playerImage.DOColor(Color.white, flashDuration));
            }
            else
            {
                // 폴백: HUD 패널 자체를 흔들기 (전체 화면 플래시 대신)
                RectTransform rt = transform as RectTransform;
                if (rt != null)
                {
                    DOTween.Kill(rt);
                    rt.DOShakePosition(shakeDuration, shakeStrength * 0.6f, shakeVibrato, 90f, false, true)
                      .SetEase(Ease.OutQuad);
                }

                // 전체 화면 오버레이가 있으면 작은 범위 페이드만 적용
                if (damageFlash != null)
                {
                    DOTween.Kill(damageFlash);
                    damageFlash.color = new Color(1f, 0f, 0f, 0.25f);
                    damageFlash.DOFade(0f, flashDuration * 2f).SetEase(Ease.OutQuad);
                }
            }
        }

        // ── 카드 사용 애니메이션 ────────────────────────────────────

        /// <summary>
        /// 방어·버프 카드 사용 중 연쇄 피해(가시 방벽 등)가 발생했을 때
        /// 후속 반격 모션을 재생한다. 기존 애니메이션을 중단하고 휴식 위치 기준으로 돌진한다.
        /// </summary>
        public void PlayTriggeredAttackAnim(System.Action onImpact = null, System.Action onComplete = null)
        {
            if (playerImage == null)
            {
                onImpact?.Invoke();
                onComplete?.Invoke();
                return;
            }

            DOTween.Kill(playerImage.rectTransform);
            DOTween.Kill(playerImage);

            Vector2 rest = GetPlayerRestAnchoredPosition();

            DOTween.Sequence()
                .Append(playerImage.rectTransform.DOAnchorPosX(rest.x + 50f, 0.12f).SetEase(Ease.OutQuint))
                .AppendCallback(() => onImpact?.Invoke())
                .Append(playerImage.rectTransform.DOAnchorPos(rest, 0.18f).SetEase(Ease.OutQuad))
                .OnComplete(() => onComplete?.Invoke());
            DOTween.Sequence()
                .Append(playerImage.DOColor(new Color(1f, 0.5f, 0.1f), 0.1f))
                .Append(playerImage.DOColor(Color.white, 0.2f));
        }

        /// <summary>
        /// 카드 사용 시 플레이어 스프라이트 애니메이션을 재생한다.
        /// onImpact: 애니메이션 피크(타격 순간)에 호출 — 여기서 카드 효과를 발동한다.
        /// </summary>
        public void PlayCardUsedAnimation(CardData data, System.Action onImpact = null)
        {
            if (playerImage == null || data == null)
            {
                onImpact?.Invoke();
                return;
            }

            CachePlayerRestPoseIfNeeded();
            DOTween.Kill(playerImage.rectTransform);
            DOTween.Kill(playerImage);
            playerImage.rectTransform.anchoredPosition = GetPlayerRestAnchoredPosition();
            playerImage.rectTransform.localScale = Vector3.one;
            playerImage.color = Color.white;

            switch (data.effectType)
            {
                // ── 다중 타격 ──────────────────────────────────────
                case CardEffectType.DoubleStrike:
                case CardEffectType.MultiHitAttack:
                case CardEffectType.MultiHitAndGainStrength:
                case CardEffectType.MultiHitWithCritFromDodge:
                case CardEffectType.PlayHandRandomly:
                    PlayerMultiHitAnim(data, onImpact);
                    break;

                // ── 버서커 (자해 + 강타) ───────────────────────────
                case CardEffectType.BerserkerAttack:
                    PlayerBerserkerAnim(onImpact);
                    break;

                // ── 일반 공격 ─────────────────────────────────────
                case CardEffectType.BasicAttack:
                case CardEffectType.ShieldBash:
                case CardEffectType.AttackAndDefend:
                case CardEffectType.AttackAndApplyStatus:
                case CardEffectType.AttackAndGainBlockEqualDamage:
                case CardEffectType.ConsumeBlockToDealDamage:
                case CardEffectType.DamageAndApplyStatus:
                case CardEffectType.ConsumeAllEnergyAndAttack:
                case CardEffectType.AttackAndGainDodge:
                case CardEffectType.PoisonAndDetonateAllPoison:
                case CardEffectType.AttackAndShuffleBackToDeck:
                    PlayerAttackAnim(onImpact);
                    break;

                // ── 방어 ──────────────────────────────────────────
                case CardEffectType.BasicDefense:
                case CardEffectType.DefenseAndDraw:
                case CardEffectType.DrawAndDefense:
                case CardEffectType.BlockAndNextTurnEnergy:
                case CardEffectType.DealDamageWhenBlockGained:
                case CardEffectType.GainBlockWhenDamageDealt:
                case CardEffectType.DrawCardWhenBlockGained:
                case CardEffectType.GainStrengthEqualCurrentBlock:
                    PlayerDefenseAnim(onImpact);
                    break;

                // ── 힘 버프 ──────────────────────────────────────
                case CardEffectType.Rage:
                case CardEffectType.GainStrength:
                case CardEffectType.GrantEnemyStrengthAndRetaliateNext:
                case CardEffectType.GainDodgeWhenPlayingFreeCards:
                case CardEffectType.MultiplyDodgeStacks:
                    PlayerPowerBuffAnim(onImpact);
                    break;

                // ── 적에게 상태이상 투척 ──────────────────────────
                case CardEffectType.Taunt:
                case CardEffectType.ApplyStatusToEnemy:
                case CardEffectType.FreezeEnemyNextAction:
                case CardEffectType.ApplyPoisonWhenDamageDealt:
                    PlayerStatusThrowAnim(onImpact);
                    break;

                // ── 자신 버프/회복 ────────────────────────────────
                case CardEffectType.ApplyStatusToPlayer:
                    PlayerHealAnim(onImpact);
                    break;

                // ── 드로우 / 유틸리티 ─────────────────────────────
                case CardEffectType.DrawCards:
                case CardEffectType.DrawCardWhenPlayingFreeCards:
                case CardEffectType.DrawCardsGainDodgeOnFreeDraw:
                case CardEffectType.GainEnergyThisTurn:
                case CardEffectType.MakeFirstAttackFreeThisTurn:
                    PlayerUtilityAnim(onImpact);
                    break;

                default:
                    if      (data.cardType == CardType.Attack)  PlayerAttackAnim(onImpact);
                    else if (data.cardType == CardType.Defense)  PlayerDefenseAnim(onImpact);
                    else                                         PlayerPowerBuffAnim(onImpact);
                    break;
            }
        }

        // 공격: 전진 피크에서 효과 발동 → 복귀
        private void PlayerAttackAnim(System.Action onImpact = null)
        {
            Vector2 rest = GetPlayerRestAnchoredPosition();
            DOTween.Sequence()
                .Append(playerImage.rectTransform.DOAnchorPosX(rest.x + 56f, 0.14f).SetEase(Ease.OutQuint))
                .AppendCallback(() => onImpact?.Invoke())
                .Append(playerImage.rectTransform.DOAnchorPos(rest, 0.18f).SetEase(Ease.OutQuad));
        }

        // 다중타격: 시전 제스처(스케일 펄스)만 재생하고 onImpact 호출.
        // 실제 각 히트의 전진/복귀는 PlayMultiHitSubAnim이 담당한다.
        private void PlayerMultiHitAnim(CardData data, System.Action onImpact = null)
        {
            DOTween.Sequence()
                .Append(playerImage.rectTransform.DOScale(Vector3.one * 1.12f, 0.07f).SetEase(Ease.OutBack))
                .Append(playerImage.rectTransform.DOScale(Vector3.one, 0.09f).SetEase(Ease.OutQuad))
                .AppendCallback(() => onImpact?.Invoke());
            DOTween.Sequence()
                .Append(playerImage.DOColor(new Color(1f, 0.6f, 0.2f), 0.06f))
                .Append(playerImage.DOColor(Color.white, 0.12f));
        }

        /// <summary>
        /// 다단 히트 시퀀스의 서브히트 1회 애니메이션.
        /// BattleUIManager 가 IsMultiHit 요청마다 호출한다.
        /// </summary>
        public void PlayMultiHitSubAnim(System.Action onImpact = null, System.Action onComplete = null)
        {
            if (playerImage == null)
            {
                onImpact?.Invoke();
                onComplete?.Invoke();
                return;
            }

            DOTween.Kill(playerImage.rectTransform, complete: false);
            DOTween.Kill(playerImage, complete: false);

            Vector2 rest = GetPlayerRestAnchoredPosition();
            // 이전 트윈이 중간에 끊겨도 깨끗하게 시작
            playerImage.rectTransform.anchoredPosition = rest;
            playerImage.color = Color.white;

            DOTween.Sequence()
                .Append(playerImage.rectTransform.DOAnchorPosX(rest.x + 38f, 0.08f).SetEase(Ease.OutQuint))
                .AppendCallback(() => onImpact?.Invoke())
                .Append(playerImage.rectTransform.DOAnchorPos(rest, 0.13f).SetEase(Ease.OutQuad))
                .OnComplete(() => onComplete?.Invoke());
            DOTween.Sequence()
                .Append(playerImage.DOColor(new Color(1f, 0.55f, 0.15f), 0.07f))
                .Append(playerImage.DOColor(Color.white, 0.15f));
        }

        // 버서커: 전진 피크에서 효과 발동 → 복귀
        private void PlayerBerserkerAnim(System.Action onImpact = null)
        {
            Vector2 rest = GetPlayerRestAnchoredPosition();
            DOTween.Sequence()
                .Append(playerImage.DOColor(new Color(1f, 0.25f, 0.25f), 0.07f))
                .Join(playerImage.rectTransform.DOAnchorPosX(rest.x + 64f, 0.13f).SetEase(Ease.OutQuint))
                .AppendCallback(() => onImpact?.Invoke())
                .Append(playerImage.DOColor(Color.white, 0.28f))
                .Join(playerImage.rectTransform.DOAnchorPos(rest, 0.18f).SetEase(Ease.OutQuad));
        }

        // 방어: 후퇴 완료(방어 자세)에서 효과 발동 → 복귀
        private void PlayerDefenseAnim(System.Action onImpact = null)
        {
            Vector2 rest = GetPlayerRestAnchoredPosition();
            DOTween.Sequence()
                .Append(playerImage.rectTransform.DOAnchorPosX(rest.x - 18f, 0.1f).SetEase(Ease.OutQuad))
                .AppendCallback(() => onImpact?.Invoke())
                .Append(playerImage.rectTransform.DOAnchorPos(rest, 0.18f).SetEase(Ease.OutQuad));
            DOTween.Sequence()
                .Append(playerImage.DOColor(new Color(0.35f, 0.6f, 1f), 0.12f))
                .Append(playerImage.DOColor(Color.white, 0.25f));
            DOTween.Sequence()
                .Append(playerImage.rectTransform.DOScale(Vector3.one * 1.1f,  0.12f).SetEase(Ease.OutQuad))
                .Append(playerImage.rectTransform.DOScale(Vector3.one,          0.2f));
        }

        // 힘/버프: 광채 피크에서 효과 발동
        private void PlayerPowerBuffAnim(System.Action onImpact = null)
        {
            DOTween.Sequence()
                .Append(playerImage.DOColor(new Color(1f, 0.78f, 0.1f), 0.15f))
                .AppendCallback(() => onImpact?.Invoke())
                .Append(playerImage.DOColor(Color.white, 0.3f));
            DOTween.Sequence()
                .Append(playerImage.rectTransform.DOScale(Vector3.one * 1.15f, 0.2f).SetEase(Ease.OutBack))
                .Append(playerImage.rectTransform.DOScale(Vector3.one,          0.2f));
        }

        // 상태이상 투척: 전진 피크에서 효과 발동 → 복귀
        private void PlayerStatusThrowAnim(System.Action onImpact = null)
        {
            Vector2 rest = GetPlayerRestAnchoredPosition();
            DOTween.Sequence()
                .Append(playerImage.rectTransform.DOAnchorPosX(rest.x + 24f, 0.1f).SetEase(Ease.OutQuad))
                .AppendCallback(() => onImpact?.Invoke())
                .Append(playerImage.rectTransform.DOAnchorPos(rest, 0.16f).SetEase(Ease.OutQuad));
            DOTween.Sequence()
                .Append(playerImage.DOColor(new Color(0.65f, 0.3f, 1f), 0.1f))
                .Append(playerImage.DOColor(Color.white, 0.22f));
        }

        // 자신 회복/버프: 광채 피크에서 효과 발동
        private void PlayerHealAnim(System.Action onImpact = null)
        {
            DOTween.Sequence()
                .Append(playerImage.DOColor(new Color(0.3f, 1f, 0.45f), 0.15f))
                .AppendCallback(() => onImpact?.Invoke())
                .Append(playerImage.DOColor(Color.white, 0.3f));
            DOTween.Sequence()
                .Append(playerImage.rectTransform.DOScale(Vector3.one * 1.08f, 0.15f).SetEase(Ease.OutBack))
                .Append(playerImage.rectTransform.DOScale(Vector3.one,          0.2f));
        }

        // 드로우/유틸리티: 점프 피크에서 효과 발동 → 착지
        private void PlayerUtilityAnim(System.Action onImpact = null)
        {
            Vector2 rest = GetPlayerRestAnchoredPosition();
            DOTween.Sequence()
                .Append(playerImage.rectTransform.DOAnchorPosY(rest.y + 20f, 0.15f).SetEase(Ease.OutQuad))
                .AppendCallback(() => onImpact?.Invoke())
                .Append(playerImage.rectTransform.DOAnchorPos(rest, 0.18f).SetEase(Ease.OutQuad));
            DOTween.Sequence()
                .Append(playerImage.DOColor(new Color(0.45f, 0.85f, 1f), 0.15f))
                .Append(playerImage.DOColor(Color.white, 0.25f));
        }

        // ── 내부 ───────────────────────────────────────────────────

        private void CachePlayerRestPoseIfNeeded()
        {
            if (playerImage == null || playerRestPosCached)
            {
                return;
            }

            playerRestAnchoredPos = playerImage.rectTransform.anchoredPosition;
            if (IsSuspiciousAvatarPosition(playerRestAnchoredPos))
            {
                playerRestAnchoredPos = Vector2.zero;
                playerImage.rectTransform.anchoredPosition = playerRestAnchoredPos;
            }

            playerRestPosCached = true;
        }

        private Vector2 GetPlayerRestAnchoredPosition()
        {
            CachePlayerRestPoseIfNeeded();
            if (IsSuspiciousAvatarPosition(playerRestAnchoredPos))
            {
                playerRestAnchoredPos = Vector2.zero;
            }

            return playerRestPosCached
                ? playerRestAnchoredPos
                : playerImage.rectTransform.anchoredPosition;
        }

        private static bool IsSuspiciousAvatarPosition(Vector2 position)
        {
            return Mathf.Abs(position.x) > SuspiciousAvatarOffset || Mathf.Abs(position.y) > SuspiciousAvatarOffset;
        }

        private StatusEffectData FindStatusData(StatusEffectType type)
        {
            if (statusDataLibrary == null) return null;
            foreach (StatusEffectData d in statusDataLibrary)
                if (d != null && d.effectType == type) return d;
            return null;
        }

        private void RefreshStatusIcons(BattleCombatantState combatant, int turnAttackBonus = 0)
        {
            if (statusContainer == null) return;

            foreach (Transform child in statusContainer)
                Destroy(child.gameObject);

            if (statusIconPrefab == null || combatant == null) return;

            foreach (BattleStatusInstance status in combatant.Statuses)
            {
                if (status.IsExpired) continue;
                BattleStatusIconView icon = Instantiate(statusIconPrefab, statusContainer);
                icon.Bind(status, FindStatusData(status.EffectType));
            }

            // 이번 턴 공격 보너스(분노 등): Strength 아이콘 + 황금 틴트
            if (turnAttackBonus > 0)
            {
                BattleStatusIconView icon = Instantiate(statusIconPrefab, statusContainer);
                StatusEffectData strengthData = turnStrengthStatusData ?? FindStatusData(StatusEffectType.Strength);
                if (strengthData != null)
                {
                    icon.Bind(new BattleStatusInstance(strengthData, turnAttackBonus));
                    icon.SetIconTint(new Color(1f, 0.85f, 0.1f, 1f));
                }
                else
                {
                    icon.Bind(new BattleStatusInstance(StatusEffectType.Strength, turnAttackBonus, 0),
                              FindStatusData(StatusEffectType.Strength));
                }
            }
        }
    }
}

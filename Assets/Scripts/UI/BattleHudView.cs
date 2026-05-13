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

        [Header("피격 연출")]
        [SerializeField] private float shakeDuration  = 0.3f;
        [SerializeField] private float shakeStrength  = 18f;
        [SerializeField] private int   shakeVibrato   = 20;
        [SerializeField] private float flashDuration  = 0.15f;
        [Tooltip("playerImage 없을 때 폴백으로 사용할 전체 화면 오버레이 Image")]
        [SerializeField] private Graphic damageFlash;

        // 코드-side 주입 (BattleUIManager가 직업 데이터를 통해 설정)
        private Image playerImage;

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

            // 상태이상
            RefreshStatusIcons(c);
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

        // ── 내부 ───────────────────────────────────────────────────

        private void RefreshStatusIcons(BattleCombatantState combatant)
        {
            if (statusContainer == null) return;

            // 기존 아이콘 제거
            foreach (Transform child in statusContainer)
            {
                Destroy(child.gameObject);
            }

            if (statusIconPrefab == null || combatant == null) return;

            foreach (BattleStatusInstance status in combatant.Statuses)
            {
                if (status.IsExpired) continue;
                BattleStatusIconView icon = Instantiate(statusIconPrefab, statusContainer);
                icon.Bind(status);
            }
        }
    }
}

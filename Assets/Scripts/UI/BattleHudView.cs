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

        [Header("턴/페이즈 표시")]
        [SerializeField] private TextMeshProUGUI turnText;

        [Header("피격 연출")]
        [SerializeField] private Graphic damageFlash;      // 화면 가장자리 붉은 이미지 등
        [SerializeField] private float   flashDuration = 0.3f;

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

        /// <summary>피격 플래시 연출 (플레이어가 데미지를 받을 때 호출).</summary>
        public void PlayDamageFlash()
        {
            if (damageFlash == null) return;
            DOTween.Kill(damageFlash);
            damageFlash.color = new Color(1f, 0f, 0f, 0.4f);
            damageFlash.DOFade(0f, flashDuration).SetEase(Ease.OutQuad);
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

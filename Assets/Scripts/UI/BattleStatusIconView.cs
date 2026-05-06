using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 상태이상 한 종류를 표시하는 아이콘 컴포넌트.
    /// BattleHudView / BattleEnemyView 의 statusContainer 아래에 생성된다.
    /// </summary>
    public class BattleStatusIconView : MonoBehaviour
    {
        [SerializeField] private Image           iconImage;
        [SerializeField] private TextMeshProUGUI stacksText;
        [SerializeField] private TextMeshProUGUI durationText;

        [Header("타입별 색상 — 아이콘 배경")]
        [SerializeField] private Color poisonColor       = new Color(0.40f, 0.80f, 0.20f);
        [SerializeField] private Color regenColor        = new Color(0.20f, 0.90f, 0.60f);
        [SerializeField] private Color weakColor         = new Color(0.85f, 0.55f, 0.10f);
        [SerializeField] private Color vulnerableColor   = new Color(0.85f, 0.20f, 0.20f);
        [SerializeField] private Color strengthColor     = new Color(0.90f, 0.75f, 0.10f);
        [SerializeField] private Color defaultStatusColor = Color.gray;

        public void Bind(BattleStatusInstance status)
        {
            if (status == null) return;

            // 아이콘 색상
            if (iconImage != null)
            {
                iconImage.color = GetStatusColor(status.EffectType);

                // ScriptableObject 기반 상태이상이라면 스프라이트 적용
                if (status.Data != null && status.Data.icon != null)
                {
                    iconImage.sprite = status.Data.icon;
                }
            }

            // 스택 표시
            if (stacksText != null)
            {
                stacksText.text = status.Stacks > 1 ? status.Stacks.ToString() : string.Empty;
            }

            // 지속시간 표시 (HasTimedDuration이 false면 무제한)
            if (durationText != null)
            {
                durationText.text = status.HasTimedDuration
                    ? status.RemainingDuration.ToString()
                    : string.Empty;
            }

            // 툴팁용 이름은 GameObject 이름에 저장 (선택적)
            gameObject.name = $"Status_{status.EffectType}";
        }

        private Color GetStatusColor(StatusEffectType type) => type switch
        {
            StatusEffectType.Poison       => poisonColor,
            StatusEffectType.Regeneration => regenColor,
            StatusEffectType.Weak         => weakColor,
            StatusEffectType.Vulnerable   => vulnerableColor,
            StatusEffectType.Strength     => strengthColor,
            _                             => defaultStatusColor,
        };
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 상태이상 아이콘 컴포넌트. 마우스 오버 시 툴팁을 표시합니다.
    /// </summary>
    public class BattleStatusIconView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
    {
        [SerializeField] private Image           iconImage;
        [SerializeField] private TextMeshProUGUI stacksText;
        [SerializeField] private TextMeshProUGUI durationText;

        [Header("색상 설정")]
        [SerializeField] private Color poisonColor        = new Color(0.4f, 0.8f, 0.2f);
        [SerializeField] private Color regenColor         = new Color(0.2f, 0.9f, 0.6f);
        [SerializeField] private Color weakColor          = new Color(0.8f, 0.5f, 0.1f);
        [SerializeField] private Color vulnerableColor    = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color strengthColor      = new Color(0.9f, 0.7f, 0.1f);
        [SerializeField] private Color defaultStatusColor = Color.gray;

        private BattleStatusInstance boundStatus;

        private void Awake()
        {
            // 이 오브젝트(StatusIcon) 자체가 레이캐스트를 받으려면 Graphic 컴포넌트가 필요합니다.
            // 투명한 이미지를 추가하여 마우스 이벤트를 감지하도록 합니다.
            var raycastTarget = GetComponent<Image>();
            if (raycastTarget == null)
            {
                raycastTarget = gameObject.AddComponent<Image>();
                raycastTarget.color = Color.clear; // 완전히 투명하게 설정
            }
            raycastTarget.raycastTarget = true;
        }

        public void Bind(BattleStatusInstance status)
        {
            if (status == null) return;
            boundStatus = status;

            if (iconImage != null)
            {
                iconImage.color = GetStatusColor(status.EffectType);
                if (status.Data != null && status.Data.icon != null)
                {
                    iconImage.sprite = status.Data.icon;
                }
            }

            if (stacksText != null)
            {
                stacksText.text = status.Stacks > 1 ? status.Stacks.ToString() : "";
            }

            if (durationText != null)
            {
                durationText.text = status.HasTimedDuration ? status.RemainingDuration.ToString() : "";
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (boundStatus == null) return;
            
            Debug.Log($"[StatusIcon] Pointer Enter: {boundStatus.EffectType}");
            StatusTooltipPanel.Instance?.Show(boundStatus, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log("[StatusIcon] Pointer Exit");
            StatusTooltipPanel.Instance?.Hide();
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            StatusTooltipPanel.Instance?.UpdatePosition(eventData.position);
        }

        private void OnDisable()
        {
            if (StatusTooltipPanel.Instance != null)
                StatusTooltipPanel.Instance.Hide();
        }

        private Color GetStatusColor(StatusEffectType type) => type switch
        {
            StatusEffectType.Poison => poisonColor,
            StatusEffectType.Regeneration => regenColor,
            StatusEffectType.Weak => weakColor,
            StatusEffectType.Vulnerable => vulnerableColor,
            StatusEffectType.Strength => strengthColor,
            _ => defaultStatusColor,
        };
    }
}

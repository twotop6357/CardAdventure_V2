using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 상태이상 정보를 마우스 위치에 표시하는 툴팁.
    /// </summary>
    public class StatusTooltipPanel : MonoBehaviour
    {
        private static StatusTooltipPanel _instance;
        public static StatusTooltipPanel Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Object.FindAnyObjectByType<StatusTooltipPanel>(FindObjectsInactive.Include);
                return _instance;
            }
        }

        [Header("UI 구성 요소")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private Image         iconImage;
        [SerializeField] private Image         iconBackground;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI stacksText;
        [SerializeField] private TextMeshProUGUI durationText;

        [Header("설정")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Vector2     offset = new Vector2(20, -20);
        [SerializeField] private float       edgePadding = 10f;

        private Coroutine _fadeCoroutine;
        private Canvas rootCanvas;

        private void Awake()
        {
            _instance = this;
            rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        public void Show(BattleStatusInstance status, Vector2 screenPosition)
        {
            if (status == null) return;

            Bind(status);
            UpdatePosition(screenPosition);

            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                DOTween.Kill(canvasGroup);
                canvasGroup.alpha = 1f; // 불투명하게 즉시 표시
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        public void UpdatePosition(Vector2 screenPosition)
        {
            if (panelRect == null || rootCanvas == null) return;

            // ScreenSpace - Overlay 혹은 Camera 모드 모두 대응하는 범용 위치 계산
            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
            Camera uiCamera = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint))
            {
                Vector2 targetPos = localPoint + offset;

                // 화면 밖으로 나가지 않게 클램프 (Pivot 0, 1 가정)
                float canvasW = canvasRect.rect.width;
                float canvasH = canvasRect.rect.height;
                float panelW  = panelRect.rect.width;
                float panelH  = panelRect.rect.height;

                // (0,0) 이 캔버스 중앙임
                float minX = -canvasW * 0.5f + edgePadding;
                float maxX =  canvasW * 0.5f - panelW - edgePadding;
                float minY = -canvasH * 0.5f + panelH + edgePadding;
                float maxY =  canvasH * 0.5f - edgePadding;

                targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
                targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

                panelRect.anchoredPosition = targetPos;
            }
        }

        private void Bind(BattleStatusInstance status)
        {
            if (nameText != null)
                nameText.text = status.Data != null ? status.Data.effectName : GetTypeName(status.EffectType);

            if (descriptionText != null)
                descriptionText.text = status.Data != null ? status.Data.description : GetTypeDesc(status.EffectType);

            if (stacksText != null)
                stacksText.text = status.Stacks > 0 ? $"강도: {status.Stacks}" : "";

            if (durationText != null)
                durationText.text = status.HasTimedDuration ? $"{status.RemainingDuration}턴" : "영구";

            if (iconBackground != null)
                iconBackground.color = status.Data != null ? status.Data.displayColor : Color.gray;

            if (iconImage != null)
            {
                iconImage.enabled = status.Data != null && status.Data.icon != null;
                if (iconImage.enabled)
                {
                    iconImage.sprite = status.Data.icon;
                    iconImage.color = Color.white;
                }
            }
        }

        private string GetTypeName(StatusEffectType t) => t switch {
            StatusEffectType.Poison => "독",
            StatusEffectType.Weak => "약화",
            StatusEffectType.Vulnerable => "취약",
            StatusEffectType.Strength => "힘",
            StatusEffectType.Regeneration => "재생",
            StatusEffectType.Burn => "화상",
            _ => "상태이상"
        };

        private string GetTypeDesc(StatusEffectType t) => t switch {
            StatusEffectType.Poison => "매 턴 시작 시 피해를 입습니다.",
            StatusEffectType.Weak => "공격 데미지가 25% 감소합니다.",
            StatusEffectType.Vulnerable => "받는 데미지가 50% 증가합니다.",
            StatusEffectType.Strength => "공격 데미지가 증가합니다.",
            StatusEffectType.Regeneration => "매 턴 시작 시 체력을 회복합니다.",
            StatusEffectType.Burn => "매 턴 시작 시 화상 중첩만큼 피해를 입습니다.",
            _ => "활성화된 상태 효과입니다."
        };
    }
}

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 손패 카드 한 장을 표시하는 uGUI 컴포넌트.
    /// BattleHandView가 생성/파괴를 관리한다.
    /// </summary>
    public class BattleCardView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI 참조")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image cardTypeIcon;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI energyCostText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("타입별 색상")]
        [SerializeField] private Color attackColor  = new Color(0.85f, 0.25f, 0.25f);
        [SerializeField] private Color defenseColor = new Color(0.25f, 0.50f, 0.85f);
        [SerializeField] private Color skillColor   = new Color(0.25f, 0.75f, 0.40f);
        [SerializeField] private Color statusColor  = new Color(0.65f, 0.25f, 0.85f);

        [Header("호버/선택 연출")]
        [SerializeField] private float hoverLiftY    = 30f;
        [SerializeField] private float hoverDuration = 0.15f;
        [SerializeField] private float selectedScale = 1.08f;

        // ── 내부 상태 ──────────────────────────────────────────────
        private BattleRuntimeCard runtimeCard;
        private Vector3 baseLocalPosition;
        private bool isSelected;
        private bool isInteractable;

        /// <summary>카드가 클릭되었을 때 통지. BattleHandView가 구독한다.</summary>
        public event System.Action<BattleCardView> Clicked;

        public BattleRuntimeCard RuntimeCard => runtimeCard;

        // ── 초기화 ─────────────────────────────────────────────────

        /// <summary>카드 데이터를 이 뷰에 바인딩한다.</summary>
        public void Bind(BattleRuntimeCard card, bool interactable = true)
        {
            runtimeCard    = card;
            isInteractable = interactable;
            isSelected     = false;
            Refresh();
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;
            if (cardBackground != null)
            {
                cardBackground.color = value
                    ? GetTypeColor(runtimeCard?.Data?.cardType ?? CardType.Skill)
                    : Color.gray;
            }
        }

        // ── 데이터 표시 ────────────────────────────────────────────

        private void Refresh()
        {
            if (runtimeCard == null || runtimeCard.Data == null)
            {
                return;
            }

            CardData data = runtimeCard.Data;

            if (cardNameText   != null) cardNameText.text   = data.cardName;
            if (energyCostText != null) energyCostText.text = data.energyCost.ToString();
            if (descriptionText != null) descriptionText.text = data.GetFormattedDescription();

            Color typeColor = GetTypeColor(data.cardType);
            if (cardBackground != null) cardBackground.color = typeColor;

            // 카드 아이콘이 있으면 표시
            if (cardTypeIcon != null)
            {
                if (data.cardIcon != null)
                {
                    cardTypeIcon.sprite  = data.cardIcon;
                    cardTypeIcon.enabled = true;
                }
                else
                {
                    cardTypeIcon.enabled = false;
                }
            }
        }

        private Color GetTypeColor(CardType type) => type switch
        {
            CardType.Attack      => attackColor,
            CardType.Defense     => defenseColor,
            CardType.Skill       => skillColor,
            CardType.StatusEffect => statusColor,
            _                    => Color.white,
        };

        // ── 레이아웃 위치 저장 ─────────────────────────────────────

        /// <summary>부채꼴 배치 완료 후 HandView가 호출해 기준 위치를 저장한다.</summary>
        public void SaveBasePosition()
        {
            baseLocalPosition = transform.localPosition;
        }

        // ── 호버/선택 연출 ─────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable || isSelected) return;
            DOTween.Kill(transform, complete: true);
            transform.DOLocalMoveY(baseLocalPosition.y + hoverLiftY, hoverDuration)
                     .SetEase(Ease.OutQuad);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isSelected) return;
            DOTween.Kill(transform, complete: true);
            transform.DOLocalMoveY(baseLocalPosition.y, hoverDuration)
                     .SetEase(Ease.OutQuad);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable) return;
            Clicked?.Invoke(this);
        }

        // ── 선택 강조 ──────────────────────────────────────────────

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            DOTween.Kill(transform, complete: true);

            if (selected)
            {
                transform.DOLocalMoveY(baseLocalPosition.y + hoverLiftY * 1.5f, hoverDuration)
                         .SetEase(Ease.OutBack);
                transform.DOScale(selectedScale, hoverDuration).SetEase(Ease.OutBack);
            }
            else
            {
                transform.DOLocalMoveY(baseLocalPosition.y, hoverDuration).SetEase(Ease.OutQuad);
                transform.DOScale(1f, hoverDuration).SetEase(Ease.OutQuad);
            }
        }

        // ── 사용 연출 ──────────────────────────────────────────────

        /// <summary>카드를 화면 중앙으로 날아가게 한 뒤 callback을 호출한다.</summary>
        public void PlayCardAnimation(Vector3 worldTarget, System.Action onComplete = null)
        {
            DOTween.Kill(transform, complete: true);
            transform.DOMove(worldTarget, 0.35f)
                     .SetEase(Ease.InBack)
                     .OnComplete(() =>
                     {
                         onComplete?.Invoke();
                         Destroy(gameObject);
                     });
            transform.DOScale(0f, 0.35f).SetEase(Ease.InBack);
        }

        /// <summary>카드를 즉시 제거한다 (애니메이션 없음).</summary>
        public void DestroyImmediate()
        {
            DOTween.Kill(transform, complete: false);
            Destroy(gameObject);
        }
    }
}

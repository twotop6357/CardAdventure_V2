using UnityEngine;
using UnityEngine.EventSystems;

namespace CardAdventure
{
    /// <summary>
    /// 카드 프리뷰 중 원래 손패 위치에 생성되는 투명 히트박스.
    /// 마우스 클릭/드래그를 BattleCardView로 전달해 원래 위치에서도 카드를 사용할 수 있게 한다.
    /// BattleCardView.SpawnProxy()가 런타임에 생성하며, ClosePreview()가 제거한다.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public class CardHoverProxy : MonoBehaviour, 
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private BattleCardView owner;

        public void Init(BattleCardView cardView)
        {
            owner = cardView;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner != null)
            {
                owner.OnProxyClick(eventData);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (owner != null)
            {
                // 드래그 대상을 프록시가 아닌 원본 카드로 리다이렉션
                eventData.pointerDrag = owner.gameObject;
                owner.OnPointerDown(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (owner != null)
            {
                owner.OnPointerUp(eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner != null)
            {
                owner.OnBeginDrag(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner != null)
            {
                owner.OnDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner != null)
            {
                owner.OnEndDrag(eventData);
            }
        }
    }
}

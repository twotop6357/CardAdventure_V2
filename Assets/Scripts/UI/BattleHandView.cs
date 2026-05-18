using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardAdventure
{
    /// <summary>
    /// 플레이어 손패(Hand)의 카드 뷰 목록을 관리하고 부채꼴로 배치한다.
    /// BattleUIManager가 StateChanged 이벤트마다 RefreshHand()를 호출한다.
    ///
    /// 드로우 애니메이션 방침:
    ///   - 기존 손패 카드는 뷰를 유지하면서 새 팬 위치로 부드럽게 이동한다.
    ///   - 새로 드로우된 카드만 deckOriginMarker(덱 위치)에서 딜 애니메이션으로 등장한다.
    ///   - 따라서 1장 드로우 시에는 1장에 대해서만 딜 애니메이션이 재생된다.
    /// </summary>
    public class BattleHandView : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private BattleCardView    cardViewPrefab;
        [SerializeField] private RectTransform     handContainer;
        [SerializeField] private CardSpriteLibrary spriteLibrary;

        [Header("레이아웃 — 부채꼴")]
        [Tooltip("카드 간 최대 가로 간격 (픽셀)")]
        [SerializeField] private float cardSpacing = 120f;
        [Tooltip("부채꼴 회전 최대 각도 (±degree, 카드 장수에 따라 비례 감소)")]
        [SerializeField] private float maxFanAngle = 15f;
        [Tooltip("부채꼴 아래 눌림 (y 오프셋 곡선 강도)")]
        [SerializeField] private float fanDropY = 30f;

        [Header("드로우 연출")]
        [SerializeField] private float dealDuration      = 0.4f;
        [SerializeField] private Vector3 dealOriginLocal = new Vector3(520f, -280f, 0);
        [Tooltip("덱 위치 마커 (없으면 dealOriginLocal 사용). 우하단에 배치한다.")]
        [SerializeField] private RectTransform deckOriginMarker;
        [Tooltip("deckOriginMarker가 없을 때 화면 오른쪽 하단에서 안쪽으로 띄우는 여백.")]
        [SerializeField] private Vector2 screenDealOriginPadding = new Vector2(120f, 90f);
        [Tooltip("카드 한 장당 딜 시작 딜레이 간격 (초)")]
        [SerializeField] private float cardDealStagger = 0.08f;

        // ── 내부 ───────────────────────────────────────────────────
        // cardViews[i]와 boundRuntimeCards[i]는 항상 1:1 대응한다.
        private readonly List<BattleCardView>    cardViews         = new List<BattleCardView>();
        private readonly List<BattleRuntimeCard> boundRuntimeCards = new List<BattleRuntimeCard>();

        private BattleCardView selectedCardView;
        private bool isInteractable = true;

        /// <summary>카드가 클릭(선택)되었을 때 통지. BattleUIManager가 구독한다.</summary>
        public event System.Action<BattleCardView> CardSelected;

        public event System.Action<BattleCardView, PointerEventData> CardBeginDrag;
        public event System.Action<BattleCardView, PointerEventData> CardDrag;
        public event System.Action<BattleCardView, PointerEventData> CardEndDrag;

        // ── 공개 메서드 ────────────────────────────────────────────

        /// <summary>
        /// 손패 카드 목록에 맞춰 뷰를 갱신한다.
        /// 기존 카드 뷰는 재사용하고, 새로 추가된 카드만 딜 애니메이션으로 등장시킨다.
        /// </summary>
        public void RefreshHand(IReadOnlyList<BattleRuntimeCard> handCards, bool interactable = true)
        {
            isInteractable   = interactable;
            selectedCardView = null;

            // ── 빈 손패 처리 ──────────────────────────────────────
            if (handCards == null || handCards.Count == 0)
            {
                foreach (BattleCardView cv in cardViews)
                    if (cv != null) Destroy(cv.gameObject);
                cardViews.Clear();
                boundRuntimeCards.Clear();
                return;
            }

            // ── 손패에서 빠진 카드 뷰 제거 ───────────────────────
            for (int i = cardViews.Count - 1; i >= 0; i--)
            {
                bool stillInHand = false;
                foreach (BattleRuntimeCard rc in handCards)
                {
                    if (rc == boundRuntimeCards[i]) { stillInHand = true; break; }
                }

                if (!stillInHand)
                {
                    if (cardViews[i] != null) Destroy(cardViews[i].gameObject);
                    cardViews.RemoveAt(i);
                    boundRuntimeCards.RemoveAt(i);
                }
            }

            // ── handCards 순서대로 뷰 목록 재구성 ────────────────
            // 새 카드가 처음 등장하는 인덱스를 기록한다.
            var newCardViews    = new List<BattleCardView>();
            var newBoundCards   = new List<BattleRuntimeCard>();
            int firstDealIndex  = handCards.Count;   // 새 카드 없으면 count(=전부 reposition)

            for (int i = 0; i < handCards.Count; i++)
            {
                BattleRuntimeCard rc = handCards[i];
                int existingIdx = boundRuntimeCards.IndexOf(rc);

                if (existingIdx >= 0)
                {
                    // 기존 카드: 뷰 재사용, 인터랙션 상태만 갱신
                    BattleCardView existing = cardViews[existingIdx];
                    existing.SetInteractable(interactable);
                    newCardViews.Add(existing);
                    newBoundCards.Add(rc);
                }
                else
                {
                    // 새 카드: 뷰 생성
                    BattleCardView cv = Instantiate(cardViewPrefab, handContainer);
                    if (spriteLibrary != null)
                        cv.SetSpriteLibrary(spriteLibrary);
                    cv.Bind(rc, interactable);
                    cv.Clicked += OnCardClicked;
                    cv.BeginDragged += OnCardBeginDrag;
                    cv.Dragged += OnCardDrag;
                    cv.EndDragged += OnCardEndDrag;
                    newCardViews.Add(cv);
                    newBoundCards.Add(rc);

                    // 첫 번째 새 카드 인덱스 기록
                    if (i < firstDealIndex) firstDealIndex = i;
                }
            }

            cardViews.Clear();
            cardViews.AddRange(newCardViews);
            boundRuntimeCards.Clear();
            boundRuntimeCards.AddRange(newBoundCards);

            // 새 카드: 딜 애니메이션 / 기존 카드: 부드러운 재배치
            ArrangeCards(animate: true, firstDealIndex: firstDealIndex);
        }

        /// <summary>지정 카드만 손패에서 시각적으로 제거한다 (PlayCard 이후 호출).</summary>
        public void RemoveCardView(BattleCardView cv)
        {
            if (cv == null) return;

            int idx = cardViews.IndexOf(cv);
            if (idx >= 0)
            {
                cardViews.RemoveAt(idx);
                boundRuntimeCards.RemoveAt(idx);
            }
            // cv는 PlayCardAnimation 내부에서 파괴됨 — 여기서는 목록만 정리
            // 남은 카드는 딜 없이 새 위치로 부드럽게 재배치
            ArrangeCards(animate: true, firstDealIndex: cardViews.Count);
        }

        /// <summary>
        /// 카드 뷰를 손패 목록에서 분리하고 Canvas 루트로 올린다.
        /// PlayCard 호출 전에 실행해 StateChanged/RefreshHand와의 충돌을 방지한다.
        /// </summary>
        public void DetachCardView(BattleCardView cv)
        {
            if (cv == null) return;

            cv.Clicked -= OnCardClicked;
            cv.BeginDragged -= OnCardBeginDrag;
            cv.Dragged -= OnCardDrag;
            cv.EndDragged -= OnCardEndDrag;

            int idx = cardViews.IndexOf(cv);
            if (idx >= 0)
            {
                cardViews.RemoveAt(idx);
                boundRuntimeCards.RemoveAt(idx);
            }

            if (selectedCardView == cv) selectedCardView = null;

            // Canvas 루트로 이동 (씬에서 사라지지 않고 독립적으로 애니메이션)
            Canvas rootCanvas = cv.GetComponentInParent<Canvas>();
            if (rootCanvas != null)
                cv.transform.SetParent(rootCanvas.transform, worldPositionStays: true);

            // 남은 카드 즉시 재배치 (드래그 중이므로 애니메이션 없음)
            ArrangeCards(animate: false);
        }

        /// <summary>
        /// 카드 사용 실패 시 분리했던 카드 뷰를 손패로 복원한다.
        /// </summary>
        public void ReattachCardView(BattleCardView cv, BattleRuntimeCard runtimeCard)
        {
            if (cv == null) return;

            cv.transform.SetParent(handContainer, worldPositionStays: true);
            cv.Clicked += OnCardClicked;
            cv.BeginDragged += OnCardBeginDrag;
            cv.Dragged += OnCardDrag;
            cv.EndDragged += OnCardEndDrag;
            cv.Bind(runtimeCard, isInteractable);
            cardViews.Add(cv);
            boundRuntimeCards.Add(runtimeCard);
            ArrangeCards(animate: false);
        }

        /// <summary>모든 카드 클릭 가능 여부를 설정한다 (적 턴 중 비활성화 등).</summary>
        public void SetInteractable(bool value)
        {
            isInteractable = value;
            foreach (BattleCardView cv in cardViews)
                cv.SetInteractable(value);
        }

        public bool IsScreenPointAboveHand(Vector2 screenPoint, Canvas canvas)
        {
            if (handContainer == null) return true;

            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector3[] corners = new Vector3[4];
            handContainer.GetWorldCorners(corners);

            float topY = float.MinValue;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 screenCorner = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                topY = Mathf.Max(topY, screenCorner.y);
            }

            return screenPoint.y > topY;
        }

        /// <summary>현재 선택된 카드를 해제한다.</summary>
        public void ClearSelection()
        {
            if (selectedCardView != null)
            {
                selectedCardView.SetSelected(false);
                selectedCardView = null;
            }
        }

        // ── 내부 ───────────────────────────────────────────────────

        private void OnCardClicked(BattleCardView cv)
        {
            if (!isInteractable) return;

            if (selectedCardView == cv)
            {
                cv.SetSelected(false);
                selectedCardView = null;
                CardSelected?.Invoke(null);
                return;
            }

            if (selectedCardView != null)
                selectedCardView.SetSelected(false);

            selectedCardView = cv;
            cv.SetSelected(true);
            CardSelected?.Invoke(cv);
        }

        private void OnCardBeginDrag(BattleCardView cv, PointerEventData eventData)
        {
            if (!isInteractable) return;
            CardBeginDrag?.Invoke(cv, eventData);
        }

        private void OnCardDrag(BattleCardView cv, PointerEventData eventData)
        {
            if (!isInteractable) return;
            CardDrag?.Invoke(cv, eventData);
        }

        private void OnCardEndDrag(BattleCardView cv, PointerEventData eventData)
        {
            if (!isInteractable) return;
            CardEndDrag?.Invoke(cv, eventData);
        }

        /// <summary>
        /// 카드를 부채꼴로 배치한다.
        ///
        /// firstDealIndex:
        ///   i >= firstDealIndex → 덱에서 딜 애니메이션 (새 카드)
        ///   i <  firstDealIndex → 현재 위치에서 새 팬 위치로 부드럽게 이동 (기존 카드)
        ///   firstDealIndex == 0 → 전체 딜 (전투 시작 첫 드로우)
        ///   firstDealIndex == count → 딜 없음, 전체 재배치만
        /// </summary>
        private void ArrangeCards(bool animate, int firstDealIndex = 0)
        {
            int count = cardViews.Count;
            if (count == 0) return;

            float totalWidth = cardSpacing * (count - 1);
            float angleStep  = count > 1 ? maxFanAngle * 2f / (count - 1) : 0f;

            int dealCount = 0;   // 딜 애니메이션을 받은 카드 수 (stagger 계산용)

            for (int i = 0; i < count; i++)
            {
                BattleCardView cv = cardViews[i];
                if (cv == null) continue;

                float t           = count > 1 ? (float)i / (count - 1) : 0.5f;
                float x           = -totalWidth * 0.5f + cardSpacing * i;
                float normalizedT = t * 2f - 1f;
                float y           = -fanDropY * (normalizedT * normalizedT);
                float angle       = maxFanAngle - angleStep * i;

                Vector3    targetPos = new Vector3(x, y, 0f);
                Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);

                RectTransform rt = cv.GetComponent<RectTransform>();
                if (rt == null) continue;

                cv.SetBaseState(targetPos, targetRot, i);

                if (!animate)
                {
                    // 즉시 스냅
                    rt.localPosition = targetPos;
                    rt.localRotation = targetRot;
                    rt.localScale    = Vector3.one;
                }
                else if (i >= firstDealIndex)
                {
                    // ── 새 카드: 덱 위치에서 딜 애니메이션 ──────────
                    Vector3 startPos = GetDealStartLocalPosition();

                    float delay = dealCount * cardDealStagger;
                    dealCount++;

                    rt.localPosition = startPos;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale    = Vector3.zero;

                    rt.DOLocalMove(targetPos, dealDuration)
                        .SetEase(Ease.OutBack).SetDelay(delay);
                    rt.DOLocalRotate(targetRot.eulerAngles, dealDuration)
                        .SetEase(Ease.OutQuad).SetDelay(delay);
                    rt.DOScale(Vector3.one, dealDuration)
                        .SetEase(Ease.OutBack).SetDelay(delay);
                }
                else
                {
                    // ── 기존 카드: 현재 위치 → 새 팬 위치로 부드럽게 이동 ──
                    rt.localScale = Vector3.one;
                    rt.DOLocalMove(targetPos, dealDuration * 0.5f).SetEase(Ease.OutQuad);
                    rt.DOLocalRotate(targetRot.eulerAngles, dealDuration * 0.5f).SetEase(Ease.OutQuad);
                }
            }
        }

        private Vector3 GetDealStartLocalPosition()
        {
            if (deckOriginMarker != null)
            {
                return handContainer.InverseTransformPoint(deckOriginMarker.position);
            }

            if (handContainer == null)
            {
                return dealOriginLocal;
            }

            Canvas canvas = handContainer.GetComponentInParent<Canvas>()?.rootCanvas;
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector2 screenPoint = new Vector2(
                Mathf.Max(0f, Screen.width - screenDealOriginPadding.x),
                Mathf.Max(0f, screenDealOriginPadding.y));

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    handContainer,
                    screenPoint,
                    camera,
                    out Vector2 localPoint))
            {
                return localPoint;
            }

            return dealOriginLocal;
        }
    }
}

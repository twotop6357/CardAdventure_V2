using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 플레이어 손패(Hand)의 카드 뷰 목록을 관리하고 부채꼴로 배치한다.
    /// BattleUIManager가 StateChanged 이벤트마다 RefreshHand()를 호출한다.
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
        [SerializeField] private float dealDuration   = 0.25f;
        [SerializeField] private Vector3 dealOriginLocal = new Vector3(0, -300f, 0);

        // ── 내부 ───────────────────────────────────────────────────
        private readonly List<BattleCardView> cardViews = new List<BattleCardView>();
        private BattleCardView selectedCardView;
        private bool isInteractable = true;

        /// <summary>카드가 클릭(선택)되었을 때 통지. BattleUIManager가 구독한다.</summary>
        public event System.Action<BattleCardView> CardSelected;

        // ── 공개 메서드 ────────────────────────────────────────────

        /// <summary>손패 카드 목록에 맞춰 뷰를 완전히 재구성한다.</summary>
        public void RefreshHand(IReadOnlyList<BattleRuntimeCard> handCards, bool interactable = true)
        {
            isInteractable  = interactable;
            selectedCardView = null;

            // 기존 카드 뷰 제거
            foreach (BattleCardView cv in cardViews)
            {
                if (cv != null) Destroy(cv.gameObject);
            }
            cardViews.Clear();

            if (handCards == null || handCards.Count == 0) return;

            // 새 카드 뷰 생성
            foreach (BattleRuntimeCard rc in handCards)
            {
                BattleCardView cv = Instantiate(cardViewPrefab, handContainer);
                if (spriteLibrary != null)
                    cv.SetSpriteLibrary(spriteLibrary);
                cv.Bind(rc, interactable);
                cv.Clicked += OnCardClicked;
                cardViews.Add(cv);
            }

            ArrangeCards(animate: true);
        }

        /// <summary>지정 카드만 손패에서 시각적으로 제거한다 (PlayCard 이후 호출).</summary>
        public void RemoveCardView(BattleCardView cv)
        {
            if (cv == null) return;
            cardViews.Remove(cv);
            // cv는 PlayCardAnimation 내부에서 파괴됨 — 여기서는 목록만 정리
            ArrangeCards(animate: true);
        }

        /// <summary>
        /// 카드 뷰를 손패 목록에서 분리하고 Canvas 루트로 올린다.
        /// PlayCard 호출 전에 실행해 StateChanged/RefreshHand와의 충돌을 방지한다.
        /// </summary>
        public void DetachCardView(BattleCardView cv)
        {
            if (cv == null) return;

            cv.Clicked -= OnCardClicked;
            cardViews.Remove(cv);

            if (selectedCardView == cv) selectedCardView = null;

            // Canvas 루트로 이동 (씬에서 사라지지 않고 독립적으로 애니메이션)
            Canvas rootCanvas = cv.GetComponentInParent<Canvas>();
            if (rootCanvas != null)
                cv.transform.SetParent(rootCanvas.transform, worldPositionStays: true);

            // 남은 카드 재배치
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
            cv.Bind(runtimeCard, isInteractable);
            cardViews.Add(cv);
            ArrangeCards(animate: false);
        }

        /// <summary>모든 카드 클릭 가능 여부를 설정한다 (적 턴 중 비활성화 등).</summary>
        public void SetInteractable(bool value)
        {
            isInteractable = value;
            foreach (BattleCardView cv in cardViews)
            {
                cv.SetInteractable(value);
            }
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
                // 이미 선택된 카드를 다시 클릭 → 선택 해제
                cv.SetSelected(false);
                selectedCardView = null;
                CardSelected?.Invoke(null);
                return;
            }

            // 이전 선택 해제
            if (selectedCardView != null)
            {
                selectedCardView.SetSelected(false);
            }

            selectedCardView = cv;
            cv.SetSelected(true);
            CardSelected?.Invoke(cv);
        }

        /// <summary>카드를 부채꼴로 배치한다.</summary>
        private void ArrangeCards(bool animate)
        {
            int count = cardViews.Count;
            if (count == 0) return;

            float totalWidth = cardSpacing * (count - 1);
            float angleStep  = count > 1 ? maxFanAngle * 2f / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                BattleCardView cv = cardViews[i];
                if (cv == null) continue;

                float t          = count > 1 ? (float)i / (count - 1) : 0.5f; // 0..1
                float x          = -totalWidth * 0.5f + cardSpacing * i;
                float normalizedT = t * 2f - 1f;                              // -1..1
                float y          = -fanDropY * (normalizedT * normalizedT);   // 포물선 아래
                float angle      = maxFanAngle - angleStep * i;               // 좌→우 기울기

                Vector3 targetPos = new Vector3(x, y, 0f);
                Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);

                RectTransform rt = cv.GetComponent<RectTransform>();
                if (rt == null) continue;

                if (animate)
                {
                    // 드로우 연출: dealOriginLocal → 목표 위치
                    rt.localPosition = dealOriginLocal;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale    = Vector3.zero;

                    rt.DOLocalMove(targetPos, dealDuration).SetEase(Ease.OutBack);
                    rt.DOLocalRotate(targetRot.eulerAngles, dealDuration).SetEase(Ease.OutQuad);
                    rt.DOScale(Vector3.one, dealDuration).SetEase(Ease.OutBack)
                      .OnComplete(() => cv.SaveBasePosition());
                }
                else
                {
                    rt.localPosition = targetPos;
                    rt.localRotation = targetRot;
                    rt.localScale    = Vector3.one;
                    cv.SaveBasePosition();
                }
            }
        }
    }
}

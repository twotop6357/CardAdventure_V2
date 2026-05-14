using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 손패 카드 한 장을 표시하는 uGUI 컴포넌트.
    ///
    /// [호버 프리뷰 동작]
    /// 1. 마우스를 카드 위에 previewDelay(1초) 유지 → 카드가 Canvas 루트로 reparent되어
    ///    화면 정중앙으로 이동·확대된다.
    /// 2. 동시에 원래 손패 위치에 투명 히트박스(CardHoverProxy)를 생성한다.
    /// 3. 프리뷰 중 닫힘 판단은 OnPointerExit 대신 Update() 폴링으로 처리한다:
    ///    - 중앙 카드 위 OR 원래 위치(프록시) 위 → 유지
    ///    - 두 영역 모두 벗어남 → 프리뷰 닫기
    /// 4. 클릭은 중앙 카드와 원래 위치(프록시) 양쪽 모두 유효하다.
    /// </summary>
    public class BattleCardView : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // ── Inspector ──────────────────────────────────────────────
        [Header("UI 참조 (자동 탐색 — 비워도 됨)")]
        [SerializeField] private Image           cardBackground;
        [SerializeField] private Image           cardArtImage;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI energyCostText;

        [Header("스프라이트 라이브러리")]
        [SerializeField] private CardSpriteLibrary spriteLibrary;

        [Header("일반 호버 연출")]
        [SerializeField] private float hoverLiftY    = 30f;
        [SerializeField] private float hoverDuration = 0.15f;
        [SerializeField] private float selectedScale = 1.08f;

        [Header("프리뷰 (1초 호버 → 화면 중앙 확대)")]
        [Tooltip("프리뷰가 열릴 때까지 대기 시간 (초)")]
        [SerializeField] private float previewDelay    = 0.33f;
        [Tooltip("프리뷰 스케일 배율 (비율 유지)")]
        [SerializeField] private float previewScale    = 2.4f;
        [Tooltip("화면 중앙 기준 Y 오프셋 (0 = 정중앙)")]
        [SerializeField] private float previewOffsetY  = 0f;
        [Tooltip("프리뷰 전환 애니메이션 시간 (초)")]
        [SerializeField] private float previewDuration = 0.22f;

        // ── 내부 상태 ──────────────────────────────────────────────
        private BattleRuntimeCard runtimeCard;
        private bool              isSelected;
        private bool              isInteractable;

        // 기준 상태 (SaveBasePosition에서 기록)
        private Vector3    baseLocalPosition;
        private Quaternion baseLocalRotation;
        private int        baseSiblingIndex;
        private Vector2    baseSize;

        // 프리뷰 상태
        private bool      isPreviewActive;
        private bool      isTransitioning;   // 애니메이션 진행 중 플래그 (이 동안 폴링 스킵)
        private Coroutine previewCoroutine;
        private Transform previewOriginalParent;
        private int       previewOriginalSiblingIndex;
        private Transform pointerFollowOriginalParent;
        private int       pointerFollowOriginalSiblingIndex;
        private bool      isPointerFollowing;

        // 프록시 (원래 손패 위치 히트박스)
        private GameObject    proxyGo;
        private RectTransform proxyRt;

        // Canvas 루트 캐시
        private Canvas rootCanvas;
        private static BattleCardView activeHoverView;

        /// <summary>카드 클릭 이벤트. BattleHandView가 구독한다.</summary>
        public event System.Action<BattleCardView> Clicked;

        public BattleRuntimeCard RuntimeCard => runtimeCard;
        public bool IsPointerFollowing => isPointerFollowing;

        // ── 초기화 ─────────────────────────────────────────────────

        private void Awake()
        {
            if (cardBackground == null)
                cardBackground = GetComponent<Image>();

            if (cardNameText == null)
            {
                Transform t = transform.Find("CardName") ?? transform.Find("NameText");
                if (t != null) cardNameText = t.GetComponent<TextMeshProUGUI>();
            }

            if (descriptionText == null)
            {
                Transform t = transform.Find("CardDescription") ?? transform.Find("DescText");
                if (t != null) descriptionText = t.GetComponent<TextMeshProUGUI>();
            }

            if (cardArtImage == null)
            {
                Transform t = transform.Find("CardArtImage") ?? transform.Find("CardImage") ?? transform.Find("CardIcon");
                if (t != null) cardArtImage = t.GetComponent<Image>();
            }

            if (energyCostText == null)
            {
                Transform t = transform.Find("ManaCostText") ?? transform.Find("CostText");
                if (t != null) energyCostText = t.GetComponent<TextMeshProUGUI>();
            }

            Canvas c = GetComponentInParent<Canvas>();
            rootCanvas = c != null ? c.rootCanvas : null;
        }

        // ── 매 프레임 폴링 ─────────────────────────────────────────

        private void Update()
        {
            if (isPointerFollowing)
            {
                SetPositionToScreenPoint(Input.mousePosition);
                return;
            }

            if (!isPreviewActive || isTransitioning) return;

            Camera uiCam = (rootCanvas != null &&
                            rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                           ? rootCanvas.worldCamera : null;

            Vector2 ptr = Input.mousePosition;

            bool overProxy = proxyRt != null &&
                             RectTransformUtility.RectangleContainsScreenPoint(
                                 proxyRt, ptr, uiCam);

            if (!overProxy)
            {
                CancelPreviewCoroutine();
                ClosePreview(animate: true);
            }
        }

        // ── 공개 메서드 ────────────────────────────────────────────

        public void Bind(BattleRuntimeCard card, bool interactable = true)
        {
            runtimeCard    = card;
            isInteractable = interactable;
            isSelected     = false;
            Refresh();
        }

        public void SetSpriteLibrary(CardSpriteLibrary library)
        {
            spriteLibrary = library;
            if (runtimeCard != null)
                ApplyBackgroundSprite(runtimeCard.Data);
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;
            if (cardBackground != null)
            {
                Color c = cardBackground.color;
                c.a = value ? 1f : 0.5f;
                cardBackground.color = c;
            }
        }

        /// <summary>부채꼴 배치 완료 후 BattleHandView가 호출 — 기준 상태 기록.</summary>
        public void SaveBasePosition()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
            baseSiblingIndex  = transform.GetSiblingIndex();

            RectTransform rt = transform as RectTransform;
            baseSize = rt != null ? rt.sizeDelta : new Vector2(200f, 300f);
        }

        public void SetBaseState(Vector3 localPosition, Quaternion localRotation, int siblingIndex)
        {
            baseLocalPosition = localPosition;
            baseLocalRotation = localRotation;
            baseSiblingIndex  = siblingIndex;

            RectTransform rt = transform as RectTransform;
            baseSize = rt != null ? rt.sizeDelta : new Vector2(200f, 300f);
        }

        // ── 데이터 표시 ────────────────────────────────────────────

        private void Refresh()
        {
            if (runtimeCard == null || runtimeCard.Data == null) return;

            CardData data = runtimeCard.Data;

            if (cardNameText    != null) cardNameText.text    = data.cardName;
            if (energyCostText  != null) energyCostText.text  = data.energyCost.ToString();
            if (descriptionText != null) descriptionText.text = data.GetFormattedDescription();

            if (cardArtImage != null)
            {
                if (data.cardIcon != null)
                {
                    cardArtImage.sprite  = data.cardIcon;
                    cardArtImage.enabled = true;
                }
                else
                {
                    cardArtImage.enabled = false;
                }
            }

            ApplyBackgroundSprite(data);
        }

        private void ApplyBackgroundSprite(CardData data)
        {
            if (cardBackground == null || data == null) return;

            if (spriteLibrary != null)
            {
                Sprite bg = spriteLibrary.GetCardSprite(data.cardClass, data.grade);
                if (bg != null)
                {
                    cardBackground.sprite = bg;
                    cardBackground.color  = Color.white;
                    return;
                }
            }

            cardBackground.sprite = null;
            cardBackground.color  = GetTypeColor(data.cardType);
        }

        private Color GetTypeColor(CardType type) => type switch
        {
            CardType.Attack       => new Color(0.85f, 0.25f, 0.25f),
            CardType.Defense      => new Color(0.25f, 0.50f, 0.85f),
            CardType.Skill        => new Color(0.25f, 0.75f, 0.40f),
            CardType.StatusEffect => new Color(0.65f, 0.25f, 0.85f),
            _                     => Color.white,
        };

        // ── 포인터 이벤트 ─────────────────────────────────────────

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isPreviewActive || isTransitioning) return;
            if (!isInteractable || isSelected) return;

            if (activeHoverView != null && activeHoverView != this)
            {
                activeHoverView.CancelHoverAndPreview(animate: true);
            }
            activeHoverView = this;

            DOTween.Kill(transform, complete: true);
            transform.localRotation = baseLocalRotation;
            transform.DOLocalMoveY(baseLocalPosition.y + hoverLiftY, hoverDuration)
                     .SetEase(Ease.OutQuad);

            CancelPreviewCoroutine();
            previewCoroutine = StartCoroutine(PreviewRoutine());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // 프리뷰 중에는 Update() 폴링이 닫힘을 담당 — 여기서는 무시
            if (isPreviewActive) return;

            CancelPreviewCoroutine();
            if (isSelected) return;

            ClosePreview(animate: true);
            if (activeHoverView == this)
            {
                activeHoverView = null;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isPointerFollowing) return;
            if (!isInteractable) return;

            CancelPreviewCoroutine();
            ClosePreview(animate: false);
            Clicked?.Invoke(this);
        }

        /// <summary>원래 손패 위치의 프록시가 클릭되었을 때 호출된다.</summary>
        internal void OnProxyClick(PointerEventData eventData)
        {
            if (!isInteractable) return;

            CancelPreviewCoroutine();
            ClosePreview(animate: false);
            Clicked?.Invoke(this);
        }

        // ── 선택 강조 ──────────────────────────────────────────────

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            CancelPreviewCoroutine();
            ClosePreview(animate: false);

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

        public void BeginPointerFollow(Canvas targetCanvas)
        {
            CancelPreviewCoroutine();
            ClosePreview(animate: false);
            DOTween.Kill(transform, complete: false);

            if (!isPointerFollowing)
            {
                pointerFollowOriginalParent = transform.parent;
                pointerFollowOriginalSiblingIndex = transform.GetSiblingIndex();
            }

            Canvas canvas = targetCanvas != null ? targetCanvas.rootCanvas : rootCanvas;
            if (canvas != null)
            {
                transform.SetParent(canvas.transform, worldPositionStays: true);
            }

            transform.SetAsLastSibling();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            SetPositionToScreenPoint(Input.mousePosition);
            isPointerFollowing = true;
        }

        // Screen Space Overlay / Camera 양쪽에서 스크린 좌표를 올바른 UI 위치로 변환한다.
        private void SetPositionToScreenPoint(Vector2 screenPoint)
        {
            if (rootCanvas == null)
            {
                transform.position = screenPoint;
                return;
            }

            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                transform.position = screenPoint;
            }
            else
            {
                Camera cam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
                RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect, screenPoint, cam, out Vector2 localPoint))
                {
                    transform.localPosition = localPoint;
                }
            }
        }

        public void EndPointerFollow(bool restoreToHand)
        {
            if (!isPointerFollowing)
            {
                return;
            }

            DOTween.Kill(transform, complete: false);
            isPointerFollowing = false;

            if (restoreToHand && pointerFollowOriginalParent != null)
            {
                transform.SetParent(pointerFollowOriginalParent, worldPositionStays: false);
                transform.SetSiblingIndex(pointerFollowOriginalSiblingIndex);
                transform.localPosition = baseLocalPosition;
                transform.localRotation = baseLocalRotation;
                transform.localScale = Vector3.one;
            }

            pointerFollowOriginalParent = null;
        }


        // ── 프리뷰 ─────────────────────────────────────────────────

        private IEnumerator PreviewRoutine()
        {
            yield return new WaitForSeconds(previewDelay);

            isPreviewActive = true;
            isTransitioning = true;

            // 부모 저장
            previewOriginalParent       = transform.parent;
            previewOriginalSiblingIndex = transform.GetSiblingIndex();

            // 원래 위치에 투명 프록시 생성 (클릭 감지용)
            SpawnProxy();

            // Canvas 루트로 reparent → 모든 UI 위에 표시
            if (rootCanvas != null)
                transform.SetParent(rootCanvas.transform, worldPositionStays: true);
            transform.SetAsLastSibling();

            // 화면 중앙으로 이동 + 기울기 제거 + 확대
            Vector3 centerPos = new Vector3(0f, previewOffsetY, 0f);
            DOTween.Kill(transform, complete: true);
            transform.DOLocalMove(centerPos, previewDuration).SetEase(Ease.OutCubic);
            transform.DOLocalRotate(Vector3.zero, previewDuration).SetEase(Ease.OutCubic);
            transform.DOScale(previewScale, previewDuration).SetEase(Ease.OutCubic);

            // 애니메이션이 끝난 뒤 폴링 시작
            yield return new WaitForSeconds(previewDuration + 0.05f);
            isTransitioning = false;

            previewCoroutine = null;
        }

        private void SpawnProxy()
        {
            if (previewOriginalParent == null) return;

            proxyGo = new GameObject("CardPreviewProxy");
            proxyGo.transform.SetParent(previewOriginalParent, worldPositionStays: false);
            proxyGo.transform.SetSiblingIndex(previewOriginalSiblingIndex);

            proxyRt               = proxyGo.AddComponent<RectTransform>();
            proxyRt.localPosition = baseLocalPosition;
            proxyRt.localRotation = baseLocalRotation;
            proxyRt.localScale    = Vector3.one;
            proxyRt.sizeDelta     = baseSize;

            // 투명하지만 Raycast는 받는 Image
            Image img         = proxyGo.AddComponent<Image>();
            img.color         = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;

            CardHoverProxy proxy = proxyGo.AddComponent<CardHoverProxy>();
            proxy.Init(this);
        }

        private void CancelPreviewCoroutine()
        {
            if (previewCoroutine != null)
            {
                StopCoroutine(previewCoroutine);
                previewCoroutine = null;
            }
        }

        /// <summary>
        /// 프리뷰를 닫고 원래 부모·위치·회전·스케일·프록시를 모두 복원/제거한다.
        /// </summary>
        private void ClosePreview(bool animate)
        {
            bool wasPreview = isPreviewActive;
            isPreviewActive = false;
            isTransitioning = false;

            // 프록시 제거
            if (proxyGo != null)
            {
                Destroy(proxyGo);
                proxyGo = null;
                proxyRt = null;
            }

            // 원래 부모로 복귀
            if (wasPreview && previewOriginalParent != null)
            {
                transform.SetParent(previewOriginalParent, worldPositionStays: true);
                transform.SetSiblingIndex(previewOriginalSiblingIndex);
                previewOriginalParent = null;
            }

            DOTween.Kill(transform, complete: true);

            if (animate)
            {
                transform.DOLocalMove(baseLocalPosition, hoverDuration).SetEase(Ease.OutQuad);
                transform.DOLocalRotate(baseLocalRotation.eulerAngles, hoverDuration).SetEase(Ease.OutQuad);
                transform.DOScale(1f, hoverDuration).SetEase(Ease.OutQuad);
            }
            else
            {
                transform.localPosition = baseLocalPosition;
                transform.localRotation = baseLocalRotation;
                transform.localScale    = Vector3.one;
            }

            if (activeHoverView == this)
            {
                activeHoverView = null;
            }
        }

        private void CancelHoverAndPreview(bool animate)
        {
            CancelPreviewCoroutine();
            if (isSelected) return;

            ClosePreview(animate);
            if (activeHoverView == this)
            {
                activeHoverView = null;
            }
        }

        // ── 카드 사용 연출 ─────────────────────────────────────────

        public void PlayCardAnimation(Vector3 worldTarget, System.Action onComplete = null)
        {
            CancelPreviewCoroutine();
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

        private void OnDisable()
        {
            CancelPreviewCoroutine();
            isPointerFollowing = false;
            pointerFollowOriginalParent = null;
            if (activeHoverView == this)
            {
                activeHoverView = null;
            }
            DOTween.Kill(transform, complete: false);
            if (proxyGo != null)
            {
                Destroy(proxyGo);
                proxyGo = null;
                proxyRt = null;
            }
        }

        public void DestroyImmediate()
        {
            CancelPreviewCoroutine();
            isPointerFollowing = false;
            pointerFollowOriginalParent = null;
            if (activeHoverView == this)
            {
                activeHoverView = null;
            }
            DOTween.Kill(transform, complete: false);
            Destroy(gameObject);
        }
    }
}

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
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
        private bool              wasPreviewedOnDown; // 프리뷰 도중 마우스 다운 여부 트래킹

        // 기준 상태 (SaveBasePosition에서 기록)
        private Vector3    baseLocalPosition;
        private Quaternion baseLocalRotation;
        private int        baseSiblingIndex;
        private Vector2    baseSize;
        private Transform  originalHandParent;    // 원래 손패 부모 컨테이너 캐시
        private float      hoverDisableTimer = 0f; // 드래그 취소 후 호버링 지연 쿨다운 타이머

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

        public event System.Action<BattleCardView, PointerEventData> BeginDragged;
        public event System.Action<BattleCardView, PointerEventData> Dragged;
        public event System.Action<BattleCardView, PointerEventData> EndDragged;

        private bool wasDragged;

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
            if (hoverDisableTimer > 0f)
            {
                hoverDisableTimer -= Time.deltaTime;
            }

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
            originalHandParent = transform.parent;

            RectTransform rt = transform as RectTransform;
            baseSize = rt != null ? rt.sizeDelta : new Vector2(200f, 300f);
        }

        public void SetBaseState(Vector3 localPosition, Quaternion localRotation, int siblingIndex)
        {
            baseLocalPosition = localPosition;
            baseLocalRotation = localRotation;
            baseSiblingIndex  = siblingIndex;
            originalHandParent = transform.parent;

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
            if (!isInteractable || isSelected || isPointerFollowing) return;
            if (hoverDisableTimer > 0f) return;
            if (Input.GetMouseButton(0)) return;

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

        public void OnPointerDown(PointerEventData eventData)
        {
            wasDragged = false;
            wasPreviewedOnDown = false;
            if (isPreviewActive || previewCoroutine != null)
            {
                CancelPreviewCoroutine();
                if (isPreviewActive)
                {
                    wasPreviewedOnDown = true;
                    isPreviewActive = false;
                    isTransitioning = false;

                    if (activeHoverView == this)
                    {
                        activeHoverView = null;
                    }

                    DOTween.Kill(transform, complete: true);
                    transform.localScale = Vector3.one;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (wasPreviewedOnDown && !wasDragged && !isPointerFollowing)
            {
                wasPreviewedOnDown = false;
                ClosePreview(animate: false);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isInteractable) return;
            wasDragged = true;
            CancelPreviewCoroutine();
            if (wasPreviewedOnDown)
            {
                wasPreviewedOnDown = false;
            }
            ClosePreview(animate: false, delayProxyDestruction: true);
            BeginDragged?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isInteractable) return;
            Dragged?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isInteractable) return;
            EndDragged?.Invoke(this, eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (wasDragged) return;
            if (isPointerFollowing) return;
            if (!isInteractable) return;

            CancelPreviewCoroutine();
            if (wasPreviewedOnDown)
            {
                wasPreviewedOnDown = false;
            }
            ClosePreview(animate: false);
            Clicked?.Invoke(this);
        }

        /// <summary>원래 손패 위치의 프록시가 클릭되었을 때 호출된다.</summary>
        internal void OnProxyClick(PointerEventData eventData)
        {
            if (!isInteractable) return;

            CancelPreviewCoroutine();
            if (wasPreviewedOnDown)
            {
                wasPreviewedOnDown = false;
            }
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
            ClosePreview(animate: false, delayProxyDestruction: true);
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

            // 지연되었던 프리뷰 프록시를 이 시점에 확실히 파괴
            if (proxyGo != null)
            {
                Destroy(proxyGo);
                proxyGo = null;
                proxyRt = null;
            }

            if (restoreToHand)
            {
                hoverDisableTimer = 0.4f; // 드래그 완료/취소 후 즉시 프리뷰 줌이 켜지는 현상 방지 쿨다운
                Transform targetParent = originalHandParent != null ? originalHandParent : pointerFollowOriginalParent;
                if (targetParent != null)
                {
                    transform.SetParent(targetParent, worldPositionStays: false);
                    transform.SetSiblingIndex(baseSiblingIndex);
                    transform.localPosition = baseLocalPosition;
                    transform.localRotation = baseLocalRotation;
                    transform.localScale = Vector3.one;
                }
            }

            pointerFollowOriginalParent = null;
        }


        // ── 프리뷰 ─────────────────────────────────────────────────

        private IEnumerator PreviewRoutine()
        {
            yield return new WaitForSeconds(previewDelay);

            if (Input.GetMouseButton(0) || isSelected || isPointerFollowing || !isInteractable || hoverDisableTimer > 0f)
            {
                previewCoroutine = null;
                yield break;
            }

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
            Transform targetParent = originalHandParent != null ? originalHandParent : previewOriginalParent;
            if (targetParent == null) return;

            proxyGo = new GameObject("CardPreviewProxy");
            proxyGo.transform.SetParent(targetParent, worldPositionStays: false);
            proxyGo.transform.SetSiblingIndex(baseSiblingIndex);

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
        private void ClosePreview(bool animate, bool delayProxyDestruction = false)
        {
            isPreviewActive = false;
            isTransitioning = false;

            // 프록시 제거 (드래그/포인터 팔로우 전환 시 EventSystem 포커스 유지를 위해 파괴 지연 지원)
            if (proxyGo != null && !delayProxyDestruction)
            {
                Destroy(proxyGo);
                proxyGo = null;
                proxyRt = null;
            }

            // 원래 부모로 복귀
            Transform targetParent = originalHandParent != null ? originalHandParent : previewOriginalParent;
            if (targetParent != null && transform.parent != targetParent)
            {
                transform.SetParent(targetParent, worldPositionStays: false);
                transform.SetSiblingIndex(baseSiblingIndex);
            }
            previewOriginalParent = null;

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

        public void PlayCardUseAnimation(Vector2 clickScreenPosition, bool exhaust, System.Action onComplete = null)
        {
            CancelPreviewCoroutine();
            DOTween.Kill(transform, complete: false);
            isPointerFollowing = false;

            transform.SetAsLastSibling();
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            SetPositionToScreenPoint(clickScreenPosition);

            if (exhaust)
            {
                PlayExhaustAnimation(onComplete);
                return;
            }

            PlayDiscardSuctionAnimation(onComplete);
        }

        private void PlayDiscardSuctionAnimation(System.Action onComplete)
        {
            Vector2 targetScreenPoint = new Vector2(Screen.width - 96f, 88f);
            const float duration = 0.42f;

            Sequence sequence = DOTween.Sequence()
                .Append(transform.DOScale(1.08f, 0.08f).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(0.12f, duration).SetEase(Ease.InBack))
                .Join(transform.DORotate(new Vector3(0f, 0f, -28f), duration).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    onComplete?.Invoke();
                    Destroy(gameObject);
                });

            Tween moveTween = CreateScreenPointMoveTween(targetScreenPoint, duration);
            if (moveTween != null)
            {
                sequence.Join(moveTween.SetEase(Ease.InCubic));
            }
        }

        private void PlayExhaustAnimation(System.Action onComplete)
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;

            DOTween.Sequence()
                .Append(transform.DOScale(1.18f, 0.1f).SetEase(Ease.OutBack))
                .Join(transform.DORotate(new Vector3(0f, 0f, 10f), 0.1f).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(0f, 0.26f).SetEase(Ease.InBack))
                .Join(transform.DORotate(new Vector3(0f, 0f, 180f), 0.26f).SetEase(Ease.InQuad))
                .Join(canvasGroup.DOFade(0f, 0.24f).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    onComplete?.Invoke();
                    Destroy(gameObject);
                });
        }

        private Tween CreateScreenPointMoveTween(Vector2 screenPoint, float duration)
        {
            if (rootCanvas == null)
            {
                return transform.DOMove(screenPoint, duration);
            }

            if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return transform.DOMove(screenPoint, duration);
            }

            Camera cam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : Camera.main;
            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
            if (canvasRect != null
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    cam,
                    out Vector2 localPoint))
            {
                return transform.DOLocalMove(localPoint, duration);
            }

            return transform.DOMove(screenPoint, duration);
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

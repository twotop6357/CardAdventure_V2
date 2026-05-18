using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure.UI
{
    /// <summary>
    /// ESC 인게임 메뉴 "내 카드" 서브패널.
    ///
    /// 레이아웃 (패널 860×500 / 캔버스 1920×1080 기준):
    ///   - 좌측 200px : 총 카드 수 + 에너지 비용별 통계
    ///   - 우측 나머지: 카드 그리드 ScrollRect (에너지 오름차순 정렬)
    ///
    /// 카드 표시: 기존 CardView 프리팹(200×300)을 래퍼 안에서 0.6 스케일로 인스턴스화.
    /// CardSpriteLibrary로 직업별 배경 이미지 표시 (상점과 동일 방식).
    /// BattleCardView 컴포넌트는 비주얼 바인딩 후 disabled → 호버·드래그 없음.
    /// </summary>
    public class MyCardsPanelController : MonoBehaviour
    {
        [Header("패널 루트")]
        public CanvasGroup panelCanvasGroup;
        public Button closeButton;

        [Header("좌측 통계 패널")]
        public TextMeshProUGUI totalCountText;  // "총 N장" 표시
        public Transform statsContainer;         // VerticalLayoutGroup — 비용별 항목

        [Header("우측 카드 그리드")]
        public RectTransform cardGridContent;   // GridLayoutGroup + ContentSizeFitter

        [Header("카드 프리팹 (CardView.prefab)")]
        public BattleCardView cardPrefab;       // 에디터 Setup 툴이 주입
        public CardSpriteLibrary spriteLibrary; // 상점과 동일한 직업별 카드 배경 스프라이트

        // ── 런타임 동적 오브젝트 ───────────────────────────────────
        private readonly List<GameObject> _statItems = new List<GameObject>();
        private readonly List<GameObject> _cardItems = new List<GameObject>();
        private bool _hasBuiltContent;
        private int _lastDeckSignature = int.MinValue;

        // 카드 원본 크기 vs 셀 크기 → 스케일 (0.6)
        private const float CardNativeW = 200f;
        private const float CardNativeH = 300f;
        private const float CellW       = 120f;
        private const float CellH       = 180f;
        private static readonly Vector3 CardScale =
            new Vector3(CellW / CardNativeW, CellH / CardNativeH, 1f); // (0.6, 0.6, 1)

        // ── 라이프사이클 ───────────────────────────────────────────

        private void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        // ── 공개 API ───────────────────────────────────────────────

        public void Show()
        {
            // 진행 중인 트윈 제거 → OnComplete SetActive(false) 콜백이 뒤늦게 발화하는 것 방지
            if (panelCanvasGroup != null) panelCanvasGroup.DOKill();

            gameObject.SetActive(true);
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.DOFade(1f, 0.18f).SetUpdate(true);
            }
            int currentDeckSignature = GetCurrentDeckSignature();
            if (!_hasBuiltContent || currentDeckSignature != _lastDeckSignature)
            {
                if (GameDataManager.Instance != null)
                {
                    Refresh();
                    _lastDeckSignature = GetCurrentDeckSignature();
                    _hasBuiltContent = true;
                }
            }
            else
            {
                ResetScrollPosition();
            }
        }

        public void Hide()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.DOKill();
                panelCanvasGroup.DOFade(0f, 0.14f).SetUpdate(true)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
                gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (GameDataManager.Instance == null) return;

            var deck  = GameDataManager.Instance.Deck;
            int total = deck.Count;

            // 에너지 비용별 집계
            var costCount = new Dictionary<int, int>();
            foreach (var card in deck)
            {
                if (!costCount.ContainsKey(card.energyCost))
                    costCount[card.energyCost] = 0;
                costCount[card.energyCost]++;
            }

            // ── 좌측 통계 갱신 ────────────────────────────────────
            if (totalCountText != null)
                totalCountText.text = $"총  <b>{total}</b>장";

            foreach (var item in _statItems)
                DestroyItem(item);
            _statItems.Clear();

            var sortedCosts = new List<int>(costCount.Keys);
            sortedCosts.Sort();
            foreach (int cost in sortedCosts)
                _statItems.Add(CreateStatItem(cost, costCount[cost]));

            // ── 우측 카드 그리드 갱신 ─────────────────────────────
            foreach (var item in _cardItems)
                DestroyItem(item);
            _cardItems.Clear();

            var sorted = new List<CardData>(deck);
            sorted.Sort((a, b) =>
            {
                int cmp = a.energyCost.CompareTo(b.energyCost);
                return cmp != 0 ? cmp : string.CompareOrdinal(a.cardName, b.cardName);
            });

            foreach (var card in sorted)
                _cardItems.Add(CreateCardItem(card));

            // 스크롤 최상단으로 초기화
            if (cardGridContent != null)
            {
                var sr = cardGridContent.GetComponentInParent<ScrollRect>();
                if (sr != null) sr.verticalNormalizedPosition = 1f;
            }
        }

        // ── 통계 항목 빌더 ─────────────────────────────────────────

        public void RebuildContentOnNextShow()
        {
            _hasBuiltContent = false;
            _lastDeckSignature = int.MinValue;
        }

        private int GetCurrentDeckSignature()
        {
            GameDataManager manager = GameDataManager.Instance;
            if (manager == null)
            {
                return int.MinValue;
            }

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)manager.SelectedJobClass;
                hash = hash * 31 + (manager.SelectedJobInfo != null ? manager.SelectedJobInfo.GetInstanceID() : 0);

                List<CardData> deck = manager.Deck;
                hash = hash * 31 + (deck != null ? deck.Count : 0);
                if (deck != null)
                {
                    for (int i = 0; i < deck.Count; i++)
                    {
                        CardData card = deck[i];
                        hash = hash * 31 + (card != null ? card.GetInstanceID() : 0);
                        hash = hash * 31 + (card != null ? card.energyCost : 0);
                    }
                }

                return hash;
            }
        }

        private void ResetScrollPosition()
        {
            if (cardGridContent == null)
            {
                return;
            }

            var sr = cardGridContent.GetComponentInParent<ScrollRect>();
            if (sr != null)
            {
                sr.verticalNormalizedPosition = 1f;
            }
        }

        private static void DestroyItem(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            item.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(item);
            }
            else
            {
                DestroyImmediate(item);
            }
        }

        private GameObject CreateStatItem(int cost, int count)
        {
            var go = new GameObject($"Stat_Cost{cost}");
            go.transform.SetParent(statsContainer, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(168f, 42f);

            // 배경
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.10f, 0.10f, 0.85f);

            // 왼쪽 비용 컬러 바
            var barGo = new GameObject("ColorBar");
            barGo.transform.SetParent(go.transform, false);
            var barImg = barGo.AddComponent<Image>();
            barImg.color = CostColor(cost);
            var barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = Vector2.zero;
            barRt.anchorMax = new Vector2(0f, 1f);
            barRt.pivot     = new Vector2(0f, 0.5f);
            barRt.sizeDelta = new Vector2(5f, 0f);
            barRt.anchoredPosition = Vector2.zero;

            // "X코스트" 라벨
            var labelGo  = new GameObject("CostLabel");
            labelGo.transform.SetParent(go.transform, false);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text      = $"{cost}코스트";
            labelTmp.fontSize  = 16f;
            labelTmp.color     = new Color(0.72f, 0.74f, 0.74f, 1f);
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = new Vector2(0.62f, 1f);
            labelRt.offsetMin = new Vector2(10f, 0f);
            labelRt.offsetMax = Vector2.zero;

            // "N장" 숫자
            var numGo  = new GameObject("CountLabel");
            numGo.transform.SetParent(go.transform, false);
            var numTmp = numGo.AddComponent<TextMeshProUGUI>();
            numTmp.text      = $"<b>{count}</b>장";
            numTmp.fontSize  = 19f;
            numTmp.color     = new Color(0.96f, 0.96f, 0.92f, 1f);
            numTmp.alignment = TextAlignmentOptions.MidlineRight;
            var numRt = numGo.GetComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0.58f, 0f);
            numRt.anchorMax = Vector2.one;
            numRt.offsetMin = Vector2.zero;
            numRt.offsetMax = new Vector2(-6f, 0f);

            return go;
        }

        // ── 카드 아이템 빌더 (프리팹 재활용, 상점 방식 이미지 표시) ──

        private GameObject CreateCardItem(CardData card)
        {
            // ── 래퍼 (GridLayoutGroup이 cellSize로 제어하는 컨테이너) ──
            var wrapper = new GameObject($"CardWrapper_{card.cardName}");
            wrapper.transform.SetParent(cardGridContent, false);
            wrapper.AddComponent<RectTransform>(); // GridLayoutGroup이 sizeDelta를 설정

            if (cardPrefab == null)
            {
                // 프리팹 없을 경우 단순 사각형 fallback
                var fb = new GameObject("FallbackCard");
                fb.transform.SetParent(wrapper.transform, false);
                var fbRt = fb.AddComponent<RectTransform>();
                fbRt.anchorMin = Vector2.zero;
                fbRt.anchorMax = Vector2.one;
                fbRt.sizeDelta = Vector2.zero;
                var fbImg = fb.AddComponent<Image>();
                fbImg.color = CardTypeColor(card.cardType);
                return wrapper;
            }

            // ── 프리팹 인스턴스화 ─────────────────────────────────
            BattleCardView cardView = Instantiate(cardPrefab, wrapper.transform);
            var cardRt = cardView.GetComponent<RectTransform>();
            if (cardRt != null)
            {
                // 래퍼 중앙에 고정, 스케일로 200×300 → 100×150 축소
                cardRt.anchorMin = new Vector2(0.5f, 0.5f);
                cardRt.anchorMax = new Vector2(0.5f, 0.5f);
                cardRt.pivot     = new Vector2(0.5f, 0.5f);
                cardRt.anchoredPosition = Vector2.zero;
                cardView.transform.localScale = CardScale;
            }

            // ── 비주얼 바인딩 (BattleCardView.Bind 활용) ─────────
            var runtimeCard = new BattleRuntimeCard(card);
            cardView.Bind(runtimeCard, false);

            // ── 상점과 동일하게 직업별 배경 스프라이트 적용 ──────
            if (spriteLibrary != null)
                cardView.SetSpriteLibrary(spriteLibrary);

            // ── 인터랙션 비활성화 (호버·드래그·프리뷰 불필요) ────
            cardView.enabled = false;

            return wrapper;
        }

        // ── 색상 헬퍼 ──────────────────────────────────────────────

        private static Color CardTypeColor(CardType type)
        {
            switch (type)
            {
                case CardType.Attack:       return new Color(0.85f, 0.20f, 0.15f, 1f);
                case CardType.Defense:      return new Color(0.10f, 0.45f, 0.80f, 1f);
                case CardType.Skill:        return new Color(0.60f, 0.35f, 0.80f, 1f);
                case CardType.StatusEffect: return new Color(0.15f, 0.65f, 0.25f, 1f);
                default:                    return new Color(0.40f, 0.40f, 0.40f, 1f);
            }
        }

        private static Color CostColor(int cost)
        {
            switch (cost)
            {
                case 0:  return new Color(0.65f, 0.65f, 0.65f, 1f);
                case 1:  return new Color(0.25f, 0.75f, 0.30f, 1f);
                case 2:  return new Color(0.20f, 0.50f, 0.90f, 1f);
                case 3:  return new Color(0.95f, 0.60f, 0.10f, 1f);
                default: return new Color(0.90f, 0.20f, 0.20f, 1f);
            }
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using TMPro;
using TheraBytes.BetterUi;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 전투 승리 후 카드 보상 선택 및 카드 삭제 UI를 관리한다.
    ///
    /// 흐름: 승리 연출 → 메인 메뉴 (카드 획득 / 카드 삭제 선택)
    ///       → 카드 획득: 3장 중 1장 선택 → 복귀
    ///       → 카드 삭제: 덱 전체 표시, 1장 선택 삭제 → 복귀
    ///       → 하단 복귀 버튼: 바로 어드벤처씬으로 복귀
    /// </summary>
    public class BattleRewardUIController : MonoBehaviour
    {
        [Header("보상 카드 풀 (현재 직업으로 필터링)")]
        [SerializeField] private List<CardData> rewardCardPool = new List<CardData>();
        [SerializeField, Range(1, 5)] private int rewardCardCount = 3;

        [Header("카드 프리팹")]
        [SerializeField] private BattleCardView cardViewPrefab;
        [SerializeField] private CardSpriteLibrary spriteLibrary;

        // ── 런타임 ────────────────────────────────────────────────
        private BattleManager bm;
        private bool isActive;
        private int  savedHp;
        private int  goldEarned;
        private readonly List<GameObject> spawnedRoots = new List<GameObject>();

        // ── UI 참조 ───────────────────────────────────────────────
        private Canvas          rootCanvas;
        private CanvasGroup     rootCg;
        private RectTransform   panelRoot;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI subtitleText;
        private GameObject      summaryRoot;          // 전투 요약 화면
        private RectTransform   summaryScrollContent; // 요약 텍스트 컨텐츠
        private TextMeshProUGUI goldBadgeText;        // 골드 보상 표시
        private GameObject      mainMenuRoot;         // 행동 선택 메뉴
        private RectTransform   rewardCardContainer;  // 보상 카드 영역
        private GameObject      deleteViewRoot;       // 삭제 스크롤 뷰 루트
        private RectTransform   scrollContent;        // 덱 카드 목록 부모
        private Button          skipButton;
        private TextMeshProUGUI skipLabel;

        // ════════════════════════════════════════════════════════
        //  공개 API
        // ════════════════════════════════════════════════════════

        public void Show(BattleManager battleManager)
        {
            if (isActive) return;
            isActive = true;
            bm = battleManager;
            savedHp = bm?.Player?.Combatant?.CurrentHp ?? 0;

            // 골드 보상 즉시 적용
            goldEarned = UnityEngine.Random.Range(100, 301);
            GameDataManager.Instance?.EarnGold(goldEarned);

            EnsureUI();
            gameObject.SetActive(true);

            if (rootCg != null)
            {
                rootCg.alpha = 0f;
                rootCg.DOFade(1f, 0.4f).SetEase(Ease.OutQuad)
                      .OnComplete(() => StartSummaryPhase());
            }
            else
            {
                StartSummaryPhase();
            }
        }

        // ════════════════════════════════════════════════════════
        //  페이즈
        // ════════════════════════════════════════════════════════

        private void StartSummaryPhase()
        {
            SetTitle("전투 결과", new Color(0.55f, 0.85f, 1f));
            SetSubtitle("");
            SetSkip("계속 →", () => StartMainMenu());

            summaryRoot.SetActive(true);
            mainMenuRoot.SetActive(false);
            rewardCardContainer.gameObject.SetActive(false);
            deleteViewRoot.SetActive(false);

            BuildSummaryContent();

            if (goldBadgeText != null)
                goldBadgeText.text = $"골드 + {goldEarned} G  획득!";
        }

        private void BuildSummaryContent()
        {
            foreach (Transform child in summaryScrollContent)
                Destroy(child.gameObject);

            VerticalLayoutGroup vl = summaryScrollContent.GetComponent<VerticalLayoutGroup>()
                ?? summaryScrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment        = TextAnchor.UpperLeft;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            vl.spacing = 4f;
            vl.padding = new RectOffset(18, 18, 12, 12);

            List<BattleTurnSummary> log = bm?.TurnLog;
            if (log == null || log.Count == 0)
            {
                AddSummaryLine("기록 없음", 18, new Color(0.6f, 0.6f, 0.6f));
            }
            else
            {
                int totalDamage = 0, totalReceived = 0;
                foreach (BattleTurnSummary turn in log)
                {
                    totalDamage   += turn.DamageDealtToEnemy;
                    totalReceived += turn.DamageTakenByPlayer;

                    // 턴 헤더
                    AddSummaryLine($"── 턴  {turn.TurnNumber} ──────────────────────────",
                        17, new Color(1f, 0.88f, 0.35f), FontStyles.Bold);

                    // 사용 카드
                    string cards = turn.CardsUsed.Count > 0
                        ? string.Join(", ", turn.CardsUsed)
                        : "카드 사용 없음";
                    AddSummaryLine($"  사용 카드: {cards}", 16, new Color(0.85f, 0.92f, 0.82f));

                    // 가한 피해
                    if (turn.DamageDealtToEnemy > 0)
                        AddSummaryLine($"  적에게 가한 피해: {turn.DamageDealtToEnemy}",
                            16, new Color(1f, 0.62f, 0.40f));

                    // 적 행동
                    string enemyLine = $"  적 행동: {turn.EnemyActionDesc}";
                    if (turn.PlayerDodged)
                        enemyLine += "  → 회피 성공!";
                    else if (turn.DamageTakenByPlayer > 0)
                        enemyLine += $"  (받은 피해 {turn.DamageTakenByPlayer})";
                    AddSummaryLine(enemyLine, 16, new Color(0.72f, 0.78f, 1f));
                }

                // 총 합계
                AddSummaryLine("────────────────────────────────────────",
                    14, new Color(0.4f, 0.4f, 0.4f));
                AddSummaryLine(
                    $"총  {log.Count}턴  |  가한 피해: {totalDamage}  |  받은 피해: {totalReceived}",
                    17, new Color(0.92f, 0.92f, 0.92f), FontStyles.Bold);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(summaryScrollContent);
        }

        private void AddSummaryLine(string text, int fontSize, Color color,
            FontStyles style = FontStyles.Normal)
        {
            TextMeshProUGUI tmp = CreateText("Line", summaryScrollContent,
                text, fontSize, TextAlignmentOptions.Left);
            tmp.color     = color;
            tmp.fontStyle = style;
            tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = fontSize + 10f;
        }

        private void StartMainMenu()
        {
            SetTitle("전투 승리!", new Color(1f, 0.88f, 0.2f));
            SetSubtitle("행동을 선택하세요.");
            SetSkip("복귀", () => ReturnToAdventure());

            // 요약 패널 비활성화 + 텍스트 내용 제거 (다음 패널 배경에 잔류하지 않도록)
            summaryRoot.SetActive(false);
            if (summaryScrollContent != null)
                foreach (Transform child in summaryScrollContent)
                    Destroy(child.gameObject);

            mainMenuRoot.SetActive(true);
            rewardCardContainer.gameObject.SetActive(false);
            deleteViewRoot.SetActive(false);

            // 메인 메뉴 페이드인
            CanvasGroup cg = mainMenuRoot.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 0f; cg.DOFade(1f, 0.2f); }
        }

        private void BackToMainMenu()
        {
            ClearSpawnedCards();
            summaryRoot.SetActive(false);
            StartMainMenu();
        }

        private void StartRewardPhase()
        {
            SetTitle("카드 획득", new Color(1f, 0.88f, 0.2f));
            SetSubtitle("카드 1장을 선택하여 덱에 추가합니다.");
            SetSkip("← 뒤로", () => BackToMainMenu());

            mainMenuRoot.SetActive(false);
            rewardCardContainer.gameObject.SetActive(true);
            deleteViewRoot.SetActive(false);

            List<CardData> options = PickRewardCards();
            BuildRewardCards(options);
        }

        private void TransitionToDelete()
        {
            SetTitle("카드 삭제", new Color(0.9f, 0.45f, 0.3f));
            SetSubtitle("덱에서 삭제할 카드를 선택하세요.");
            SetSkip("← 뒤로", () => BackToMainMenu());

            mainMenuRoot.SetActive(false);
            rewardCardContainer.gameObject.SetActive(false);
            deleteViewRoot.SetActive(true);
            deleteViewRoot.GetComponent<CanvasGroup>()?.DOFade(1f, 0.25f);

            ClearSpawnedCards();
            BuildDeleteCards();
        }

        private void ReturnToAdventure()
        {
            if (!isActive) return;
            isActive = false;

            if (SceneLoader.Instance != null)
                SceneLoader.Instance.ReturnFromBattle(savedHp);
        }

        // ════════════════════════════════════════════════════════
        //  카드 보상 페이즈
        // ════════════════════════════════════════════════════════

        private List<CardData> PickRewardCards()
        {
            CardClass jobClass = bm?.ActiveJob?.cardClass
                                 ?? GameDataManager.Instance?.SelectedJobClass
                                 ?? CardClass.Warrior;

            List<CardData> pool = rewardCardPool.FindAll(
                c => c != null && (c.cardClass == jobClass || c.cardClass == CardClass.Universal));

            // Fisher-Yates 셔플
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            return pool.GetRange(0, Mathf.Min(rewardCardCount, pool.Count));
        }

        private void BuildRewardCards(List<CardData> cards)
        {
            ClearSpawnedCards();
            if (cards.Count == 0) { TransitionToDelete(); return; }

            int cols = Mathf.Min(3, cards.Count);
            SetupBetterGrid(rewardCardContainer, cols, new Vector2(16f, 16f), new Vector2(1200f, 1600f));

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];

                // 상점의 Offer_N 래퍼와 동일한 패턴
                GameObject wrapper = CreateImage($"RewardCard_{i + 1}", rewardCardContainer,
                    new Color(0.18f, 0.15f, 0.12f, 1f));
                AddOutline(wrapper, new Color(0.44f, 0.34f, 0.2f, 0.9f), new Vector2(2f, -2f));
                wrapper.AddComponent<LayoutElement>().preferredWidth = 310f;
                wrapper.GetComponent<RectTransform>().sizeDelta = new Vector2(310f, 414f);

                wrapper.transform.localScale = Vector3.zero;
                wrapper.transform.DOScale(Vector3.one, 0.38f)
                       .SetEase(Ease.OutBack)
                       .SetDelay(0.08f + i * 0.13f);

                BattleCardView cv = SpawnCardView(wrapper.GetComponent<RectTransform>(), card);

                CardData captured = card;
                SetupClickableWrapper(wrapper, () => OnRewardPicked(captured));
                spawnedRoots.Add(wrapper);
            }

            // 카드 스폰 후에 레이아웃 강제 갱신 (상점의 ForceShopLayout 패턴)
            var betterGrid = rewardCardContainer.GetComponent<BetterGridLayoutGroup>();
            if (betterGrid != null) betterGrid.CalculateCellSize();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rewardCardContainer);
        }

        private void OnRewardPicked(CardData card)
        {
            if (GameDataManager.Instance != null)
                GameDataManager.Instance.AddCardToDeck(card);
            ReturnToAdventure();
        }

        // ════════════════════════════════════════════════════════
        //  카드 삭제 페이즈
        // ════════════════════════════════════════════════════════

        private void BuildDeleteCards()
        {
            foreach (Transform child in scrollContent) Destroy(child.gameObject);

            List<CardData> deck = GameDataManager.Instance != null
                ? new List<CardData>(GameDataManager.Instance.Deck)
                : new List<CardData>();

            if (deck.Count == 0) { ReturnToAdventure(); return; }

            // GridLayoutGroup (고정 셀 크기, ContentSizeFitter 가 높이 결정)
            GridLayoutGroup grid = scrollContent.GetComponent<GridLayoutGroup>()
                                ?? scrollContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment  = TextAnchor.UpperCenter;
            grid.cellSize        = new Vector2(200f, 270f);
            grid.spacing         = new Vector2(20f, 20f);
            grid.padding         = new RectOffset(12, 12, 12, 12);

            for (int i = 0; i < deck.Count; i++)
            {
                CardData card = deck[i];
                if (card == null) continue;

                BattleCardView cv = SpawnCardView(scrollContent, card);
                cv.transform.localScale = Vector3.zero;
                cv.transform.DOScale(Vector3.one, 0.28f)
                  .SetEase(Ease.OutBack)
                  .SetDelay(i * 0.03f);

                int idx = i;
                SetupClickable(cv, () => OnDeletePicked(idx, card));
                spawnedRoots.Add(cv.gameObject);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        }

        private void OnDeletePicked(int deckIndex, CardData card)
        {
            if (GameDataManager.Instance == null) { ReturnToAdventure(); return; }

            List<CardData> deck = GameDataManager.Instance.Deck;
            // 인덱스 유효성 확인 후 삭제
            if (deckIndex < deck.Count && deck[deckIndex] == card)
                deck.RemoveAt(deckIndex);

            ReturnToAdventure();
        }

        // ════════════════════════════════════════════════════════
        //  카드 뷰 생성 헬퍼
        // ════════════════════════════════════════════════════════

        private BattleCardView SpawnCardView(RectTransform parent, CardData card)
        {
            BattleCardView cv;
            if (cardViewPrefab != null)
            {
                cv = Instantiate(cardViewPrefab, parent);
            }
            else
            {
                GameObject go = new GameObject("Card", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                cv = go.AddComponent<BattleCardView>();
            }

            // 상점의 ResetCardPreviewTransform과 동일하게 center-anchor 설정
            RectTransform rt = cv.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            cv.transform.localRotation = Quaternion.identity;
            cv.transform.localScale    = Vector3.one;

            cv.Bind(new BattleRuntimeCard(card), false);
            cv.enabled = false;
            if (spriteLibrary != null) cv.SetSpriteLibrary(spriteLibrary);

            SetChildText(cv.transform, "CardName",        card.cardName);
            SetChildText(cv.transform, "NameText",         card.cardName);
            SetChildText(cv.transform, "ManaCostText",     card.energyCost.ToString());
            SetChildText(cv.transform, "CostText",         card.energyCost.ToString());
            SetChildText(cv.transform, "CardDescription",  card.GetFormattedDescription());
            SetChildText(cv.transform, "DescText",         card.GetFormattedDescription());
            return cv;
        }

        // 래퍼 오브젝트에 클릭 이벤트 설정 (보상 카드 선택 페이즈용)
        private void SetupClickableWrapper(GameObject wrapper, System.Action onClick)
        {
            Button btn = wrapper.GetComponent<Button>() ?? wrapper.AddComponent<Button>();
            Image  bg  = wrapper.GetComponent<Image>();
            if (bg != null)
            {
                bg.raycastTarget  = true;
                btn.targetGraphic = bg;
                ColorBlock c = btn.colors;
                c.highlightedColor = new Color(1f, 0.92f, 0.55f);
                c.pressedColor     = new Color(0.8f, 0.7f, 0.3f);
                btn.colors = c;
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                btn.interactable = false;
                wrapper.transform.DOScale(1.08f, 0.07f).SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                        wrapper.transform.DOScale(0f, 0.18f).SetEase(Ease.InBack)
                            .OnComplete(() => onClick?.Invoke()));
            });
        }

        private void SetupClickable(BattleCardView cv, System.Action onClick)
        {
            Button btn = cv.GetComponent<Button>() ?? cv.gameObject.AddComponent<Button>();
            Image  bg  = FindImage(cv.transform, "Background") ?? cv.GetComponent<Image>();
            if (bg != null)
            {
                bg.raycastTarget = true;
                btn.targetGraphic = bg;
                ColorBlock c = btn.colors;
                c.highlightedColor = new Color(1f, 0.92f, 0.55f);
                c.pressedColor     = new Color(0.8f, 0.7f, 0.3f);
                btn.colors = c;
                AddOutline(cv.gameObject, new Color(0.44f, 0.34f, 0.2f, 0.9f), new Vector2(2f, -2f));
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                btn.interactable = false;
                cv.transform.DOScale(1.08f, 0.07f).SetEase(Ease.OutQuad)
                  .OnComplete(() =>
                      cv.transform.DOScale(0f, 0.18f).SetEase(Ease.InBack)
                        .OnComplete(() => onClick?.Invoke()));
            });
        }

        private void ClearSpawnedCards()
        {
            foreach (GameObject root in spawnedRoots)
                if (root != null) Destroy(root);
            spawnedRoots.Clear();
        }

        // ════════════════════════════════════════════════════════
        //  UI 생성
        // ════════════════════════════════════════════════════════

        private void EnsureUI()
        {
            if (rootCanvas != null) return;

            // ── Canvas ──────────────────────────────────────────
            GameObject canvasGo = new GameObject("BattleRewardCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            rootCanvas = canvasGo.GetComponent<Canvas>();
            rootCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 60;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight  = 0.5f;

            rootCg = canvasGo.AddComponent<CanvasGroup>();

            // ── 딤 오버레이 ─────────────────────────────────────
            Stretch(CreateImage("DimOverlay", canvasGo.transform, new Color(0f, 0f, 0f, 0.72f)).GetComponent<RectTransform>());

            // ── 메인 패널 ───────────────────────────────────────
            GameObject panel = CreateImage("RewardPanel", canvasGo.transform,
                new Color(0.09f, 0.07f, 0.12f, 0.97f));
            panelRoot = panel.GetComponent<RectTransform>();
            panelRoot.anchorMin       = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax       = new Vector2(0.5f, 0.5f);
            panelRoot.pivot           = new Vector2(0.5f, 0.5f);
            panelRoot.sizeDelta       = new Vector2(1100f, 660f);
            panelRoot.anchoredPosition = Vector2.zero;
            AddOutline(panel, new Color(0.55f, 0.35f, 0.85f, 0.9f), new Vector2(3f, -3f));

            // ── 헤더 ────────────────────────────────────────────
            GameObject header = CreateImage("Header", panelRoot, new Color(0.14f, 0.09f, 0.20f, 1f));
            SetStretchOffsets(header.GetComponent<RectTransform>(), new Vector2(0f, 556f), Vector2.zero);

            titleText = CreateText("TitleText", header.transform, "전투 승리!", 42, TextAlignmentOptions.Center);
            Stretch(titleText.rectTransform);
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.88f, 0.2f);

            subtitleText = CreateText("SubtitleText", panelRoot, "", 19, TextAlignmentOptions.Center);
            SetRect(subtitleText.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -115f), new Vector2(-60f, 30f),
                new Vector2(0.5f, 1f));
            subtitleText.color = new Color(0.82f, 0.82f, 0.82f, 1f);

            // ── 전투 요약 뷰 ─────────────────────────────────────
            summaryRoot = new GameObject("SummaryView", typeof(RectTransform));
            summaryRoot.transform.SetParent(panelRoot, false);
            summaryRoot.AddComponent<CanvasGroup>();
            SetStretchOffsets(summaryRoot.GetComponent<RectTransform>(),
                new Vector2(16f, 86f), new Vector2(-16f, -142f));

            // 스크롤 영역 (아래 66px은 골드 배지용)
            GameObject sumScrollGo = CreateImage("SummaryScroll", summaryRoot.transform,
                new Color(0.06f, 0.04f, 0.09f, 0.85f));
            SetStretchOffsets(sumScrollGo.GetComponent<RectTransform>(),
                new Vector2(0f, 66f), Vector2.zero);

            GameObject sumVp = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            sumVp.transform.SetParent(sumScrollGo.transform, false);
            Stretch(sumVp.GetComponent<RectTransform>());
            sumVp.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            sumVp.GetComponent<Mask>().showMaskGraphic = false;

            GameObject sumCo = new GameObject("ScrollContent", typeof(RectTransform));
            sumCo.transform.SetParent(sumVp.transform, false);
            summaryScrollContent = sumCo.GetComponent<RectTransform>();
            summaryScrollContent.anchorMin = new Vector2(0f, 1f);
            summaryScrollContent.anchorMax = new Vector2(1f, 1f);
            summaryScrollContent.pivot     = new Vector2(0.5f, 1f);
            summaryScrollContent.offsetMin = Vector2.zero;
            summaryScrollContent.offsetMax = Vector2.zero;
            ContentSizeFitter sumCsf = sumCo.AddComponent<ContentSizeFitter>();
            sumCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sumSr = sumScrollGo.AddComponent<ScrollRect>();
            sumSr.content          = summaryScrollContent;
            sumSr.viewport         = sumVp.GetComponent<RectTransform>();
            sumSr.horizontal       = false;
            sumSr.vertical         = true;
            sumSr.scrollSensitivity = 30f;
            sumSr.movementType     = ScrollRect.MovementType.Clamped;

            // 골드 배지
            GameObject goldBadgeGo = CreateImage("GoldBadge", summaryRoot.transform,
                new Color(0.15f, 0.10f, 0.04f, 1f));
            AddOutline(goldBadgeGo, new Color(1f, 0.82f, 0.20f, 0.8f), new Vector2(2f, -2f));
            RectTransform goldRect = goldBadgeGo.GetComponent<RectTransform>();
            goldRect.anchorMin        = new Vector2(0.5f, 0f);
            goldRect.anchorMax        = new Vector2(0.5f, 0f);
            goldRect.pivot            = new Vector2(0.5f, 0f);
            goldRect.sizeDelta        = new Vector2(440f, 56f);
            goldRect.anchoredPosition = new Vector2(0f, 5f);
            goldBadgeText = CreateText("GoldText", goldBadgeGo.transform, "", 26, TextAlignmentOptions.Center);
            Stretch(goldBadgeText.rectTransform);
            goldBadgeText.color     = new Color(1f, 0.88f, 0.30f, 1f);
            goldBadgeText.fontStyle = FontStyles.Bold;

            // ── 메인 메뉴 (행동 선택) ───────────────────────────
            mainMenuRoot = new GameObject("MainMenuView", typeof(RectTransform));
            mainMenuRoot.transform.SetParent(panelRoot, false);
            mainMenuRoot.AddComponent<CanvasGroup>().alpha = 1f;
            SetStretchOffsets(mainMenuRoot.GetComponent<RectTransform>(),
                new Vector2(24f, 86f), new Vector2(-24f, -142f));

            // 두 버튼을 좌우로 배치
            GameObject gainBtn = CreateMenuChoiceButton("GainCardButton", mainMenuRoot.transform,
                "카드 획득",
                "직업 카드 1장을\n덱에 추가합니다",
                new Color(0.15f, 0.11f, 0.05f, 1f),
                new Color(0.28f, 0.22f, 0.08f, 1f),
                new Color(1f, 0.80f, 0.20f, 0.85f));
            RectTransform gainRect = gainBtn.GetComponent<RectTransform>();
            gainRect.anchorMin = new Vector2(0f, 0f);
            gainRect.anchorMax = new Vector2(0.47f, 1f);
            gainRect.offsetMin = new Vector2(0f, 20f);
            gainRect.offsetMax = new Vector2(-8f, -20f);
            gainBtn.GetComponent<Button>().onClick.AddListener(StartRewardPhase);

            GameObject delBtn = CreateMenuChoiceButton("DeleteCardButton", mainMenuRoot.transform,
                "카드 삭제",
                "덱에서 카드 1장을\n제거합니다",
                new Color(0.16f, 0.06f, 0.04f, 1f),
                new Color(0.30f, 0.11f, 0.07f, 1f),
                new Color(1f, 0.42f, 0.20f, 0.85f));
            RectTransform delRect = delBtn.GetComponent<RectTransform>();
            delRect.anchorMin = new Vector2(0.53f, 0f);
            delRect.anchorMax = new Vector2(1f, 1f);
            delRect.offsetMin = new Vector2(8f, 20f);
            delRect.offsetMax = new Vector2(0f, -20f);
            delBtn.GetComponent<Button>().onClick.AddListener(TransitionToDelete);

            // ── 카드 보상 영역 ──────────────────────────────────
            GameObject rewardArea = new GameObject("RewardCardArea", typeof(RectTransform));
            rewardArea.transform.SetParent(panelRoot, false);
            rewardCardContainer = rewardArea.GetComponent<RectTransform>();
            SetStretchOffsets(rewardCardContainer, new Vector2(24f, 86f), new Vector2(-24f, -142f));

            // ── 카드 삭제 스크롤 영역 ───────────────────────────
            deleteViewRoot = CreateImage("DeleteScrollView", panelRoot,
                new Color(0.06f, 0.04f, 0.08f, 0.9f));
            SetStretchOffsets(deleteViewRoot.GetComponent<RectTransform>(),
                new Vector2(8f, 86f), new Vector2(-8f, -142f));
            deleteViewRoot.AddComponent<CanvasGroup>().alpha = 0f;

            // Viewport
            GameObject viewport = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(deleteViewRoot.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            // ScrollContent
            GameObject contentGo = new GameObject("ScrollContent", typeof(RectTransform));
            contentGo.transform.SetParent(viewport.transform, false);
            scrollContent = contentGo.GetComponent<RectTransform>();
            scrollContent.anchorMin = new Vector2(0f, 1f);
            scrollContent.anchorMax = new Vector2(1f, 1f);
            scrollContent.pivot     = new Vector2(0.5f, 1f);
            scrollContent.offsetMin = new Vector2(0f, 0f);
            scrollContent.offsetMax = new Vector2(0f, 0f);
            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ScrollRect
            ScrollRect sr = deleteViewRoot.AddComponent<ScrollRect>();
            sr.content   = scrollContent;
            sr.viewport  = viewport.GetComponent<RectTransform>();
            sr.horizontal = false;
            sr.vertical   = true;
            sr.scrollSensitivity = 25f;
            sr.movementType = ScrollRect.MovementType.Clamped;

            // Scrollbar
            GameObject sbGo = CreateImage("VerticalScrollbar", deleteViewRoot.transform,
                new Color(0.18f, 0.12f, 0.22f, 0.9f));
            RectTransform sbRect = sbGo.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1f, 0f);
            sbRect.anchorMax = Vector2.one;
            sbRect.pivot     = new Vector2(1f, 0.5f);
            sbRect.offsetMin = new Vector2(-14f, 0f);
            sbRect.offsetMax = Vector2.zero;

            Scrollbar sb = sbGo.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            GameObject handle = CreateImage("Handle", sbGo.transform, new Color(0.5f, 0.3f, 0.75f, 1f));
            handle.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            sb.handleRect    = handle.GetComponent<RectTransform>();
            sb.targetGraphic = handle.GetComponent<Image>();
            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            // ── 푸터 / 건너뛰기 버튼 ────────────────────────────
            GameObject footer = CreateImage("Footer", panelRoot,
                new Color(0.12f, 0.08f, 0.17f, 1f));
            SetStretchOffsets(footer.GetComponent<RectTransform>(),
                Vector2.zero, new Vector2(0f, -574f));

            skipButton = CreateButton("SkipButton", footer.transform, "건너뛰기",
                new Color(0.2f, 0.15f, 0.28f, 1f), new Color(0.36f, 0.25f, 0.48f, 1f));
            SetRect(skipButton.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(240f, 50f), new Vector2(0.5f, 0.5f));
            skipLabel = skipButton.GetComponentInChildren<TextMeshProUGUI>();

            summaryRoot.SetActive(false);
            mainMenuRoot.SetActive(false);
            deleteViewRoot.SetActive(false);
        }

        // ════════════════════════════════════════════════════════
        //  메뉴 버튼 빌더
        // ════════════════════════════════════════════════════════

        private GameObject CreateMenuChoiceButton(string goName, Transform parent,
            string title, string desc,
            Color normalColor, Color hoverColor, Color borderColor)
        {
            GameObject go = CreateImage(goName, parent, normalColor);
            AddOutline(go, borderColor, new Vector2(2f, -2f));

            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = normalColor;
            cb.highlightedColor = hoverColor;
            cb.pressedColor     = Color.Lerp(normalColor, Color.black, 0.3f);
            btn.colors          = cb;
            btn.targetGraphic   = go.GetComponent<Image>();

            VerticalLayoutGroup vl = go.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment        = TextAnchor.MiddleCenter;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            vl.padding = new RectOffset(16, 16, 32, 32);
            vl.spacing = 16f;

            TextMeshProUGUI titleTmp = CreateText("Title", go.transform, title, 34, TextAlignmentOptions.Center);
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(0.98f, 0.93f, 0.72f, 1f);
            titleTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

            TextMeshProUGUI descTmp = CreateText("Desc", go.transform, desc, 18, TextAlignmentOptions.Center);
            descTmp.color = new Color(0.76f, 0.72f, 0.65f, 1f);
            descTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            return go;
        }

        // ════════════════════════════════════════════════════════
        //  BetterUI 그리드 헬퍼
        // ════════════════════════════════════════════════════════

        private static void SetupBetterGrid(RectTransform container,
            int columns, Vector2 spacing, Vector2 cellRatio)
        {
            BetterGridLayoutGroup grid = container.GetComponent<BetterGridLayoutGroup>()
                                      ?? container.gameObject.AddComponent<BetterGridLayoutGroup>();

            // BetterUI fallback 초기화 (ShopUIController 패턴)
            var field = typeof(BetterGridLayoutGroup)
                .GetField("settingsFallback", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null && field.GetValue(grid) == null)
            {
                var s = new BetterGridLayoutGroup.Settings(grid) { ScreenConfigName = "Fallback" };
                field.SetValue(grid, s);
            }

            GridLayoutGroup baseGrid = grid;
            baseGrid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            baseGrid.constraintCount = columns;
            baseGrid.childAlignment  = TextAnchor.MiddleCenter;
            baseGrid.startAxis       = GridLayoutGroup.Axis.Horizontal;
            baseGrid.spacing         = spacing;
            baseGrid.padding         = new RectOffset(8, 8, 8, 8);

            grid.Fit                 = true;
            grid.KeepCellAspectRatio = true;
            grid.CellSizer.OptimizedSize = cellRatio;
            // ForceUpdateCanvases는 카드 스폰 후 호출자가 직접 호출한다
        }

        // ════════════════════════════════════════════════════════
        //  UI 텍스트 / 버튼 상태 제어
        // ════════════════════════════════════════════════════════

        private void SetTitle(string text, Color color)
        {
            if (titleText == null) return;
            titleText.text  = text;
            titleText.color = color;
            titleText.transform.DOKill();
            titleText.transform.localScale = Vector3.zero;
            titleText.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        private void SetSubtitle(string text)
        {
            if (subtitleText != null) subtitleText.text = text;
        }

        private void SetSkip(string label, System.Action onClick)
        {
            if (skipLabel != null) skipLabel.text = label;
            if (skipButton == null) return;
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() => onClick?.Invoke());
        }

        // ════════════════════════════════════════════════════════
        //  공통 UI 빌더 유틸 (ShopUIController 패턴)
        // ════════════════════════════════════════════════════════

        private GameObject CreateImage(string goName, Transform parent, Color color)
        {
            GameObject go = new GameObject(goName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private TextMeshProUGUI CreateText(string goName, Transform parent,
            string text, int fontSize, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(goName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text            = text;
            tmp.fontSize        = fontSize;
            tmp.alignment       = align;
            tmp.color           = new Color(0.96f, 0.92f, 0.82f, 1f);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode    = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private Button CreateButton(string goName, Transform parent, string label,
            Color normal, Color highlighted)
        {
            GameObject go = CreateImage(goName, parent, normal);
            AddOutline(go, new Color(0.8f, 0.6f, 1f, 0.5f), new Vector2(1.5f, -1.5f));
            Button btn = go.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = normal;
            cb.highlightedColor = highlighted;
            cb.pressedColor     = Color.Lerp(normal, Color.black, 0.35f);
            cb.disabledColor    = new Color(0.2f, 0.18f, 0.22f, 0.6f);
            btn.colors = cb;
            var tmp = CreateText("Label", go.transform, label, 20, TextAlignmentOptions.Center);
            Stretch(tmp.rectTransform);
            return btn;
        }

        private static Outline AddOutline(GameObject target, Color color, Vector2 dist)
        {
            Outline o = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            o.effectColor    = color;
            o.effectDistance = dist;
            return o;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin       = Vector2.zero;
            rt.anchorMax       = Vector2.one;
            rt.pivot           = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta       = Vector2.zero;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 sizeDelta, Vector2 pivot)
        {
            rt.anchorMin       = anchorMin;
            rt.anchorMax       = anchorMax;
            rt.pivot           = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta       = sizeDelta;
        }

        private static void SetStretchOffsets(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void SetChildText(Transform parent, string childName, string text)
        {
            Transform t = parent.Find(childName);
            if (t == null) return;
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        private static Image FindImage(Transform parent, string childName)
        {
            Transform t = parent.Find(childName);
            return t != null ? t.GetComponent<Image>() : null;
        }
    }
}

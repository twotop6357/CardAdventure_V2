using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CardAdventure
{
    public class ShopUIController : MonoBehaviour
    {
        [Header("Shop Data")]
        [SerializeField] private List<CardData> cardPool = new List<CardData>();
        [SerializeField, Range(1, 6)] private int offerCount = 3;
        [SerializeField] private int commonPrice = 45;
        [SerializeField] private int uncommonPrice = 70;
        [SerializeField] private int rarePrice = 110;
        [SerializeField] private int legendaryPrice = 180;
        [SerializeField] private BattleCardView cardViewPrefab;
        [SerializeField] private CardSpriteLibrary spriteLibrary;

        [Header("Runtime UI")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform offerContainer;

        private List<CardData> currentOffers = new List<CardData>();
        private Dictionary<CardClass, List<CardData>> jobPersistentOffers = new Dictionary<CardClass, List<CardData>>();
        private readonly List<OfferView> offerViews = new List<OfferView>();
        private PlayerController cachedPlayer;
        private bool isOpen;

        public static bool IsAnyOpen { get; private set; }
        public bool IsOpen => isOpen;
        public int CurrentOfferCount => currentOffers.Count;

        public event Action<CardData> OnCardPurchased;
        public event Action OnClosed;

        private const string DEFAULT_MESSAGE = "카드를 눌러 구매하세요.";
        private Color defaultMessageColor = Color.white;
        private Coroutine messageCoroutine;

        private void Awake()
        {
            EnsureRuntimeUI();
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        private void Start()
        {
            if (!isOpen)
            {
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isOpen || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.xKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            IsAnyOpen = false;
            SetPlayerInput(true);
        }

        public void Open()
        {
            EnsureRuntimeUI();
            
            isOpen = true;
            IsAnyOpen = true;
            gameObject.SetActive(true);
            ResetMessage();

            CardClass currentJob = GameDataManager.Instance != null ? GameDataManager.Instance.SelectedJobClass : CardClass.Warrior;
            if (jobPersistentOffers.TryGetValue(currentJob, out var saved))
            {
                currentOffers = saved;
            }
            else
            {
                GenerateOffers();
            }

            if (canvas != null)
            {
                canvas.enabled = true;
            }

            SetPlayerInput(false);
            Refresh();
            ForceShopLayout();
        }

        public void Close()
        {
            isOpen = false;
            IsAnyOpen = false;
            SetPlayerInput(true);

            if (canvas != null)
            {
                canvas.enabled = false;
            }

            gameObject.SetActive(false);
            ResetMessage();
            OnClosed?.Invoke();
        }

        public bool TryBuyOffer(int index)
        {
            if (index < 0 || index >= currentOffers.Count)
            {
                return false;
            }

            CardData card = currentOffers[index];
            if (card == null)
            {
                return false;
            }

            GameDataManager gameData = GameDataManager.Instance;
            int price = GetPrice(card);
            if (gameData == null || !gameData.SpendGold(price))
            {
                SetMessage("소지금이 부족합니다.", Color.red, 3f);
                Refresh();
                return false;
            }

            gameData.AddCardToDeck(card);
            
            // 사용자의 요청: 구매 시에만 목록 갱신 (GenerateOffers 호출)
            GenerateOffers();
            
            OnCardPurchased?.Invoke(card);
            SetMessage($"{card.cardName} 카드를 구매했습니다.", defaultMessageColor, 3f);
            Refresh();
            return true;
        }

        public void SetCardPool(List<CardData> cards)
        {
            cardPool = cards ?? new List<CardData>();
        }

        private void GenerateOffers()
        {
            CardClass selectedClass = GameDataManager.Instance != null
                ? GameDataManager.Instance.SelectedJobClass
                : CardClass.Warrior;

            List<CardData> candidates = cardPool.FindAll(c => c != null && (c.cardClass == selectedClass || c.cardClass == CardClass.Universal));
            
            List<CardData> newList = new List<CardData>();
            int count = Mathf.Min(offerCount, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
                newList.Add(candidates[randomIndex]);
                candidates.RemoveAt(randomIndex);
            }

            jobPersistentOffers[selectedClass] = newList;
            currentOffers = newList;
        }

        private int GetPrice(CardData card)
        {
            if (card == null)
            {
                return 0;
            }

            switch (card.grade)
            {
                case CardGrade.Uncommon:
                    return uncommonPrice;
                case CardGrade.Rare:
                    return rarePrice;
                case CardGrade.Legendary:
                    return legendaryPrice;
                default:
                    return commonPrice;
            }
        }

        private void Refresh()
        {
            int gold = GameDataManager.Instance != null ? GameDataManager.Instance.Gold : 0;
            if (goldText != null)
            {
                goldText.text = $"보유 골드: {gold}";
            }

            EnsureOfferViewCount(Mathf.Max(offerCount, currentOffers.Count));

            for (int i = 0; i < offerViews.Count; i++)
            {
                bool hasOffer = i < currentOffers.Count && currentOffers[i] != null;
                offerViews[i].Root.SetActive(hasOffer);
                if (!hasOffer)
                {
                    continue;
                }

                CardData card = currentOffers[i];
                int price = GetPrice(card);
                offerViews[i].PriceText.text = $"{price}G";
                BattleCardView preview = RecreateCardPreview(offerViews[i], i);
                ApplyCardPreview(preview, card);
                offerViews[i].CardButton.interactable = true;
            }

            if (currentOffers.Count == 0)
            {
                SetMessage("판매할 카드가 없습니다.");
            }
            else if (messageText != null && string.IsNullOrEmpty(messageText.text))
            {
                SetMessage("오늘의 카드를 골라보세요.");
            }

            ForceShopLayout();
        }

        private string GetClassName(CardClass cardClass)
        {
            switch (cardClass)
            {
                case CardClass.Warrior:
                    return "전사";
                case CardClass.Mage:
                    return "마법사";
                case CardClass.Rogue:
                    return "도적";
                case CardClass.Archer:
                    return "궁수";
                default:
                    return "공용";
            }
        }

        private string GetTypeName(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Attack:
                    return "공격";
                case CardType.Defense:
                    return "방어";
                case CardType.Skill:
                    return "스킬";
                case CardType.StatusEffect:
                    return "상태";
                default:
                    return cardType.ToString();
            }
        }

        private void SetMessage(string message)
        {
            SetMessage(message, defaultMessageColor);
        }

        private void SetMessage(string message, Color color, float duration = 0f)
        {
            if (messageText == null) return;

            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
                messageCoroutine = null;
            }

            messageText.text = message;
            messageText.color = color;

            if (duration > 0f)
            {
                messageCoroutine = StartCoroutine(RevertMessageAfterDelay(duration));
            }
        }

        private void ResetMessage()
        {
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
                messageCoroutine = null;
            }

            if (messageText != null)
            {
                messageText.text = DEFAULT_MESSAGE;
                messageText.color = defaultMessageColor;
            }
        }

        private System.Collections.IEnumerator RevertMessageAfterDelay(float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            messageText.text = DEFAULT_MESSAGE;
            messageText.color = defaultMessageColor;
            messageCoroutine = null;
        }

        private void SetPlayerInput(bool enabled)
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindFirstObjectByType<PlayerController>();
            }

            if (cachedPlayer != null)
            {
                cachedPlayer.SetInputEnabled(enabled);
            }
        }

        private void EnsureRuntimeUI()
        {
            if (canvas == null)
            {
                canvas = GetComponentInChildren<Canvas>(true);
            }

            if (canvas == null)
            {
                CreateRuntimeUI();
            }

            if (offerContainer == null && panelRoot != null)
            {
                Transform found = panelRoot.Find("OfferContainer");
                offerContainer = found != null ? found : panelRoot;
            }

            EnsureEventSystem();
        }

        private void CreateRuntimeUI()
        {
            GameObject canvasObject = new GameObject("ShopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            Stretch(canvasObject.GetComponent<RectTransform>());

            GameObject dim = CreateImage("Dim", canvasObject.transform, ClassicPixelUiTheme.DimBlack);
            Stretch(dim.GetComponent<RectTransform>());

            GameObject panel = CreateImage("Panel", canvasObject.transform, ClassicPixelUiTheme.WindowBlack);
            panelRoot = panel.GetComponent<RectTransform>();
            panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            panelRoot.pivot = new Vector2(0.5f, 0.5f);
            panelRoot.sizeDelta = new Vector2(1080f, 640f);
            panelRoot.anchoredPosition = Vector2.zero;
            AddPanelOutline(panel, ClassicPixelUiTheme.Gold, new Vector2(3f, -3f));

            GameObject header = CreateImage("Header", panelRoot, ClassicPixelUiTheme.InnerBlack);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            SetStretchOffsets(headerRect, new Vector2(0f, 544f), new Vector2(0f, 0f));

            TextMeshProUGUI title = CreateText("Title", headerRect, "카드 상점", 36, TextAlignmentOptions.Left);
            SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 0f), new Vector2(300f, 50f), new Vector2(0f, 0.5f));
            title.fontStyle = FontStyles.Bold;

            TextMeshProUGUI subtitle = CreateText("Subtitle", headerRect, "오늘의 추천 카드", 18, TextAlignmentOptions.Left);
            SetRect(subtitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34f, -28f), new Vector2(360f, 26f), new Vector2(0f, 0.5f));
            subtitle.color = ClassicPixelUiTheme.MutedText;

            GameObject goldPill = CreateImage("GoldPill", headerRect, ClassicPixelUiTheme.WindowBlack);
            AddPanelOutline(goldPill, ClassicPixelUiTheme.Blue, new Vector2(2f, -2f));
            SetRect(goldPill.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-148f, 0f), new Vector2(210f, 46f), new Vector2(1f, 0.5f));

            goldText = CreateText("GoldText", goldPill.transform, string.Empty, 22, TextAlignmentOptions.Center);
            Stretch(goldText.rectTransform);
            goldText.color = ClassicPixelUiTheme.Energy;

            closeButton = CreateButton("CloseButton", headerRect, "닫기", ClassicPixelUiTheme.WindowBlack, new Color(0.16f, 0.04f, 0.04f, 1f));
            SetRect(closeButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(78f, 42f), new Vector2(1f, 0.5f));

            GameObject container = new GameObject("OfferContainer", typeof(RectTransform), typeof(GridLayoutGroup));
            container.transform.SetParent(panelRoot, false);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            SetStretchOffsets(containerRect, new Vector2(34f, 82f), new Vector2(-34f, -124f));
            offerContainer = container.transform;

            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.spacing = new Vector2(28f, 12f);
            grid.cellSize = new Vector2(292f, 400f);
            grid.padding = new RectOffset(0, 0, 4, 4);

            GameObject footer = CreateImage("Footer", panelRoot, ClassicPixelUiTheme.InnerBlack);
            SetStretchOffsets(footer.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0f, -572f));

            messageText = CreateText("MessageText", footer.transform, DEFAULT_MESSAGE, 20, TextAlignmentOptions.Left);
            SetRect(messageText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(28f, 0f), new Vector2(-56f, 36f), new Vector2(0.5f, 0.5f));
            messageText.color = defaultMessageColor;
        }

        private void EnsureOfferViewCount(int targetCount)
        {
            while (offerViews.Count < targetCount)
            {
                offerViews.Add(CreateOfferView(offerViews.Count));
            }
        }

        private OfferView CreateOfferView(int index)
        {
            GameObject root = CreateImage($"Offer_{index + 1}", offerContainer, ClassicPixelUiTheme.WindowBlack);
            AddPanelOutline(root, ClassicPixelUiTheme.Gold, new Vector2(2f, -2f));
            // sizeDelta는 GridLayoutGroup.cellSize가 결정 (Vector2.zero = 자동)
            root.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            // 카드를 root에 직접 배치 — 프리팹 원본 비율 유지, localScale로 크기 조정
            // 셀(292x400), 가격 52px, 패딩 8px×2 → 카드 가용 276×332
            // 카드 원본 200×300 → scale = min(276/200, 332/300) = 1.107
            BattleCardView cardPreview = CreateCardPreview(root.transform);
            Button cardButton = ConfigureCardPreviewButton(cardPreview, index);

            TextMeshProUGUI price = CreateText("Price", root.transform, string.Empty, 22, TextAlignmentOptions.Center);
            SetRect(price.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 24f), new Vector2(0f, 40f), new Vector2(0.5f, 0f));
            price.fontStyle = FontStyles.Bold;
            price.color = ClassicPixelUiTheme.Energy;

            return new OfferView(root, root.transform, cardPreview, cardButton, price);
        }

        private BattleCardView RecreateCardPreview(OfferView offerView, int offerIndex)
        {
            // Clear all existing children to prevent overlap/accumulation, especially in Play Mode
            foreach (Transform child in offerView.PreviewParent)
            {
                DestroyCardPreview(child.gameObject);
            }

            BattleCardView preview = CreateCardPreview(offerView.PreviewParent);
            Button cardButton = ConfigureCardPreviewButton(preview, offerIndex);
            offerView.SetCardPreview(preview, cardButton);
            return preview;
        }

        private void DestroyCardPreview(GameObject previewObject)
        {
            if (previewObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(previewObject);
            }
            else
            {
                DestroyImmediate(previewObject);
            }
        }

        private BattleCardView CreateCardPreview(Transform parent)
        {
            BattleCardView preview = null;
            if (cardViewPrefab != null)
            {
                preview = Instantiate(cardViewPrefab, parent);
            }

            if (preview == null)
            {
                GameObject fallback = CreateImage("CardPreviewFallback", parent, ClassicPixelUiTheme.WindowBlack);
                preview = fallback.AddComponent<BattleCardView>();
            }

            ApplyCardPreviewTransform(preview);
            preview.enabled = false;

            return preview;
        }

        private Button ConfigureCardPreviewButton(BattleCardView preview, int offerIndex)
        {
            Button button = preview.GetComponent<Button>();
            if (button == null)
            {
                button = preview.gameObject.AddComponent<Button>();
            }

            Image background = FindImage(preview.transform, "Background");
            if (background == null)
            {
                background = preview.GetComponent<Image>();
            }

            if (background != null)
            {
                background.raycastTarget = true;
                button.targetGraphic = background;

                Outline outline = AddPanelOutline(background.gameObject, ClassicPixelUiTheme.Gold, new Vector2(2f, -2f));
                ShopOfferHoverFeedback hover = preview.gameObject.GetComponent<ShopOfferHoverFeedback>();
                if (hover == null)
                {
                    hover = preview.gameObject.AddComponent<ShopOfferHoverFeedback>();
                }

                hover.Configure(outline, ClassicPixelUiTheme.Gold, ClassicPixelUiTheme.Cyan);
            }

            int capturedIndex = offerIndex;
            button.onClick.AddListener(() => TryBuyOffer(capturedIndex));
            return button;
        }

        private void ApplyCardPreview(BattleCardView preview, CardData card)
        {
            if (preview == null || card == null)
            {
                return;
            }

            ResetCardPreviewTransform(preview);
            preview.Bind(new BattleRuntimeCard(card), false);
            preview.enabled = false;
            if (spriteLibrary != null)
            {
                preview.SetSpriteLibrary(spriteLibrary);
            }

            SetChildText(preview.transform, "NameText", card.cardName);
            SetChildText(preview.transform, "CardName", card.cardName);
            SetChildText(preview.transform, "CostText", card.energyCost.ToString());
            SetChildText(preview.transform, "ManaCostText", card.energyCost.ToString());
            SetChildText(preview.transform, "DescText", card.GetFormattedDescription());
            SetChildText(preview.transform, "CardDescription", card.GetFormattedDescription());

            Image icon = FindImage(preview.transform, "CardIcon");
            if (icon == null)
            {
                icon = FindImage(preview.transform, "CardArtImage");
            }

            if (icon == null)
            {
                icon = FindImage(preview.transform, "CardImage");
            }

            if (icon != null)
            {
                icon.sprite = card.cardIcon;
                icon.enabled = card.cardIcon != null;
                icon.preserveAspect = true;
            }
        }

        // 카드 프리팹을 원본 비율 그대로 유지하며 셀 크기에 맞게 배치
        // 셀 292x400, 가격 52px, 패딩 8px*2 → 가용 276x332 / 원본 200x300
        // scale = min(276/200, 332/300) = min(1.38, 1.107) = 1.107
        private const float k_CardPreviewScale = 1.107f;

        // 가격 영역 높이의 절반만큼 Y 위로 오프셋하여 카드를 카드 영역 중앙에 배치
        private const float k_CardPriceOffset = 26f; // 52px / 2

        private void ApplyCardPreviewTransform(BattleCardView preview)
        {
            RectTransform rect = preview.GetComponent<RectTransform>();
            if (rect == null) return;

            // 원본 sizeDelta 유지 (프리팹 그대로) — anchor는 중앙 고정
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            // 가격 영역만큼 위로 오프셋해서 카드 영역 중앙 정렬
            rect.anchoredPosition = new Vector2(0f, k_CardPriceOffset);
            rect.localRotation = Quaternion.identity;
            // scale 적용 — 텍스트/아이콘 등 모든 자식이 함께 커짐
            rect.localScale = new Vector3(k_CardPreviewScale, k_CardPreviewScale, 1f);
        }

        private void ResetCardPreviewTransform(BattleCardView preview)
        {
            ApplyCardPreviewTransform(preview);
        }

        private void ForceShopLayout()
        {
            if (offerContainer is RectTransform offerRect)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(offerRect);
            }

            if (panelRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
            }
        }

        private void SetChildText(Transform parent, string childName, string text)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                return;
            }

            TextMeshProUGUI label = child.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = text;
            }
        }

        private Image FindImage(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private GameObject CreateImage(string objectName, Transform parent, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private TextMeshProUGUI CreateText(string objectName, Transform parent, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            ClassicPixelUiTheme.ApplyText(label);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private Button CreateButton(string objectName, Transform parent, string label)
        {
            return CreateButton(objectName, parent, label, ClassicPixelUiTheme.WindowBlack, new Color(0.08f, 0.17f, 0.24f, 1f));
        }

        private Button CreateButton(string objectName, Transform parent, string label, Color normalColor, Color highlightedColor)
        {
            GameObject go = CreateImage(objectName, parent, normalColor);
            // Outline removed in favor of ApplyShopButton
            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.35f);
            colors.disabledColor = new Color(0.05f, 0.05f, 0.05f, 0.55f);
            button.colors = colors;

            TextMeshProUGUI text = CreateText("Label", go.transform, label, 20, TextAlignmentOptions.Center);
            Stretch(text.rectTransform);
            ClassicPixelUiTheme.ApplyShopButton(button, ClassicPixelUiTheme.ShopPanelAccent.Gold);
            return button;
        }

        private Outline AddPanelOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            return outline;
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(transform.root, false);
        }

        private void Stretch(RectTransform rect)
        {
            SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        }

        private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private void SetStretchOffsets(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private sealed class OfferView
        {
            public OfferView(GameObject root, Transform previewParent, BattleCardView cardPreview, Button cardButton, TextMeshProUGUI priceText)
            {
                Root = root;
                PreviewParent = previewParent;
                CardPreview = cardPreview;
                CardButton = cardButton;
                PriceText = priceText;
            }

            public GameObject Root { get; }
            public Transform PreviewParent { get; }
            public BattleCardView CardPreview { get; private set; }
            public Button CardButton { get; private set; }
            public TextMeshProUGUI PriceText { get; }

            public void SetCardPreview(BattleCardView cardPreview, Button cardButton)
            {
                CardPreview = cardPreview;
                CardButton = cardButton;
            }
        }

        private sealed class ShopOfferHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private Outline outline;
            private Color normalColor;
            private Color hoverColor;

            public void Configure(Outline targetOutline, Color normal, Color hover)
            {
                outline = targetOutline;
                normalColor = normal;
                hoverColor = hover;
                if (outline != null)
                {
                    outline.effectColor = normalColor;
                }
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (outline != null)
                {
                    outline.effectColor = hoverColor;
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (outline != null)
                {
                    outline.effectColor = normalColor;
                }
            }
        }
    }
}

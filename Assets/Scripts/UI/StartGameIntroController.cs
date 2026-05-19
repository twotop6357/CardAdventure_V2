using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardAdventure.UI
{
    /// <summary>
    /// 새 게임 시작 시 로비 대화, 최초 직업 선택, 후속 대화, 어드벤쳐 씬 진입을 순서대로 진행한다.
    /// </summary>
    public class StartGameIntroController : MonoBehaviour
    {
        private const string AdventureSceneName = "AdventureScene";

        [Header("Scene Visuals")]
        [SerializeField] private Sprite lobbyBackground;
        [SerializeField] private Sprite jobChangerImage;

        [Header("Dialogue")]
        [SerializeField] private DialogueData startDialogue;
        [SerializeField] private DialogueData afterJobSelectDialogue;
        [SerializeField] private GameObject dialogueCanvasPrefab;

        [Header("Job Selection")]
        [SerializeField] private List<JobClassInfo> jobs = new List<JobClassInfo>();

        [Header("Layout")]
        [SerializeField] private Vector2 characterSize = new Vector2(360f, 430f);
        [SerializeField] private Vector2 characterOffset = new Vector2(0f, 110f);

        private Canvas rootCanvas;
        private GameObject root;
        private GameObject characterGo;
        private DialogueManager dialogueManager;
        private DialogueView dialogueView;
        private JobChangeUIController jobChangeUI;
        private bool isRunning;

        public void Begin()
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;
            EnsureIntroObjects();
            EnsureDialogueManager();
            EnsureEventSystem();

            root.SetActive(true);
            SetCharacterVisible(true);
            PlayDialogue(startDialogue, HandleStartDialogueEnded);
        }

        private void EnsureIntroObjects()
        {
            if (root != null)
            {
                return;
            }

            root = new GameObject("StartGameIntroRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);

            rootCanvas = root.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 700;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            CreateBackground(root.transform);
            CreateCharacter(root.transform);
            CreateDialogueCanvas();
            CreateJobSelectionUi(root.transform);
            root.SetActive(false);
        }

        private void CreateBackground(Transform parent)
        {
            GameObject bgGo = new GameObject("LobbyBackground", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            bgGo.transform.SetParent(parent, false);

            Image bg = bgGo.GetComponent<Image>();
            bg.sprite = lobbyBackground;
            bg.color = Color.white;
            bg.preserveAspect = true;
            bg.raycastTarget = false;

            RectTransform rt = bgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            AspectRatioFitter fitter = bgGo.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (lobbyBackground != null && lobbyBackground.rect.height > 0f)
            {
                fitter.aspectRatio = lobbyBackground.rect.width / lobbyBackground.rect.height;
            }
        }

        private void CreateCharacter(Transform parent)
        {
            characterGo = new GameObject("JobChangerImage", typeof(RectTransform), typeof(Image));
            characterGo.transform.SetParent(parent, false);

            Image image = characterGo.GetComponent<Image>();
            image.sprite = jobChangerImage;
            image.preserveAspect = true;
            image.raycastTarget = false;

            RectTransform rt = characterGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = characterSize;
            rt.anchoredPosition = characterOffset;
        }

        private void CreateDialogueCanvas()
        {
            GameObject panelGo = new GameObject("StartGameDialoguePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(DialogueView));
            panelGo.transform.SetParent(root.transform, false);

            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 0f);
            panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.offsetMin = new Vector2(48f, 28f);
            panelRt.offsetMax = new Vector2(-48f, 196f);

            Image panelImage = panelGo.GetComponent<Image>();
            ClassicPixelUiTheme.ApplyShopPanel(panelImage, ClassicPixelUiTheme.ShopPanelAccent.Gold, true);

            CanvasGroup group = panelGo.GetComponent<CanvasGroup>();
            group.alpha = 0f;

            GameObject nameBox = new GameObject("NameBox", typeof(RectTransform), typeof(Image));
            nameBox.transform.SetParent(panelGo.transform, false);
            RectTransform nameRt = nameBox.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(0f, 1f);
            nameRt.pivot = new Vector2(0f, 1f);
            nameRt.sizeDelta = new Vector2(210f, 44f);
            nameRt.anchoredPosition = new Vector2(148f, -14f);
            ClassicPixelUiTheme.ApplyShopPanel(nameBox.GetComponent<Image>(), ClassicPixelUiTheme.ShopPanelAccent.Blue, false);

            TextMeshProUGUI speakerText = CreateText(nameBox.transform, "SpeakerNameText", string.Empty, 20f, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform speakerRt = speakerText.rectTransform;
            speakerRt.anchorMin = Vector2.zero;
            speakerRt.anchorMax = Vector2.one;
            speakerRt.offsetMin = new Vector2(10f, 0f);
            speakerRt.offsetMax = new Vector2(-10f, 0f);

            GameObject portraitRoot = new GameObject("PortraitRoot", typeof(RectTransform), typeof(Image));
            portraitRoot.transform.SetParent(panelGo.transform, false);
            RectTransform portraitFrameRt = portraitRoot.GetComponent<RectTransform>();
            portraitFrameRt.anchorMin = new Vector2(0f, 0f);
            portraitFrameRt.anchorMax = new Vector2(0f, 0f);
            portraitFrameRt.pivot = new Vector2(0f, 0f);
            portraitFrameRt.sizeDelta = new Vector2(100f, 100f);
            portraitFrameRt.anchoredPosition = new Vector2(24f, 24f);
            ClassicPixelUiTheme.ApplyShopPanel(portraitRoot.GetComponent<Image>(), ClassicPixelUiTheme.ShopPanelAccent.Blue, false);

            GameObject portraitGo = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
            portraitGo.transform.SetParent(portraitRoot.transform, false);
            Image portraitImage = portraitGo.GetComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            RectTransform portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.anchorMin = Vector2.zero;
            portraitRt.anchorMax = Vector2.one;
            portraitRt.offsetMin = new Vector2(8f, 8f);
            portraitRt.offsetMax = new Vector2(-8f, -8f);

            TextMeshProUGUI bodyText = CreateText(panelGo.transform, "DialogueText", string.Empty, 23f, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            RectTransform bodyRt = bodyText.rectTransform;
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(154f, 34f);
            bodyRt.offsetMax = new Vector2(-72f, -72f);

            TextMeshProUGUI arrowText = CreateText(panelGo.transform, "NextArrow", "▼", 22f, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform arrowRt = arrowText.rectTransform;
            arrowRt.anchorMin = new Vector2(1f, 0f);
            arrowRt.anchorMax = new Vector2(1f, 0f);
            arrowRt.pivot = new Vector2(1f, 0f);
            arrowRt.sizeDelta = new Vector2(44f, 34f);
            arrowRt.anchoredPosition = new Vector2(-24f, 20f);

            dialogueView = panelGo.GetComponent<DialogueView>();
            dialogueView.ConfigureRuntime(group, panelRt, speakerText, nameBox, bodyText, portraitRoot, portraitImage, arrowText.gameObject);
            panelGo.SetActive(false);
        }

        private void CreateJobSelectionUi(Transform parent)
        {
            GameObject panelGo = new GameObject("InitialJobSelectionPanel", typeof(RectTransform), typeof(Image), typeof(JobChangeUIController));
            panelGo.transform.SetParent(parent, false);
            panelGo.SetActive(false);

            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(820f, 520f);
            panelRt.anchoredPosition = Vector2.zero;

            Image panelImage = panelGo.GetComponent<Image>();
            ClassicPixelUiTheme.ApplyShopPanel(panelImage, ClassicPixelUiTheme.ShopPanelAccent.Gold, true);

            TextMeshProUGUI title = CreateText(panelGo.transform, "TitleText", "직업 선택", 34f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetOffsets(title.rectTransform, new Vector2(24f, -66f), new Vector2(-24f, -18f), new Vector2(0f, 1f), new Vector2(1f, 1f));

            List<Button> jobButtons = new List<Button>();
            for (int i = 0; i < 3; i++)
            {
                Button button = CreateButton(panelGo.transform, "JobButton" + (i + 1), string.Empty, ClassicPixelUiTheme.ShopPanelAccent.Blue);
                RectTransform rt = button.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(220f, 70f);
                rt.anchoredPosition = new Vector2(36f, -106f - (i * 84f));
                jobButtons.Add(button);
            }

            GameObject previewFrame = new GameObject("PreviewFrame", typeof(RectTransform), typeof(Image));
            previewFrame.transform.SetParent(panelGo.transform, false);
            RectTransform previewFrameRt = previewFrame.GetComponent<RectTransform>();
            previewFrameRt.anchorMin = new Vector2(0.5f, 0.5f);
            previewFrameRt.anchorMax = new Vector2(0.5f, 0.5f);
            previewFrameRt.pivot = new Vector2(0.5f, 0.5f);
            previewFrameRt.sizeDelta = new Vector2(210f, 280f);
            previewFrameRt.anchoredPosition = new Vector2(-18f, -26f);
            ClassicPixelUiTheme.ApplyShopPanel(previewFrame.GetComponent<Image>(), ClassicPixelUiTheme.ShopPanelAccent.Blue, false);

            GameObject previewGo = new GameObject("CharacterPreview", typeof(RectTransform), typeof(Image));
            previewGo.transform.SetParent(previewFrame.transform, false);
            Image preview = previewGo.GetComponent<Image>();
            preview.preserveAspect = true;
            preview.raycastTarget = false;
            RectTransform previewRt = previewGo.GetComponent<RectTransform>();
            previewRt.anchorMin = new Vector2(0.5f, 0.5f);
            previewRt.anchorMax = new Vector2(0.5f, 0.5f);
            previewRt.pivot = new Vector2(0.5f, 0.5f);
            previewRt.sizeDelta = new Vector2(180f, 240f);
            previewRt.anchoredPosition = Vector2.zero;

            GameObject infoPanel = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image));
            infoPanel.transform.SetParent(panelGo.transform, false);
            RectTransform infoRt = infoPanel.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(1f, 1f);
            infoRt.anchorMax = new Vector2(1f, 1f);
            infoRt.pivot = new Vector2(1f, 1f);
            infoRt.sizeDelta = new Vector2(248f, 292f);
            infoRt.anchoredPosition = new Vector2(-36f, -106f);
            ClassicPixelUiTheme.ApplyShopPanel(infoPanel.GetComponent<Image>(), ClassicPixelUiTheme.ShopPanelAccent.Blue, false);

            TextMeshProUGUI description = CreateText(infoPanel.transform, "JobDescriptionText", string.Empty, 18f, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            SetFixedRect(description.rectTransform, new Vector2(18f, -18f), new Vector2(212f, 132f), new Vector2(0f, 1f));

            TextMeshProUGUI stats = CreateText(infoPanel.transform, "JobStatsText", string.Empty, 18f, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            SetFixedRect(stats.rectTransform, new Vector2(18f, -166f), new Vector2(212f, 108f), new Vector2(0f, 1f));

            Button confirm = CreateButton(panelGo.transform, "YesButton", "선택", ClassicPixelUiTheme.ShopPanelAccent.Gold);
            SetButtonRect(confirm, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(132f, 58f), new Vector2(0f, 32f));

            jobChangeUI = panelGo.GetComponent<JobChangeUIController>();
            jobChangeUI.ConfigureRuntime(GetJobs(), jobButtons, preview, description, stats, confirm, null, panelRt, title);
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.fontStyle = style;
            label.textWrappingMode = TextWrappingModes.Normal;
            ClassicPixelUiTheme.ApplyText(label);
            return label;
        }

        private Button CreateButton(Transform parent, string name, string label, ClassicPixelUiTheme.ShopPanelAccent accent)
        {
            GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(parent, false);
            Button button = buttonGo.GetComponent<Button>();
            ClassicPixelUiTheme.ApplyShopButton(button, accent);

            TextMeshProUGUI text = CreateText(buttonGo.transform, "Label", label, 20f, TextAlignmentOptions.Center, FontStyles.Bold);
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            return button;
        }

        private static void SetOffsets(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void SetFixedRect(RectTransform rt, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
        }

        private static void SetButtonRect(Button button, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPosition)
        {
            RectTransform rt = button.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;
        }

        private void HandleStartDialogueEnded()
        {
            StopListeningDialogueEnd(HandleStartDialogueEnded);
            OpenJobSelection();
        }

        private void OpenJobSelection()
        {
            if (characterGo != null)
            {
                characterGo.SetActive(false);
            }

            jobChangeUI.OnJobConfirmed += HandleJobConfirmed;
            jobChangeUI.OnCancelled += HandleJobCancelled;
            jobChangeUI.OpenInitialSelection("직업 선택");
        }

        private void HandleJobConfirmed(JobClassInfo selectedJob)
        {
            jobChangeUI.OnJobConfirmed -= HandleJobConfirmed;
            jobChangeUI.OnCancelled -= HandleJobCancelled;

            NewGameStartContext.SetSelectedJob(selectedJob);

            SetCharacterVisible(true);
            PlayDialogue(afterJobSelectDialogue, HandleAfterJobDialogueEnded);
        }

        private void HandleJobCancelled()
        {
            jobChangeUI.OpenInitialSelection("직업 선택");
        }

        private void HandleAfterJobDialogueEnded()
        {
            StopListeningDialogueEnd(HandleAfterJobDialogueEnded);
            LoadAdventureScene();
        }

        private void PlayDialogue(DialogueData data, System.Action endedHandler)
        {
            if (dialogueManager == null)
            {
                EnsureDialogueManager();
            }

            if (dialogueManager == null || data == null)
            {
                endedHandler?.Invoke();
                return;
            }

            dialogueManager.OnDialogueEnded += endedHandler;
            dialogueManager.BeginDialogue(data);
        }

        private void StopListeningDialogueEnd(System.Action handler)
        {
            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueEnded -= handler;
            }
        }

        private void EnsureDialogueManager()
        {
            dialogueManager = DialogueManager.Instance;
            if (dialogueManager == null)
            {
                GameObject managerGo = new GameObject("StartGameDialogueManager");
                dialogueManager = managerGo.AddComponent<DialogueManager>();
            }

            if (dialogueView != null)
            {
                dialogueManager.SetDialogueView(dialogueView);
            }
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            EventSystem.current = eventSystemGo.GetComponent<EventSystem>();
        }

        private List<JobClassInfo> GetJobs()
        {
            if (jobs != null && jobs.Count > 0)
            {
                return jobs;
            }

            GameDatabase database = Resources.Load<GameDatabase>("GameDatabase");
            if (database != null && database.allJobs != null)
            {
                return database.allJobs;
            }

            return new List<JobClassInfo>();
        }

        private void SetCharacterVisible(bool visible)
        {
            if (characterGo == null)
            {
                return;
            }

            characterGo.SetActive(visible);
            characterGo.transform.DOKill();
            if (visible)
            {
                characterGo.transform.localScale = Vector3.one * 0.96f;
                characterGo.transform.DOScale(Vector3.one, 0.22f).SetEase(Ease.OutQuad);
            }
        }

        private void LoadAdventureScene()
        {
            if (GameDataManager.Instance != null && NewGameStartContext.TryConsumeSelectedJob(out JobClassInfo selectedJob))
            {
                GameDataManager.Instance.ResetForNewGame();
                GameDataManager.Instance.UpdateJob(selectedJob);
            }

            SceneManager.LoadScene(AdventureSceneName);
        }
    }
}

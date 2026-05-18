using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;
using CardAdventure.UI;

namespace CardAdventure.EditorTools
{
    public static class SetupLobbyScene
    {
        [MenuItem("CardAdventure/Setup/1. Setup Lobby Scene")]
        public static void BuildLobbyScene()
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;
            
            // EventSystem
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Title Text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "카드 어드벤처";
            titleText.fontSize = 120;
            titleText.alignment = TextAlignmentOptions.Center;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 300);
            titleRect.sizeDelta = new Vector2(1000, 200);

            // MainMenu Panel
            GameObject mainPanel = new GameObject("MainMenuPanel");
            mainPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
            mainRect.anchoredPosition = new Vector2(0, -100);
            
            VerticalLayoutGroup vlg = mainPanel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 20;
            vlg.childControlHeight = false;
            vlg.childControlWidth = false;

            Button newGameBtn = CreateButton(mainPanel.transform, "NewGameButton", "새 게임");
            Button continueBtn = CreateButton(mainPanel.transform, "ContinueButton", "이어하기");
            Button settingsBtn = CreateButton(mainPanel.transform, "SettingsButton", "설정");
            Button exitBtn = CreateButton(mainPanel.transform, "ExitButton", "나가기");

            // SaveSlot Panel
            GameObject savePanel = new GameObject("SaveSlotPanel");
            savePanel.transform.SetParent(canvasObj.transform, false);
            savePanel.SetActive(false);
            RectTransform saveRect = savePanel.AddComponent<RectTransform>();
            saveRect.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup svlg = savePanel.AddComponent<VerticalLayoutGroup>();
            svlg.childAlignment = TextAnchor.MiddleCenter;
            svlg.spacing = 30;
            svlg.childControlHeight = false;
            svlg.childControlWidth = false;

            SaveSlotView[] slots = new SaveSlotView[3];
            for (int i = 0; i < 3; i++)
            {
                slots[i] = CreateSaveSlot(savePanel.transform, i);
            }
            Button closeSaveBtn = CreateButton(savePanel.transform, "CloseSaveButton", "뒤로 가기");

            // Settings Panel
            GameObject settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(canvasObj.transform, false);
            settingsPanel.SetActive(false);
            VerticalLayoutGroup setVlg = settingsPanel.AddComponent<VerticalLayoutGroup>();
            setVlg.childAlignment = TextAnchor.MiddleCenter;
            setVlg.spacing = 40;

            Slider bgmSlider = CreateSlider(settingsPanel.transform, "BGM Volume");
            Slider sfxSlider = CreateSlider(settingsPanel.transform, "SFX Volume");
            Button closeSettingsBtn = CreateButton(settingsPanel.transform, "CloseSettingsButton", "닫기");

            SettingsUIController setCtrl = settingsPanel.AddComponent<SettingsUIController>();
            setCtrl.settingsPanel = settingsPanel;
            setCtrl.bgmSlider = bgmSlider;
            setCtrl.sfxSlider = sfxSlider;
            setCtrl.closeButton = closeSettingsBtn;

            // Controller
            LobbyUIController lobbyCtrl = canvasObj.AddComponent<LobbyUIController>();
            lobbyCtrl.mainMenuPanel = mainPanel;
            lobbyCtrl.newGameButton = newGameBtn;
            lobbyCtrl.continueButton = continueBtn;
            lobbyCtrl.settingsButton = settingsBtn;
            lobbyCtrl.exitButton = exitBtn;
            
            lobbyCtrl.saveSlotPanel = savePanel;
            lobbyCtrl.saveSlots = new System.Collections.Generic.List<SaveSlotView>(slots);
            lobbyCtrl.closeSaveSlotButton = closeSaveBtn;

            lobbyCtrl.settingsController = setCtrl;

            // Save Scene
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            EditorSceneManager.SaveScene(newScene, "Assets/Scenes/LobbyScene.unity");
            
            Debug.Log("[Setup] LobbyScene 생성 완료.");
        }

        [MenuItem("CardAdventure/Setup/2. Setup InGame Menu")]
        public static void BuildInGameMenu()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "AdventureScene")
            {
                Debug.LogError("AdventureScene을 연 상태에서 실행해주세요.");
                return;
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas를 찾을 수 없습니다.");
                return;
            }

            GameObject menuContainer = new GameObject("InGameMenuCanvas");
            menuContainer.transform.SetParent(canvas.transform, false);
            RectTransform containerRect = menuContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;

            GameObject menuPanel = new GameObject("MenuPanel");
            menuPanel.transform.SetParent(menuContainer.transform, false);
            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 0);
            panelRect.anchoredPosition = new Vector2(-400, 0);

            Image panelImg = menuPanel.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            CanvasGroup cg = menuPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            VerticalLayoutGroup vlg = menuPanel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 30;

            Button saveBtn = CreateButton(menuPanel.transform, "SaveButton", "저장하기");
            Button settingsBtn = CreateButton(menuPanel.transform, "SettingsButton", "설정");
            Button myCardsBtn = CreateButton(menuPanel.transform, "MyCardsButton", "내 카드");
            Button exitBtn = CreateButton(menuPanel.transform, "ExitButton", "로비로 나가기");
            Button closeBtn = CreateButton(menuPanel.transform, "CloseMenuButton", "메뉴 닫기");

            // Exit Warning
            GameObject warningPanel = new GameObject("ExitWarningPanel");
            warningPanel.transform.SetParent(menuContainer.transform, false);
            warningPanel.SetActive(false);
            RectTransform warnRect = warningPanel.AddComponent<RectTransform>();
            warnRect.anchoredPosition = Vector2.zero;
            warnRect.sizeDelta = new Vector2(600, 300);
            Image warnImg = warningPanel.AddComponent<Image>();
            warnImg.color = new Color(0.2f, 0.1f, 0.1f, 0.98f);

            GameObject warnTextObj = new GameObject("WarningText");
            warnTextObj.transform.SetParent(warningPanel.transform, false);
            TextMeshProUGUI warnText = warnTextObj.AddComponent<TextMeshProUGUI>();
            warnText.text = "저장하지 않은 진행 상황은 유실됩니다.\n정말 나가시겠습니까?";
            warnText.alignment = TextAlignmentOptions.Center;
            warnTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);

            Button confirmBtn = CreateButton(warningPanel.transform, "ConfirmExitButton", "확인");
            confirmBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120, -50);
            Button cancelBtn = CreateButton(warningPanel.transform, "CancelExitButton", "취소");
            cancelBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, -50);

            // Save Slot Panel (Optional for InGame)
            GameObject savePanel = new GameObject("SaveSlotPanel");
            savePanel.transform.SetParent(menuContainer.transform, false);
            savePanel.SetActive(false);
            VerticalLayoutGroup svlg = savePanel.AddComponent<VerticalLayoutGroup>();
            svlg.childAlignment = TextAnchor.MiddleCenter;
            svlg.spacing = 30;

            SaveSlotView[] slots = new SaveSlotView[3];
            for (int i = 0; i < 3; i++)
            {
                slots[i] = CreateSaveSlot(savePanel.transform, i);
            }
            Button closeSaveBtn = CreateButton(savePanel.transform, "CloseSaveButton", "닫기");

            // Settings Panel
            GameObject settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(menuContainer.transform, false);
            settingsPanel.SetActive(false);
            VerticalLayoutGroup setVlg = settingsPanel.AddComponent<VerticalLayoutGroup>();
            setVlg.childAlignment = TextAnchor.MiddleCenter;
            setVlg.spacing = 40;

            Slider bgmSlider = CreateSlider(settingsPanel.transform, "BGM Volume");
            Slider sfxSlider = CreateSlider(settingsPanel.transform, "SFX Volume");
            Button closeSettingsBtn = CreateButton(settingsPanel.transform, "CloseSettingsButton", "닫기");

            SettingsUIController setCtrl = settingsPanel.AddComponent<SettingsUIController>();
            setCtrl.settingsPanel = settingsPanel;
            setCtrl.bgmSlider = bgmSlider;
            setCtrl.sfxSlider = sfxSlider;
            setCtrl.closeButton = closeSettingsBtn;

            InGameMenuController ctrl = menuContainer.AddComponent<InGameMenuController>();
            ctrl.menuPanelRect = panelRect;
            ctrl.menuCanvasGroup = cg;
            ctrl.hiddenPosX = -400f;
            ctrl.shownPosX = 0f;
            ctrl.slideDuration = 0.3f;
            
            ctrl.saveButton = saveBtn;
            ctrl.settingsButton = settingsBtn;
            ctrl.myCardsButton = myCardsBtn;
            ctrl.exitButton = exitBtn;
            ctrl.closeMenuButton = closeBtn;

            ctrl.exitWarningPanel = warningPanel;
            ctrl.confirmExitButton = confirmBtn;
            ctrl.cancelExitButton = cancelBtn;

            ctrl.saveSlotPanel = savePanel;
            ctrl.saveSlots = slots;
            ctrl.closeSaveSlotButton = closeSaveBtn;

            ctrl.settingsController = setCtrl;

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Setup] InGameMenu 추가 완료.");
        }

        private static Button CreateButton(Transform parent, string name, string text)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f);
            Button btn = btnObj.AddComponent<Button>();
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 80);

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            return btn;
        }

        private static SaveSlotView CreateSaveSlot(Transform parent, int index)
        {
            GameObject slotObj = new GameObject($"Slot_{index}");
            slotObj.transform.SetParent(parent, false);
            Image img = slotObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f);
            Button btn = slotObj.AddComponent<Button>();
            RectTransform rt = slotObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 150);

            GameObject numObj = new GameObject("NumberText");
            numObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI numTmp = numObj.AddComponent<TextMeshProUGUI>();
            numTmp.fontSize = 24;
            numTmp.alignment = TextAlignmentOptions.TopLeft;
            numObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, -10);

            GameObject dateObj = new GameObject("DateText");
            dateObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI dateTmp = dateObj.AddComponent<TextMeshProUGUI>();
            dateTmp.fontSize = 24;
            dateTmp.alignment = TextAlignmentOptions.TopRight;
            dateObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-10, -10);

            GameObject infoObj = new GameObject("InfoText");
            infoObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI infoTmp = infoObj.AddComponent<TextMeshProUGUI>();
            infoTmp.fontSize = 36;
            infoTmp.alignment = TextAlignmentOptions.Center;
            infoObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

            SaveSlotView view = slotObj.AddComponent<SaveSlotView>();
            view.slotNumberText = numTmp;
            view.dateText = dateTmp;
            view.infoText = infoTmp;
            view.slotButton = btn;

            return view;
        }

        private static Slider CreateSlider(Transform parent, string labelText)
        {
            GameObject container = new GameObject(labelText + "_Container");
            container.transform.SetParent(parent, false);
            RectTransform rt = container.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(500, 80);

            GameObject label = new GameObject("Label");
            label.transform.SetParent(container.transform, false);
            TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 30;
            tmp.alignment = TextAlignmentOptions.Left;
            label.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 0);

            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(container.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();
            RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
            sliderRt.anchoredPosition = new Vector2(100, 0);
            sliderRt.sizeDelta = new Vector2(300, 40);

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = Color.gray;
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.25f);
            bgRt.anchorMax = new Vector2(1, 0.75f);
            bgRt.sizeDelta = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.25f);
            faRt.anchorMax = new Vector2(1, 0.75f);
            faRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = Color.green;
            RectTransform fRt = fill.GetComponent<RectTransform>();
            fRt.sizeDelta = Vector2.zero;

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform haRt = handleArea.AddComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.sizeDelta = Vector2.zero;

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            RectTransform hRt = handle.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(40, 0);

            slider.fillRect = fRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.75f;

            return slider;
        }
    }
}

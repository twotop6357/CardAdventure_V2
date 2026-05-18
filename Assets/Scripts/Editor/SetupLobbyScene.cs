using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;
using CardAdventure.UI;
using CardAdventure.Editor;

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
            saveRect.anchorMin = Vector2.zero;
            saveRect.anchorMax = Vector2.one;
            saveRect.sizeDelta = Vector2.zero;
            saveRect.anchoredPosition = Vector2.zero;

            Image savePanelBg = savePanel.AddComponent<Image>();
            savePanelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

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
            RectTransform setRect = settingsPanel.AddComponent<RectTransform>();
            setRect.sizeDelta = new Vector2(800, 500);
            Image setImg = settingsPanel.AddComponent<Image>();
            setImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            
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
            
            ShopStyleUiTool.ApplyToOpenScene();

            Debug.Log("[Setup] LobbyScene 생성 및 상점 스타일 적용 완료.");
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
            Button itemMenuBtn = CreateButton(menuPanel.transform, "ItemMenuButton", "아이템 사용"); // 아이템 사용 메뉴 버튼 추가
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
            warnText.text = "저장하지 않은 데이터는 유실됩니다.\n정말 나가시겠습니까?";
            warnText.alignment = TextAlignmentOptions.Center;
            warnText.fontSize = 28;
            warnText.color = Color.white;
            RectTransform warnTextRt = warnTextObj.GetComponent<RectTransform>();
            warnTextRt.sizeDelta = new Vector2(550, 120); // 넉넉한 텍스트 가로세로 영역 지정으로 완성되지 않거나 잘리는 문제 해결!
            warnTextRt.anchoredPosition = new Vector2(0, 50);

            Button confirmBtn = CreateButton(warningPanel.transform, "ConfirmExitButton", "확인");
            RectTransform confirmRt = confirmBtn.GetComponent<RectTransform>();
            confirmRt.sizeDelta = new Vector2(180, 60); // 아담하고 세련된 크기 조절로 겹침 현상 원천 방지!
            confirmRt.anchoredPosition = new Vector2(-120, -60);
            TextMeshProUGUI confirmText = confirmBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (confirmText != null) confirmText.fontSize = 28;

            Button cancelBtn = CreateButton(warningPanel.transform, "CancelExitButton", "취소");
            RectTransform cancelRt = cancelBtn.GetComponent<RectTransform>();
            cancelRt.sizeDelta = new Vector2(180, 60); // 아담하고 세련된 크기 조절로 겹침 현상 원천 방지!
            cancelRt.anchoredPosition = new Vector2(120, -60);
            TextMeshProUGUI cancelText = cancelBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (cancelText != null) cancelText.fontSize = 28;

            // Save Slot Panel (Optional for InGame)
            GameObject savePanel = new GameObject("SaveSlotPanel");
            savePanel.transform.SetParent(menuContainer.transform, false);
            savePanel.SetActive(false);
            RectTransform saveRect = savePanel.AddComponent<RectTransform>();
            saveRect.anchorMin = Vector2.zero;
            saveRect.anchorMax = Vector2.one;
            saveRect.sizeDelta = Vector2.zero;
            saveRect.anchoredPosition = Vector2.zero;

            Image savePanelBg = savePanel.AddComponent<Image>();
            savePanelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

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
            Button closeSaveBtn = CreateButton(savePanel.transform, "CloseSaveButton", "닫기");

            // Settings Panel
            GameObject settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(menuContainer.transform, false);
            settingsPanel.SetActive(false);
            RectTransform setRect = settingsPanel.AddComponent<RectTransform>();
            setRect.sizeDelta = new Vector2(800, 500);
            Image setImg = settingsPanel.AddComponent<Image>();
            setImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

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

            // 1. ItemUse Panel (아이템 사용 전용 서브 패널) 생성
            GameObject itemUsePanel = new GameObject("ItemUsePanel");
            itemUsePanel.transform.SetParent(menuContainer.transform, false);
            itemUsePanel.SetActive(false);
            RectTransform itemUseRect = itemUsePanel.AddComponent<RectTransform>();
            itemUseRect.sizeDelta = new Vector2(800, 520); // 크기 설정
            Image itemUseImg = itemUsePanel.AddComponent<Image>();
            itemUseImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // 타이틀 텍스트 추가
            GameObject titleObj = new GameObject("ItemUseTitle");
            titleObj.transform.SetParent(itemUsePanel.transform, false);
            TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            titleTxt.text = "아이템 사용 & 캐릭터 정보";
            titleTxt.fontSize = 32;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.sizeDelta = new Vector2(600, 50);
            titleRt.anchoredPosition = new Vector2(0, -25);

            // 닫기 버튼 추가
            Button closeItemUseBtn = CreateButton(itemUsePanel.transform, "CloseItemUseButton", "닫기");
            RectTransform closeItemUseRt = closeItemUseBtn.GetComponent<RectTransform>();
            closeItemUseRt.anchorMin = new Vector2(0.5f, 0f);
            closeItemUseRt.anchorMax = new Vector2(0.5f, 0f);
            closeItemUseRt.pivot = new Vector2(0.5f, 0f);
            closeItemUseRt.sizeDelta = new Vector2(200, 60);
            closeItemUseRt.anchoredPosition = new Vector2(0, 30);

            // 내부 상태 컨텐츠 배치용 statusPanel 생성
            GameObject statusPanel = new GameObject("PlayerStatusPanel");
            statusPanel.transform.SetParent(itemUsePanel.transform, false);
            RectTransform statusRt = statusPanel.AddComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.5f, 0.5f);
            statusRt.anchorMax = new Vector2(0.5f, 0.5f);
            statusRt.pivot = new Vector2(0.5f, 0.5f);
            statusRt.sizeDelta = new Vector2(700, 300); // 넉넉한 내부 컨테이너
            statusRt.anchoredPosition = new Vector2(0, 15);

            Image statusImg = statusPanel.AddComponent<Image>();
            statusImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // 2. 직업 얼굴 초상화 Image
            GameObject faceObj = new GameObject("PlayerFaceImage");
            faceObj.transform.SetParent(statusPanel.transform, false);
            Image faceImg = faceObj.AddComponent<Image>();
            faceImg.preserveAspect = true;
            RectTransform faceRt = faceObj.GetComponent<RectTransform>();
            faceRt.anchorMin = new Vector2(0, 0.5f);
            faceRt.anchorMax = new Vector2(0, 0.5f);
            faceRt.pivot = new Vector2(0, 0.5f);
            faceRt.sizeDelta = new Vector2(100, 100);
            faceRt.anchoredPosition = new Vector2(40, 55); // 좌측 상단

            // 3. 직업 이름 텍스트
            GameObject jobTextObj = new GameObject("PlayerJobText");
            jobTextObj.transform.SetParent(statusPanel.transform, false);
            TextMeshProUGUI jobTmp = jobTextObj.AddComponent<TextMeshProUGUI>();
            jobTmp.text = "직업: 초보자";
            jobTmp.fontSize = 28;
            jobTmp.alignment = TextAlignmentOptions.Left;
            jobTmp.color = Color.white;
            RectTransform jobRt = jobTextObj.GetComponent<RectTransform>();
            jobRt.anchorMin = new Vector2(0, 0.5f);
            jobRt.anchorMax = new Vector2(0, 0.5f);
            jobRt.pivot = new Vector2(0, 0.5f);
            jobRt.sizeDelta = new Vector2(250, 40);
            jobRt.anchoredPosition = new Vector2(165, 80); // 초상화 우측 위

            // 4. HP 텍스트
            GameObject hpTextObj = new GameObject("PlayerHPText");
            hpTextObj.transform.SetParent(statusPanel.transform, false);
            TextMeshProUGUI hpTmp = hpTextObj.AddComponent<TextMeshProUGUI>();
            hpTmp.text = "HP: 50/50";
            hpTmp.fontSize = 28;
            hpTmp.alignment = TextAlignmentOptions.Left;
            hpTmp.color = Color.white;
            RectTransform hpTxtRt = hpTextObj.GetComponent<RectTransform>();
            hpTxtRt.anchorMin = new Vector2(0, 0.5f);
            hpTxtRt.anchorMax = new Vector2(0, 0.5f);
            hpTxtRt.pivot = new Vector2(0, 0.5f);
            hpTxtRt.sizeDelta = new Vector2(250, 40);
            hpTxtRt.anchoredPosition = new Vector2(165, 35); // 초상화 우측 아래

            // 5. HP 슬라이더 바 (단순 출력용)
            GameObject hpSliderObj = new GameObject("PlayerHPSlider");
            hpSliderObj.transform.SetParent(statusPanel.transform, false);
            Slider hpSlider = hpSliderObj.AddComponent<Slider>();
            RectTransform sliderRt = hpSliderObj.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRt.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRt.pivot = new Vector2(0.5f, 0.5f);
            sliderRt.anchoredPosition = new Vector2(0, -20); // 중간 영역
            sliderRt.sizeDelta = new Vector2(620, 24);

            // HP 슬라이더 내부 Background
            GameObject hpBg = new GameObject("Background");
            hpBg.transform.SetParent(hpSliderObj.transform, false);
            Image hpBgImg = hpBg.AddComponent<Image>();
            hpBgImg.color = new Color(0.2f, 0.2f, 0.2f);
            RectTransform hpBgRt = hpBg.GetComponent<RectTransform>();
            hpBgRt.anchorMin = Vector2.zero;
            hpBgRt.anchorMax = Vector2.one;
            hpBgRt.sizeDelta = Vector2.zero;

            // HP 슬라이더 내부 Fill Area
            GameObject hpFillArea = new GameObject("Fill Area");
            hpFillArea.transform.SetParent(hpSliderObj.transform, false);
            RectTransform hpFaRt = hpFillArea.AddComponent<RectTransform>();
            hpFaRt.anchorMin = Vector2.zero;
            hpFaRt.anchorMax = Vector2.one;
            hpFaRt.sizeDelta = Vector2.zero;

            // HP 슬라이더 내부 Fill
            GameObject hpFill = new GameObject("Fill");
            hpFill.transform.SetParent(hpFillArea.transform, false);
            Image hpFillImg = hpFill.AddComponent<Image>();
            hpFillImg.color = new Color(0.8f, 0.2f, 0.2f); // 붉은 계열의 체력 바
            RectTransform hpFRt = hpFill.GetComponent<RectTransform>();
            hpFRt.sizeDelta = Vector2.zero;

            hpSlider.fillRect = hpFRt;
            hpSlider.direction = Slider.Direction.LeftToRight;
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 50f;
            hpSlider.value = 50f;

            // 6. 포션 아이콘 Image
            GameObject potIconObj = new GameObject("PotionIcon");
            potIconObj.transform.SetParent(statusPanel.transform, false);
            Image potIconImg = potIconObj.AddComponent<Image>();
            potIconImg.preserveAspect = true;
            RectTransform potIconRt = potIconObj.GetComponent<RectTransform>();
            potIconRt.anchorMin = new Vector2(0, 0.5f);
            potIconRt.anchorMax = new Vector2(0, 0.5f);
            potIconRt.pivot = new Vector2(0, 0.5f);
            potIconRt.sizeDelta = new Vector2(50, 50);
            potIconRt.anchoredPosition = new Vector2(40, -85); // 좌측 하단

            // 7. 보유 포션 개수 텍스트
            GameObject potCountTextObj = new GameObject("PotionCountText");
            potCountTextObj.transform.SetParent(statusPanel.transform, false);
            TextMeshProUGUI potCountTmp = potCountTextObj.AddComponent<TextMeshProUGUI>();
            potCountTmp.text = "보유 포션: 0개";
            potCountTmp.fontSize = 24;
            potCountTmp.alignment = TextAlignmentOptions.Left;
            potCountTmp.color = Color.white;
            RectTransform potCountRt = potCountTextObj.GetComponent<RectTransform>();
            potCountRt.anchorMin = new Vector2(0, 0.5f);
            potCountRt.anchorMax = new Vector2(0, 0.5f);
            potCountRt.pivot = new Vector2(0, 0.5f);
            potCountRt.sizeDelta = new Vector2(250, 35);
            potCountRt.anchoredPosition = new Vector2(105, -85); // 포션 아이콘 우측

            // 8. 포션 사용 Button
            Button usePotionBtn = CreateButton(statusPanel.transform, "UsePotionButton", "사용");
            RectTransform usePotionRt = usePotionBtn.GetComponent<RectTransform>();
            usePotionRt.anchorMin = new Vector2(1, 0.5f);
            usePotionRt.anchorMax = new Vector2(1, 0.5f);
            usePotionRt.pivot = new Vector2(1, 0.5f);
            usePotionRt.sizeDelta = new Vector2(130, 50);
            usePotionRt.anchoredPosition = new Vector2(-40, -85); // 우측 하단
            
            // 기존 버튼 텍스트의 폰트 크기 조절
            TextMeshProUGUI usePotionTmp = usePotionBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (usePotionTmp != null) usePotionTmp.fontSize = 28;

            // InGameMenuController 세팅
            InGameMenuController ctrl = menuContainer.AddComponent<InGameMenuController>();
            ctrl.menuPanelRect = panelRect;
            ctrl.menuCanvasGroup = cg;
            ctrl.hiddenPosX = -400f;
            ctrl.shownPosX = 0f;
            ctrl.slideDuration = 0.3f;
            
            ctrl.saveButton = saveBtn;
            ctrl.settingsButton = settingsBtn;
            ctrl.myCardsButton = myCardsBtn;
            ctrl.itemMenuButton = itemMenuBtn; // 세팅 완료
            ctrl.exitButton = exitBtn;
            ctrl.closeMenuButton = closeBtn;

            ctrl.exitWarningPanel = warningPanel;
            ctrl.confirmExitButton = confirmBtn;
            ctrl.cancelExitButton = cancelBtn;

            ctrl.saveSlotPanel = savePanel;
            ctrl.saveSlots = slots;
            ctrl.closeSaveSlotButton = closeSaveBtn;

            ctrl.itemUsePanel = itemUsePanel; // 세팅 완료
            ctrl.closeItemUseButton = closeItemUseBtn; // 세팅 완료
            ctrl.settingsController = setCtrl;

            // 포션 및 페이스 에셋 주입
            Sprite potionSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/Sprites/Items/Potion.png");
            Sprite warriorSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/Sprites/FaceImage/Warrior_FaceImage.png");
            Sprite magicianSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/Sprites/FaceImage/Magician_FaceImage.png");
            Sprite rogueSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/Sprites/FaceImage/Rogue_FaceImage.png");

            potIconImg.sprite = potionSpr; // 포션 아이콘에도 디폴트 이미지 세팅
            faceImg.sprite = warriorSpr;   // 디폴트 얼굴 세팅

            ctrl.potionSprite = potionSpr;
            ctrl.warriorFaceSprite = warriorSpr;
            ctrl.magicianFaceSprite = magicianSpr;
            ctrl.rogueFaceSprite = rogueSpr;

            ctrl.playerFaceImage = faceImg;
            ctrl.playerJobText = jobTmp;
            ctrl.hpText = hpTmp;
            ctrl.hpSlider = hpSlider;
            ctrl.potionCountText = potCountTmp;
            ctrl.usePotionButton = usePotionBtn;

            // 씬 상에 존재하는 ShopUIController를 찾아서 potionSprite 바인딩
            ShopUIController shopCtrl = Object.FindFirstObjectByType<ShopUIController>();
            if (shopCtrl != null)
            {
                shopCtrl.potionSprite = potionSpr;
                EditorUtility.SetDirty(shopCtrl);
                Debug.Log("[Setup] ShopUIController.potionSprite 자동 주입 완료.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            
            ShopStyleUiTool.ApplyToOpenScene();

            Debug.Log("[Setup] InGameMenu 추가 및 상점 스타일 적용 완료.");
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
            numTmp.fontSize = 28;
            numTmp.alignment = TextAlignmentOptions.TopLeft;
            RectTransform numRt = numObj.GetComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0, 1);
            numRt.anchorMax = new Vector2(0, 1);
            numRt.pivot = new Vector2(0, 1);
            numRt.sizeDelta = new Vector2(200, 40);
            numRt.anchoredPosition = new Vector2(20, -10);

            GameObject dateObj = new GameObject("DateText");
            dateObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI dateTmp = dateObj.AddComponent<TextMeshProUGUI>();
            dateTmp.fontSize = 24;
            dateTmp.alignment = TextAlignmentOptions.TopRight;
            RectTransform dateRt = dateObj.GetComponent<RectTransform>();
            dateRt.anchorMin = new Vector2(1, 1);
            dateRt.anchorMax = new Vector2(1, 1);
            dateRt.pivot = new Vector2(1, 1);
            dateRt.sizeDelta = new Vector2(400, 40);
            dateRt.anchoredPosition = new Vector2(-20, -10);

            GameObject infoObj = new GameObject("InfoText");
            infoObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI infoTmp = infoObj.AddComponent<TextMeshProUGUI>();
            infoTmp.fontSize = 36;
            infoTmp.alignment = TextAlignmentOptions.Center;
            RectTransform infoRt = infoObj.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0.5f, 0.5f);
            infoRt.anchorMax = new Vector2(0.5f, 0.5f);
            infoRt.pivot = new Vector2(0.5f, 0.5f);
            infoRt.sizeDelta = new Vector2(560, 80);
            infoRt.anchoredPosition = new Vector2(0, -15);

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

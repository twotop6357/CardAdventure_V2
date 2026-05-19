using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

namespace CardAdventure.UI
{
    public class LobbyUIController : MonoBehaviour
    {
        [Header("Main Menu")]
        public GameObject mainMenuPanel;
        public Button newGameButton;
        public Button continueButton;
        public Button settingsButton;
        public Button exitButton;

        [Header("Save Slots")]
        public GameObject saveSlotPanel;
        public List<SaveSlotView> saveSlots;
        public Button closeSaveSlotButton;

        [Header("Settings")]
        public SettingsUIController settingsController;

        private bool isNewGameMode = false;

        private void Start()
        {
            SetupMainMenu();
            SetupSaveSlots();
            ShowMainMenu();
        }

        private void SetupMainMenu()
        {
            newGameButton.onClick.AddListener(OnNewGameClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            exitButton.onClick.AddListener(OnExitClicked);

            // 저장된 데이터가 하나도 없으면 이어하기 버튼 비활성화
            continueButton.interactable = SaveManager.HasAnySaveData();
        }

        private void SetupSaveSlots()
        {
            closeSaveSlotButton.onClick.AddListener(ShowMainMenu);

            for (int i = 0; i < saveSlots.Count; i++)
            {
                int slotIndex = i;
                SaveData data = SaveManager.LoadSaveData(slotIndex);
                saveSlots[i].Bind(slotIndex, data, OnSaveSlotClicked);
            }
        }

        private void ShowMainMenu()
        {
            mainMenuPanel.SetActive(true);
            saveSlotPanel.SetActive(false);
            if (settingsController != null) settingsController.Hide();
            
            continueButton.interactable = SaveManager.HasAnySaveData();
        }

        private void OnNewGameClicked()
        {
            isNewGameMode = true;
            mainMenuPanel.SetActive(false);
            saveSlotPanel.SetActive(true);

            // 데이터 갱신
            for (int i = 0; i < saveSlots.Count; i++)
            {
                SaveData data = SaveManager.LoadSaveData(i);
                saveSlots[i].Bind(i, data, OnSaveSlotClicked);
            }
        }

        private void OnContinueClicked()
        {
            isNewGameMode = false;
            mainMenuPanel.SetActive(false);
            saveSlotPanel.SetActive(true);
            
            // 데이터 갱신
            for (int i = 0; i < saveSlots.Count; i++)
            {
                SaveData data = SaveManager.LoadSaveData(i);
                saveSlots[i].Bind(i, data, OnSaveSlotClicked);
            }
        }

        private void OnSaveSlotClicked(int slotIndex)
        {
            if (isNewGameMode)
            {
                SaveData data = SaveManager.LoadSaveData(slotIndex);
                if (data != null)
                {
                    ShowOverwriteWarningPopup(slotIndex);
                }
                else
                {
                    StartNewGame(slotIndex);
                }
            }
            else
            {
                SaveData data = SaveManager.LoadSaveData(slotIndex);
                if (data == null)
                {
                    ShowEmptySlotPopup(slotIndex);
                    return;
                }

                // GameDatabase를 통해 데이터 복원
                SaveManager.SetCurrentSlotIndex(slotIndex);

                GameDatabase db = Resources.Load<GameDatabase>("GameDatabase");
                if (db == null)
                {
                    Debug.LogError("[Lobby] GameDatabase를 Resources에서 찾을 수 없습니다.");
                    return;
                }

                if (GameDataManager.Instance != null)
                {
                    GameDataManager.Instance.LoadFromSaveData(data, db);
                }

                SceneManager.LoadScene(data.currentSceneName);
            }
        }

        private void StartNewGame(int slotIndex)
        {
            SaveManager.SetCurrentSlotIndex(slotIndex);

            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.ResetForNewGame();
            }
            SceneManager.LoadScene("AdventureScene");
        }

        private void ShowOverwriteWarningPopup(int slotIndex)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) return;

            // 딤(Dim) 배경 생성
            GameObject dimGo = new GameObject("OverwriteWarningDim", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(parentCanvas.transform, false);
            Image dimImg = dimGo.GetComponent<Image>();
            dimImg.color = ClassicPixelUiTheme.DimBlack;
            
            RectTransform dimRt = dimGo.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;

            // 팝업 패널 생성
            GameObject panelGo = new GameObject("PopupPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(dimGo.transform, false);
            Image panelImg = panelGo.GetComponent<Image>();
            ClassicPixelUiTheme.ApplyShopPanel(panelImg, ClassicPixelUiTheme.ShopPanelAccent.Gold, true);

            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(480f, 260f);
            panelRt.anchoredPosition = Vector2.zero;

            // 경고 타이틀
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(panelGo.transform, false);
            TextMeshProUGUI titleTxt = titleGo.GetComponent<TextMeshProUGUI>();
            titleTxt.text = "경 고";
            titleTxt.fontSize = 28;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = ClassicPixelUiTheme.Danger;
            ClassicPixelUiTheme.ApplyText(titleTxt);
            titleTxt.fontStyle = FontStyles.Bold;

            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.offsetMin = new Vector2(20f, -60f);
            titleRt.offsetMax = new Vector2(-20f, -10f);

            // 경고 내용
            GameObject messageGo = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            messageGo.transform.SetParent(panelGo.transform, false);
            TextMeshProUGUI messageTxt = messageGo.GetComponent<TextMeshProUGUI>();
            messageTxt.text = $"슬롯 {slotIndex + 1}에 기존 데이터가 존재합니다.\n덮어쓰시겠습니까?\n<color=red>(이전 데이터는 완전히 삭제됩니다)</color>";
            messageTxt.fontSize = 18;
            messageTxt.alignment = TextAlignmentOptions.Center;
            ClassicPixelUiTheme.ApplyText(messageTxt);

            RectTransform messageRt = messageGo.GetComponent<RectTransform>();
            messageRt.anchorMin = new Vector2(0f, 0.5f);
            messageRt.anchorMax = new Vector2(1f, 0.5f);
            messageRt.pivot = new Vector2(0.5f, 0.5f);
            messageRt.offsetMin = new Vector2(20f, -40f);
            messageRt.offsetMax = new Vector2(-20f, 40f);

            // "예 (덮어쓰기)" 버튼
            Button yesBtn = MakePopupButton(panelGo.transform, "YesButton", "덮어쓰기", ClassicPixelUiTheme.ShopPanelAccent.Gold);
            RectTransform yesRt = yesBtn.GetComponent<RectTransform>();
            yesRt.anchorMin = new Vector2(0f, 0f);
            yesRt.anchorMax = new Vector2(0.5f, 0f);
            yesRt.pivot = new Vector2(0f, 0f);
            yesRt.offsetMin = new Vector2(30f, 25f);
            yesRt.offsetMax = new Vector2(-15f, 75f);

            yesBtn.onClick.AddListener(() => {
                Destroy(dimGo);
                StartNewGame(slotIndex);
            });

            // "아니오 (취소)" 버튼
            Button noBtn = MakePopupButton(panelGo.transform, "NoButton", "취소", ClassicPixelUiTheme.ShopPanelAccent.Blue);
            RectTransform noRt = noBtn.GetComponent<RectTransform>();
            noRt.anchorMin = new Vector2(0.5f, 0f);
            noRt.anchorMax = new Vector2(1f, 0f);
            noRt.pivot = new Vector2(1f, 0f);
            noRt.offsetMin = new Vector2(15f, 25f);
            noRt.offsetMax = new Vector2(-30f, 75f);

            noBtn.onClick.AddListener(() => {
                Destroy(dimGo);
            });

            // 팝업 스케일 애니메이션 (DOTween)
            panelGo.transform.localScale = Vector3.one * 0.8f;
            panelGo.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        }

        private void ShowEmptySlotPopup(int slotIndex)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null) return;

            // 딤(Dim) 배경 생성
            GameObject dimGo = new GameObject("EmptySlotDim", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(parentCanvas.transform, false);
            Image dimImg = dimGo.GetComponent<Image>();
            dimImg.color = ClassicPixelUiTheme.DimBlack;

            RectTransform dimRt = dimGo.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;

            // 팝업 패널 생성
            GameObject panelGo = new GameObject("PopupPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(dimGo.transform, false);
            Image panelImg = panelGo.GetComponent<Image>();
            ClassicPixelUiTheme.ApplyShopPanel(panelImg, ClassicPixelUiTheme.ShopPanelAccent.Gold, true);

            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(480f, 260f);
            panelRt.anchoredPosition = Vector2.zero;

            // 알림 타이틀
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(panelGo.transform, false);
            TextMeshProUGUI titleTxt = titleGo.GetComponent<TextMeshProUGUI>();
            titleTxt.text = "알 림";
            titleTxt.fontSize = 28;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = ClassicPixelUiTheme.Gold;
            ClassicPixelUiTheme.ApplyText(titleTxt);
            titleTxt.fontStyle = FontStyles.Bold;

            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.offsetMin = new Vector2(20f, -60f);
            titleRt.offsetMax = new Vector2(-20f, -10f);

            // 내용
            GameObject messageGo = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            messageGo.transform.SetParent(panelGo.transform, false);
            TextMeshProUGUI messageTxt = messageGo.GetComponent<TextMeshProUGUI>();
            messageTxt.text = $"슬롯 {slotIndex + 1}은 비어 있는 슬롯입니다.\n새로운 모험을 시작하시겠습니까?";
            messageTxt.fontSize = 18;
            messageTxt.alignment = TextAlignmentOptions.Center;
            ClassicPixelUiTheme.ApplyText(messageTxt);

            RectTransform messageRt = messageGo.GetComponent<RectTransform>();
            messageRt.anchorMin = new Vector2(0f, 0.5f);
            messageRt.anchorMax = new Vector2(1f, 0.5f);
            messageRt.pivot = new Vector2(0.5f, 0.5f);
            messageRt.offsetMin = new Vector2(20f, -40f);
            messageRt.offsetMax = new Vector2(-20f, 40f);

            // "새 게임 시작" 버튼
            Button yesBtn = MakePopupButton(panelGo.transform, "YesButton", "시작하기", ClassicPixelUiTheme.ShopPanelAccent.Gold);
            RectTransform yesRt = yesBtn.GetComponent<RectTransform>();
            yesRt.anchorMin = new Vector2(0f, 0f);
            yesRt.anchorMax = new Vector2(0.5f, 0f);
            yesRt.pivot = new Vector2(0f, 0f);
            yesRt.offsetMin = new Vector2(30f, 25f);
            yesRt.offsetMax = new Vector2(-15f, 75f);

            yesBtn.onClick.AddListener(() => {
                Destroy(dimGo);
                StartNewGame(slotIndex);
            });

            // "취소" 버튼
            Button noBtn = MakePopupButton(panelGo.transform, "NoButton", "취소", ClassicPixelUiTheme.ShopPanelAccent.Blue);
            RectTransform noRt = noBtn.GetComponent<RectTransform>();
            noRt.anchorMin = new Vector2(0.5f, 0f);
            noRt.anchorMax = new Vector2(1f, 0f);
            noRt.pivot = new Vector2(1f, 0f);
            noRt.offsetMin = new Vector2(15f, 25f);
            noRt.offsetMax = new Vector2(-30f, 75f);

            noBtn.onClick.AddListener(() => {
                Destroy(dimGo);
            });

            // 팝업 스케일 애니메이션 (DOTween)
            panelGo.transform.localScale = Vector3.one * 0.8f;
            panelGo.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        }

        /// <summary>
        /// 팝업용 버튼 + 텍스트 자식을 생성하는 헬퍼 메서드
        /// </summary>
        private Button MakePopupButton(Transform parent, string goName, string label, ClassicPixelUiTheme.ShopPanelAccent accent)
        {
            GameObject btnGo = new GameObject(goName, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            // 텍스트 자식 먼저 생성
            GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            TextMeshProUGUI tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            ClassicPixelUiTheme.ApplyText(tmp);

            RectTransform txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            Button btn = btnGo.GetComponent<Button>();
            ClassicPixelUiTheme.ApplyShopButton(btn, accent);
            return btn;
        }

        private void OnSettingsClicked()
        {
            if (settingsController != null)
            {
                mainMenuPanel.SetActive(false);
                settingsController.Show(ShowMainMenu);
            }
        }

        private void OnExitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}

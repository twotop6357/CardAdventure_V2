using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace CardAdventure.UI
{
    public class InGameMenuController : MonoBehaviour
    {
        [Header("Main Menu Panel")]
        public RectTransform menuPanelRect;
        public CanvasGroup menuCanvasGroup;
        public float slideDuration = 0.3f;
        public float hiddenPosX = -800f;
        public float shownPosX = 0f;

        [Header("Buttons")]
        public Button settingsButton;
        public Button saveButton;
        public Button myCardsButton;
        public Button exitButton;
        public Button closeMenuButton;

        [Header("Sub Panels")]
        public SettingsUIController settingsController;
        public GameObject exitWarningPanel;
        public Button confirmExitButton;
        public Button cancelExitButton;

        [Header("Save Slot Panel (Optional)")]
        public GameObject saveSlotPanel;
        public SaveSlotView[] saveSlots;
        public Button closeSaveSlotButton;

        private bool isMenuOpen = false;

        private void Start()
        {
            // 초기 위치 숨김
            menuPanelRect.anchoredPosition = new Vector2(hiddenPosX, menuPanelRect.anchoredPosition.y);
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;

            if (exitWarningPanel != null) exitWarningPanel.SetActive(false);
            if (saveSlotPanel != null) saveSlotPanel.SetActive(false);

            settingsButton.onClick.AddListener(OnSettingsClicked);
            saveButton.onClick.AddListener(OnSaveClicked);
            myCardsButton.onClick.AddListener(OnMyCardsClicked);
            exitButton.onClick.AddListener(OnExitClicked);
            closeMenuButton.onClick.AddListener(CloseMenu);

            confirmExitButton.onClick.AddListener(ConfirmExit);
            cancelExitButton.onClick.AddListener(CancelExit);

            if (closeSaveSlotButton != null)
            {
                closeSaveSlotButton.onClick.AddListener(() => saveSlotPanel.SetActive(false));
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (settingsController != null && settingsController.settingsPanel.activeSelf)
                {
                    settingsController.Hide();
                }
                else if (exitWarningPanel != null && exitWarningPanel.activeSelf)
                {
                    CancelExit();
                }
                else if (saveSlotPanel != null && saveSlotPanel.activeSelf)
                {
                    saveSlotPanel.SetActive(false);
                }
                else
                {
                    ToggleMenu();
                }
            }
        }

        public void ToggleMenu()
        {
            if (isMenuOpen) CloseMenu();
            else OpenMenu();
        }

        public void OpenMenu()
        {
            isMenuOpen = true;
            menuCanvasGroup.interactable = true;
            menuCanvasGroup.blocksRaycasts = true;
            
            menuPanelRect.DOAnchorPosX(shownPosX, slideDuration).SetEase(Ease.OutBack);
            menuCanvasGroup.DOFade(1f, slideDuration);
        }

        public void CloseMenu()
        {
            isMenuOpen = false;
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;

            menuPanelRect.DOAnchorPosX(hiddenPosX, slideDuration).SetEase(Ease.InBack);
            menuCanvasGroup.DOFade(0f, slideDuration);

            if (exitWarningPanel != null) exitWarningPanel.SetActive(false);
            if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
            if (settingsController != null) settingsController.Hide();
        }

        private void OnSettingsClicked()
        {
            if (settingsController != null)
            {
                settingsController.Show();
            }
        }

        private void OnSaveClicked()
        {
            if (saveSlotPanel != null && saveSlots != null && saveSlots.Length > 0)
            {
                saveSlotPanel.SetActive(true);
                for (int i = 0; i < saveSlots.Length; i++)
                {
                    int slotIndex = i;
                    SaveData data = SaveManager.LoadSaveData(slotIndex);
                    saveSlots[i].Bind(slotIndex, data, OnSaveSlotClicked);
                }
            }
            else
            {
                // 기본적으로 슬롯 0에 저장하는 예시 (UI가 없을 경우)
                SaveManager.SaveGame(0);
                Debug.Log("게임 저장 완료 (슬롯 0)");
            }
        }

        private void OnSaveSlotClicked(int slotIndex)
        {
            SaveManager.SaveGame(slotIndex);
            Debug.Log($"게임 저장 완료 (슬롯 {slotIndex})");

            // UI 갱신
            for (int i = 0; i < saveSlots.Length; i++)
            {
                SaveData data = SaveManager.LoadSaveData(i);
                saveSlots[i].Bind(i, data, OnSaveSlotClicked);
            }
        }

        private void OnMyCardsClicked()
        {
            Debug.Log("내 카드 기능은 아직 준비 중입니다.");
        }

        private void OnExitClicked()
        {
            if (exitWarningPanel != null)
            {
                exitWarningPanel.SetActive(true);
            }
            else
            {
                ConfirmExit();
            }
        }

        private void ConfirmExit()
        {
            Time.timeScale = 1f; // 혹시 멈춰있을 경우 대비
            SceneManager.LoadScene("LobbyScene");
        }

        private void CancelExit()
        {
            if (exitWarningPanel != null)
            {
                exitWarningPanel.SetActive(false);
            }
        }
    }
}

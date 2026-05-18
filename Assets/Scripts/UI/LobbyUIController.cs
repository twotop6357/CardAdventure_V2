using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
            SaveManager.SetCurrentSlotIndex(0);

            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.ResetForNewGame();
            }
            SceneManager.LoadScene("AdventureScene");
        }

        private void OnContinueClicked()
        {
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
            SaveData data = SaveManager.LoadSaveData(slotIndex);
            if (data == null)
            {
                Debug.Log($"[Lobby] 슬롯 {slotIndex} 비어있음. 무시합니다.");
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

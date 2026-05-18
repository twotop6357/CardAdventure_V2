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
        public Button itemMenuButton; // 아이템 사용 메뉴 버튼
        public Button exitButton;
        public Button closeMenuButton;

        [Header("Sub Panels")]
        public SettingsUIController settingsController;
        public GameObject exitWarningPanel;
        public Button confirmExitButton;
        public Button cancelExitButton;
        public GameObject itemUsePanel; // 아이템 사용 전용 서브 패널
        public Button closeItemUseButton; // 아이템 사용 패널 닫기 버튼
        public MyCardsPanelController myCardsPanel; // 내 카드 서브 패널

        [Header("Save Slot Panel (Optional)")]
        public GameObject saveSlotPanel;
        public SaveSlotView[] saveSlots;
        public Button closeSaveSlotButton;

        [Header("Item & Face Assets")]
        public Sprite potionSprite;
        public Sprite warriorFaceSprite;
        public Sprite magicianFaceSprite;
        public Sprite rogueFaceSprite;

        [Header("Status & Item UI Elements")]
        public Image playerFaceImage;
        public TMPro.TextMeshProUGUI playerJobText;
        public TMPro.TextMeshProUGUI hpText;
        public Slider hpSlider;
        public TMPro.TextMeshProUGUI potionCountText;
        public Button usePotionButton;

        private bool isMenuOpen = false;

        private void Start()
        {
            // 초기 위치 숨김
            menuPanelRect.anchoredPosition = new Vector2(hiddenPosX, menuPanelRect.anchoredPosition.y);
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;

            if (playerFaceImage != null)
            {
                playerFaceImage.preserveAspect = true;
            }

            if (exitWarningPanel != null) exitWarningPanel.SetActive(false);
            if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
            if (itemUsePanel != null) itemUsePanel.SetActive(false);

            settingsButton.onClick.AddListener(OnSettingsClicked);
            saveButton.onClick.AddListener(OnSaveClicked);
            myCardsButton.onClick.AddListener(OnMyCardsClicked);
            
            if (itemMenuButton != null)
            {
                itemMenuButton.onClick.AddListener(OnItemMenuClicked);
            }
            
            exitButton.onClick.AddListener(OnExitClicked);
            closeMenuButton.onClick.AddListener(CloseMenu);

            confirmExitButton.onClick.AddListener(ConfirmExit);
            cancelExitButton.onClick.AddListener(CancelExit);

            if (closeSaveSlotButton != null)
            {
                closeSaveSlotButton.onClick.AddListener(() => saveSlotPanel.SetActive(false));
            }

            if (closeItemUseButton != null)
            {
                closeItemUseButton.onClick.AddListener(() => itemUsePanel.SetActive(false));
            }

            if (usePotionButton != null)
            {
                usePotionButton.onClick.AddListener(OnUsePotionClicked);
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
                else if (itemUsePanel != null && itemUsePanel.activeSelf)
                {
                    itemUsePanel.SetActive(false);
                }
                else if (myCardsPanel != null && myCardsPanel.gameObject.activeSelf)
                {
                    myCardsPanel.Hide();
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

            RefreshStatusAndItemUI();
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
            if (itemUsePanel != null) itemUsePanel.SetActive(false);
            if (myCardsPanel != null && myCardsPanel.gameObject.activeSelf) myCardsPanel.Hide();
            if (settingsController != null) settingsController.Hide();
        }

        /// <summary>
        /// ESC 메뉴 본체만 슬라이드 아웃. 서브패널은 건드리지 않음.
        /// 서브패널을 열기 전에 호출해서 "메뉴가 뒤에 남아 있다가 다시 나타나는" 현상을 방지.
        /// </summary>
        private void HideMainMenuPanel()
        {
            isMenuOpen = false;
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
            menuPanelRect.DOAnchorPosX(hiddenPosX, slideDuration).SetEase(Ease.InBack);
            menuCanvasGroup.DOFade(0f, slideDuration);
        }

        private void OnItemMenuClicked()
        {
            if (itemUsePanel != null)
            {
                HideMainMenuPanel();
                itemUsePanel.SetActive(true);
                RefreshStatusAndItemUI();
            }
        }

        private void OnSettingsClicked()
        {
            if (settingsController != null)
            {
                HideMainMenuPanel();
                settingsController.Show();
            }
        }

        private void OnSaveClicked()
        {
            if (saveSlotPanel != null && saveSlots != null && saveSlots.Length > 0)
            {
                HideMainMenuPanel();
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
                CapturePlayerPosition();
                SaveManager.SaveGame(SaveManager.CurrentSlotIndex);
                Debug.Log($"게임 저장 완료 (슬롯 {SaveManager.CurrentSlotIndex})");
            }
        }

        private void CapturePlayerPosition()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null && GameDataManager.Instance != null)
            {
                GameDataManager.Instance.HasSavedPosition = true;
                GameDataManager.Instance.SavedPosition = player.transform.position;
                GameDataManager.Instance.SavedFacingDirection = player.FacingDirection;
            }
        }

        private void OnSaveSlotClicked(int slotIndex)
        {
            CapturePlayerPosition();
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
            if (myCardsPanel != null)
            {
                HideMainMenuPanel();
                myCardsPanel.Show();
            }
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

        public void RefreshStatusAndItemUI()
        {
            if (GameDataManager.Instance == null) return;

            int currentHp = GameDataManager.Instance.CurrentHp;
            int maxHp = GameDataManager.Instance.MaxHp;
            int potionCount = GameDataManager.Instance.PotionCount;
            CardClass job = GameDataManager.Instance.SelectedJobClass;

            // 직업 텍스트 및 이미지 설정
            if (playerJobText != null)
            {
                playerJobText.text = GetJobKoreanName(job);
            }

            if (playerFaceImage != null)
            {
                switch (job)
                {
                    case CardClass.Warrior:
                        playerFaceImage.sprite = warriorFaceSprite;
                        break;
                    case CardClass.Mage:
                        playerFaceImage.sprite = magicianFaceSprite;
                        break;
                    case CardClass.Rogue:
                        playerFaceImage.sprite = rogueFaceSprite;
                        break;
                    default:
                        playerFaceImage.sprite = warriorFaceSprite;
                        break;
                }
            }

            // 체력 표시
            if (hpText != null)
            {
                hpText.text = $"HP {currentHp} / {maxHp}";
            }
 
            if (hpSlider != null)
            {
                hpSlider.maxValue = maxHp;
                hpSlider.DOKill(); // 기존 트윈 제거
                hpSlider.DOValue(currentHp, 0.4f).SetEase(Ease.OutQuad);
            }
 
            // 포션 표시
            if (potionCountText != null)
            {
                potionCountText.text = $"보유 포션: {potionCount}개";
            }
 
            // 포션 버튼 활성 상태
            if (usePotionButton != null)
            {
                usePotionButton.interactable = (potionCount > 0 && currentHp < maxHp);
            }
        }
 
        private string GetJobKoreanName(CardClass cardClass)
        {
            switch (cardClass)
            {
                case CardClass.Warrior: return "전사";
                case CardClass.Mage: return "마법사";
                case CardClass.Rogue: return "도적";
                default: return "초보자";
            }
        }
 
        private void OnUsePotionClicked()
        {
            if (GameDataManager.Instance == null) return;
 
            if (GameDataManager.Instance.UsePotion())
            {
                RefreshStatusAndItemUI();
                
                // 프리미엄 체력 회복 리액션 연출 (슬라이더 및 초상화 펀치 효과)
                if (hpSlider != null)
                {
                    hpSlider.transform.DOPunchScale(new Vector3(0.05f, 0.05f, 0.05f), 0.3f, 10, 1f);
                }
                if (playerFaceImage != null)
                {
                    playerFaceImage.transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.4f, 10, 1f);
                }

                Debug.Log("포션을 사용하여 체력을 25 회복했습니다!");
            }
        }
    }
}

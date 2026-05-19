using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;
using CardAdventure.Audio;

namespace CardAdventure
{
    /// <summary>
    /// 시험관 배틀 승리 후 카드 선택이 끝나면 화면을 페이드아웃 하고
    /// 자격증 정보(직업, 카드 구매 누적 금액, 포션 사용 개수, 최대 데미지 카드, 현재 날짜)를 보여준다.
    /// 클릭 시 로비 씬으로 복귀한다.
    /// </summary>
    public class EndingLicenseUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image faceImage;
        [SerializeField] private TextMeshProUGUI licenseText;
        [SerializeField] private CanvasGroup canvasGroup;

        private bool canClick = false;

        private void Awake()
        {
            // Sort of Layer (Sorting Order) 강제 999 설정
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
            }

            // 컴포넌트 자동 탐색 (프리팹 구조 대응)
            if (faceImage == null)
            {
                Transform faceTransform = transform.Find("License/FaceImage");
                if (faceTransform != null)
                {
                    faceImage = faceTransform.GetComponent<Image>();
                }
            }

            if (licenseText == null)
            {
                Transform textTransform = transform.Find("License/LicenseText");
                if (textTransform != null)
                {
                    licenseText = textTransform.GetComponent<TextMeshProUGUI>();
                }
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        public void Setup(Sprite faceSprite, string jobName, int spentGold, int potionsUsed, string maxDmgCard, int maxDmg, string dateString)
        {
            if (faceImage != null)
            {
                if (faceSprite != null)
                {
                    faceImage.sprite = faceSprite;
                    faceImage.gameObject.SetActive(true);
                }
                else
                {
                    faceImage.gameObject.SetActive(false);
                }
            }

            if (licenseText != null)
            {
                string cardInfo = string.IsNullOrEmpty(maxDmgCard) || maxDmgCard == "없음" 
                    ? "없음" 
                    : $"{maxDmgCard} ({maxDmg} DMG)";

                licenseText.text = $"직업 : {jobName}\n" +
                                   $"카드 구매 비용 : {spentGold:N0} G\n" +
                                   $"소모한 물약 : {potionsUsed}개\n" +
                                   $"가장 많은 데미지를 준 카드 : {cardInfo}\n" +
                                   $"라이센스 획득일 : {dateString}";
            }

            // 페이드인 연출
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOKill();
                canvasGroup.DOFade(1f, 0.6f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true) // 타임스케일 영향 배제
                    .OnComplete(() =>
                    {
                        canClick = true;
                    });
            }
            else
            {
                canClick = true;
            }

            // 화면 전체 클릭 대응을 위해 Button 컴포넌트 추가
            Button btn = GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClickLicense);

            // 하단에 픽셀 아트 스타일의 저장하기 버튼 동적 배치
            CreateSaveButton();
        }

        private void OnClickLicense()
        {
            if (!canClick) return;
            canClick = false;

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, 0.4f)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .OnComplete(ReturnToLobby);
            }
            else
            {
                ReturnToLobby();
            }
        }

        private void ReturnToLobby()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("LobbyScene");
        }

        #region Dynamic Save & Storage Systems
        private GameObject dynamicSavePanel;

        private void CreateSaveButton()
        {
            // "License" 트랜스폼을 찾는다
            Transform licensePanel = transform.Find("License");
            if (licensePanel == null) return;

            // 저장하기 버튼을 Canvas(this) 직속 자식으로 생성 — License 패널 외부
            GameObject btnGo = new GameObject("SaveButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(transform, false);

            // License 패널의 RectTransform 참조
            RectTransform licensePanelRt = licensePanel.GetComponent<RectTransform>();

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            // License 패널 하단 중앙에 맞춰서 그 아래에 배치
            // License 패널과 같은 앵커(중앙)를 사용하되, Y를 패널 하단 아래로 내림
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            // License 패널의 anchoredPosition.y - sizeDelta.y * (1 - pivot.y) 에서 추가 여백
            float licensePanelBottom = licensePanelRt.anchoredPosition.y
                                       - licensePanelRt.sizeDelta.y * (1f - licensePanelRt.pivot.y);
            rt.anchoredPosition = new Vector2(licensePanelRt.anchoredPosition.x, licensePanelBottom - 10f);
            rt.sizeDelta = new Vector2(160f, 45f);

            // 이미지 및 픽셀 테마 적용
            Image img = btnGo.GetComponent<Image>();
            Button btn = btnGo.GetComponent<Button>();
            btnGo.AddComponent<ButtonAudioHook>();
            
            // 저장하기 텍스트 생성
            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI txt = textGo.GetComponent<TextMeshProUGUI>();
            txt.text = "저장하기";
            txt.fontSize = 18f;
            txt.alignment = TextAlignmentOptions.Center;
            
            // 스타일 적용
            ClassicPixelUiTheme.ApplyShopButton(btn, ClassicPixelUiTheme.ShopPanelAccent.Gold);
            txt.color = new Color(0.96f, 0.96f, 0.92f, 1f);
            
            // 한글 픽셀 폰트 적용
            if (licenseText != null)
            {
                txt.font = licenseText.font;
            }

            // 이벤트 바인딩
            btn.onClick.AddListener(OnSaveButtonClicked);
        }

        private void OnSaveButtonClicked()
        {
            if (dynamicSavePanel != null)
            {
                dynamicSavePanel.SetActive(true);
                RefreshSaveSlots();
                return;
            }

            // 최상단 오버레이용 판넬 생성
            dynamicSavePanel = new GameObject("DynamicSavePanel", typeof(RectTransform), typeof(Image));
            dynamicSavePanel.transform.SetParent(transform, false); // EndingLicenseUI(Canvas) 바로 아래
            
            RectTransform rt = dynamicSavePanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 뒷배경 어둡게 차단
            Image bgImage = dynamicSavePanel.GetComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.75f);
            bgImage.raycastTarget = true; // 뒤의 클릭 차단

            // 팝업 메인 윈도우 생성
            GameObject winGo = new GameObject("Window", typeof(RectTransform), typeof(Image));
            winGo.transform.SetParent(dynamicSavePanel.transform, false);
            RectTransform winRt = winGo.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = new Vector2(400f, 460f);

            Image winImg = winGo.GetComponent<Image>();
            // 픽셀 패널 스타일 적용
            ClassicPixelUiTheme.ApplyShopPanel(winImg, ClassicPixelUiTheme.ShopPanelAccent.Gold);

            // 타이틀 텍스트
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(winGo.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -20f);
            titleRt.sizeDelta = new Vector2(300f, 40f);

            TextMeshProUGUI titleTxt = titleGo.GetComponent<TextMeshProUGUI>();
            titleTxt.text = "저장할 슬롯을 선택하세요";
            titleTxt.fontSize = 20f;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = ClassicPixelUiTheme.Gold;
            if (licenseText != null) titleTxt.font = licenseText.font;

            // 3개 슬롯 컨테이너
            for (int i = 0; i < 3; i++)
            {
                int slotIndex = i;
                GameObject slotGo = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                slotGo.transform.SetParent(winGo.transform, false);
                
                RectTransform slotRt = slotGo.GetComponent<RectTransform>();
                slotRt.anchorMin = new Vector2(0.5f, 1f);
                slotRt.anchorMax = new Vector2(0.5f, 1f);
                slotRt.pivot = new Vector2(0.5f, 1f);
                slotRt.anchoredPosition = new Vector2(0f, -80f - (i * 95f));
                slotRt.sizeDelta = new Vector2(340f, 80f);

                Image slotImg = slotGo.GetComponent<Image>();
                Button slotBtn = slotGo.GetComponent<Button>();
                ClassicPixelUiTheme.ApplyShopButton(slotBtn, ClassicPixelUiTheme.ShopPanelAccent.Blue);
                slotGo.AddComponent<ButtonAudioHook>();

                // 슬롯 텍스트 정보 바인딩
                GameObject infoGo = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI));
                infoGo.transform.SetParent(slotGo.transform, false);
                RectTransform infoRt = infoGo.GetComponent<RectTransform>();
                infoRt.anchorMin = Vector2.zero;
                infoRt.anchorMax = Vector2.one;
                infoRt.offsetMin = new Vector2(15f, 10f);
                infoRt.offsetMax = new Vector2(-15f, -10f);

                TextMeshProUGUI infoTxt = infoGo.GetComponent<TextMeshProUGUI>();
                infoTxt.fontSize = 14f;
                infoTxt.alignment = TextAlignmentOptions.Left;
                infoTxt.color = new Color(0.96f, 0.96f, 0.92f, 1f);
                if (licenseText != null) infoTxt.font = licenseText.font;

                // 데이터 바인딩
                SaveData data = SaveManager.LoadSaveData(slotIndex);
                if (data == null)
                {
                    infoTxt.text = $"<color=#00e5ff><b>슬롯 {slotIndex + 1}</b></color>\n빈 슬롯 (새로운 모험을 시작하세요.)";
                }
                else
                {
                    string jobName = !string.IsNullOrEmpty(data.selectedJobId) ? data.selectedJobId.Replace("Job_", "") : "전사";
                    infoTxt.text = $"<color=#ffb300><b>슬롯 {slotIndex + 1}</b></color>  <color=#888888>{data.saveDateStr}</color>\nLV.{data.chapterProgress} | {jobName} | {data.gold} G";
                }

                // 클릭 리스너
                slotBtn.onClick.AddListener(() => OnSaveSlotSelected(slotIndex));
            }

            // 닫기/취소 버튼
            GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(winGo.transform, false);
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 15f);
            closeRt.sizeDelta = new Vector2(120f, 35f);

            Image closeImg = closeGo.GetComponent<Image>();
            Button closeBtn = closeGo.GetComponent<Button>();
            ClassicPixelUiTheme.ApplyShopButton(closeBtn, ClassicPixelUiTheme.ShopPanelAccent.Cyan);
            closeGo.AddComponent<ButtonAudioHook>();;

            GameObject closeTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeTextGo.transform.SetParent(closeGo.transform, false);
            RectTransform closeTextRt = closeTextGo.GetComponent<RectTransform>();
            closeTextRt.anchorMin = Vector2.zero;
            closeTextRt.anchorMax = Vector2.one;
            closeTextRt.offsetMin = Vector2.zero;
            closeTextRt.offsetMax = Vector2.zero;

            TextMeshProUGUI closeTxt = closeTextGo.GetComponent<TextMeshProUGUI>();
            closeTxt.text = "돌아가기";
            closeTxt.fontSize = 14f;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = new Color(0.96f, 0.96f, 0.92f, 1f);
            if (licenseText != null) closeTxt.font = licenseText.font;

            closeBtn.onClick.AddListener(() => dynamicSavePanel.SetActive(false));
        }

        private void OnSaveSlotSelected(int slotIndex)
        {
            // 플레이어 위치 캡처 및 저장 준비
            CardAdventure.PlayerController player = FindFirstObjectByType<CardAdventure.PlayerController>();
            if (player != null && GameDataManager.Instance != null)
            {
                GameDataManager.Instance.HasSavedPosition = true;
                GameDataManager.Instance.SavedPosition = player.transform.position;
                GameDataManager.Instance.SavedFacingDirection = player.FacingDirection;
            }

            // 게임 저장 실행
            SaveManager.SaveGame(slotIndex);
            Debug.Log($"자격증 화면에서 슬롯 {slotIndex}에 모험 진행 기록 저장 완료!");

            // 저장 패널 비활성화
            if (dynamicSavePanel != null)
            {
                dynamicSavePanel.SetActive(false);
            }

            // 자격증 화면 및 전체를 페이드아웃 하고 타이틀(로비)로 안전하게 이동
            canClick = false; // 추가 클릭 방지
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, 0.4f)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .OnComplete(ReturnToLobby);
            }
            else
            {
                ReturnToLobby();
            }
        }

        private void RefreshSaveSlots()
        {
            if (dynamicSavePanel == null) return;

            Transform winTrans = dynamicSavePanel.transform.Find("Window");
            if (winTrans == null) return;

            for (int i = 0; i < 3; i++)
            {
                Transform slotTrans = winTrans.Find($"Slot_{i}");
                if (slotTrans == null) continue;

                Transform infoTrans = slotTrans.Find("Info");
                if (infoTrans == null) continue;

                TextMeshProUGUI infoTxt = infoTrans.GetComponent<TextMeshProUGUI>();
                if (infoTxt == null) continue;

                SaveData data = SaveManager.LoadSaveData(i);
                if (data == null)
                {
                    infoTxt.text = $"<color=#00e5ff><b>슬롯 {i + 1}</b></color>\n빈 슬롯 (새로운 모험을 시작하세요.)";
                }
                else
                {
                    string jobName = !string.IsNullOrEmpty(data.selectedJobId) ? data.selectedJobId.Replace("Job_", "") : "전사";
                    infoTxt.text = $"<color=#ffb300><b>슬롯 {i + 1}</b></color>  <color=#888888>{data.saveDateStr}</color>\nLV.{data.chapterProgress} | {jobName} | {data.gold} G";
                }
            }
        }
        #endregion
    }
}

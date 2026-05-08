using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 직업 선택/변경 UI 컨트롤러.
    ///
    /// 계층 구조 (JobSelectionSetup이 자동 생성):
    ///   JobSelectionCanvas (Canvas, CanvasScaler)
    ///     └─ Blocker (반투명 오버레이 Image)
    ///          └─ JobSelectionPanel (CanvasGroup + Image — 외곽 테두리)
    ///               └─ PanelInner (Image — 내부 채움, VerticalLayoutGroup)
    ///                    ├─ TitleBox  — "직업 선택" 제목
    ///                    ├─ ContentArea (HorizontalLayoutGroup)
    ///                    │    ├─ LeftPanel — 직업 목록 (커서 ▶ + 이름)
    ///                    │    └─ RightPanel — 캐릭터 미리보기 + 능력치 스탯
    ///                    └─ BottomArea (HorizontalLayoutGroup)
    ///                         ├─ DescriptionBox — 직업 이름 + 설명 텍스트
    ///                         └─ ButtonBox      — 결정 / 취소 버튼
    ///
    /// 키보드 조작:
    ///   ↑/↓          → 직업 목록 이동
    ///   Enter / Z    → 직업 목록에서 버튼 열로 이동 (또는 결정 실행)
    ///   ↓ (목록 끝)  → 버튼 열로 이동
    ///   ←/→          → 버튼 열에서 결정/취소 전환
    ///   ↑ (버튼 열)  → 목록 열로 복귀
    ///   Enter / Z    → 버튼 열에서 현재 포커스 항목 실행
    ///   Escape / X   → 취소
    /// </summary>
    public class JobSelectionUI : MonoBehaviour
    {
        // ── 데이터 ─────────────────────────────────────────────
        [Header("직업 데이터 (Inspector에서 할당)")]
        [SerializeField] private List<JobClassInfo> jobs = new List<JobClassInfo>();

        // ── 패널 ───────────────────────────────────────────────
        [Header("패널")]
        [SerializeField] private CanvasGroup    canvasGroup;
        [SerializeField] private RectTransform  panelRoot;

        // ── 직업 목록 ──────────────────────────────────────────
        [Header("직업 목록 (직업 순서와 동일한 순서로 할당)")]
        [SerializeField] private List<TextMeshProUGUI> jobCursorTexts = new List<TextMeshProUGUI>();
        [SerializeField] private List<TextMeshProUGUI> jobLabelTexts  = new List<TextMeshProUGUI>();

        // ── 캐릭터 미리보기 ────────────────────────────────────
        [Header("캐릭터 미리보기")]
        [SerializeField] private Image characterPreviewImage;

        // ── 능력치 텍스트 ──────────────────────────────────────
        [Header("능력치 스타 텍스트 (■□ 문자열)")]
        [SerializeField] private TextMeshProUGUI attackStarsText;
        [SerializeField] private TextMeshProUGUI defenseStarsText;
        [SerializeField] private TextMeshProUGUI magicStarsText;
        [SerializeField] private TextMeshProUGUI speedStarsText;

        // ── 직업 설명 ──────────────────────────────────────────
        [Header("직업 설명 텍스트")]
        [SerializeField] private TextMeshProUGUI jobNameInDescription;
        [SerializeField] private TextMeshProUGUI descriptionText;

        // ── 버튼 ───────────────────────────────────────────────
        [Header("버튼 커서 텍스트")]
        [SerializeField] private TextMeshProUGUI confirmCursorText;
        [SerializeField] private TextMeshProUGUI cancelCursorText;

        // ── 상수 ──────────────────────────────────────────────
        private const string CURSOR_ON   = "▶";
        private const string CURSOR_OFF  = "  ";
        private const string STAR_FILLED = "■";
        private const string STAR_EMPTY  = "□";
        private const int    MAX_STARS   = 5;

        // ── 런타임 상태 ────────────────────────────────────────
        private int  selectedJobIndex = 0;
        private bool isInButtonRow    = false;  // false = 직업 목록 열, true = 버튼 열
        private bool confirmFocused   = true;   // 버튼 열에서 결정(true) / 취소(false)
        private bool isOpen           = false;

        // ── 이벤트 ────────────────────────────────────────────
        /// <summary>결정 버튼이 선택됐을 때 발행. 선택된 JobClassInfo를 인수로 전달한다.</summary>
        public event Action<JobClassInfo> OnJobConfirmed;

        /// <summary>취소 버튼이 선택됐을 때 발행.</summary>
        public event Action OnCancelled;

        // ── 공개 프로퍼티 ──────────────────────────────────────
        public bool IsOpen => isOpen;

        // ══════════════════════════════════════════════════════
        //  Unity 생명주기
        // ══════════════════════════════════════════════════════

        private void Awake()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (panelRoot   != null) panelRoot.localScale = Vector3.one * 0.92f;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            DOTween.Kill(canvasGroup);
            if (panelRoot != null) DOTween.Kill(panelRoot);
        }

        private void Update()
        {
            if (!isOpen) return;
            HandleInput();
        }

        // ══════════════════════════════════════════════════════
        //  공개 API
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 직업 선택 UI를 열고 팝업 페이드인 애니메이션을 재생한다.
        /// </summary>
        /// <param name="initialIndex">초기 선택 직업 인덱스 (기본 0)</param>
        public void Open(int initialIndex = 0)
        {
            if (jobs == null || jobs.Count == 0)
            {
                Debug.LogWarning("[JobSelectionUI] jobs 목록이 비어 있습니다. Inspector에서 직업 데이터를 할당하세요.", this);
                return;
            }

            gameObject.SetActive(true);
            selectedJobIndex = Mathf.Clamp(initialIndex, 0, jobs.Count - 1);
            isInButtonRow    = false;
            confirmFocused   = true;
            isOpen           = false;  // 애니메이션 완료 후 입력 활성화

            InitJobLabels();
            RefreshDisplay();

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (panelRoot   != null) panelRoot.localScale = Vector3.one * 0.9f;

            Sequence openSeq = DOTween.Sequence();
            if (canvasGroup != null)
                openSeq.Join(canvasGroup.DOFade(1f, 0.2f).SetEase(Ease.OutQuad));
            if (panelRoot != null)
                openSeq.Join(panelRoot.DOScale(Vector3.one, 0.22f).SetEase(Ease.OutBack));
            openSeq.OnComplete(() => isOpen = true);
        }

        /// <summary>직업 선택 UI를 닫는다 (페이드아웃 후 비활성화).</summary>
        public void Close()
        {
            isOpen = false;
            DOTween.Kill(canvasGroup);

            Sequence closeSeq = DOTween.Sequence();
            if (canvasGroup != null)
                closeSeq.Join(canvasGroup.DOFade(0f, 0.15f).SetEase(Ease.InQuad));
            closeSeq.OnComplete(() => gameObject.SetActive(false));
        }

        /// <summary>런타임에서 직업 목록을 동적으로 교체할 때 사용.</summary>
        public void SetJobs(List<JobClassInfo> jobInfoList)
        {
            jobs = jobInfoList ?? new List<JobClassInfo>();
        }

        // ══════════════════════════════════════════════════════
        //  입력 처리
        // ══════════════════════════════════════════════════════

        private void HandleInput()
        {
            if (Keyboard.current == null) return;

            bool up      = Keyboard.current.upArrowKey.wasPressedThisFrame;
            bool down    = Keyboard.current.downArrowKey.wasPressedThisFrame;
            bool left    = Keyboard.current.leftArrowKey.wasPressedThisFrame;
            bool right   = Keyboard.current.rightArrowKey.wasPressedThisFrame;
            bool confirm = Keyboard.current.enterKey.wasPressedThisFrame ||
                           Keyboard.current.zKey.wasPressedThisFrame;
            bool cancel  = Keyboard.current.escapeKey.wasPressedThisFrame ||
                           Keyboard.current.xKey.wasPressedThisFrame;

            if (!isInButtonRow)
                HandleJobListInput(up, down, confirm, cancel);
            else
                HandleButtonRowInput(up, left, right, confirm, cancel);
        }

        private void HandleJobListInput(bool up, bool down, bool confirm, bool cancel)
        {
            if (up)
            {
                // 첫 번째 항목에서 위로 → 마지막 항목으로 순환
                selectedJobIndex = (selectedJobIndex - 1 + jobs.Count) % jobs.Count;
                RefreshDisplay();
            }
            else if (down)
            {
                if (selectedJobIndex < jobs.Count - 1)
                {
                    selectedJobIndex++;
                    RefreshDisplay();
                }
                else
                {
                    // 마지막 항목에서 아래로 → 버튼 열로 이동
                    isInButtonRow  = true;
                    confirmFocused = true;
                    RefreshCursors();
                }
            }
            else if (confirm)
            {
                // Enter → 버튼 열로 이동 (결정 포커스)
                isInButtonRow  = true;
                confirmFocused = true;
                RefreshCursors();
            }
            else if (cancel)
            {
                ExecuteCancel();
            }
        }

        private void HandleButtonRowInput(bool up, bool left, bool right, bool confirm, bool cancel)
        {
            if (up)
            {
                // 버튼 열에서 위로 → 목록 열 복귀
                isInButtonRow = false;
                RefreshCursors();
            }
            else if (left || right)
            {
                confirmFocused = !confirmFocused;
                RefreshCursors();
            }
            else if (confirm)
            {
                if (confirmFocused) ExecuteConfirm();
                else                ExecuteCancel();
            }
            else if (cancel)
            {
                ExecuteCancel();
            }
        }

        // ══════════════════════════════════════════════════════
        //  디스플레이 갱신
        // ══════════════════════════════════════════════════════

        private void InitJobLabels()
        {
            for (int i = 0; i < jobLabelTexts.Count && i < jobs.Count; i++)
            {
                if (jobLabelTexts[i] != null)
                    jobLabelTexts[i].text = jobs[i] != null ? jobs[i].displayName : string.Empty;
            }
        }

        private void RefreshDisplay()
        {
            RefreshCursors();
            RefreshPreview();
            RefreshStats();
            RefreshDescription();
        }

        private void RefreshCursors()
        {
            for (int i = 0; i < jobCursorTexts.Count; i++)
            {
                if (jobCursorTexts[i] == null) continue;
                jobCursorTexts[i].text = (!isInButtonRow && i == selectedJobIndex)
                    ? CURSOR_ON : CURSOR_OFF;
            }

            if (confirmCursorText != null)
                confirmCursorText.text = (isInButtonRow && confirmFocused)  ? CURSOR_ON : CURSOR_OFF;
            if (cancelCursorText  != null)
                cancelCursorText.text  = (isInButtonRow && !confirmFocused) ? CURSOR_ON : CURSOR_OFF;
        }

        private void RefreshPreview()
        {
            if (characterPreviewImage == null) return;
            if (jobs == null || selectedJobIndex >= jobs.Count) return;

            JobClassInfo info = jobs[selectedJobIndex];
            if (info == null) return;

            bool hasSprite = info.previewSprite != null;
            characterPreviewImage.sprite  = info.previewSprite;
            characterPreviewImage.enabled = hasSprite;
        }

        private void RefreshStats()
        {
            if (jobs == null || selectedJobIndex >= jobs.Count) return;
            JobClassInfo info = jobs[selectedJobIndex];
            if (info == null) return;

            if (attackStarsText  != null) attackStarsText.text  = BuildStarBar(info.attackStars);
            if (defenseStarsText != null) defenseStarsText.text = BuildStarBar(info.defenseStars);
            if (magicStarsText   != null) magicStarsText.text   = BuildStarBar(info.magicStars);
            if (speedStarsText   != null) speedStarsText.text   = BuildStarBar(info.difficulty);
        }

        private void RefreshDescription()
        {
            if (jobs == null || selectedJobIndex >= jobs.Count) return;
            JobClassInfo info = jobs[selectedJobIndex];
            if (info == null) return;

            if (jobNameInDescription != null) jobNameInDescription.text = info.displayName;
            if (descriptionText      != null) descriptionText.text      = info.description;
        }

        private static string BuildStarBar(int filled)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(MAX_STARS);
            for (int i = 1; i <= MAX_STARS; i++)
                sb.Append(i <= filled ? STAR_FILLED : STAR_EMPTY);
            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════
        //  실행 액션
        // ══════════════════════════════════════════════════════

        private void ExecuteConfirm()
        {
            if (jobs == null || selectedJobIndex >= jobs.Count) return;
            JobClassInfo selected = jobs[selectedJobIndex];
            Close();
            OnJobConfirmed?.Invoke(selected);
        }

        private void ExecuteCancel()
        {
            Close();
            OnCancelled?.Invoke();
        }
    }
}

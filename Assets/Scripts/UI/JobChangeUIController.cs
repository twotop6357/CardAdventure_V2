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
    /// 직업 선택/변경 UI 컨트롤러 (Button 기반, 2단계 키보드 내비게이션).
    ///
    /// 【1단계 — 직업 목록】
    ///   W / ↑       → 직업 위로
    ///   S / ↓       → 직업 아래로
    ///   Space       → 버튼 선택 단계로 전환 (결정 포커스)
    ///   Esc / X     → 취소
    ///   마우스 클릭  → 직업 선택 (1단계 유지)
    ///
    /// 【2단계 — 버튼 선택】
    ///   W / ↑       → 결정 버튼으로 포커스
    ///   S / ↓       → 취소 버튼으로 포커스
    ///   Space       → 현재 포커스 버튼 실행
    ///   Esc / X     → 취소
    ///   마우스 클릭  → 직업·결정·취소 버튼 직접 클릭 가능
    ///
    /// UI가 열려 있는 동안 PlayerController가 비활성화되어 이동이 차단된다.
    /// </summary>
    public class JobChangeUIController : MonoBehaviour
    {
        // ── 데이터 ────────────────────────────────────────────────
        [Header("직업 데이터 (Inspector에서 할당, 버튼 순서와 동일)")]
        [SerializeField] private List<JobClassInfo> jobs = new List<JobClassInfo>();

        // ── 직업 버튼 ─────────────────────────────────────────────
        [Header("직업 버튼 (버튼 순서 = 직업 순서)")]
        [SerializeField] private List<Button> jobButtons = new List<Button>();

        // ── 캐릭터 미리보기 ────────────────────────────────────────
        [Header("캐릭터 미리보기")]
        [SerializeField] private Image characterPreviewImage;

        // ── 직업 설명 ──────────────────────────────────────────────
        [Header("직업 설명 텍스트")]
        [SerializeField] private TextMeshProUGUI jobDescriptionText;

        // ── 결정 / 취소 버튼 ──────────────────────────────────────
        [Header("결정 / 취소 버튼")]
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        // ── 애니메이션 ────────────────────────────────────────────
        [Header("팝업 애니메이션 루트 (없으면 스킵)")]
        [SerializeField] private RectTransform panelRoot;

        // ── 선택 강조 색상 ────────────────────────────────────────
        [Header("선택 강조 색상")]
        [SerializeField] private Color selectedColor = new Color(1f, 0.92f, 0.5f, 1f);
        [SerializeField] private Color normalColor   = Color.white;

        // ── 런타임 상태 ────────────────────────────────────────────
        private int              selectedJobIndex = 0;
        private bool             isOpen           = false;
        private bool             isInButtonPhase  = false;   // false = 직업 목록, true = 버튼 선택
        private bool             confirmFocused   = true;    // 버튼 단계: true = 결정, false = 취소
        private PlayerController cachedPlayer;

        // ── 전역 상태 (DialogueManager 등 외부에서 참조) ──────────
        /// <summary>직업 선택 UI가 하나라도 열려 있으면 true.</summary>
        public static bool IsAnyOpen { get; private set; }

        // ── 이벤트 ────────────────────────────────────────────────
        /// <summary>결정이 실행됐을 때 발행. 선택된 JobClassInfo를 인수로 전달.</summary>
        public event Action<JobClassInfo> OnJobConfirmed;

        /// <summary>취소가 실행됐을 때 발행.</summary>
        public event Action OnCancelled;

        // ── 공개 프로퍼티 ──────────────────────────────────────────
        public bool IsOpen => isOpen;

        // ══════════════════════════════════════════════════════════
        //  Unity 생명주기
        // ══════════════════════════════════════════════════════════

        private void Awake()
        {
            SetupButtonListeners();
        }

        private void Start()
        {
            if (!isOpen)
                gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isOpen) return;
            HandleKeyboardInput();
        }

        private void OnDestroy()
        {
            if (panelRoot != null) DOTween.Kill(panelRoot);
            IsAnyOpen = false;
            SetPlayerMovement(true);
        }

        // ══════════════════════════════════════════════════════════
        //  버튼 리스너 초기화
        // ══════════════════════════════════════════════════════════

        private void SetupButtonListeners()
        {
            for (int i = 0; i < jobButtons.Count; i++)
            {
                if (jobButtons[i] == null) continue;
                int captured = i;
                jobButtons[i].onClick.AddListener(() => SelectJobByMouse(captured));
            }

            if (yesButton != null) yesButton.onClick.AddListener(ExecuteConfirm);
            if (noButton  != null) noButton.onClick.AddListener(ExecuteCancel);
        }

        // ══════════════════════════════════════════════════════════
        //  키보드 입력 — 2단계 분기
        // ══════════════════════════════════════════════════════════

        private void HandleKeyboardInput()
        {
            if (Keyboard.current == null) return;

            bool up     = Keyboard.current.wKey.wasPressedThisFrame
                       || Keyboard.current.upArrowKey.wasPressedThisFrame;
            bool down   = Keyboard.current.sKey.wasPressedThisFrame
                       || Keyboard.current.downArrowKey.wasPressedThisFrame;
            bool space  = Keyboard.current.spaceKey.wasPressedThisFrame;
            bool cancel = Keyboard.current.escapeKey.wasPressedThisFrame
                       || Keyboard.current.xKey.wasPressedThisFrame;

            if (!isInButtonPhase)
                HandleJobPhaseInput(up, down, space, cancel);
            else
                HandleButtonPhaseInput(up, down, space, cancel);
        }

        /// <summary>1단계: 직업 목록 내비게이션</summary>
        private void HandleJobPhaseInput(bool up, bool down, bool space, bool cancel)
        {
            if      (up)     MoveJobSelection(-1);
            else if (down)   MoveJobSelection(+1);
            else if (space)  EnterButtonPhase();
            else if (cancel) ExecuteCancel();
        }

        /// <summary>2단계: 결정/취소 버튼 선택</summary>
        private void HandleButtonPhaseInput(bool up, bool down, bool space, bool cancel)
        {
            if (up)
            {
                confirmFocused = true;
                RefreshButtonPhaseHighlights();
            }
            else if (down)
            {
                confirmFocused = false;
                RefreshButtonPhaseHighlights();
            }
            else if (space)
            {
                if (confirmFocused) ExecuteConfirm();
                else                ExecuteCancel();
            }
            else if (cancel)
            {
                ExecuteCancel();
            }
        }

        private void EnterButtonPhase()
        {
            isInButtonPhase = true;
            confirmFocused  = true;
            RefreshButtonPhaseHighlights();
        }

        // ══════════════════════════════════════════════════════════
        //  공개 API
        // ══════════════════════════════════════════════════════════

        public void Open(int initialIndex = 0)
        {
            if (jobs == null || jobs.Count == 0)
            {
                Debug.LogWarning("[JobChangeUIController] jobs 목록이 비어 있습니다.", this);
                return;
            }

            isOpen          = true;
            IsAnyOpen       = true;
            isInButtonPhase = false;
            confirmFocused  = true;
            gameObject.SetActive(true);
            selectedJobIndex = Mathf.Clamp(initialIndex, 0, jobs.Count - 1);

            SetPlayerMovement(false);

            InitJobButtonLabels();
            RefreshDisplay();

            if (panelRoot != null)
            {
                DOTween.Kill(panelRoot);
                panelRoot.localScale = Vector3.one * 0.88f;
                panelRoot.DOScale(Vector3.one, 0.22f).SetEase(Ease.OutBack);
            }
        }

        public void Close()
        {
            isOpen    = false;
            IsAnyOpen = false;
            SetPlayerMovement(true);

            if (panelRoot != null)
            {
                DOTween.Kill(panelRoot);
                panelRoot.DOScale(Vector3.one * 0.88f, 0.15f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void SetJobs(List<JobClassInfo> jobInfoList)
        {
            jobs = jobInfoList ?? new List<JobClassInfo>();
        }

        // ══════════════════════════════════════════════════════════
        //  내부: 플레이어 이동 차단
        // ══════════════════════════════════════════════════════════

        private void SetPlayerMovement(bool enabled)
        {
            if (cachedPlayer == null)
                cachedPlayer = FindFirstObjectByType<PlayerController>();
            if (cachedPlayer != null)
                cachedPlayer.enabled = enabled;
        }

        // ══════════════════════════════════════════════════════════
        //  내부: 직업 선택
        // ══════════════════════════════════════════════════════════

        /// <summary>키보드로 직업 목록 이동 (순환)</summary>
        private void MoveJobSelection(int direction)
        {
            if (jobs == null || jobs.Count == 0) return;
            selectedJobIndex = (selectedJobIndex + direction + jobs.Count) % jobs.Count;
            RefreshDisplay();
        }

        /// <summary>마우스 클릭으로 직업 선택 — 1단계로 복귀</summary>
        private void SelectJobByMouse(int index)
        {
            if (index < 0 || index >= jobs.Count) return;
            selectedJobIndex = index;
            isInButtonPhase  = false;
            RefreshDisplay();
        }

        // ══════════════════════════════════════════════════════════
        //  내부: 디스플레이 갱신
        // ══════════════════════════════════════════════════════════

        private void InitJobButtonLabels()
        {
            for (int i = 0; i < jobButtons.Count && i < jobs.Count; i++)
            {
                if (jobButtons[i] == null) continue;
                TextMeshProUGUI label = jobButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null && jobs[i] != null)
                    label.text = jobs[i].displayName;
            }
        }

        private void RefreshDisplay()
        {
            RefreshJobButtonHighlights();
            RefreshButtonPhaseHighlights();
            RefreshPreview();
            RefreshDescription();
        }

        /// <summary>직업 버튼 강조 (1단계 선택 표시)</summary>
        private void RefreshJobButtonHighlights()
        {
            for (int i = 0; i < jobButtons.Count; i++)
            {
                if (jobButtons[i] == null) continue;
                Image img = jobButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = (i == selectedJobIndex) ? selectedColor : normalColor;
            }
        }

        /// <summary>결정/취소 버튼 강조 (2단계 포커스 표시)</summary>
        private void RefreshButtonPhaseHighlights()
        {
            if (yesButton != null)
            {
                Image img = yesButton.GetComponent<Image>();
                if (img != null)
                    img.color = (isInButtonPhase && confirmFocused)  ? selectedColor : normalColor;
            }
            if (noButton != null)
            {
                Image img = noButton.GetComponent<Image>();
                if (img != null)
                    img.color = (isInButtonPhase && !confirmFocused) ? selectedColor : normalColor;
            }
        }

        private void RefreshPreview()
        {
            if (characterPreviewImage == null) return;
            if (jobs == null || selectedJobIndex >= jobs.Count) return;

            JobClassInfo info = jobs[selectedJobIndex];
            if (info == null) return;

            characterPreviewImage.sprite  = info.previewSprite;
            characterPreviewImage.enabled = info.previewSprite != null;
        }

        private void RefreshDescription()
        {
            if (jobDescriptionText == null) return;
            if (jobs == null || selectedJobIndex >= jobs.Count) return;

            JobClassInfo info = jobs[selectedJobIndex];
            if (info == null) return;

            jobDescriptionText.text = info.description;
        }

        // ══════════════════════════════════════════════════════════
        //  내부: 실행 액션
        // ══════════════════════════════════════════════════════════

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

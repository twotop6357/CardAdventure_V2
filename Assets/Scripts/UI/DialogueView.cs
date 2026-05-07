using DG.Tweening;
using Febucci.UI;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 포켓몬 스타일 하단 대화창 UI 컴포넌트.
    ///
    /// 계층 구조 (DialogueSceneSetup이 자동 생성):
    ///   DialoguePanel (CanvasGroup, RectTransform)
    ///     ├─ PanelBg       (Image — 흰 배경)
    ///     ├─ NameBox        (Image — 이름 박스)
    ///     │    └─ NameText  (TextMeshProUGUI)
    ///     ├─ DialogueText   (TextMeshProUGUI + TextAnimator_TMP + TypewriterByCharacter)
    ///     └─ NextArrow      (TextMeshProUGUI "▼", 깜빡임 DOTween)
    ///
    /// 사용:
    ///   view.Show(speakerName, firstLine);   // 패널 슬라이드 인 + 첫 줄 타이핑
    ///   view.ShowLine(text);                 // 줄 교체
    ///   view.SkipTypewriter();               // 현재 줄 즉시 완성
    ///   view.Hide();                         // 패널 슬라이드 아웃
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        // ── Inspector 직렬화 ───────────────────────────────────
        [Header("패널")]
        [SerializeField] private CanvasGroup   canvasGroup;
        [SerializeField] private RectTransform dialoguePanel;

        [Header("텍스트")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private GameObject      nameBoxRoot;    // 이름 박스 전체 (이름 없으면 숨김)
        [SerializeField] private TextMeshProUGUI dialogueText;

        [Header("Febucci Typewriter")]
        [Tooltip("DialogueText와 같은 GameObject에 있는 TypewriterByCharacter 컴포넌트")]
        [SerializeField] private TypewriterByCharacter typewriter;

        [Header("화살표")]
        [SerializeField] private GameObject nextArrow;          // ▼ 오브젝트

        // ── 런타임 상태 ────────────────────────────────────────
        private bool     isTyping;
        private bool     typewriterUnavailable;
        private Sequence arrowSeq;
        private Tween    panelTween;

        /// <summary>타이핑 효과가 진행 중이면 true</summary>
        public bool IsTyping => isTyping;

        // ══════════════════════════════════════════════════════
        //  Unity 생명주기
        // ══════════════════════════════════════════════════════

        private void Awake()
        {
            // 시작 시 숨김
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            HideArrow();

            if (typewriter != null)
                typewriter.onTextShowed.AddListener(OnTypewriterComplete);
        }

        private void OnDestroy()
        {
            if (typewriter != null)
                typewriter.onTextShowed.RemoveListener(OnTypewriterComplete);

            arrowSeq?.Kill();
            panelTween?.Kill();
        }

        // ══════════════════════════════════════════════════════
        //  공개 API
        // ══════════════════════════════════════════════════════

        /// <summary>대화창을 슬라이드 인하며 첫 줄을 표시한다.</summary>
        public void Show(string speakerName, string firstLine)
        {
            gameObject.SetActive(true);

            // 화자 이름
            bool hasSpeaker = !string.IsNullOrEmpty(speakerName);
            if (nameBoxRoot != null) nameBoxRoot.SetActive(hasSpeaker);
            if (speakerNameText != null && hasSpeaker)
                speakerNameText.text = speakerName;

            // 슬라이드 인 애니메이션
            panelTween?.Kill();
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (dialoguePanel != null)
            {
                Vector2 hiddenPos = new Vector2(0f, -dialoguePanel.rect.height - 10f);
                dialoguePanel.anchoredPosition = hiddenPos;

                panelTween = DOTween.Sequence()
                    .Append(canvasGroup != null
                        ? canvasGroup.DOFade(1f, 0.15f)
                        : DOTween.To(() => 0f, _ => { }, 1f, 0.15f))
                    .Join(dialoguePanel.DOAnchorPosY(0f, 0.2f).SetEase(Ease.OutCubic));
            }
            else if (canvasGroup != null)
            {
                canvasGroup.DOFade(1f, 0.15f);
            }

            ShowLine(firstLine);
        }

        /// <summary>현재 줄을 새 텍스트로 교체하고 타이핑 효과를 시작한다.</summary>
        public void ShowLine(string line)
        {
            isTyping = true;
            HideArrow();

            if (typewriter != null && !typewriterUnavailable)
            {
                try
                {
                    // Febucci TypewriterByCharacter
                    typewriter.ShowText(line ?? string.Empty);
                    return;
                }
                catch (Exception ex)
                {
                    typewriterUnavailable = true;
                    Debug.LogWarning($"[DialogueView] Typewriter 표시 실패. 일반 텍스트 표시로 전환합니다. ({ex.GetType().Name}: {ex.Message})", this);
                }
            }

            if (dialogueText != null)
            {
                // 폴백: 타이핑 없이 즉시 표시
                dialogueText.text = line ?? string.Empty;
                isTyping = false;
                ShowArrow();
            }
        }

        /// <summary>타이핑 중인 텍스트를 즉시 완성한다.</summary>
        public void SkipTypewriter()
        {
            typewriter?.SkipTypewriter();
        }

        /// <summary>대화창을 슬라이드 아웃한 뒤 비활성화한다.</summary>
        public void Hide()
        {
            HideArrow();
            arrowSeq?.Kill();

            panelTween?.Kill();

            if (dialoguePanel != null)
            {
                float targetY = -dialoguePanel.rect.height - 10f;
                panelTween = DOTween.Sequence()
                    .Append(dialoguePanel.DOAnchorPosY(targetY, 0.2f).SetEase(Ease.InCubic))
                    .Join(canvasGroup != null
                        ? canvasGroup.DOFade(0f, 0.15f).SetDelay(0.05f)
                        : DOTween.To(() => 1f, _ => { }, 0f, 0.15f))
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.15f)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        // ══════════════════════════════════════════════════════
        //  내부 헬퍼
        // ══════════════════════════════════════════════════════

        private void OnTypewriterComplete()
        {
            isTyping = false;
            ShowArrow();
        }

        private void ShowArrow()
        {
            if (nextArrow == null) return;
            nextArrow.SetActive(true);

            // 위아래 깜빡임 DOTween
            arrowSeq?.Kill();
            RectTransform rt = nextArrow.GetComponent<RectTransform>();
            if (rt != null)
            {
                arrowSeq = DOTween.Sequence()
                    .Append(rt.DOAnchorPosY(rt.anchoredPosition.y - 3f, 0.4f).SetEase(Ease.InOutSine))
                    .Append(rt.DOAnchorPosY(rt.anchoredPosition.y,      0.4f).SetEase(Ease.InOutSine))
                    .SetLoops(-1);
            }
        }

        private void HideArrow()
        {
            arrowSeq?.Kill();
            if (nextArrow != null) nextArrow.SetActive(false);
        }
    }
}

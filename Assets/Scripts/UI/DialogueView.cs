using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// Adventure dialogue window view.
    /// Handles panel show/hide and a restrained character-by-character typewriter effect.
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform dialoguePanel;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private GameObject nameBoxRoot;
        [SerializeField] private TextMeshProUGUI dialogueText;

        [Header("Portrait")]
        [SerializeField] private GameObject portraitRoot;
        [SerializeField] private Image portraitImage;

        [Header("Typewriter")]
        [SerializeField, Min(0.005f)] private float characterInterval = 0.03f;
        [SerializeField, Min(0f)] private float commaPause = 0.05f;
        [SerializeField, Min(0f)] private float periodPause = 0.10f;

        [Header("Prompt")]
        [SerializeField] private GameObject nextArrow;

        private bool isTyping;
        private Sequence arrowSeq;
        private Tween panelTween;
        private Coroutine typingRoutine;
        private string currentLine = string.Empty;
        private RectTransform choiceRoot;
        private System.Action yesChoiceAction;
        private System.Action noChoiceAction;

        public bool IsTyping => isTyping;
        public bool IsChoiceActive => choiceRoot != null && choiceRoot.gameObject.activeSelf;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            HideArrow();
        }

        private void OnDestroy()
        {
            arrowSeq?.Kill();
            panelTween?.Kill();
            StopTypingRoutine();
        }

        public void Show(string speakerName, string firstLine)
        {
            Show(speakerName, null, firstLine);
        }

        public void Show(string speakerName, Sprite portrait, string firstLine)
        {
            gameObject.SetActive(true);

            bool hasSpeaker = !string.IsNullOrEmpty(speakerName);
            if (nameBoxRoot != null)
            {
                nameBoxRoot.SetActive(hasSpeaker);
            }

            if (speakerNameText != null && hasSpeaker)
            {
                speakerNameText.text = speakerName;
            }

            bool hasPortrait = portrait != null;
            if (portraitRoot != null)
            {
                portraitRoot.SetActive(hasPortrait);
            }

            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = hasPortrait;
                portraitImage.preserveAspect = true;
            }

            panelTween?.Kill();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

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
                panelTween = canvasGroup.DOFade(1f, 0.15f);
            }

            ShowLine(firstLine);
        }

        public void ShowLine(string line)
        {
            HideChoices();
            HideArrow();
            StopTypingRoutine();

            currentLine = line ?? string.Empty;
            if (dialogueText == null)
            {
                isTyping = false;
                ShowArrow();
                return;
            }

            isTyping = true;
            dialogueText.text = currentLine;
            dialogueText.maxVisibleCharacters = 0;
            typingRoutine = StartCoroutine(TypeLineByCharacter());
        }

        public void SkipTypewriter()
        {
            if (!isTyping)
            {
                return;
            }

            StopTypingRoutine();
            CompleteTyping();
        }

        public void Hide()
        {
            HideChoices();
            HideArrow();
            arrowSeq?.Kill();
            StopTypingRoutine();
            isTyping = false;

            panelTween?.Kill();

            if (dialoguePanel != null)
            {
                float targetY = -dialoguePanel.rect.height - 10f;
                panelTween = DOTween.Sequence()
                    .Append(dialoguePanel.DOAnchorPosY(targetY, 0.2f).SetEase(Ease.InCubic))
                    .Join(canvasGroup != null
                        ? canvasGroup.DOFade(0f, 0.15f).SetDelay(0.05f)
                        : DOTween.To(() => 1f, _ => { }, 0f, 0.15f))
                    .OnComplete(DeactivateIfAlive);
            }
            else if (canvasGroup != null)
            {
                panelTween = canvasGroup.DOFade(0f, 0.15f)
                    .OnComplete(DeactivateIfAlive);
            }
            else
            {
                DeactivateIfAlive();
            }
        }

        public void ShowChoices(string yesText, string noText, System.Action onYes, System.Action onNo)
        {
            StopTypingRoutine();
            CompleteTyping();
            HideArrow();
            EnsureChoiceRoot();

            yesChoiceAction = onYes;
            noChoiceAction = onNo;

            Button[] buttons = choiceRoot.GetComponentsInChildren<Button>(true);
            if (buttons.Length >= 2)
            {
                SetupChoiceButton(buttons[0], yesText, () => yesChoiceAction?.Invoke());
                SetupChoiceButton(buttons[1], noText, () => noChoiceAction?.Invoke());
            }

            choiceRoot.gameObject.SetActive(true);
            choiceRoot.SetAsLastSibling();
            choiceRoot.localScale = new Vector3(0.96f, 0.96f, 1f);
            choiceRoot.DOScale(Vector3.one, 0.12f).SetEase(Ease.OutCubic);

            if (EventSystem.current != null && buttons.Length > 0)
            {
                EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
            }
        }

        public void HideChoices()
        {
            yesChoiceAction = null;
            noChoiceAction = null;

            if (choiceRoot != null)
            {
                choiceRoot.DOKill();
                choiceRoot.gameObject.SetActive(false);
            }
        }

        private IEnumerator TypeLineByCharacter()
        {
            dialogueText.ForceMeshUpdate();
            int characterCount = dialogueText.textInfo.characterCount;

            if (characterCount <= 0)
            {
                CompleteTyping();
                yield break;
            }

            for (int i = 0; i <= characterCount; i++)
            {
                dialogueText.maxVisibleCharacters = i;

                if (i >= characterCount)
                {
                    break;
                }

                yield return new WaitForSeconds(GetTypingWaitForCharacter(i));
            }

            CompleteTyping();
        }

        private float GetTypingWaitForCharacter(int characterIndex)
        {
            if (dialogueText == null || characterIndex < 0 || characterIndex >= dialogueText.textInfo.characterCount)
            {
                return characterInterval;
            }

            char c = dialogueText.textInfo.characterInfo[characterIndex].character;
            switch (c)
            {
                case '.':
                case '!':
                case '?':
                case '。':
                case '！':
                case '？':
                    return characterInterval + periodPause;
                case ',':
                case ';':
                case ':':
                case '，':
                case '、':
                case '；':
                case '：':
                    return characterInterval + commaPause;
                default:
                    return characterInterval;
            }
        }

        private void CompleteTyping()
        {
            typingRoutine = null;
            isTyping = false;

            if (dialogueText != null)
            {
                dialogueText.text = currentLine;
                dialogueText.maxVisibleCharacters = int.MaxValue;
            }

            ShowArrow();
        }

        private void StopTypingRoutine()
        {
            if (typingRoutine == null)
            {
                return;
            }

            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        private void ShowArrow()
        {
            if (nextArrow == null)
            {
                return;
            }

            nextArrow.SetActive(true);
            arrowSeq?.Kill();

            RectTransform rt = nextArrow.GetComponent<RectTransform>();
            if (rt != null)
            {
                float baseY = rt.anchoredPosition.y;
                arrowSeq = DOTween.Sequence()
                    .Append(rt.DOAnchorPosY(baseY - 3f, 0.4f).SetEase(Ease.InOutSine))
                    .Append(rt.DOAnchorPosY(baseY, 0.4f).SetEase(Ease.InOutSine))
                    .SetLoops(-1);
            }
        }

        private void HideArrow()
        {
            arrowSeq?.Kill();
            if (nextArrow != null)
            {
                nextArrow.SetActive(false);
            }
        }

        private void EnsureChoiceRoot()
        {
            if (choiceRoot != null)
            {
                return;
            }

            RectTransform parent = dialoguePanel != null
                ? dialoguePanel
                : transform as RectTransform;

            GameObject root = new GameObject("ChoiceButtons", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            choiceRoot = root.GetComponent<RectTransform>();
            choiceRoot.anchorMin = new Vector2(1f, 0.5f);
            choiceRoot.anchorMax = new Vector2(1f, 0.5f);
            choiceRoot.pivot = new Vector2(1f, 0.5f);
            choiceRoot.anchoredPosition = new Vector2(-36f, 0f);
            choiceRoot.sizeDelta = new Vector2(286f, 56f);

            HorizontalLayoutGroup layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateChoiceButton("YesButton");
            CreateChoiceButton("NoButton");
            root.SetActive(false);
        }

        private Button CreateChoiceButton(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(choiceRoot, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(132f, 52f);

            LayoutElement layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 132f;
            layoutElement.preferredHeight = 52f;
            layoutElement.minWidth = 120f;
            layoutElement.minHeight = 48f;

            Image image = go.GetComponent<Image>();
            image.color = ClassicPixelUiTheme.WindowBlack;

            Button button = go.GetComponent<Button>();
            ClassicPixelUiTheme.ApplyShopButton(button, ClassicPixelUiTheme.ShopPanelAccent.Gold);

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22f;
            label.fontStyle = FontStyles.Bold;
            label.color = ClassicPixelUiTheme.Text;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            ClassicPixelUiTheme.ApplyText(label);

            return button;
        }

        private static void SetupChoiceButton(Button button, string label, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
            {
                text.text = label;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void DeactivateIfAlive()
        {
            if (this != null && gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }
    }
}

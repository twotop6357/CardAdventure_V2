using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// Yume Nikki/RPG Maker 계열의 검은 픽셀 UI를 위한 공통 색상과 적용 헬퍼.
    /// 기존 UI 생성 코드와 에디터 일괄 스타일러가 함께 사용한다.
    /// </summary>
    public static class ClassicPixelUiTheme
    {
        public static readonly Color WindowBlack = new Color(0.015f, 0.012f, 0.010f, 0.96f);
        public static readonly Color InnerBlack = new Color(0.000f, 0.000f, 0.000f, 0.96f);
        public static readonly Color DimBlack = new Color(0.000f, 0.000f, 0.000f, 0.62f);
        public static readonly Color Gold = new Color(0.86f, 0.75f, 0.08f, 1f);
        public static readonly Color DarkGold = new Color(0.33f, 0.27f, 0.04f, 1f);
        public static readonly Color Blue = new Color(0.08f, 0.30f, 0.52f, 1f);
        public static readonly Color Cyan = new Color(0.02f, 0.66f, 0.82f, 1f);
        public static readonly Color Text = new Color(0.96f, 0.96f, 0.92f, 1f);
        public static readonly Color MutedText = new Color(0.72f, 0.74f, 0.74f, 1f);
        public static readonly Color Danger = new Color(0.92f, 0.18f, 0.16f, 1f);
        public static readonly Color HpFill = new Color(0.86f, 0.10f, 0.10f, 1f);
        public static readonly Color Energy = new Color(0.98f, 0.86f, 0.20f, 1f);

        public static void ApplyPanel(Image image, bool blueBorder = false)
        {
            if (image == null)
            {
                return;
            }

            image.color = Color.clear;
            image.raycastTarget = true;
            image.sprite = null;
            RemoveLegacyEffects(image.gameObject);
            EnsureFrame(image.rectTransform, blueBorder);
        }

        public static void ApplyInnerPanel(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.color = Color.clear;
            image.sprite = null;
            RemoveLegacyEffects(image.gameObject);
            EnsureFrame(image.rectTransform, false, false);
        }

        public static void ApplyText(TextMeshProUGUI text, bool muted = false)
        {
            if (text == null)
            {
                return;
            }

            text.color = muted ? MutedText : Text;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
        }

        public static void ApplyButton(Button button)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            ApplyPanel(image);
            if (image != null)
            {
                Graphic generatedSurface = GetGeneratedSurface(image.rectTransform);
                button.targetGraphic = generatedSurface != null ? generatedSurface : image;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = WindowBlack;
            colors.highlightedColor = new Color(0.08f, 0.17f, 0.24f, 1f);
            colors.pressedColor = new Color(0.18f, 0.14f, 0.02f, 1f);
            colors.selectedColor = new Color(0.10f, 0.22f, 0.30f, 1f);
            colors.disabledColor = new Color(0.05f, 0.05f, 0.05f, 0.55f);
            button.colors = colors;

            foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                ApplyText(label);
            }
        }

        public static Outline AddOutline(GameObject target, Color color, Vector2 distance)
        {
            if (target == null)
            {
                return null;
            }

            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
            return outline;
        }

        public static ClassicPixelFrame EnsureFrame(RectTransform rect, bool blueAccent = false, bool innerLine = true)
        {
            if (rect == null)
            {
                return null;
            }

            ClassicPixelFrame frame = rect.GetComponent<ClassicPixelFrame>();
            if (frame == null)
            {
                frame = rect.gameObject.AddComponent<ClassicPixelFrame>();
            }

            frame.Configure(WindowBlack, Gold, blueAccent ? Cyan : Blue, 2f, 1f, 4f, innerLine);
            return frame;
        }

        /// <summary>
        /// ShopUI 와 동일한 단순 스타일을 Image 패널에 적용한다.
        /// ClassicPixelFrame 의 12개 자식 구조와 컴포넌트를 정리한 뒤, 검은 배경 + Outline 컴포넌트만 남긴다.
        /// </summary>
        public static Outline ApplyShopPanel(Image image, ShopPanelAccent accent = ShopPanelAccent.Gold, bool useInnerBlack = false)
        {
            if (image == null)
            {
                return null;
            }

            ClearGeneratedPanel(image.rectTransform);

            image.sprite = null;
            image.color = useInnerBlack ? InnerBlack : WindowBlack;
            image.raycastTarget = true;

            switch (accent)
            {
                case ShopPanelAccent.None:
                    RemoveOutline(image.gameObject);
                    return null;
                case ShopPanelAccent.Blue:
                    return EnsureOutline(image.gameObject, Blue, new Vector2(2f, -2f));
                case ShopPanelAccent.Cyan:
                    return EnsureOutline(image.gameObject, Cyan, new Vector2(2f, -2f));
                case ShopPanelAccent.Gold:
                default:
                    return EnsureOutline(image.gameObject, Gold, new Vector2(3f, -3f));
            }
        }

        /// <summary>ShopUI 단순 스타일의 버튼 적용.</summary>
        public static void ApplyShopButton(Button button, ShopPanelAccent accent = ShopPanelAccent.Gold)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            ApplyShopPanel(image, accent);
            if (image != null)
            {
                button.targetGraphic = image;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = WindowBlack;
            colors.highlightedColor = new Color(0.08f, 0.17f, 0.24f, 1f);
            colors.pressedColor = new Color(0.18f, 0.14f, 0.02f, 1f);
            colors.selectedColor = new Color(0.10f, 0.22f, 0.30f, 1f);
            colors.disabledColor = new Color(0.05f, 0.05f, 0.05f, 0.55f);
            button.colors = colors;

            foreach (TextMeshProUGUI label in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                ApplyText(label);
            }
        }

        public enum ShopPanelAccent
        {
            None,
            Gold,
            Blue,
            Cyan,
        }

        private static Outline EnsureOutline(GameObject target, Color color, Vector2 distance)
        {
            if (target == null)
            {
                return null;
            }

            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
            return outline;
        }

        private static void RemoveOutline(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            foreach (Outline outline in target.GetComponents<Outline>())
            {
                DestroyObject(outline);
            }
        }

        public static void ClearGeneratedPanel(RectTransform rect)
        {
            ClassicPixelFrame.ClearGenerated(rect);
        }

        public static Graphic GetGeneratedSurface(RectTransform rect)
        {
            if (rect == null)
            {
                return null;
            }

            Transform surfaceRoot = rect.Find("__ClassicPixelSurface");
            Transform surface = surfaceRoot != null ? surfaceRoot.Find("Surface") : null;
            return surface != null ? surface.GetComponent<Graphic>() : null;
        }

        private static void RemoveLegacyEffects(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            foreach (Outline outline in target.GetComponents<Outline>())
            {
                DestroyObject(outline);
            }

            foreach (Shadow shadow in target.GetComponents<Shadow>())
            {
                DestroyObject(shadow);
            }
        }

        private static void DestroyObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}

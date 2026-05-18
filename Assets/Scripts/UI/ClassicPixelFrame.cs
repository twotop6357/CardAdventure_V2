using UnityEngine;
using UnityEngine.UI;
using TheraBytes.BetterUi;

namespace CardAdventure
{
    /// <summary>
    /// 기준 이미지처럼 검은 창 위에 실제 픽셀 테두리 오브젝트를 생성한다.
    /// 색만 바꾸는 방식이 아니라 자식 Image들을 만들어 외곽선, 안쪽선, 코너 픽셀을 구성한다.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ClassicPixelFrame : MonoBehaviour
    {
        private const string RootName = "__ClassicPixelFrame";
        private const string SurfaceName = "__ClassicPixelSurface";
        private const string BuiltInSpritePath = "UI/Skin/UISprite.psd";

        [SerializeField] private Color surfaceColor = new Color(0.015f, 0.012f, 0.010f, 0.96f);
        [SerializeField] private Color outerColor = new Color(0.86f, 0.75f, 0.08f, 1f);
        [SerializeField] private Color innerColor = new Color(0.08f, 0.30f, 0.52f, 1f);
        [SerializeField] private float outerThickness = 2f;
        [SerializeField] private float innerThickness = 1f;
        [SerializeField] private float innerInset = 4f;
        [SerializeField] private bool includeInnerLine = true;

        public void Configure(Color surface, Color outer, Color inner, float outerSize = 2f, float innerSize = 1f, float inset = 4f, bool innerLine = true)
        {
            surfaceColor = surface;
            outerColor = outer;
            innerColor = inner;
            outerThickness = outerSize;
            innerThickness = innerSize;
            innerInset = inset;
            includeInnerLine = innerLine;
            Rebuild();
        }

        public void Rebuild()
        {
            RectTransform owner = transform as RectTransform;
            if (owner == null)
            {
                return;
            }

            Transform surfaceRoot = EnsureRoot(SurfaceName);
            RectTransform surfaceRect = surfaceRoot.GetComponent<RectTransform>();
            Stretch(surfaceRect, 0f);
            surfaceRoot.SetAsFirstSibling();

            BetterImage surface = EnsureBetterImage(surfaceRoot, "Surface", surfaceColor);
            surface.type = Image.Type.Sliced;
            Stretch(surface.rectTransform, 0f);

            Transform root = EnsureRoot(RootName);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect, 0f);
            root.SetAsLastSibling();

            EnsureLine(root, "OuterTop", outerColor, Anchor.Top, outerThickness, 0f);
            EnsureLine(root, "OuterBottom", outerColor, Anchor.Bottom, outerThickness, 0f);
            EnsureLine(root, "OuterLeft", outerColor, Anchor.Left, outerThickness, 0f);
            EnsureLine(root, "OuterRight", outerColor, Anchor.Right, outerThickness, 0f);

            EnsureCorner(root, "CornerLT", outerColor, new Vector2(0f, 1f), new Vector2(outerThickness * 3f, outerThickness * 3f));
            EnsureCorner(root, "CornerRT", outerColor, new Vector2(1f, 1f), new Vector2(outerThickness * 3f, outerThickness * 3f));
            EnsureCorner(root, "CornerLB", outerColor, new Vector2(0f, 0f), new Vector2(outerThickness * 3f, outerThickness * 3f));
            EnsureCorner(root, "CornerRB", outerColor, new Vector2(1f, 0f), new Vector2(outerThickness * 3f, outerThickness * 3f));

            SetActive(root, "InnerTop", includeInnerLine);
            SetActive(root, "InnerBottom", includeInnerLine);
            SetActive(root, "InnerLeft", includeInnerLine);
            SetActive(root, "InnerRight", includeInnerLine);

            if (includeInnerLine)
            {
                EnsureLine(root, "InnerTop", innerColor, Anchor.Top, innerThickness, innerInset);
                EnsureLine(root, "InnerBottom", innerColor, Anchor.Bottom, innerThickness, innerInset);
                EnsureLine(root, "InnerLeft", innerColor, Anchor.Left, innerThickness, innerInset);
                EnsureLine(root, "InnerRight", innerColor, Anchor.Right, innerThickness, innerInset);
            }
        }

        public static void ClearGenerated(RectTransform owner)
        {
            if (owner == null)
            {
                return;
            }

            DestroyChild(owner, SurfaceName);
            DestroyChild(owner, RootName);

            ClassicPixelFrame frame = owner.GetComponent<ClassicPixelFrame>();
            if (frame != null)
            {
                DestroyObject(frame);
            }
        }

        private Transform EnsureRoot(string rootName)
        {
            Transform root = transform.Find(rootName);
            if (root == null)
            {
                GameObject rootObject = new GameObject(rootName, typeof(RectTransform));
                rootObject.transform.SetParent(transform, false);
                root = rootObject.transform;
            }

            return root;
        }

        private static void SetActive(Transform root, string childName, bool active)
        {
            Transform child = root.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        private static BetterImage EnsureBetterImage(Transform root, string childName, Color color)
        {
            Transform child = root.Find(childName);
            if (child == null)
            {
                GameObject go = new GameObject(childName, typeof(RectTransform), typeof(BetterImage));
                go.transform.SetParent(root, false);
                child = go.transform;
            }

            Image oldImage = child.GetComponent<Image>();
            BetterImage image = child.GetComponent<BetterImage>();
            if (image == null && oldImage != null)
            {
                DestroyObject(oldImage);
                image = child.gameObject.AddComponent<BetterImage>();
            }
            else if (image == null)
            {
                image = child.gameObject.AddComponent<BetterImage>();
            }

            image.sprite = GetBuiltInUiSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite GetBuiltInUiSprite()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>(BuiltInSpritePath);
#else
            return Resources.GetBuiltinResource<Sprite>(BuiltInSpritePath);
#endif
        }

        private static void EnsureLine(Transform root, string childName, Color color, Anchor anchor, float thickness, float inset)
        {
            BetterImage image = EnsureBetterImage(root, childName, color);
            RectTransform rect = image.rectTransform;

            switch (anchor)
            {
                case Anchor.Top:
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.offsetMin = new Vector2(inset, -inset - thickness);
                    rect.offsetMax = new Vector2(-inset, -inset);
                    break;
                case Anchor.Bottom:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.offsetMin = new Vector2(inset, inset);
                    rect.offsetMax = new Vector2(-inset, inset + thickness);
                    break;
                case Anchor.Left:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.offsetMin = new Vector2(inset, inset);
                    rect.offsetMax = new Vector2(inset + thickness, -inset);
                    break;
                case Anchor.Right:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 0.5f);
                    rect.offsetMin = new Vector2(-inset - thickness, inset);
                    rect.offsetMax = new Vector2(-inset, -inset);
                    break;
            }
        }

        private static void EnsureCorner(Transform root, string childName, Color color, Vector2 anchor, Vector2 size)
        {
            BetterImage image = EnsureBetterImage(root, childName, color);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void DestroyChild(RectTransform owner, string childName)
        {
            Transform child = owner.Find(childName);
            if (child != null)
            {
                DestroyObject(child.gameObject);
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

        private enum Anchor
        {
            Top,
            Bottom,
            Left,
            Right
        }
    }
}

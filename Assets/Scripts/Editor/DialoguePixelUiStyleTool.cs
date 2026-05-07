using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure.Editor
{
    public static class DialoguePixelUiStyleTool
    {
        private const string PixelUiPath = "Assets/Assets/DEVNIK 2D/2D UI PIXEL BUTTONS/UI SIMPLE PIXEL UNSPLIT.png";
        private const string PanelSpriteName = "BG_BAR2";
        private const string NameBoxSpriteName = "SET_BAR";

        [MenuItem("CardAdventure/UI/Apply DEVNIK Dialogue Window")]
        public static void Apply()
        {
            ConfigurePixelImporter();
            ApplyToOpenScene();
        }

        private static void ConfigurePixelImporter()
        {
            TextureImporter importer = AssetImporter.GetAtPath(PixelUiPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[DialoguePixelUiStyleTool] DEVNIK UI texture not found: " + PixelUiPath);
                return;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                changed = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }
            if (importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                changed = true;
            }
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, 100f))
            {
                importer.spritePixelsPerUnit = 100f;
                changed = true;
            }

#pragma warning disable 0618
            SpriteMetaData[] sheet = importer.spritesheet;
            for (int i = 0; i < sheet.Length; i++)
            {
                SpriteMetaData meta = sheet[i];
                Vector4 border = GetBorder(meta.name);
                if (meta.border != border)
                {
                    meta.border = border;
                    changed = true;
                }
                sheet[i] = meta;
            }

            if (changed)
            {
                importer.spritesheet = sheet;
                importer.SaveAndReimport();
            }
#pragma warning restore 0618
        }

        private static Vector4 GetBorder(string spriteName)
        {
            switch (spriteName)
            {
                case "BG_BAR2":
                case "BG_UI":
                    return new Vector4(32f, 32f, 32f, 32f);
                case "SET_BAR":
                case "PLAY BAR":
                case "LEVEL_BAR":
                case "EXIT_BAR":
                case "BAR1":
                case "BAR2":
                case "BAR3":
                case "BAR4":
                    return new Vector4(28f, 24f, 28f, 24f);
                case "BAR_BG":
                case "BAR_OUTLINE":
                    return new Vector4(16f, 8f, 16f, 8f);
                default:
                    return Vector4.zero;
            }
        }

        private static void ApplyToOpenScene()
        {
            GameObject panel = GameObject.Find("DialoguePanel");
            if (panel == null)
            {
                Debug.LogError("[DialoguePixelUiStyleTool] DialoguePanel not found in the open scene.");
                return;
            }

            Sprite panelSprite = LoadSprite(PanelSpriteName);
            Sprite nameBoxSprite = LoadSprite(NameBoxSpriteName);

            RectTransform panelRt = panel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                panelRt.anchorMin = new Vector2(0f, 0f);
                panelRt.anchorMax = new Vector2(1f, 0f);
                panelRt.pivot = new Vector2(0.5f, 0f);
                panelRt.anchoredPosition = Vector2.zero;
                panelRt.sizeDelta = new Vector2(0f, 156f);
            }

            Transform bg = panel.transform.Find("PanelBg");
            if (bg != null)
            {
                RectTransform bgRt = bg.GetComponent<RectTransform>();
                if (bgRt != null)
                {
                    bgRt.anchorMin = Vector2.zero;
                    bgRt.anchorMax = Vector2.one;
                    bgRt.offsetMin = new Vector2(24f, 10f);
                    bgRt.offsetMax = new Vector2(-24f, -10f);
                }

                Image bgImage = bg.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.sprite = panelSprite;
                    bgImage.type = Image.Type.Sliced;
                    bgImage.fillCenter = true;
                    bgImage.pixelsPerUnitMultiplier = 1f;
                    bgImage.color = new Color(1f, 1f, 1f, 0.98f);
                    bgImage.raycastTarget = false;
                }

                Outline outline = bg.GetComponent<Outline>();
                if (outline != null)
                {
                    Object.DestroyImmediate(outline);
                }
            }

            Transform nameBox = panel.transform.Find("NameBox");
            if (nameBox != null)
            {
                RectTransform nameBoxRt = nameBox.GetComponent<RectTransform>();
                if (nameBoxRt != null)
                {
                    nameBoxRt.anchorMin = new Vector2(0f, 1f);
                    nameBoxRt.anchorMax = new Vector2(0f, 1f);
                    nameBoxRt.pivot = new Vector2(0f, 0f);
                    nameBoxRt.anchoredPosition = new Vector2(44f, -4f);
                    nameBoxRt.sizeDelta = new Vector2(190f, 40f);
                }

                Image nameBoxImage = nameBox.GetComponent<Image>();
                if (nameBoxImage != null)
                {
                    nameBoxImage.sprite = nameBoxSprite;
                    nameBoxImage.type = Image.Type.Sliced;
                    nameBoxImage.fillCenter = true;
                    nameBoxImage.pixelsPerUnitMultiplier = 1f;
                    nameBoxImage.color = Color.white;
                    nameBoxImage.raycastTarget = false;
                }
            }

            Transform nameText = nameBox != null ? nameBox.Find("NameText") : null;
            if (nameText != null)
            {
                RectTransform nameTextRt = nameText.GetComponent<RectTransform>();
                if (nameTextRt != null)
                {
                    nameTextRt.offsetMin = new Vector2(16f, 7f);
                    nameTextRt.offsetMax = new Vector2(-16f, -7f);
                }

                TextMeshProUGUI tmp = nameText.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 16f;
                    tmp.color = Color.white;
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                }
            }

            Transform dialogueText = panel.transform.Find("DialogueText");
            if (dialogueText != null)
            {
                RectTransform textRt = dialogueText.GetComponent<RectTransform>();
                if (textRt != null)
                {
                    textRt.offsetMin = new Vector2(44f, 26f);
                    textRt.offsetMax = new Vector2(-56f, -28f);
                }

                TextMeshProUGUI tmp = dialogueText.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 17f;
                    tmp.color = new Color(0.08f, 0.07f, 0.06f, 1f);
                }
            }

            Transform arrow = panel.transform.Find("NextArrow");
            if (arrow != null)
            {
                RectTransform arrowRt = arrow.GetComponent<RectTransform>();
                if (arrowRt != null)
                {
                    arrowRt.anchoredPosition = new Vector2(-42f, 24f);
                    arrowRt.sizeDelta = new Vector2(24f, 24f);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[DialoguePixelUiStyleTool] DEVNIK pixel dialogue window applied and scene saved.");
        }

        private static Sprite LoadSprite(string spriteName)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(PixelUiPath)
                .OfType<Sprite>()
                .FirstOrDefault(item => item.name == spriteName);

            if (sprite == null)
            {
                Debug.LogError("[DialoguePixelUiStyleTool] Sprite not found: " + spriteName);
            }

            return sprite;
        }
    }
}

using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardAdventure.Editor
{
    /// <summary>
    /// 현재 프로젝트의 자체 UI 씬/프리팹을 기준 이미지 스타일로 일괄 정리한다.
    /// 메뉴: CardAdventure/UI/Apply Classic Pixel UI To Project
    /// </summary>
    public static class ClassicPixelUiStyleTool
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/AdventureScene.unity",
            "Assets/Scenes/BattleTest.unity",
        };

        private static readonly string[] PrefabPaths =
        {
            "Assets/Prefabs/UI/Card.prefab",
            "Assets/Prefabs/UI/CardView.prefab",
            "Assets/Prefabs/UI/DialogueCanvas.prefab",
            "Assets/Prefabs/UI/JobSelection.prefab",
            "Assets/Prefabs/UI/StatusIcon.prefab",
        };

        [MenuItem("CardAdventure/UI/Apply Classic Pixel UI To Project")]
        public static void ApplyToProject()
        {
            string activeScenePath = SceneManager.GetActiveScene().path;

            foreach (string scenePath in ScenePaths)
            {
                if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                ApplyToRoots(scene.GetRootGameObjects());
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            foreach (string prefabPath in PrefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                ApplyToRoots(new[] { root });
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }

            if (!string.IsNullOrEmpty(activeScenePath) && System.IO.File.Exists(activeScenePath))
            {
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ClassicPixelUiStyleTool] 전체 자체 UI에 클래식 픽셀 스타일을 적용했습니다.");
        }

        [MenuItem("CardAdventure/UI/Apply Classic Pixel UI To Open Scene")]
        public static void ApplyToOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            ApplyToRoots(scene.GetRootGameObjects());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ClassicPixelUiStyleTool] 현재 씬에 클래식 픽셀 스타일을 적용했습니다: {scene.path}");
        }

        [MenuItem("CardAdventure/UI/Apply Classic Pixel UI To UI Prefabs")]
        public static void ApplyToUiPrefabs()
        {
            foreach (string prefabPath in PrefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                ApplyToRoots(new[] { root });
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ClassicPixelUiStyleTool] UI 프리팹에 클래식 픽셀 스타일을 적용했습니다.");
        }

        private static void ApplyToRoots(GameObject[] roots)
        {
            foreach (GameObject root in roots)
            {
                ApplyDialogueStructure(root);

                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    if (IsGeneratedFrameImage(image))
                    {
                        continue;
                    }

                    ApplyImage(image);
                }

                foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    ApplyText(text);
                }

                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    ClassicPixelUiTheme.ApplyButton(button);
                }

                foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
                {
                    ApplySlider(slider);
                }
            }
        }

        private static void ApplyDialogueStructure(GameObject root)
        {
            Transform panel = FindChild(root.transform, "DialoguePanel");
            if (panel == null)
            {
                return;
            }

            RectTransform panelRt = panel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                panelRt.anchorMin = new Vector2(0f, 0f);
                panelRt.anchorMax = new Vector2(1f, 0f);
                panelRt.pivot = new Vector2(0.5f, 0f);
                panelRt.anchoredPosition = Vector2.zero;
                panelRt.sizeDelta = new Vector2(0f, 156f);
            }

            Transform bg = panel.Find("PanelBg");
            if (bg != null)
            {
                RectTransform bgRt = bg.GetComponent<RectTransform>();
                if (bgRt != null)
                {
                    bgRt.anchorMin = Vector2.zero;
                    bgRt.anchorMax = Vector2.one;
                    bgRt.offsetMin = new Vector2(10f, 6f);
                    bgRt.offsetMax = new Vector2(-10f, -6f);
                }
            }

            Transform portraitFrame = panel.Find("PortraitFrame");
            if (portraitFrame == null)
            {
                GameObject frame = new GameObject("PortraitFrame", typeof(RectTransform), typeof(Image));
                frame.transform.SetParent(panel, false);
                portraitFrame = frame.transform;

                GameObject portrait = new GameObject("PortraitImage", typeof(RectTransform), typeof(Image));
                portrait.transform.SetParent(portraitFrame, false);
            }

            RectTransform frameRt = portraitFrame.GetComponent<RectTransform>();
            if (frameRt != null)
            {
                frameRt.anchorMin = new Vector2(0f, 0.5f);
                frameRt.anchorMax = new Vector2(0f, 0.5f);
                frameRt.pivot = new Vector2(0f, 0.5f);
                frameRt.anchoredPosition = new Vector2(24f, 0f);
                frameRt.sizeDelta = new Vector2(126f, 126f);
            }

            Image frameImage = portraitFrame.GetComponent<Image>();
            ClassicPixelUiTheme.ApplyPanel(frameImage);

            Transform portraitImage = portraitFrame.Find("PortraitImage");
            if (portraitImage != null)
            {
                RectTransform portraitRt = portraitImage.GetComponent<RectTransform>();
                if (portraitRt != null)
                {
                    portraitRt.anchorMin = Vector2.zero;
                    portraitRt.anchorMax = Vector2.one;
                    portraitRt.offsetMin = new Vector2(6f, 6f);
                    portraitRt.offsetMax = new Vector2(-6f, -6f);
                }
            }

            Transform dialogueText = panel.Find("DialogueText");
            if (dialogueText != null)
            {
                RectTransform textRt = dialogueText.GetComponent<RectTransform>();
                if (textRt != null)
                {
                    textRt.anchorMin = Vector2.zero;
                    textRt.anchorMax = Vector2.one;
                    textRt.offsetMin = new Vector2(174f, 26f);
                    textRt.offsetMax = new Vector2(-44f, -24f);
                }
            }

            DialogueView view = panel.GetComponent<DialogueView>();
            if (view != null)
            {
                SerializedObject so = new SerializedObject(view);
                so.FindProperty("dialoguePanel").objectReferenceValue = panelRt;
                so.FindProperty("canvasGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();
                so.FindProperty("portraitRoot").objectReferenceValue = portraitFrame.gameObject;
                so.FindProperty("portraitImage").objectReferenceValue =
                    portraitImage != null ? portraitImage.GetComponent<Image>() : null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ApplyImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            if (IsGeneratedClassicPixelImage(image))
            {
                return;
            }

            if (IsBattleSceneBackground(image))
            {
                RestoreBattleSceneBackground(image);
                return;
            }

            if (ShouldKeepArtImage(image))
            {
                return;
            }

            string name = image.gameObject.name;
            if (IsOverlay(name))
            {
                Color c = ClassicPixelUiTheme.DimBlack;
                if (name.IndexOf("DamageFlash", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    c = new Color(1f, 0f, 0f, 0f);
                }
                image.color = c;
                return;
            }

            if (name.IndexOf("NameBox", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Header", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Intent", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ClassicPixelUiTheme.ApplyPanel(image, true);
                return;
            }

            if (LooksLikePanel(name))
            {
                ClassicPixelUiTheme.ApplyPanel(image);
            }
        }

        private static void ApplyText(TextMeshProUGUI text)
        {
            if (text == null)
            {
                return;
            }

            ClassicPixelUiTheme.ApplyText(text, IsMutedText(text.gameObject.name));

            if (text.gameObject.name.IndexOf("Arrow", StringComparison.OrdinalIgnoreCase) >= 0
                || text.text.Contains("▼"))
            {
                text.color = ClassicPixelUiTheme.Cyan;
            }
        }

        private static void ApplySlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            Image target = slider.targetGraphic as Image;
            ClassicPixelUiTheme.ApplyInnerPanel(target);

            if (slider.fillRect != null)
            {
                Image fill = slider.fillRect.GetComponent<Image>();
                if (fill != null)
                {
                    fill.color = ClassicPixelUiTheme.HpFill;
                }
            }
        }

        private static bool LooksLikePanel(string name)
        {
            string[] markers =
            {
                "Panel", "Bg", "Background", "Box", "Frame", "Button", "Footer",
                "Offer", "Scroll", "Hud", "HUD", "Pill", "Badge", "Bar", "Block"
            };
            return markers.Any(marker => name.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsGeneratedFrameImage(Image image)
        {
            return IsGeneratedClassicPixelImage(image);
        }

        private static bool IsGeneratedClassicPixelImage(Image image)
        {
            Transform parent = image != null ? image.transform.parent : null;
            while (parent != null)
            {
                if (parent.name == "__ClassicPixelFrame" || parent.name == "__ClassicPixelSurface")
                {
                    return true;
                }

                parent = parent.parent;
            }

            return false;
        }

        private static bool IsBattleSceneBackground(Image image)
        {
            if (image == null || !string.Equals(image.gameObject.name, "Background", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Canvas canvas = image.GetComponentInParent<Canvas>(true);
            if (canvas == null || !string.Equals(canvas.gameObject.name, "BattleCanvas", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            RectTransform rect = image.rectTransform;
            bool fullStretch = rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one;
            return fullStretch && SceneManager.GetActiveScene().path.EndsWith("BattleTest.unity", StringComparison.OrdinalIgnoreCase);
        }

        private static void RestoreBattleSceneBackground(Image image)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Assets/Sprites/BattleBackground/CrowBattleBackground.png");
            if (sprite != null)
            {
                image.sprite = sprite;
            }

            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            ClassicPixelUiTheme.ClearGeneratedPanel(image.rectTransform);

            foreach (Outline outline in image.GetComponents<Outline>())
            {
                UnityEngine.Object.DestroyImmediate(outline);
            }

            foreach (Shadow shadow in image.GetComponents<Shadow>())
            {
                UnityEngine.Object.DestroyImmediate(shadow);
            }
        }

        private static bool ShouldKeepArtImage(Image image)
        {
            string name = image.gameObject.name;
            string[] markers =
            {
                "PortraitImage", "CardIcon", "CardArt", "CardImage", "EnemyImage",
                "PlayerAvatar", "CharacterPreview", "IconImage", "IntentIcon",
                "NpcPortraitImage", "Preview", "ArrowHead", "Segment"
            };

            return markers.Any(marker => name.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                || image.sprite != null && !LooksLikePanel(name);
        }

        private static bool IsOverlay(string name)
        {
            return name.IndexOf("Dim", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Overlay", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("FadeMask", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("DamageFlash", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMutedText(string name)
        {
            return name.IndexOf("Subtitle", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Desc", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Message", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
    /// 프로젝트 자체 UI(대화창, 직업 변경창, 카드, 전투 HUD, 상태 아이콘, 배틀 보상, 직업 선택 등)에
    /// ShopUIController 의 단순 검은 패널 + Gold/Blue Outline 스타일을 일괄 적용한다.
    /// ClassicPixelFrame 의 12개 자식 구조와 컴포넌트를 정리해 "상점과 동일한 단순 스타일"로 통일한다.
    /// </summary>
    public static class ShopStyleUiTool
    {
        private static readonly string[] TargetScenes =
        {
            "Assets/Scenes/AdventureScene.unity",
            "Assets/Scenes/BattleTest.unity",
        };

        private static readonly string[] TargetPrefabs =
        {
            "Assets/Prefabs/UI/Card.prefab",
            "Assets/Prefabs/UI/CardView.prefab",
            "Assets/Prefabs/UI/DialogueCanvas.prefab",
            "Assets/Prefabs/UI/StatusIcon.prefab",
        };

        [MenuItem("CardAdventure/UI/Apply Shop Style To Project")]
        public static void ApplyToProject()
        {
            string activeScenePath = SceneManager.GetActiveScene().path;
            int totalCleanedFrames = 0;
            int totalStyledPanels = 0;

            foreach (string prefabPath in TargetPrefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                int cleaned = 0, styled = 0;
                ApplyToRoot(root, ref cleaned, ref styled);
                totalCleanedFrames += cleaned;
                totalStyledPanels += styled;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log($"[ShopStyleUiTool] {prefabPath} — ClassicPixelFrame 제거 {cleaned}, 패널 재스타일 {styled}");
            }

            foreach (string scenePath in TargetScenes)
            {
                if (string.IsNullOrEmpty(scenePath) || !System.IO.File.Exists(scenePath))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int cleaned = 0, styled = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    ApplyToRoot(root, ref cleaned, ref styled);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                totalCleanedFrames += cleaned;
                totalStyledPanels += styled;
                Debug.Log($"[ShopStyleUiTool] {scenePath} — ClassicPixelFrame 제거 {cleaned}, 패널 재스타일 {styled}");
            }

            if (!string.IsNullOrEmpty(activeScenePath) && System.IO.File.Exists(activeScenePath))
            {
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ShopStyleUiTool] ✅ 전체 자체 UI 상점 스타일 적용 완료. 총 ClassicPixelFrame 제거 {totalCleanedFrames}, 패널 재스타일 {totalStyledPanels}");
        }

        [MenuItem("CardAdventure/UI/Apply Shop Style To Open Scene")]
        public static void ApplyToOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            int cleaned = 0, styled = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                ApplyToRoot(root, ref cleaned, ref styled);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShopStyleUiTool] 현재 씬({scene.path}) 적용 완료. ClassicPixelFrame 제거 {cleaned}, 패널 재스타일 {styled}");
        }

        // ══════════════════════════════════════════════════════════════
        //  핵심 로직
        // ══════════════════════════════════════════════════════════════

        private static void ApplyToRoot(GameObject root, ref int cleanedFrames, ref int styledPanels)
        {
            if (root == null)
            {
                return;
            }

            // 1) 모든 ClassicPixelFrame 컴포넌트와 생성된 자식(__ClassicPixelFrame/__ClassicPixelSurface)을 수집.
            List<ClassicPixelFrame> frames = new List<ClassicPixelFrame>(
                root.GetComponentsInChildren<ClassicPixelFrame>(true));

            // 2) 프레임을 가지고 있던 Image 들을 상점 스타일로 재적용.
            foreach (ClassicPixelFrame frame in frames)
            {
                if (frame == null || frame.gameObject == null)
                {
                    continue;
                }

                RectTransform rect = frame.transform as RectTransform;
                Image image = frame.GetComponent<Image>();
                bool wasBlueAccent = frame.gameObject.name.IndexOf("NameBox", StringComparison.OrdinalIgnoreCase) >= 0
                                    || frame.gameObject.name.IndexOf("Pill", StringComparison.OrdinalIgnoreCase) >= 0
                                    || frame.gameObject.name.IndexOf("Header", StringComparison.OrdinalIgnoreCase) >= 0
                                    || frame.gameObject.name.IndexOf("Intent", StringComparison.OrdinalIgnoreCase) >= 0;

                // ClassicPixelFrame 제거(생성된 __ClassicPixelFrame/__ClassicPixelSurface 자식도 함께 정리)
                ClassicPixelFrame.ClearGenerated(rect);
                cleanedFrames++;

                if (image == null)
                {
                    // 만약 Image 가 없으면 추가.
                    image = frame.gameObject.GetComponent<Image>();
                    if (image == null && rect != null)
                    {
                        image = frame.gameObject.AddComponent<Image>();
                    }
                }

                if (image != null)
                {
                    ClassicPixelUiTheme.ShopPanelAccent accent = wasBlueAccent
                        ? ClassicPixelUiTheme.ShopPanelAccent.Blue
                        : ClassicPixelUiTheme.ShopPanelAccent.Gold;
                    ClassicPixelUiTheme.ApplyShopPanel(image, accent);
                    styledPanels++;
                }
            }

            // 3) 패널/박스/버튼류이지만 ClassicPixelFrame 없이 색만 적용돼 있는 Image 도 검은 패널로 정리.
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image == null || image.gameObject == null)
                {
                    continue;
                }

                if (IsGeneratedFrameChild(image))
                {
                    continue;
                }

                if (ShouldKeepArtImage(image))
                {
                    continue;
                }

                string name = image.gameObject.name;
                if (IsTransparentClickBlocker(image))
                {
                    image.sprite = null;
                    image.color = Color.clear;
                    image.raycastTarget = true;
                    RemoveOutline(image.gameObject);
                    continue;
                }

                if (IsOverlay(name))
                {
                    image.color = ClassicPixelUiTheme.DimBlack;
                    image.sprite = null;
                    RemoveOutline(image.gameObject);
                    continue;
                }

                if (!LooksLikePanel(name))
                {
                    continue;
                }

                bool blueAccent = name.IndexOf("NameBox", StringComparison.OrdinalIgnoreCase) >= 0
                                  || name.IndexOf("Pill", StringComparison.OrdinalIgnoreCase) >= 0
                                  || name.IndexOf("Header", StringComparison.OrdinalIgnoreCase) >= 0
                                  || name.IndexOf("Intent", StringComparison.OrdinalIgnoreCase) >= 0;
                bool useInnerBlack = name.IndexOf("Inner", StringComparison.OrdinalIgnoreCase) >= 0
                                     || name.IndexOf("Footer", StringComparison.OrdinalIgnoreCase) >= 0;

                ClassicPixelUiTheme.ShopPanelAccent accent = blueAccent
                    ? ClassicPixelUiTheme.ShopPanelAccent.Blue
                    : ClassicPixelUiTheme.ShopPanelAccent.Gold;

                ClassicPixelUiTheme.ApplyShopPanel(image, accent, useInnerBlack);
                styledPanels++;
            }

            // 4) 텍스트, 슬라이더, 버튼 후처리.
            foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                ClassicPixelUiTheme.ApplyText(text, IsMutedText(text.gameObject.name));
                if (text.gameObject.name.IndexOf("Arrow", StringComparison.OrdinalIgnoreCase) >= 0
                    || (text.text != null && text.text.Contains("▼")))
                {
                    text.color = ClassicPixelUiTheme.Cyan;
                }
            }

            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                ClassicPixelUiTheme.ApplyShopButton(button);
            }

            foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
            {
                Image target = slider.targetGraphic as Image;
                if (target != null)
                {
                    ClassicPixelUiTheme.ApplyShopPanel(target, ClassicPixelUiTheme.ShopPanelAccent.None, true);
                }

                if (slider.fillRect != null)
                {
                    Image fill = slider.fillRect.GetComponent<Image>();
                    if (fill != null)
                    {
                        fill.color = ClassicPixelUiTheme.HpFill;
                    }
                }
            }
        }

        private static bool IsGeneratedFrameChild(Image image)
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

        private static bool ShouldKeepArtImage(Image image)
        {
            string name = image.gameObject.name;
            string[] artMarkers =
            {
                "PortraitImage", "CardIcon", "CardArt", "CardImage", "EnemyImage",
                "PlayerAvatar", "CharacterPreview", "IconImage", "IntentIcon",
                "NpcPortraitImage", "Preview", "ArrowHead", "Segment", "JobImage",
                "Illust", "Thumbnail", "Sprite", "Logo"
            };

            // 명시적인 아트 노드만 보호. (sprite가 있다고 무조건 아트로 취급하지 않는다 — 기존
            // 디자인 sprite가 박힌 패널/박스 이미지를 상점 스타일로 갱신할 수 있어야 한다.)
            return artMarkers.Any(marker => name.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool LooksLikePanel(string name)
        {
            // 사실상 자체 UI 노드 중 아트가 아닌 모든 Image는 패널로 취급한다.
            // (JobDescription, JobStatsText, JobButton, JobChangeBackground 같은
            // 도메인 이름들은 별도 마커가 없어도 상점 스타일을 적용해야 한다.)
            string[] markers =
            {
                "Panel", "Bg", "BackGround", "Box", "Frame", "Button", "Footer",
                "Offer", "Scroll", "Hud", "HUD", "Pill", "Badge", "Bar", "Block",
                "Container", "Header", "Inner", "Outer", "Window", "Title",
                "Description", "Stats", "Job", "Dialogue", "Name", "Slot",
                "Group", "Row", "Cell", "Tab", "List"
            };
            return markers.Any(marker => name.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsOverlay(string name)
        {
            return name.IndexOf("Dim", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Overlay", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("FadeMask", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Blocker", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("DamageFlash", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsTransparentClickBlocker(Image image)
        {
            if (image == null)
            {
                return false;
            }

            return image.GetComponent<CardAdventure.UI.MyCardsPanelController>() != null
                || string.Equals(image.gameObject.name, "MyCardsPanel", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMutedText(string name)
        {
            return name.IndexOf("Subtitle", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Desc", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Message", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void RemoveOutline(GameObject target)
        {
            foreach (Outline outline in target.GetComponents<Outline>())
            {
                UnityEngine.Object.DestroyImmediate(outline);
            }
        }
    }
}
#endif

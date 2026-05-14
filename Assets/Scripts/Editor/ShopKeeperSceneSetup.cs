using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardAdventure.Editor
{
    public static class ShopKeeperSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/AdventureScene.unity";
        private const string DialoguePath = "Assets/ScriptableObjects/Dialogues/NPC_ShopKeeper_Dialogue.asset";
        private const string SpritePath = "Assets/Assets/Sprites/NPCs/ShopKeeper_Sprites.png";
        private const string CardsRoot = "Assets/ScriptableObjects/Cards";

        [MenuItem("CardAdventure/Setup Shop Keeper")]
        public static void Setup()
        {
            OpenAdventureSceneIfNeeded();

            DialogueData dialogue = EnsureDialogueData();
            Sprite sprite = LoadShopKeeperSprite();
            ShopUIController shopUI = EnsureShopUI();
            GameObject npc = EnsureShopKeeperNpc(sprite, dialogue, shopUI);

            EditorSceneManager.MarkSceneDirty(npc.scene);
            EditorSceneManager.SaveScene(npc.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ShopKeeperSceneSetup] 상점 NPC, 대화, 상점 UI, 판매 카드 풀 설정 완료.");
        }

        private static void OpenAdventureSceneIfNeeded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
        }

        private static DialogueData EnsureDialogueData()
        {
            EnsureFolder("Assets/ScriptableObjects", "Dialogues");

            DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(DialoguePath);
            if (dialogue == null)
            {
                dialogue = ScriptableObject.CreateInstance<DialogueData>();
                AssetDatabase.CreateAsset(dialogue, DialoguePath);
            }

            dialogue.speakerName = "상점 주인";
            dialogue.lines = new[]
            {
                "어서 오세요. 오늘도 쓸 만한 카드가 들어왔어요.",
                "마음에 드는 카드가 있으면 골라보세요."
            };
            EditorUtility.SetDirty(dialogue);
            return dialogue;
        }

        private static Sprite LoadShopKeeperSprite()
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(SpritePath)
                .OfType<Sprite>()
                .OrderBy(sprite => ExtractTrailingNumber(sprite.name))
                .ToArray();

            if (sprites.Length == 0)
            {
                Debug.LogError($"[ShopKeeperSceneSetup] ShopKeeper 스프라이트를 찾지 못했습니다: {SpritePath}");
                return null;
            }

            return sprites[Mathf.Min(1, sprites.Length - 1)];
        }

        private static int ExtractTrailingNumber(string name)
        {
            Match match = Regex.Match(name, @"(\d+)$");
            return match.Success ? int.Parse(match.Value) : 0;
        }

        private static ShopUIController EnsureShopUI()
        {
            GameObject uiObject = GameObject.Find("ShopUI");
            if (uiObject == null)
            {
                uiObject = new GameObject("ShopUI");
            }

            ShopUIController shopUI = uiObject.GetComponent<ShopUIController>();
            if (shopUI == null)
            {
                shopUI = uiObject.AddComponent<ShopUIController>();
            }

            SerializedObject serialized = new SerializedObject(shopUI);
            SerializedProperty cardPool = serialized.FindProperty("cardPool");
            List<CardData> cards = LoadAllCards();
            cardPool.arraySize = cards.Count;
            for (int i = 0; i < cards.Count; i++)
            {
                cardPool.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }

            serialized.FindProperty("offerCount").intValue = 3;
            serialized.FindProperty("commonPrice").intValue = 45;
            serialized.FindProperty("uncommonPrice").intValue = 70;
            serialized.FindProperty("rarePrice").intValue = 110;
            serialized.FindProperty("legendaryPrice").intValue = 180;
            serialized.FindProperty("cardViewPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BattleCardView>("Assets/Prefabs/UI/CardView.prefab");
            serialized.FindProperty("spriteLibrary").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<CardSpriteLibrary>("Assets/ScriptableObjects/CardSpriteLibrary.asset");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(shopUI);
            return shopUI;
        }

        private static GameObject EnsureShopKeeperNpc(Sprite sprite, DialogueData dialogue, ShopUIController shopUI)
        {
            GameObject npc = GameObject.Find("NPC_ShopKeeper");
            if (npc == null)
            {
                npc = new GameObject("NPC_ShopKeeper");
                npc.transform.position = new Vector3(-3.5f, 5.5f, 0f);
            }

            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(npc);
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Default";

            Rigidbody2D rb = GetOrAdd<Rigidbody2D>(npc);
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            BoxCollider2D collider = GetOrAdd<BoxCollider2D>(npc);
            collider.isTrigger = false;

            NpcInteractable interactable = GetOrAdd<NpcInteractable>(npc);
            interactable.SetDialogueData(dialogue);

            ShopKeeperNpc shopKeeper = GetOrAdd<ShopKeeperNpc>(npc);
            SerializedObject serialized = new SerializedObject(shopKeeper);
            serialized.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            serialized.FindProperty("footCollider").objectReferenceValue = collider;
            serialized.FindProperty("shopUI").objectReferenceValue = shopUI;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (sprite != null)
            {
                float visualScale = AdventureGridUtility.GetVisualScaleForReferenceHeight(sprite);
                npc.transform.localScale = new Vector3(visualScale, visualScale, 1f);
                AdventureGridUtility.ConfigureFootCollider(
                    collider,
                    npc.transform,
                    renderer,
                    AdventureGridUtility.GetCellSize(1f));
            }

            EditorUtility.SetDirty(npc);
            return npc;
        }

        private static List<CardData> LoadAllCards()
        {
            return AssetDatabase.FindAssets("t:CardData", new[] { CardsRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<CardData>)
                .Where(card => card != null)
                .OrderBy(card => card.cardClass)
                .ThenBy(card => card.grade)
                .ThenBy(card => card.cardName)
                .ToList();
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}

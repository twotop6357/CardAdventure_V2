#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardAdventure.EditorTools
{
    public static class CardAdventureTestContentBuilder
    {
        private const string RootFolder = "Assets/ScriptableObjects";
        private const string BattleTestScenePath = "Assets/Scenes/BattleTest.unity";

        [MenuItem("CardAdventure/Build Phase 1 Battle Test Content")]
        public static void BuildFromMenu()
        {
            BuildPhase1BattleTestContent();
        }

        public static string BuildPhase1BattleTestContent()
        {
            string effectsFolder = EnsureFolder(RootFolder, "StatusEffects");
            string enemiesFolder = EnsureFolder(RootFolder, "Enemies");

            StatusEffectData poison = CreatePoison(effectsFolder);
            CreateWeak(effectsFolder);

            CardData strike = LoadCard("Card_Warrior_Strike");
            CardData defend = LoadCard("Card_Warrior_Defend");
            CardData shieldBash = LoadCard("Card_Warrior_ShieldBash");
            CardData rage = LoadCard("Card_Warrior_Rage");
            CardData taunt = LoadCard("Card_Warrior_Taunt");

            EnemyData slime = CreateVerdeSlime(enemiesFolder, strike, defend, shieldBash);
            CreateSporeRogue(enemiesFolder, poison, defend, rage, taunt);
            CreateBattleTestScene(slime, new[] { strike, strike, strike, defend, defend, shieldBash, rage, taunt });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return BattleTestScenePath;
        }

        private static string EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }

            return path;
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static CardData LoadCard(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<CardData>(
                "Assets/ScriptableObjects/Cards/Warrior/" + assetName + ".asset");
        }

        private static StatusEffectData CreatePoison(string effectsFolder)
        {
            StatusEffectData poison = LoadOrCreateAsset<StatusEffectData>(effectsFolder + "/Status_Poison.asset");
            poison.effectName = "독";
            poison.effectType = StatusEffectType.Poison;
            poison.description = "대상 턴 시작 시 스택만큼 HP를 잃는다.";
            poison.defaultStacks = 2;
            poison.duration = 0;
            poison.displayColor = new Color(0.35f, 0.75f, 0.25f, 1f);
            EditorUtility.SetDirty(poison);
            return poison;
        }

        private static StatusEffectData CreateWeak(string effectsFolder)
        {
            StatusEffectData weak = LoadOrCreateAsset<StatusEffectData>(effectsFolder + "/Status_Weak.asset");
            weak.effectName = "약화";
            weak.effectType = StatusEffectType.Weak;
            weak.description = "공격 피해가 25% 감소한다.";
            weak.defaultStacks = 1;
            weak.duration = 1;
            weak.displayColor = new Color(0.6f, 0.6f, 0.9f, 1f);
            EditorUtility.SetDirty(weak);
            return weak;
        }

        private static EnemyData CreateVerdeSlime(string enemiesFolder, CardData strike, CardData defend, CardData shieldBash)
        {
            EnemyData slime = LoadOrCreateAsset<EnemyData>(enemiesFolder + "/Enemy_Verde_Slime.asset");
            slime.enemyName = "베르데 슬라임";
            slime.maxHp = 24;
            slime.startingBlock = 0;
            slime.randomizeActions = false;
            slime.rewardGoldMin = 5;
            slime.rewardGoldMax = 8;
            slime.isBoss = false;
            slime.actionPattern = new List<EnemyAction>
            {
                new EnemyAction { actionType = EnemyActionType.Attack, value = 5, intentDescription = "몸통박치기 5" },
                new EnemyAction { actionType = EnemyActionType.Defend, value = 4, intentDescription = "점액 방어 4" },
                new EnemyAction { actionType = EnemyActionType.Attack, value = 7, intentDescription = "강한 몸통박치기 7" }
            };
            slime.rewardCardPool = new List<CardData> { strike, defend, shieldBash };
            EditorUtility.SetDirty(slime);
            return slime;
        }

        private static EnemyData CreateSporeRogue(
            string enemiesFolder,
            StatusEffectData poison,
            CardData defend,
            CardData rage,
            CardData taunt)
        {
            EnemyData spore = LoadOrCreateAsset<EnemyData>(enemiesFolder + "/Enemy_Spore_Rogue.asset");
            spore.enemyName = "포자 도적";
            spore.maxHp = 30;
            spore.startingBlock = 0;
            spore.randomizeActions = false;
            spore.rewardGoldMin = 8;
            spore.rewardGoldMax = 12;
            spore.isBoss = false;
            spore.actionPattern = new List<EnemyAction>
            {
                new EnemyAction { actionType = EnemyActionType.Attack, value = 4, intentDescription = "단검 찌르기 4" },
                new EnemyAction
                {
                    actionType = EnemyActionType.DebuffPlayer,
                    intentDescription = "독 포자 2",
                    statusEffect = poison,
                    statusEffectStacks = 2
                },
                new EnemyAction { actionType = EnemyActionType.Attack, value = 6, intentDescription = "기습 6" }
            };
            spore.rewardCardPool = new List<CardData> { defend, rage, taunt };
            EditorUtility.SetDirty(spore);
            return spore;
        }

        private static void CreateBattleTestScene(EnemyData enemy, CardData[] deck)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject root = new GameObject("BattleTestRoot");
            GameObject managerObject = new GameObject("BattleManager");
            managerObject.transform.SetParent(root.transform);

            BattleManager manager = managerObject.AddComponent<BattleManager>();
            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("playerName").stringValue = "전사 테스트 플레이어";
            serializedManager.FindProperty("playerMaxHp").intValue = 50;
            serializedManager.FindProperty("enemyData").objectReferenceValue = enemy;
            serializedManager.FindProperty("startOnAwake").boolValue = true;
            serializedManager.FindProperty("cardsDrawnPerTurn").intValue = 5;

            SerializedProperty deckProperty = serializedManager.FindProperty("startingDeck");
            deckProperty.arraySize = deck.Length;
            for (int i = 0; i < deck.Length; i++)
            {
                deckProperty.GetArrayElementAtIndex(i).objectReferenceValue = deck[i];
            }

            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, BattleTestScenePath);
        }
    }
}
#endif

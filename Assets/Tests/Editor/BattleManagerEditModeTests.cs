using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CardAdventure.Tests
{
    public sealed class BattleManagerEditModeTests
    {
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject spawnedObject in spawnedObjects)
            {
                if (spawnedObject != null)
                {
                    Object.DestroyImmediate(spawnedObject);
                }
            }

            spawnedObjects.Clear();
        }

        [Test]
        public void StartBattle_DrawsFiveCardsAndSelectsEnemyIntent()
        {
            BattleManager battleManager = CreateBattleManager(CreateAttackEnemy(20, 5), CreateDeck());

            battleManager.StartBattle();

            Assert.AreEqual(BattlePhase.PlayerTurn, battleManager.Phase);
            Assert.AreEqual(5, battleManager.Player.CardPiles.Hand.Count);
            Assert.AreEqual(3, battleManager.Player.CurrentEnergy);
            Assert.NotNull(battleManager.Enemy.CurrentIntent);
        }

        [Test]
        public void Strike_SpendsEnergyAndDamagesEnemy()
        {
            BattleManager battleManager = CreateBattleManager(CreateAttackEnemy(20, 5), CreateDeck());
            battleManager.StartBattle();

            BattleRuntimeCard strike = FindHandCard(battleManager, "Card_Warrior_Strike");
            BattleCardPlayResult result = battleManager.PlayCard(strike);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(14, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(2, battleManager.Player.CurrentEnergy);
            Assert.IsFalse(battleManager.Player.CardPiles.HasHandCard(strike));
            Assert.Contains(strike, battleManager.Player.CardPiles.DiscardPile.ToList());
        }

        [Test]
        public void ShieldBash_AddsCurrentBlockToDamage()
        {
            BattleManager battleManager = CreateBattleManager(CreateAttackEnemy(30, 5), CreateDeck());
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_Defend"));
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_ShieldBash"));

            Assert.AreEqual(17, battleManager.Enemy.Combatant.CurrentHp);
        }

        [Test]
        public void Rage_IncreasesAttackDamageForEachAttackThisTurn()
        {
            BattleManager battleManager = CreateBattleManager(CreateAttackEnemy(30, 5), CreateDeck());
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_Rage"));
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_Strike"));

            Assert.AreEqual(22, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(2, battleManager.Player.TurnAttackDamageBonus);
        }

        [Test]
        public void Taunt_AppliesWeakBeforeEnemyAttackThenExpires()
        {
            BattleManager battleManager = CreateBattleManager(CreateAttackEnemy(40, 12), CreateDeck());
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_Taunt"));
            battleManager.EndPlayerTurn();

            Assert.AreEqual(48, battleManager.Player.Combatant.CurrentHp);
            Assert.AreEqual(0, battleManager.Enemy.Combatant.GetStatusStacks(StatusEffectType.Weak));
            Assert.AreEqual(BattlePhase.PlayerTurn, battleManager.Phase);
        }

        [Test]
        public void Poison_DamagesAffectedCombatantAtTurnStart()
        {
            StatusEffectData poison = LoadAsset<StatusEffectData>("Assets/ScriptableObjects/StatusEffects/Status_Poison.asset");
            BattleManager battleManager = CreateBattleManager(CreateAttackEnemy(20, 5), CreateDeck());
            battleManager.StartBattle();

            battleManager.Enemy.Combatant.ApplyStatus(poison, 3);
            battleManager.EndPlayerTurn();

            Assert.AreEqual(17, battleManager.Enemy.Combatant.CurrentHp);
        }

        private BattleManager CreateBattleManager(EnemyData enemyData, IEnumerable<CardData> deck)
        {
            GameObject gameObject = new GameObject("BattleManagerTestObject");
            spawnedObjects.Add(gameObject);

            BattleManager battleManager = gameObject.AddComponent<BattleManager>();
            battleManager.Configure("Test Warrior", 50, deck, enemyData);
            return battleManager;
        }

        private static EnemyData CreateAttackEnemy(int maxHp, int attackDamage)
        {
            EnemyData enemy = ScriptableObject.CreateInstance<EnemyData>();
            enemy.enemyName = "Test Enemy";
            enemy.maxHp = maxHp;
            enemy.actionPattern = new List<EnemyAction>
            {
                new EnemyAction
                {
                    actionType = EnemyActionType.Attack,
                    value = attackDamage,
                    intentDescription = "Test Attack"
                }
            };
            return enemy;
        }

        private static List<CardData> CreateDeck()
        {
            return new List<CardData>
            {
                LoadAsset<CardData>("Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Strike.asset"),
                LoadAsset<CardData>("Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Defend.asset"),
                LoadAsset<CardData>("Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_ShieldBash.asset"),
                LoadAsset<CardData>("Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Rage.asset"),
                LoadAsset<CardData>("Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Taunt.asset")
            };
        }

        private static BattleRuntimeCard FindHandCard(BattleManager battleManager, string cardAssetName)
        {
            BattleRuntimeCard card = battleManager.Player.CardPiles.Hand.FirstOrDefault(
                runtimeCard => runtimeCard.Data != null && runtimeCard.Data.name == cardAssetName);

            Assert.NotNull(card, "Expected card in hand: " + cardAssetName);
            return card;
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.NotNull(asset, "Missing test asset: " + path);
            return asset;
        }
    }
}

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
        public void Block_RemainsAfterTurnEnds()
        {
            BattleManager battleManager = CreateBattleManager(CreateAttackEnemy(30, 0), CreateDeck());
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_Defend"));
            battleManager.EndPlayerTurn();

            Assert.AreEqual(5, battleManager.Player.Combatant.Block);
            Assert.AreEqual(BattlePhase.PlayerTurn, battleManager.Phase);
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

        [Test]
        public void Burn_DamagesAffectedCombatantAtTurnStartIgnoringBlock()
        {
            StatusEffectData burn = LoadAsset<StatusEffectData>("Assets/ScriptableObjects/StatusEffects/Status_Burn.asset");
            BattleCombatantState combatant = new BattleCombatantState("Burn Target", 30);

            combatant.AddBlock(10);
            combatant.ApplyStatus(burn, 4);
            BattleStatusTurnResult result = combatant.ApplyTurnStartStatusEffects();

            Assert.AreEqual(4, result.BurnDamage);
            Assert.AreEqual(26, combatant.CurrentHp);
            Assert.AreEqual(10, combatant.Block);
        }

        [Test]
        public void Regeneration_HealsAffectedCombatantAtTurnStart()
        {
            StatusEffectData regeneration = LoadAsset<StatusEffectData>("Assets/ScriptableObjects/StatusEffects/Status_Regeneration.asset");
            BattleCombatantState combatant = new BattleCombatantState("Regen Target", 30);

            combatant.LoseHpIgnoringBlock(8);
            combatant.ApplyStatus(regeneration, 3);
            BattleStatusTurnResult result = combatant.ApplyTurnStartStatusEffects();

            Assert.AreEqual(3, result.RegenerationHealing);
            Assert.AreEqual(25, combatant.CurrentHp);
        }

        [Test]
        public void Strength_IncreasesPlayerAttackDamage()
        {
            StatusEffectData strength = LoadAsset<StatusEffectData>("Assets/ScriptableObjects/StatusEffects/Status_Strength.asset");
            BattleManager battleManager = CreateBattleManager(CreateAttackEnemy(20, 5), CreateDeck());
            battleManager.StartBattle();

            battleManager.Player.Combatant.ApplyStatus(strength, 3);
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_Strike"));

            Assert.AreEqual(11, battleManager.Enemy.Combatant.CurrentHp);
        }

        [Test]
        public void Vulnerable_IncreasesIncomingDamage()
        {
            StatusEffectData vulnerable = LoadAsset<StatusEffectData>("Assets/ScriptableObjects/StatusEffects/Status_Vulnerable.asset");
            BattleManager battleManager = CreateBattleManager(CreateAttackEnemy(20, 5), CreateDeck());
            battleManager.StartBattle();

            battleManager.Enemy.Combatant.ApplyStatus(vulnerable, 1);
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_Strike"));

            Assert.AreEqual(11, battleManager.Enemy.Combatant.CurrentHp);
        }

        [Test]
        public void Weak_ReducesEnemyAttackDamage()
        {
            StatusEffectData weak = LoadAsset<StatusEffectData>("Assets/ScriptableObjects/StatusEffects/Status_Weak.asset");
            BattleManager battleManager = CreateBattleManager(CreateAttackEnemy(40, 12), CreateDeck());
            battleManager.StartBattle();

            battleManager.Enemy.Combatant.ApplyStatus(weak, 1);
            battleManager.EndPlayerTurn();

            Assert.AreEqual(41, battleManager.Player.Combatant.CurrentHp);
        }

        [Test]
        public void StatusEffectAssets_HaveKoreanNamesAndDescriptions()
        {
            AssertStatusText("Assets/ScriptableObjects/StatusEffects/Status_Poison.asset",
                "독", "대상 턴 시작 시 중첩 수만큼 방어막을 무시하고 HP를 잃습니다.");
            AssertStatusText("Assets/ScriptableObjects/StatusEffects/Status_Weak.asset",
                "약화", "공격 피해가 25% 감소합니다.");
            AssertStatusText("Assets/ScriptableObjects/StatusEffects/Status_Vulnerable.asset",
                "취약", "받는 피해가 50% 증가합니다.");
            AssertStatusText("Assets/ScriptableObjects/StatusEffects/Status_Strength.asset",
                "강화", "공격 피해에 중첩 수만큼 추가 피해를 더합니다.");
            AssertStatusText("Assets/ScriptableObjects/StatusEffects/Status_Regeneration.asset",
                "재생", "대상 턴 시작 시 중첩 수만큼 HP를 회복합니다.");
            AssertStatusText("Assets/ScriptableObjects/StatusEffects/Status_Burn.asset",
                "화상", "대상 턴 시작 시 중첩 수만큼 방어막을 무시하고 화상 피해를 입습니다.");
        }

        [Test]
        public void BloodArmor_GainsBlockEqualToDamageDealt()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_BloodArmor.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_BloodArmor"));

            Assert.AreEqual(18, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(2, battleManager.Player.Combatant.Block);
        }

        [Test]
        public void BarbedGuard_DamagesEnemyWhenBlockIsGained()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck(
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_BarbedGuard.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Defend.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_BarbedGuard"));
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_Defend"));

            Assert.AreEqual(15, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(5, battleManager.Player.Combatant.Block);
        }

        [Test]
        public void CounterStance_GainsBlockForDamageDealtThisTurn()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck(
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_CounterStance.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Strike.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_CounterStance"));
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_Strike"));

            Assert.AreEqual(14, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(6, battleManager.Player.Combatant.Block);
        }

        [Test]
        public void BarbedGuardAndCounterStance_ChainUntilEnemyIsDefeated()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck(
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_BarbedGuard.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_CounterStance.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Defend.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_BarbedGuard"));
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_CounterStance"));
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_Defend"));

            Assert.AreEqual(BattlePhase.Won, battleManager.Phase);
            Assert.AreEqual(0, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(25, battleManager.Player.Combatant.Block);
        }

        [Test]
        public void ShieldThrow_ConsumesAllBlockToDealDamage()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_ShieldThrow.asset"));
            battleManager.StartBattle();

            battleManager.Player.Combatant.AddBlock(7);
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_ShieldThrow"));

            Assert.AreEqual(13, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(0, battleManager.Player.Combatant.Block);
        }

        [Test]
        public void IronStrength_GainsStrengthEqualToCurrentBlock()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_IronStrength.asset"));
            battleManager.StartBattle();

            battleManager.Player.Combatant.AddBlock(4);
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_IronStrength"));

            Assert.AreEqual(4, battleManager.Player.Combatant.GetStatusStacks(StatusEffectType.Strength));
        }

        [Test]
        public void BattlePreparation_DrawsCardsAndGainsBlock()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck(
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_BattlePreparation.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Strike.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Defend.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_ShieldBash.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Rage.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Taunt.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_BloodArmor.asset"));
            battleManager.StartBattle();

            BattleRuntimeCard battlePreparation = EnsureHandCard(battleManager, "Card_Warrior_BattlePreparation");
            int handCountBefore = battleManager.Player.CardPiles.Hand.Count;
            battleManager.PlayCard(battlePreparation);

            Assert.AreEqual(10, battleManager.Player.Combatant.Block);
            Assert.GreaterOrEqual(battleManager.Player.CardPiles.Hand.Count, handCountBefore - 1);
        }

        [Test]
        public void DefensiveTactics_DrawsWhenBlockIsGained()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck(
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_DefensiveTactics.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Defend.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Strike.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_ShieldBash.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Rage.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Taunt.asset",
                    "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_BloodArmor.asset"));
            battleManager.StartBattle();

            BattleRuntimeCard defensiveTactics = EnsureHandCard(battleManager, "Card_Warrior_DefensiveTactics");
            BattleRuntimeCard defend = EnsureHandCard(battleManager, "Card_Warrior_Defend");
            battleManager.PlayCard(defensiveTactics);
            int handCountBeforeBlock = battleManager.Player.CardPiles.Hand.Count;
            battleManager.PlayCard(defend);

            Assert.AreEqual(5, battleManager.Player.Combatant.Block);
            Assert.GreaterOrEqual(battleManager.Player.CardPiles.Hand.Count, handCountBeforeBlock);
        }

        [Test]
        public void ShatteringBlow_DamagesEnemyAndAppliesVulnerable()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_ShatteringBlow.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_ShatteringBlow"));

            Assert.AreEqual(15, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(3, battleManager.Enemy.Combatant.GetStatusStacks(StatusEffectType.Vulnerable));
        }

        [Test]
        public void AdversityResolve_GrantsEnemyStrengthAndGainsStrengthFromNextEnemyDamage()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(40, 5),
                CreateDeck("Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_AdversityResolve.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Warrior_AdversityResolve"));
            battleManager.EndPlayerTurn();

            Assert.AreEqual(43, battleManager.Player.Combatant.CurrentHp);
            Assert.AreEqual(7, battleManager.Player.Combatant.GetStatusStacks(StatusEffectType.Strength));
        }

        [Test]
        public void MageManaSpark_GainsEnergyThisTurn()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Mage/Card_Mage_ManaSpark.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Mage_ManaSpark"));

            Assert.AreEqual(4, battleManager.Player.CurrentEnergy);
        }

        [Test]
        public void MageManaBarrier_GainsBlockAndNextTurnEnergy()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Mage/Card_Mage_ManaBarrier.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Mage_ManaBarrier"));
            battleManager.EndPlayerTurn();

            Assert.AreEqual(2, battleManager.Player.Combatant.Block);
            Assert.AreEqual(5, battleManager.Player.CurrentEnergy);
        }

        [Test]
        public void MageInfernoBlast_DamagesAndAppliesBurn()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(40, 0),
                CreateDeck(
                    "Assets/ScriptableObjects/Cards/Mage/Card_Mage_InfernoBlast.asset",
                    "Assets/ScriptableObjects/Cards/Mage/Card_Mage_ManaSurge.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Mage_ManaSurge"));
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Mage_InfernoBlast"));

            Assert.AreEqual(10, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(5, battleManager.Enemy.Combatant.GetStatusStacks(StatusEffectType.Burn));
        }

        [Test]
        public void MageToxicCurrent_AppliesPoisonWhenDamageIsDealt()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck(
                    "Assets/ScriptableObjects/Cards/Mage/Card_Mage_ToxicCurrent.asset",
                    "Assets/ScriptableObjects/Cards/Mage/Card_Mage_SparkBarrage.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Mage_ToxicCurrent"));
            battleManager.PlayCard(FindHandCard(battleManager, "Card_Mage_SparkBarrage"));

            Assert.AreEqual(16, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(8, battleManager.Enemy.Combatant.GetStatusStacks(StatusEffectType.Poison));
        }

        [Test]
        public void MageFrostBind_SkipsNextEnemyAction()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 7),
                CreateDeck("Assets/ScriptableObjects/Cards/Mage/Card_Mage_FrostBind.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Mage_FrostBind"));
            battleManager.EndPlayerTurn();

            Assert.AreEqual(50, battleManager.Player.Combatant.CurrentHp);
            Assert.AreEqual(BattlePhase.PlayerTurn, battleManager.Phase);
        }

        [Test]
        public void MagePowerSurge_GainsStrengthForEachDamagingHit()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(30, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Mage/Card_Mage_PowerSurge.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Mage_PowerSurge"));

            Assert.AreEqual(18, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(3, battleManager.Player.Combatant.GetStatusStacks(StatusEffectType.Strength));
        }

        [Test]
        public void MageArcaneDiscount_MakesFirstAttackFreeThenClears()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(40, 0),
                CreateDeck(
                    "Assets/ScriptableObjects/Cards/Mage/Card_Mage_ArcaneDiscount.asset",
                    "Assets/ScriptableObjects/Cards/Mage/Card_Mage_InfernoBlast.asset"));
            battleManager.StartBattle();

            BattleRuntimeCard inferno = FindHandCard(battleManager, "Card_Mage_InfernoBlast");
            Assert.AreEqual(5, inferno.EnergyCost);

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Mage_ArcaneDiscount"));
            Assert.AreEqual(0, inferno.EnergyCost);

            battleManager.PlayCard(inferno);

            Assert.AreEqual(3, battleManager.Player.CurrentEnergy);
            Assert.AreEqual(10, battleManager.Enemy.Combatant.CurrentHp);
        }

        [Test]
        public void MageMeteorShower_HitsFifteenTimes()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(30, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Mage/Card_Mage_MeteorShower.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Mage_MeteorShower"));

            Assert.AreEqual(15, battleManager.Enemy.Combatant.CurrentHp);
        }

        [Test]
        public void MageChaosCasting_PlaysOtherCardsFromHand()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(30, 0),
                CreateDeck(
                    "Assets/ScriptableObjects/Cards/Mage/Card_Mage_ChaosCasting.asset",
                    "Assets/ScriptableObjects/Cards/Mage/Card_Mage_SparkBarrage.asset",
                    "Assets/ScriptableObjects/Cards/Mage/Card_Mage_ManaSpark.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Mage_ChaosCasting"));

            Assert.AreEqual(26, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(0, battleManager.Player.CardPiles.Hand.Count);
        }

        // ── 도적 신규 카드 테스트 ──────────────────────────────

        [Test]
        public void RogueEnergyBurst_ConsumesAllEnergyAndDealsDamage()
        {
            // 3 energy: effectValue(2) + 3 * secondaryValue(2) = 8 damage
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Rogue/Card_Rogue_EnergyBurst.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Rogue_EnergyBurst"));

            Assert.AreEqual(12, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(0, battleManager.Player.CurrentEnergy);
        }

        [Test]
        public void RogueNimbleStrike_DealsDamageAndGainsDodge()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Rogue/Card_Rogue_NimbleStrike.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Rogue_NimbleStrike"));

            Assert.AreEqual(19, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(5, battleManager.Player.Combatant.GetStatusStacks(StatusEffectType.Dodge));
        }

        [Test]
        public void RogueQuickHands_TriggersDodgeOnFreeCardPlayed()
        {
            // QuickHands(cost 2) sets 1 Dodge per free card played.
            // NimbleStrike(cost 0): gives 5 Dodge from effect + 1 from QuickHands trigger = 6
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck(
                    "Assets/ScriptableObjects/Cards/Rogue/Card_Rogue_QuickHands.asset",
                    "Assets/ScriptableObjects/Cards/Rogue/Card_Rogue_NimbleStrike.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(EnsureHandCard(battleManager, "Card_Rogue_QuickHands"));
            battleManager.PlayCard(EnsureHandCard(battleManager, "Card_Rogue_NimbleStrike"));

            Assert.AreEqual(6, battleManager.Player.Combatant.GetStatusStacks(StatusEffectType.Dodge));
        }

        [Test]
        public void RogueShadowBarrage_HitsThreeTimes_NoCritWithZeroDodge()
        {
            // 0 Dodge → 0% crit → 3 hits × 1 damage = 3 damage
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Rogue/Card_Rogue_ShadowBarrage.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Rogue_ShadowBarrage"));

            Assert.AreEqual(17, battleManager.Enemy.Combatant.CurrentHp);
        }

        [Test]
        public void RogueDodgeAmplify_DoublesDodgeStacks()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck(
                    "Assets/ScriptableObjects/Cards/Rogue/Card_Rogue_NimbleStrike.asset",
                    "Assets/ScriptableObjects/Cards/Rogue/Card_Rogue_DodgeAmplify.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(EnsureHandCard(battleManager, "Card_Rogue_NimbleStrike")); // 5 Dodge
            battleManager.PlayCard(EnsureHandCard(battleManager, "Card_Rogue_DodgeAmplify")); // 5 × 2 = 10

            Assert.AreEqual(10, battleManager.Player.Combatant.GetStatusStacks(StatusEffectType.Dodge));
        }

        [Test]
        public void RoguePoisonBurst_DetonatesAllPoisonForBonusDamage()
        {
            // Apply 5 poison → remove 5 → deal 5 + 5 = 10 damage
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(30, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Rogue/Card_Rogue_PoisonBurst.asset"));
            battleManager.StartBattle();

            battleManager.PlayCard(FindHandCard(battleManager, "Card_Rogue_PoisonBurst"));

            Assert.AreEqual(20, battleManager.Enemy.Combatant.CurrentHp);
            Assert.AreEqual(0, battleManager.Enemy.Combatant.GetStatusStacks(StatusEffectType.Poison));
        }

        [Test]
        public void RogueAfterimage_ShufflesBackIntoDeck()
        {
            BattleManager battleManager = CreateBattleManager(
                CreateAttackEnemy(20, 0),
                CreateDeck("Assets/ScriptableObjects/Cards/Rogue/Card_Rogue_Afterimage.asset"));
            battleManager.StartBattle();

            BattleRuntimeCard afterimage = FindHandCard(battleManager, "Card_Rogue_Afterimage");
            battleManager.PlayCard(afterimage);

            Assert.AreEqual(18, battleManager.Enemy.Combatant.CurrentHp);
            Assert.IsFalse(battleManager.Player.CardPiles.HasHandCard(afterimage));
            Assert.IsFalse(battleManager.Player.CardPiles.DiscardPile.Contains(afterimage));
            Assert.IsTrue(battleManager.Player.CardPiles.DrawPile.Contains(afterimage));
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
            return CreateDeck(
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Strike.asset",
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Defend.asset",
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_ShieldBash.asset",
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Rage.asset",
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Taunt.asset");
        }

        private static List<CardData> CreateDeck(params string[] paths)
        {
            return paths.Select(LoadAsset<CardData>).ToList();
        }

        private static BattleRuntimeCard FindHandCard(BattleManager battleManager, string cardAssetName)
        {
            BattleRuntimeCard card = battleManager.Player.CardPiles.Hand.FirstOrDefault(
                runtimeCard => runtimeCard.Data != null && runtimeCard.Data.name == cardAssetName);

            Assert.NotNull(card, "Expected card in hand: " + cardAssetName);
            return card;
        }

        private static BattleRuntimeCard EnsureHandCard(BattleManager battleManager, string cardAssetName)
        {
            BattleRuntimeCard card = battleManager.Player.CardPiles.Hand.FirstOrDefault(
                runtimeCard => runtimeCard.Data != null && runtimeCard.Data.name == cardAssetName);

            while (card == null && battleManager.Player.CardPiles.DrawPile.Count > 0)
            {
                battleManager.Player.CardPiles.Draw(1);
                card = battleManager.Player.CardPiles.Hand.FirstOrDefault(
                    runtimeCard => runtimeCard.Data != null && runtimeCard.Data.name == cardAssetName);
            }

            Assert.NotNull(card, "Expected card in hand: " + cardAssetName);
            return card;
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.NotNull(asset, "Missing test asset: " + path);
            return asset;
        }

        private static void AssertStatusText(string path, string expectedName, string expectedDescription)
        {
            StatusEffectData status = LoadAsset<StatusEffectData>(path);

            Assert.AreEqual(expectedName, status.effectName);
            Assert.AreEqual(expectedDescription, status.description);
        }
    }
}

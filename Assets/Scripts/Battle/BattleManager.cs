using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// Coordinates the single-enemy card battle loop for Phase 1.
    /// </summary>
    public sealed class BattleManager : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private string playerName = "Player";
        [SerializeField] private int playerMaxHp = 50;
        [SerializeField] private List<CardData> startingDeck = new List<CardData>();

        [Header("Enemy")]
        [SerializeField] private EnemyData enemyData;

        [Header("Rules")]
        [SerializeField] private bool startOnAwake;
        [SerializeField] private int cardsDrawnPerTurn = 5;

        public event Action<BattleManager> BattleStarted;
        public event Action<BattleManager> StateChanged;
        public event Action<BattleManager, BattleRuntimeCard> CardPlayed;
        public event Action<BattleManager, EnemyAction> EnemyIntentSelected;
        public event Action<BattleManager, BattleCombatantState, BattleStatusTurnResult> TurnStartStatusResolved;
        public event Action<BattleManager, BattlePhase> BattleEnded;

        public BattlePlayerState Player { get; private set; }

        public BattleEnemyState Enemy { get; private set; }

        public BattlePhase Phase { get; private set; } = BattlePhase.NotStarted;

        public int PlayerTurnCount { get; private set; }

        public bool IsBattleActive => Phase == BattlePhase.PlayerTurn || Phase == BattlePhase.EnemyTurn;

        private void Awake()
        {
            if (startOnAwake)
            {
                StartBattle();
            }
        }

        public void Configure(string newPlayerName, int newPlayerMaxHp, IEnumerable<CardData> newDeck, EnemyData newEnemyData)
        {
            playerName = string.IsNullOrWhiteSpace(newPlayerName) ? playerName : newPlayerName;
            playerMaxHp = Mathf.Max(1, newPlayerMaxHp);
            enemyData = newEnemyData;

            startingDeck.Clear();
            if (newDeck == null)
            {
                return;
            }

            foreach (CardData cardData in newDeck)
            {
                if (cardData != null)
                {
                    startingDeck.Add(cardData);
                }
            }
        }

        public void StartBattle()
        {
            Player = new BattlePlayerState(playerName, playerMaxHp, startingDeck);
            Enemy = new BattleEnemyState(enemyData);
            PlayerTurnCount = 0;

            Player.DrawStartingHand();
            BattleStarted?.Invoke(this);
            BeginPlayerTurn();
        }

        public void BeginPlayerTurn()
        {
            if (Player == null || Enemy == null)
            {
                return;
            }

            Phase = BattlePhase.PlayerTurn;
            PlayerTurnCount++;
            Player.StartTurn();
            ResolveTurnStartStatuses(Player.Combatant);

            if (ResolveBattleEndOrNotify())
            {
                return;
            }

            if (PlayerTurnCount > 1)
            {
                Player.CardPiles.Draw(cardsDrawnPerTurn);
            }

            Enemy.SelectIntent();
            EnemyIntentSelected?.Invoke(this, Enemy.CurrentIntent);
            StateChanged?.Invoke(this);
        }

        public BattleCardPlayResult PlayCard(BattleRuntimeCard card)
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.BattleNotActive);
            }

            if (card == null || card.Data == null)
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.InvalidCard);
            }

            if (!Player.CardPiles.HasHandCard(card))
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.CardNotInHand);
            }

            bool needsEnemyTarget = card.Data.cardType == CardType.Attack || card.Data.statusEffect != null;
            if (needsEnemyTarget && (Enemy == null || Enemy.Combatant.IsDefeated))
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.TargetRequired);
            }

            if (!Player.SpendEnergy(card))
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.NotEnoughEnergy);
            }

            ApplyCardEffect(card);

            if (card.IsExhaust)
            {
                Player.CardPiles.MoveHandCardToExhaust(card);
            }
            else
            {
                Player.CardPiles.MoveHandCardToDiscard(card);
            }

            CardPlayed?.Invoke(this, card);
            ResolveBattleEndOrNotify();
            return BattleCardPlayResult.Succeeded();
        }

        public void EndPlayerTurn()
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                return;
            }

            Player.CardPiles.DiscardHand();
            Player.Combatant.TickStatusDurations();
            ExecuteEnemyTurn();
        }

        public void ExecuteEnemyTurn()
        {
            if (!IsBattleActive || Enemy == null || Enemy.Combatant.IsDefeated)
            {
                ResolveBattleEndOrNotify();
                return;
            }

            Phase = BattlePhase.EnemyTurn;
            Enemy.Combatant.ClearBlock();
            ResolveTurnStartStatuses(Enemy.Combatant);

            if (ResolveBattleEndOrNotify())
            {
                return;
            }

            EnemyAction action = Enemy.CurrentIntent ?? Enemy.SelectIntent();
            ApplyEnemyAction(action);
            Enemy.AdvanceTurn();
            Enemy.Combatant.TickStatusDurations();

            if (ResolveBattleEndOrNotify())
            {
                return;
            }

            BeginPlayerTurn();
        }

        private void ApplyCardEffect(BattleRuntimeCard card)
        {
            CardData data = card.Data;
            string cardAssetName = data.name;

            switch (data.cardType)
            {
                case CardType.Attack:
                    ApplyAttackCard(data, cardAssetName);
                    break;
                case CardType.Defense:
                    Player.Combatant.AddBlock(data.effectValue);
                    break;
                case CardType.StatusEffect:
                    Enemy.Combatant.ApplyStatus(data.statusEffect, data.effectValue);
                    break;
                case CardType.Skill:
                    ApplySkillCard(data);
                    break;
            }
        }

        private void ApplyAttackCard(CardData data, string cardAssetName)
        {
            int baseDamage = data.effectValue;

            if (cardAssetName == "Card_Warrior_ShieldBash")
            {
                baseDamage += Player.Combatant.Block;
            }

            int damage = Player.GetAttackDamage(baseDamage);
            damage = ApplyVulnerableDamageModifier(Enemy.Combatant, damage);
            Enemy.Combatant.ReceiveDamage(damage);
        }

        private void ApplySkillCard(CardData data)
        {
            switch (data.name)
            {
                case "Card_Warrior_Rage":
                    Player.AddAttackBonusGainedPerAttackForTurn(data.effectValue);
                    return;
                case "Card_Warrior_Taunt":
                    Player.Combatant.AddBlock(data.effectValue);
                    Enemy.Combatant.ApplyStatus(StatusEffectType.Weak, 1, 1);
                    return;
            }

            if (data.statusEffect != null)
            {
                Enemy.Combatant.ApplyStatus(data.statusEffect, Mathf.Max(1, data.statusEffect.defaultStacks));
            }
        }

        private void ApplyEnemyAction(EnemyAction action)
        {
            if (action == null)
            {
                return;
            }

            switch (action.actionType)
            {
                case EnemyActionType.Attack:
                    Player.Combatant.ReceiveDamage(GetEnemyAttackDamage(action.value));
                    break;
                case EnemyActionType.Defend:
                    Enemy.Combatant.AddBlock(action.value);
                    break;
                case EnemyActionType.Buff:
                    Enemy.Combatant.ApplyStatus(action.statusEffect, action.statusEffectStacks);
                    break;
                case EnemyActionType.DebuffPlayer:
                    Player.Combatant.ApplyStatus(action.statusEffect, action.statusEffectStacks);
                    break;
                case EnemyActionType.HealSelf:
                    Enemy.Combatant.Heal(action.value);
                    break;
            }
        }

        private void ResolveTurnStartStatuses(BattleCombatantState combatant)
        {
            if (combatant == null || combatant.IsDefeated)
            {
                return;
            }

            BattleStatusTurnResult result = combatant.ApplyTurnStartStatusEffects();

            if (result.HasAnyEffect)
            {
                TurnStartStatusResolved?.Invoke(this, combatant, result);
            }
        }

        private int GetEnemyAttackDamage(int baseDamage)
        {
            int damage = Mathf.Max(0, baseDamage + Enemy.Combatant.GetStatusStacks(StatusEffectType.Strength));

            if (Enemy.Combatant.HasStatus(StatusEffectType.Weak))
            {
                damage = Mathf.FloorToInt(damage * 0.75f);
            }

            return ApplyVulnerableDamageModifier(Player.Combatant, damage);
        }

        private static int ApplyVulnerableDamageModifier(BattleCombatantState target, int damage)
        {
            if (target != null && target.HasStatus(StatusEffectType.Vulnerable))
            {
                return Mathf.CeilToInt(damage * 1.5f);
            }

            return damage;
        }

        private bool ResolveBattleEndOrNotify()
        {
            if (Enemy != null && Enemy.Combatant.IsDefeated)
            {
                Phase = BattlePhase.Won;
                BattleEnded?.Invoke(this, Phase);
                StateChanged?.Invoke(this);
                return true;
            }

            if (Player != null && Player.Combatant.IsDefeated)
            {
                Phase = BattlePhase.Lost;
                BattleEnded?.Invoke(this, Phase);
                StateChanged?.Invoke(this);
                return true;
            }

            StateChanged?.Invoke(this);
            return false;
        }
    }
}

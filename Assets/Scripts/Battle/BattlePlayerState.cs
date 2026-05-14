using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// Runtime player state for a single card battle.
    /// </summary>
    public sealed class BattlePlayerState
    {
        public const int DefaultMaxEnergy = 3;
        public const int DefaultStartingHandSize = 5;

        public BattlePlayerState(string playerName, int maxHp, IEnumerable<CardData> deck)
        {
            Combatant = new BattleCombatantState(playerName, maxHp);
            CardPiles = new BattleCardPiles();
            CardPiles.Initialize(deck);
            MaxEnergy = DefaultMaxEnergy;
            CurrentEnergy = 0;
        }

        public BattleCombatantState Combatant { get; }

        public BattleCardPiles CardPiles { get; }

        public int MaxEnergy { get; private set; }

        public int CurrentEnergy { get; private set; }

        public int TurnAttackDamageBonus { get; private set; }

        public int AttackBonusGainedPerAttack { get; private set; }

        public int DamageDealtPerBlockGained { get; private set; }

        public int BlockGainedPerDamageDealt { get; private set; }

        public int CardsDrawnPerBlockGain { get; private set; }

        public bool GainStrengthFromNextEnemyDamage { get; private set; }

        public int PoisonAppliedPerDamageDealt { get; private set; }

        public int NextTurnEnergyBonus { get; private set; }

        public bool FirstAttackFreeThisTurn { get; private set; }

        public int DodgePerFreeCardPlayed { get; private set; }

        public int DrawPerFreeCardPlayed { get; private set; }

        public void StartTurn()
        {
            CurrentEnergy = MaxEnergy;
            if (NextTurnEnergyBonus > 0)
            {
                CurrentEnergy += NextTurnEnergyBonus;
                NextTurnEnergyBonus = 0;
            }

            TurnAttackDamageBonus = 0;
            AttackBonusGainedPerAttack = 0;
            DamageDealtPerBlockGained = 0;
            BlockGainedPerDamageDealt = 0;
            CardsDrawnPerBlockGain = 0;
            GainStrengthFromNextEnemyDamage = false;
            PoisonAppliedPerDamageDealt = 0;
            FirstAttackFreeThisTurn = false;
            DodgePerFreeCardPlayed = 0;
            DrawPerFreeCardPlayed = 0;
            CardPiles.ClearTemporaryEnergyCosts();
        }

        public void DrawStartingHand()
        {
            CardPiles.Draw(DefaultStartingHandSize);
        }

        public bool CanPayEnergy(BattleRuntimeCard card)
        {
            return card != null && CurrentEnergy >= card.EnergyCost;
        }

        public bool SpendEnergy(BattleRuntimeCard card)
        {
            if (!CanPayEnergy(card))
            {
                return false;
            }

            CurrentEnergy = Mathf.Max(0, CurrentEnergy - card.EnergyCost);
            return true;
        }

        public void AddEnergy(int amount)
        {
            CurrentEnergy = Mathf.Max(0, CurrentEnergy + amount);
        }

        public void AddNextTurnEnergyBonus(int amount)
        {
            NextTurnEnergyBonus = Mathf.Max(0, NextTurnEnergyBonus + amount);
        }

        public void AddAttackDamageBonusForTurn(int amount)
        {
            TurnAttackDamageBonus = Mathf.Max(0, TurnAttackDamageBonus + amount);
        }

        public void AddAttackBonusGainedPerAttackForTurn(int amount)
        {
            AttackBonusGainedPerAttack = Mathf.Max(0, AttackBonusGainedPerAttack + amount);
        }

        public void AddDamageDealtPerBlockGainedForTurn(int amount)
        {
            DamageDealtPerBlockGained = Mathf.Max(0, DamageDealtPerBlockGained + amount);
        }

        public void AddBlockGainedPerDamageDealtForTurn(int amount)
        {
            BlockGainedPerDamageDealt = Mathf.Max(0, BlockGainedPerDamageDealt + amount);
        }

        public void AddCardsDrawnPerBlockGainForTurn(int amount)
        {
            CardsDrawnPerBlockGain = Mathf.Max(0, CardsDrawnPerBlockGain + amount);
        }

        public void PrepareGainStrengthFromNextEnemyDamage()
        {
            GainStrengthFromNextEnemyDamage = true;
        }

        public void AddPoisonAppliedPerDamageDealtForTurn(int amount)
        {
            PoisonAppliedPerDamageDealt = Mathf.Max(0, PoisonAppliedPerDamageDealt + amount);
        }

        public void MakeFirstAttackFreeThisTurn()
        {
            FirstAttackFreeThisTurn = true;

            foreach (BattleRuntimeCard card in CardPiles.Hand)
            {
                if (card?.Data != null && card.Data.cardType == CardType.Attack)
                {
                    card.SetTemporaryEnergyCost(0);
                }
            }
        }

        public void SetDodgePerFreeCardPlayed(int amount)
        {
            DodgePerFreeCardPlayed = Mathf.Max(0, amount);
        }

        public void SetDrawPerFreeCardPlayed(int amount)
        {
            DrawPerFreeCardPlayed = Mathf.Max(0, amount);
        }

        public int ConsumeAllEnergy()
        {
            int consumed = CurrentEnergy;
            CurrentEnergy = 0;
            return consumed;
        }

        public void ConsumeFirstAttackFreeIfNeeded(BattleRuntimeCard playedCard)
        {
            if (!FirstAttackFreeThisTurn || playedCard?.Data == null || playedCard.Data.cardType != CardType.Attack)
            {
                return;
            }

            FirstAttackFreeThisTurn = false;
            CardPiles.ClearTemporaryEnergyCosts();
        }

        public void ResolveGainStrengthFromEnemyDamage(int damageTaken)
        {
            if (!GainStrengthFromNextEnemyDamage)
            {
                return;
            }

            GainStrengthFromNextEnemyDamage = false;

            if (damageTaken > 0)
            {
                Combatant.ApplyStatus(StatusEffectType.Strength, damageTaken, 0);
            }
        }

        public int GetAttackDamage(int baseDamage)
        {
            if (AttackBonusGainedPerAttack > 0)
            {
                AddAttackDamageBonusForTurn(AttackBonusGainedPerAttack);
            }

            int strengthStacks = Combatant.GetStatusStacks(StatusEffectType.Strength);
            return Mathf.Max(0, baseDamage + TurnAttackDamageBonus + strengthStacks);
        }
    }
}

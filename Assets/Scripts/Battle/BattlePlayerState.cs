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

        public void StartTurn()
        {
            Combatant.ClearBlock();
            CurrentEnergy = MaxEnergy;
            TurnAttackDamageBonus = 0;
            AttackBonusGainedPerAttack = 0;
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

        public void AddAttackDamageBonusForTurn(int amount)
        {
            TurnAttackDamageBonus = Mathf.Max(0, TurnAttackDamageBonus + amount);
        }

        public void AddAttackBonusGainedPerAttackForTurn(int amount)
        {
            AttackBonusGainedPerAttack = Mathf.Max(0, AttackBonusGainedPerAttack + amount);
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

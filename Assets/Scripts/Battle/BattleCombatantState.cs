using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// Mutable HP, block, and status state shared by player and enemies.
    /// </summary>
    public sealed class BattleCombatantState
    {
        private readonly List<BattleStatusInstance> statuses = new List<BattleStatusInstance>();

        public BattleCombatantState(string displayName, int maxHp, int startingBlock = 0)
        {
            DisplayName = displayName;
            MaxHp = Mathf.Max(1, maxHp);
            CurrentHp = MaxHp;
            Block = Mathf.Max(0, startingBlock);
        }

        public string DisplayName { get; }

        public int MaxHp { get; }

        public int CurrentHp { get; private set; }

        public int Block { get; private set; }

        public bool IsDefeated => CurrentHp <= 0;

        public IReadOnlyList<BattleStatusInstance> Statuses => statuses;

        public int ReceiveDamage(int amount)
        {
            int remainingDamage = Mathf.Max(0, amount);
            int blockedDamage = Mathf.Min(Block, remainingDamage);

            Block -= blockedDamage;
            remainingDamage -= blockedDamage;

            CurrentHp = Mathf.Max(0, CurrentHp - remainingDamage);
            return remainingDamage;
        }

        public void AddBlock(int amount)
        {
            Block = Mathf.Max(0, Block + amount);
        }

        public void ClearBlock()
        {
            Block = 0;
        }

        public void Heal(int amount)
        {
            CurrentHp = Mathf.Clamp(CurrentHp + Mathf.Max(0, amount), 0, MaxHp);
        }

        public void ApplyStatus(StatusEffectData statusEffect, int stacks)
        {
            if (statusEffect == null || stacks <= 0)
            {
                return;
            }

            BattleStatusInstance existingStatus = statuses.Find(status => status.EffectType == statusEffect.effectType);

            if (existingStatus != null)
            {
                existingStatus.AddStacks(stacks);
                return;
            }

            statuses.Add(new BattleStatusInstance(statusEffect, stacks));
        }

        public void ApplyStatus(StatusEffectType effectType, int stacks, int duration)
        {
            if (stacks <= 0)
            {
                return;
            }

            BattleStatusInstance existingStatus = statuses.Find(status => status.EffectType == effectType);

            if (existingStatus != null)
            {
                existingStatus.AddStacks(stacks);
                existingStatus.RefreshDuration(duration);
                return;
            }

            statuses.Add(new BattleStatusInstance(effectType, stacks, duration));
        }

        public int GetStatusStacks(StatusEffectType effectType)
        {
            BattleStatusInstance status = statuses.Find(instance => instance.EffectType == effectType);
            return status != null ? status.Stacks : 0;
        }

        public bool HasStatus(StatusEffectType effectType)
        {
            return GetStatusStacks(effectType) > 0;
        }

        public void TickStatusDurations()
        {
            for (int i = statuses.Count - 1; i >= 0; i--)
            {
                statuses[i].TickDuration();

                if (statuses[i].IsExpired)
                {
                    statuses.RemoveAt(i);
                }
            }
        }
    }
}

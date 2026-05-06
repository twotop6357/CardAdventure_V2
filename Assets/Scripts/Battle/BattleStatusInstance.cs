using System;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// Runtime status effect state with mutable stacks and duration.
    /// </summary>
    [Serializable]
    public sealed class BattleStatusInstance
    {
        public BattleStatusInstance(StatusEffectData data, int stacks)
        {
            Data = data;
            EffectType = data != null ? data.effectType : StatusEffectType.Strength;
            Stacks = Mathf.Max(0, stacks);
            RemainingDuration = data != null ? data.duration : 0;
            HasTimedDuration = data != null && data.duration > 0;
        }

        public BattleStatusInstance(StatusEffectType effectType, int stacks, int duration)
        {
            Data = null;
            EffectType = effectType;
            Stacks = Mathf.Max(0, stacks);
            RemainingDuration = Mathf.Max(0, duration);
            HasTimedDuration = duration > 0;
        }

        public StatusEffectData Data { get; }

        public StatusEffectType EffectType { get; }

        public int Stacks { get; private set; }

        public int RemainingDuration { get; private set; }

        public bool HasTimedDuration { get; }

        public bool IsExpired => Stacks <= 0 || (HasTimedDuration && RemainingDuration <= 0);

        public void AddStacks(int amount)
        {
            Stacks = Mathf.Max(0, Stacks + amount);

            if (HasTimedDuration && Data != null)
            {
                RemainingDuration = Mathf.Max(RemainingDuration, Data.duration);
            }
        }

        public void RefreshDuration(int duration)
        {
            if (!HasTimedDuration)
            {
                return;
            }

            RemainingDuration = Mathf.Max(RemainingDuration, duration);
        }

        public void ConsumeStacks(int amount)
        {
            Stacks = Mathf.Max(0, Stacks - amount);
        }

        public void TickDuration()
        {
            if (!HasTimedDuration)
            {
                return;
            }

            RemainingDuration--;
        }
    }
}

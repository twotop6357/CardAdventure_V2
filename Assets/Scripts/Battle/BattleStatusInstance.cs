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
            Stacks = Mathf.Max(0, stacks);
            RemainingDuration = data != null ? data.duration : 0;
        }

        public StatusEffectData Data { get; }

        public int Stacks { get; private set; }

        public int RemainingDuration { get; private set; }

        public bool IsExpired => Data == null || Stacks <= 0 || RemainingDuration < 0;

        public void AddStacks(int amount)
        {
            Stacks = Mathf.Max(0, Stacks + amount);

            if (Data != null && Data.duration > 0)
            {
                RemainingDuration = Mathf.Max(RemainingDuration, Data.duration);
            }
        }

        public void ConsumeStacks(int amount)
        {
            Stacks = Mathf.Max(0, Stacks - amount);
        }

        public void TickDuration()
        {
            if (Data == null || Data.duration == 0)
            {
                return;
            }

            RemainingDuration--;
        }
    }
}

namespace CardAdventure
{
    public readonly struct BattleTriggeredDamageRequest
    {
        public BattleTriggeredDamageRequest(int baseDamage, bool includeAttackBonuses, bool triggerDamageRewards)
            : this(baseDamage, includeAttackBonuses, triggerDamageRewards, false, default, 0) { }

        public BattleTriggeredDamageRequest(
            int baseDamage, bool includeAttackBonuses, bool triggerDamageRewards,
            bool isMultiHit,
            StatusEffectType playerGainStatus = default,
            int playerGainStatusStacks = 0)
        {
            BaseDamage             = baseDamage;
            IncludeAttackBonuses   = includeAttackBonuses;
            TriggerDamageRewards   = triggerDamageRewards;
            IsMultiHit             = isMultiHit;
            PlayerGainStatus       = playerGainStatus;
            PlayerGainStatusStacks = playerGainStatusStacks;
            HasPlayerStatusGain    = playerGainStatusStacks > 0;
        }

        public int  BaseDamage           { get; }
        public bool IncludeAttackBonuses { get; }
        public bool TriggerDamageRewards { get; }

        /// <summary>true 이면 다단 히트 시퀀스의 일부. UI가 전용 서브히트 애니메이션을 사용한다.</summary>
        public bool IsMultiHit { get; }

        /// <summary>히트 성공 시 플레이어에게 부여할 상태이상 타입 (MultiHitAndGainStrength 등).</summary>
        public StatusEffectType PlayerGainStatus       { get; }
        public int              PlayerGainStatusStacks { get; }
        public bool             HasPlayerStatusGain    { get; }
    }
}

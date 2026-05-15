namespace CardAdventure
{
    public readonly struct BattleTriggeredDamageRequest
    {
        public BattleTriggeredDamageRequest(int baseDamage, bool includeAttackBonuses, bool triggerDamageRewards)
        {
            BaseDamage = baseDamage;
            IncludeAttackBonuses = includeAttackBonuses;
            TriggerDamageRewards = triggerDamageRewards;
        }

        public int BaseDamage { get; }
        public bool IncludeAttackBonuses { get; }
        public bool TriggerDamageRewards { get; }
    }
}

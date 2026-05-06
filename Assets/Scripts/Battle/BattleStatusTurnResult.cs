namespace CardAdventure
{
    public readonly struct BattleStatusTurnResult
    {
        public BattleStatusTurnResult(int poisonDamage, int regenerationHealing)
        {
            PoisonDamage = poisonDamage;
            RegenerationHealing = regenerationHealing;
        }

        public int PoisonDamage { get; }

        public int RegenerationHealing { get; }

        public bool HasAnyEffect => PoisonDamage > 0 || RegenerationHealing > 0;
    }
}

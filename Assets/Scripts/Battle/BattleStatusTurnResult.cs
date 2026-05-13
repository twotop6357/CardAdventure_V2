namespace CardAdventure
{
    public readonly struct BattleStatusTurnResult
    {
        public BattleStatusTurnResult(int poisonDamage, int burnDamage, int regenerationHealing)
        {
            PoisonDamage = poisonDamage;
            BurnDamage = burnDamage;
            RegenerationHealing = regenerationHealing;
        }

        public int PoisonDamage { get; }

        public int BurnDamage { get; }

        public int RegenerationHealing { get; }

        public bool HasAnyEffect => PoisonDamage > 0 || BurnDamage > 0 || RegenerationHealing > 0;
    }
}

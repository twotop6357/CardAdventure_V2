namespace CardAdventure
{
    /// <summary>
    /// Runtime enemy state and cached intent for a single card battle.
    /// </summary>
    public sealed class BattleEnemyState
    {
        public BattleEnemyState(EnemyData data)
        {
            Data = data;
            string displayName = data != null ? data.enemyName : string.Empty;
            int maxHp = data != null ? data.maxHp : 1;
            int startingBlock = data != null ? data.startingBlock : 0;
            Combatant = new BattleCombatantState(displayName, maxHp, startingBlock);
        }

        public EnemyData Data { get; }

        public BattleCombatantState Combatant { get; }

        public int TurnIndex { get; private set; }

        public EnemyAction CurrentIntent { get; private set; }

        public EnemyAction SelectIntent()
        {
            CurrentIntent = Data != null ? Data.GetActionForTurn(TurnIndex) : null;
            return CurrentIntent;
        }

        public void AdvanceTurn()
        {
            TurnIndex++;
            CurrentIntent = null;
        }
    }
}

namespace CardAdventure
{
    public enum BattleCardPlayFailureReason
    {
        None,
        BattleNotActive,
        InvalidCard,
        CardNotInHand,
        NotEnoughEnergy,
        TargetRequired
    }

    public readonly struct BattleCardPlayResult
    {
        private BattleCardPlayResult(bool success, BattleCardPlayFailureReason failureReason)
        {
            Success = success;
            FailureReason = failureReason;
        }

        public bool Success { get; }

        public BattleCardPlayFailureReason FailureReason { get; }

        public static BattleCardPlayResult Succeeded()
        {
            return new BattleCardPlayResult(true, BattleCardPlayFailureReason.None);
        }

        public static BattleCardPlayResult Failed(BattleCardPlayFailureReason reason)
        {
            return new BattleCardPlayResult(false, reason);
        }
    }
}

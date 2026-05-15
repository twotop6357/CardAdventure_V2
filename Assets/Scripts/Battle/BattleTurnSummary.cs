using System.Collections.Generic;

namespace CardAdventure
{
    /// <summary>
    /// 전투 한 턴의 행동 기록.
    /// BattleManager가 전투 중 실시간으로 채우고, BattleRewardUIController가 결과 화면에 표시한다.
    /// </summary>
    public class BattleTurnSummary
    {
        public int          TurnNumber;
        public List<string> CardsUsed          = new List<string>();
        public int          DamageDealtToEnemy;
        public int          DamageTakenByPlayer;
        public string       EnemyActionDesc    = "—";
        public bool         EnemySkipped;
        public bool         PlayerDodged;
    }
}

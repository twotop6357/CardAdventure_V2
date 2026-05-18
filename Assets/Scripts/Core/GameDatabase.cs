using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "CardAdventure/GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        [Header("직업 데이터")]
        public List<JobClassInfo> allJobs = new List<JobClassInfo>();

        [Header("카드 데이터")]
        public List<CardData> allCards = new List<CardData>();

        /// <summary>
        /// 주어진 ID(이름)로 JobClassInfo를 검색합니다.
        /// </summary>
        public JobClassInfo GetJobById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var job in allJobs)
            {
                if (job != null && job.name == id)
                    return job;
            }
            return null;
        }

        /// <summary>
        /// 주어진 ID(이름)로 CardData를 검색합니다.
        /// </summary>
        public CardData GetCardById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var card in allCards)
            {
                if (card != null && card.name == id)
                    return card;
            }
            return null;
        }
    }
}

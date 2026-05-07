using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 카드 등급 하나에 해당하는 배경 스프라이트 세트.
    /// </summary>
    [System.Serializable]
    public struct CardSpriteSet
    {
        public Sprite common;
        public Sprite uncommon;
        public Sprite rare;
        public Sprite legendary;

        public Sprite GetByGrade(CardGrade grade) => grade switch
        {
            CardGrade.Common    => common,
            CardGrade.Uncommon  => uncommon,
            CardGrade.Rare      => rare,
            CardGrade.Legendary => legendary,
            _                   => common,
        };
    }

    /// <summary>
    /// 직업(CardClass) × 등급(CardGrade) 조합으로 카드 배경 스프라이트를 제공하는 ScriptableObject.
    /// BattleCardView.Bind() 시 자동으로 알맞은 배경 스프라이트를 적용한다.
    /// </summary>
    [CreateAssetMenu(fileName = "CardSpriteLibrary",
                     menuName  = "CardAdventure/Card Sprite Library",
                     order     = 4)]
    public class CardSpriteLibrary : ScriptableObject
    {
        [Header("직업별 카드 배경 스프라이트")]
        public CardSpriteSet warrior;
        public CardSpriteSet mage;
        public CardSpriteSet rogue;

        [Tooltip("Universal 카드 또는 직업 스프라이트가 없을 때 사용할 폴백")]
        public CardSpriteSet universal;

        /// <summary>
        /// 지정한 직업과 등급에 맞는 배경 스프라이트를 반환한다.
        /// 해당 스프라이트가 null이면 universal 폴백을 시도한다.
        /// </summary>
        public Sprite GetCardSprite(CardClass cardClass, CardGrade grade)
        {
            Sprite result = cardClass switch
            {
                CardClass.Warrior => warrior.GetByGrade(grade),
                CardClass.Mage    => mage.GetByGrade(grade),
                CardClass.Rogue   => rogue.GetByGrade(grade),
                _                 => null,
            };

            // 폴백: universal 세트
            if (result == null)
                result = universal.GetByGrade(grade);

            return result;
        }
    }
}

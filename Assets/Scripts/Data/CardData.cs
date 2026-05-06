using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 카드 타입 — 공격 / 방어 / 스킬 / 상태이상
    /// </summary>
    public enum CardType
    {
        Attack,
        Defense,
        Skill,
        StatusEffect
    }

    /// <summary>
    /// 카드를 사용할 수 있는 직업 계열
    /// </summary>
    public enum CardClass
    {
        Universal,  // 모든 직업 사용 가능
        Warrior,
        Mage,
        Rogue
    }

    /// <summary>
    /// 카드 등급
    /// </summary>
    public enum CardGrade
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    /// <summary>
    /// 단일 카드를 정의하는 ScriptableObject.
    /// 런타임에서는 이 데이터를 기반으로 RuntimeCard 인스턴스를 생성한다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "CardAdventure/Card Data", order = 1)]
    public class CardData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("카드 이름 (UI에 표시됨)")]
        public string cardName;

        [Tooltip("카드 사용에 필요한 에너지 비용")]
        [Range(0, 10)]
        public int energyCost;

        [Tooltip("카드 타입 (공격/방어/스킬/상태이상)")]
        public CardType cardType;

        [Tooltip("카드를 사용할 수 있는 직업")]
        public CardClass cardClass = CardClass.Universal;

        [Tooltip("카드 등급")]
        public CardGrade grade = CardGrade.Common;

        [Header("효과")]
        [Tooltip("카드 효과 수치 (데미지, 방어값 등 주 효과 수치)")]
        public int effectValue;

        [Tooltip("카드 효과 설명 (UI에 표시됨). {value} 를 effectValue로 치환 가능.")]
        [TextArea(2, 4)]
        public string effectDescription;

        [Header("비주얼")]
        [Tooltip("카드에 표시될 아이콘 스프라이트")]
        public Sprite cardIcon;

        [Tooltip("카드 배경 색상 (타입별 구분용)")]
        public Color cardColor = Color.white;

        [Header("추가 효과 (선택)")]
        [Tooltip("카드가 상태이상을 부여하는 경우 해당 상태이상 데이터")]
        public StatusEffectData statusEffect;

        [Tooltip("이 카드가 사용 후 소멸되는 단발성 카드인지 여부 (Exhaust)")]
        public bool isExhaust;

        [Tooltip("이 카드가 기본 덱에 포함되는 카드인지 여부")]
        public bool isStarterCard;

        /// <summary>
        /// effectDescription에서 {value}를 effectValue로 치환한 최종 설명 반환
        /// </summary>
        public string GetFormattedDescription()
        {
            if (string.IsNullOrEmpty(effectDescription))
                return string.Empty;
            return effectDescription.Replace("{value}", effectValue.ToString());
        }
    }
}

using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 상태이상 종류
    /// </summary>
    public enum StatusEffectType
    {
        Poison,     // 독: 매 턴 시작 시 HP 감소
        Weak,       // 약화: 공격 카드 데미지 25% 감소
        Vulnerable, // 취약: 받는 데미지 50% 증가
        Strength,   // 강화: 공격 카드 데미지 증가
        Regeneration // 재생: 매 턴 시작 시 HP 회복
    }

    /// <summary>
    /// 상태이상 하나를 정의하는 ScriptableObject.
    /// CardData.statusEffect 에서 참조하거나 독립적으로 카드 효과에 사용된다.
    /// CCGKit의 Stat / Effect / Trigger 구조를 래핑하는 대신,
    /// 싱글플레이어 전용 심플 구조로 구현한다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatusEffect", menuName = "CardAdventure/Status Effect Data", order = 3)]
    public class StatusEffectData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("상태이상 이름 (UI 표시용)")]
        public string effectName;

        [Tooltip("상태이상 종류")]
        public StatusEffectType effectType;

        [Tooltip("상태이상 설명")]
        [TextArea(2, 3)]
        public string description;

        [Header("수치")]
        [Tooltip("상태이상 부여 스택 수 / 회 수치")]
        [Range(1, 20)]
        public int defaultStacks = 1;

        [Tooltip("상태이상 지속 턴 수. 0이면 무제한(전투 종료 시까지).")]
        [Range(0, 10)]
        public int duration;

        [Header("비주얼")]
        [Tooltip("상태이상 아이콘")]
        public Sprite icon;

        [Tooltip("상태이상 표시 색상")]
        public Color displayColor = Color.green;
    }
}

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
    /// 카드의 구체적인 효과 종류.
    /// BattleManager는 이 값으로 효과를 분기한다.
    /// 새 카드를 추가할 때 이 enum에 항목을 추가하고 BattleManager에 케이스를 추가한다.
    /// </summary>
    public enum CardEffectType
    {
        // ── 공통 ──────────────────────────────────────
        None = 0,           // 효과 없음 / 미설정 (경고 출력)

        // ── 공격 (Attack) ─────────────────────────────
        BasicAttack = 100,  // 기본 공격: effectValue 만큼 피해
        ShieldBash  = 101,  // 방패치기: effectValue + 현재 방어막만큼 피해

        // ── 방어 (Defense) ────────────────────────────
        BasicDefense = 200, // 기본 방어: effectValue 만큼 방어막 획득

        // ── 스킬 (Skill) ──────────────────────────────
        Rage  = 300,        // 분노: 이번 턴 공격 카드 사용 시마다 effectValue 피해 보너스
        Taunt = 301,        // 도발: effectValue 방어막 + 적에게 약화 1턴

        // ── 상태이상 부여 (StatusEffect) ─────────────
        ApplyStatusToEnemy  = 400, // statusEffect ScriptableObject 를 적에게 부여
        ApplyStatusToPlayer = 401, // statusEffect ScriptableObject 를 플레이어에게 부여

        // ── 공격 확장 ─────────────────────────────
        DoubleStrike         = 102, // effectValue 피해 × 2회
        BerserkerAttack      = 103, // 자신에게 secondaryValue 피해, 적에게 effectValue 피해 (비용 대비 고화력)
        AttackAndDefend      = 104, // effectValue 피해 + secondaryValue 방어막 동시 획득
        AttackAndApplyStatus = 105, // effectValue 피해 후 statusEffect 를 적에게 secondaryValue 스택 부여

        // ── 방어 확장 ─────────────────────────────
        DefenseAndDraw = 202,       // effectValue 방어막 + secondaryValue 장 드로우

        // ── 스킬 확장 ─────────────────────────────
        DrawCards    = 302,         // effectValue 장 드로우 (손패 보충)
        GainStrength = 303,         // 자신에게 강화(Strength) effectValue 스택 부여
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
        [Tooltip("카드의 구체적인 효과 종류. BattleManager가 이 값으로 효과를 분기한다.")]
        public CardEffectType effectType = CardEffectType.None;

        [Tooltip("카드 효과 수치 (데미지, 방어값 등 주 효과 수치)")]
        public int effectValue;

        [Tooltip("보조 효과 수치 (두 번째 효과가 있을 때 사용. 예: 방패치기 추가 피해 배율 등)")]
        public int secondaryValue;

        [Tooltip("카드 효과 설명 (UI에 표시됨). {value} 를 effectValue로, {value2} 를 secondaryValue로 치환 가능.")]
        [TextArea(2, 4)]
        public string effectDescription;

        [Header("비주얼")]
        [Tooltip("카드에 표시될 아이콘 스프라이트")]
        public Sprite cardIcon;

        [Tooltip("카드 배경 색상 (타입별 구분용)")]
        public Color cardColor = Color.white;

        [Header("추가 효과 (선택)")]
        [Tooltip("상태이상을 부여하는 카드의 경우 해당 상태이상 데이터 (effectType이 ApplyStatus* 일 때 참조)")]
        public StatusEffectData statusEffect;

        [Tooltip("이 카드가 사용 후 소멸되는 단발성 카드인지 여부 (Exhaust)")]
        public bool isExhaust;

        [Tooltip("이 카드가 기본 덱에 포함되는 카드인지 여부")]
        public bool isStarterCard;

        /// <summary>
        /// effectDescription에서 {value}/{value2}를 실제 수치로 치환한 최종 설명 반환
        /// </summary>
        public string GetFormattedDescription()
        {
            if (string.IsNullOrEmpty(effectDescription))
                return string.Empty;
            return effectDescription
                .Replace("{value}", effectValue.ToString())
                .Replace("{value2}", secondaryValue.ToString());
        }
    }
}

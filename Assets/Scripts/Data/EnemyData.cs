using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 적이 한 턴에 취하는 행동 종류
    /// </summary>
    public enum EnemyActionType
    {
        Attack,         // 플레이어에게 데미지
        Defend,         // 방어도(Block) 획득
        Buff,           // 자신에게 강화 부여
        DebuffPlayer,   // 플레이어에게 상태이상 부여
        HealSelf        // 자신 HP 회복
    }

    /// <summary>
    /// 적의 단일 행동 패턴 항목.
    /// EnemyData.actionPattern 리스트에 순서대로 등록하여 순환 사용한다.
    /// </summary>
    [Serializable]
    public class EnemyAction
    {
        [Tooltip("이 행동의 종류")]
        public EnemyActionType actionType;

        [Tooltip("행동 수치 (데미지, 방어값, 회복량 등)")]
        public int value;

        [Tooltip("UI에 표시될 행동 설명 (예: '강타 6'). 비어있으면 자동 생성.")]
        public string intentDescription;

        [Tooltip("이 행동에 연결된 상태이상 (DebuffPlayer / Buff 타입에서 사용)")]
        public StatusEffectData statusEffect;

        [Tooltip("상태이상 부여 스택 수")]
        public int statusEffectStacks = 1;

        /// <summary>
        /// 인텐트 설명 자동 생성 (intentDescription이 비어있을 때 사용)
        /// </summary>
        public string GetIntentDescription()
        {
            if (!string.IsNullOrEmpty(intentDescription))
            {
                return intentDescription;
            }

            return actionType switch
            {
                EnemyActionType.Attack => $"공격 {value}",
                EnemyActionType.Defend => $"방어 {value}",
                EnemyActionType.Buff => statusEffect != null
                    ? $"{statusEffect.effectName} {statusEffectStacks} 적용"
                    : "강화",
                EnemyActionType.DebuffPlayer => statusEffect != null
                    ? $"{statusEffect.effectName} {statusEffectStacks} 부여"
                    : "약화",
                EnemyActionType.HealSelf => $"회복 {value}",
                _ => "알 수 없음"
            };
        }
    }

    /// <summary>
    /// 적 하나를 정의하는 ScriptableObject.
    /// 행동 패턴은 actionPattern 리스트를 순서대로 순환한다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "CardAdventure/Enemy Data", order = 2)]
    public class EnemyData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("적 이름 (UI 표시용)")]
        public string enemyName;

        [Tooltip("적 최대 HP")]
        [Range(1, 9999)]
        public int maxHp = 30;

        [Tooltip("적 시작 방어도 (전투 시작 시 기본 방어도)")]
        public int startingBlock;

        [Header("행동 패턴")]
        [Tooltip("적의 행동 패턴 목록. 매 턴 순서대로 순환한다.")]
        public List<EnemyAction> actionPattern = new List<EnemyAction>();

        [Tooltip("행동 패턴을 순환할지, 랜덤으로 선택할지 여부")]
        public bool randomizeActions;

        [Header("보상")]
        [Tooltip("적 처치 시 획득 골드 (최솟값)")]
        public int rewardGoldMin = 5;

        [Tooltip("적 처치 시 획득 골드 (최댓값)")]
        public int rewardGoldMax = 10;

        [Tooltip("적 처치 후 카드 보상 후보 풀 (이 중 몇 장을 선택지로 제시)")]
        public List<CardData> rewardCardPool = new List<CardData>();

        [Header("비주얼")]
        [Tooltip("적 스프라이트 (SPUM 미사용 시 정적 이미지로 대체)")]
        public Sprite enemySprite;

        [Tooltip("SPUM 프리팹 경로 (Resources/SPUM/SPUM_Units/ 기준 상대 경로). 비어있으면 정적 스프라이트 사용.")]
        public string spumPrefabPath;

        [Header("전투 인트로 연출")]
        [Tooltip("전투 시작 전 NPC 등장 연출 데이터. null이면 기본 '몬스터가 나타났다!' 메시지를 사용한다.")]
        public BattleIntroData introData;

        [Header("특수")]
        [Tooltip("엘리트 또는 보스 여부")]
        public bool isBoss;

        /// <summary>
        /// 현재 턴 인덱스를 기반으로 다음 행동을 반환한다.
        /// </summary>
        /// <param name="turnIndex">현재 턴 번호 (0부터 시작)</param>
        public EnemyAction GetActionForTurn(int turnIndex)
        {
            if (actionPattern == null || actionPattern.Count == 0)
                return null;

            if (randomizeActions)
            {
                return actionPattern[UnityEngine.Random.Range(0, actionPattern.Count)];
            }

            return actionPattern[turnIndex % actionPattern.Count];
        }
    }
}

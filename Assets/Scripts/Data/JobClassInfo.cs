using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 직업(클래스) 정보를 담는 ScriptableObject.
    /// 직업 선택/변경 UI에서 표시할 이름, 설명, 능력치, 미리보기 스프라이트 등을 정의한다.
    ///
    /// 생성: Assets > Create > CardAdventure > Job Class Info
    /// </summary>
    [CreateAssetMenu(fileName = "NewJobClass", menuName = "CardAdventure/Job Class Info", order = 5)]
    public class JobClassInfo : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("직업 선택 UI에 표시될 직업 이름 (예: 전사)")]
        public string displayName;

        [Tooltip("직업 선택 화면 하단에 표시될 직업 설명 텍스트")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("카드 시스템에서 사용하는 CardClass 값 (카드 필터링 기준)")]
        public CardClass cardClass;

        [Header("능력치 (1~5)")]
        [Tooltip("공격 능력치 수준 (1 = 낮음, 5 = 최고)")]
        [Range(1, 5)] public int attackStars  = 1;

        [Tooltip("방어 능력치 수준 (1 = 낮음, 5 = 최고)")]
        [Range(1, 5)] public int defenseStars = 1;

        [Tooltip("마법 능력치 수준 (1 = 낮음, 5 = 최고)")]
        [Range(1, 5)] public int magicStars   = 1;

        [Tooltip("난이도 수준 (1 = 낮음, 5 = 최고)")]
        [Range(1, 5)] public int difficulty   = 1;

        [Header("비주얼")]
        [Tooltip("직업 선택 UI에서 보여줄 캐릭터 미리보기 스프라이트. SPUM 생성 캐릭터의 Idle 프레임을 사용하는 것을 권장.")]
        public Sprite previewSprite;

        [Tooltip("배틀 씬 HUD의 PlayerFaceImage에 표시할 직업별 초상화 스프라이트.")]
        public Sprite battleFaceSprite;

        [Tooltip("어드벤처 씬에서 플레이어로 사용할 IdleFront 첫 프레임 스프라이트.")]
        public Sprite playerIdleSprite;

        [Tooltip("어드벤처 씬에서 플레이어 비주얼에 적용할 Animator Controller.")]
        public RuntimeAnimatorController playerAnimatorController;

        [Tooltip("Idle 애니메이션 스프라이트를 기준 키에 맞추기 위한 배율. 직업별 플레이어 애니메이션 시트 크기 차이를 보정한다.")]
        public float playerIdleVisualScaleMultiplier = 1f;

        [Tooltip("이동 방향에 따른 SpriteRenderer.flipX 로직을 반전시킬지 여부. (스프라이트 시트의 기본 방향이 다를 경우 사용)")]
        public bool invertVisualFlip = false;

        [Header("게임 데이터")]
        [Tooltip("이 직업을 선택했을 때 시작 덱에 들어갈 카드 목록")]
        public List<CardData> starterCards = new List<CardData>();

        [Tooltip("이 직업의 기본 최대 HP")]
        public int baseMaxHp = 50;

        [Tooltip("이 직업의 기본 최대 에너지 (배틀 시 매 턴 회복량)")]
        public int baseMaxEnergy = 3;
    }
}

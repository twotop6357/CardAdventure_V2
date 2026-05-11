using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 전투 시작 전 NPC 등장 연출 데이터.
    ///
    /// 사용처:
    ///   - EnemyData.introData 필드에 연결 → 해당 적과 전투 시작 시 자동 적용
    ///   - BattleIntroDirector.SetIntroData()로 런타임에 직접 주입 (어드벤처 씬 연동 등)
    ///
    /// 흐름 우선순위:
    ///   1. dialogueData가 있으면 dialogueData.lines를 사용
    ///   2. 없으면 speakerName + defaultSummonMessage 표시
    ///   3. 모두 비어있으면 "몬스터가 나타났다!" 표시
    /// </summary>
    [CreateAssetMenu(menuName = "CardAdventure/Battle Intro Data", fileName = "BattleIntro_")]
    public class BattleIntroData : ScriptableObject
    {
        [Header("NPC 초상화")]
        [Tooltip("전투 시작 시 화면 오른쪽에서 등장할 NPC 이미지 스프라이트")]
        public Sprite npcPortrait;

        [Header("대화 데이터 (선택)")]
        [Tooltip("null이면 speakerName + defaultSummonMessage를 단일 대사로 사용한다.")]
        public DialogueData dialogueData;

        [Header("기본 소환 메시지 (dialogueData 없을 때)")]
        [Tooltip("대화창 이름 박스에 표시할 화자 이름. 비워두면 이름 박스를 숨긴다.")]
        public string speakerName = "";

        [Tooltip("dialogueData가 없을 때 대화창에 표시할 기본 메시지")]
        [TextArea(2, 4)]
        public string defaultSummonMessage = "몬스터가 나타났다!";

        // ── 헬퍼 ──────────────────────────────────────────────────

        /// <summary>실제 사용할 화자 이름을 반환한다.</summary>
        public string GetSpeakerName()
        {
            if (dialogueData != null && !string.IsNullOrEmpty(dialogueData.speakerName))
                return dialogueData.speakerName;
            return speakerName;
        }

        /// <summary>실제 사용할 대사 배열을 반환한다. 항상 1개 이상.</summary>
        public string[] GetLines()
        {
            if (dialogueData != null
                && dialogueData.lines != null
                && dialogueData.lines.Length > 0)
            {
                return dialogueData.lines;
            }

            string msg = string.IsNullOrEmpty(defaultSummonMessage)
                ? "몬스터가 나타났다!"
                : defaultSummonMessage;

            return new[] { msg };
        }
    }
}

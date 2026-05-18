using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// NPC 한 명의 대화 데이터. 화자 이름과 한 줄씩 표시할 대사 배열을 담는다.
    /// Assets/ScriptableObjects/Dialogues/ 폴더에 저장 권장.
    /// </summary>
    [CreateAssetMenu(menuName = "CardAdventure/Dialogue Data", fileName = "NewDialogue")]
    public class DialogueData : ScriptableObject
    {
        [Header("화자 정보")]
        [Tooltip("대화창 이름 박스에 표시될 이름. 비워두면 이름 박스를 숨긴다.")]
        public string speakerName;

        [Tooltip("대화창 왼쪽 초상화 슬롯에 표시할 이미지. 비워두면 초상화 슬롯을 숨긴다.")]
        public Sprite speakerPortrait;

        [Header("대화 내용")]
        [Tooltip("Space를 누를 때마다 한 줄씩 넘어간다.")]
        [TextArea(2, 5)]
        public string[] lines;
    }
}

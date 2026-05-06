using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// NPC 오브젝트에 붙이는 컴포넌트.
    /// 플레이어가 상호작용 범위(Collider2D IsTrigger)에 들어오면
    /// DialogueManager에 이 NPC를 등록하고, 나가면 해제한다.
    /// Space 입력 처리는 DialogueManager가 담당한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NpcInteractable : MonoBehaviour
    {
        [Header("대화 데이터")]
        [SerializeField] private DialogueData dialogueData;

        [Header("상호작용 힌트 (선택)")]
        [Tooltip("플레이어가 범위 안에 있을 때 표시할 힌트 오브젝트 (예: 'Space' 말풍선)")]
        [SerializeField] private GameObject interactHint;

        [Header("설정")]
        [Tooltip("대화 종료 후 다시 말을 걸 수 있으면 true")]
        [SerializeField] private bool repeatable = true;

        private bool hasSpoken;

        public DialogueData DialogueData => dialogueData;

        private void Awake()
        {
            // 트리거 확인
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[NpcInteractable] '{gameObject.name}'의 Collider2D를 IsTrigger=true로 자동 설정했습니다.", this);
            }

            if (interactHint != null)
                interactHint.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (!repeatable && hasSpoken) return;
            if (dialogueData == null) return;

            DialogueManager.Instance?.RegisterNpc(this);

            if (interactHint != null)
                interactHint.SetActive(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            DialogueManager.Instance?.UnregisterNpc(this);

            if (interactHint != null)
                interactHint.SetActive(false);
        }

        /// <summary>DialogueManager가 대화 종료 후 호출한다.</summary>
        public void OnDialogueFinished()
        {
            hasSpoken = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null) return;

            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
            if (col is CircleCollider2D circle)
                Gizmos.DrawSphere(transform.position, circle.radius);
            else if (col is BoxCollider2D box)
                Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);

            if (dialogueData != null)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.7f,
                    $"[NPC] {dialogueData.speakerName}");
            }
        }
#endif
    }
}

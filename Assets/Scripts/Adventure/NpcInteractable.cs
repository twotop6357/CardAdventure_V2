using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// NPC 오브젝트에 붙이는 컴포넌트.
    ///
    /// 변경 사항 (Kinematic-Kinematic 트리거 미발동 문제 해결):
    ///   - OnTriggerEnter/Exit2D 제거. 물리 트리거 의존 없음.
    ///   - AllNpcs 정적 리스트에 자신을 등록하여
    ///     DialogueManager가 매 프레임 거리 기반으로 상호작용 대상을 결정한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NpcInteractable : MonoBehaviour
    {
        // ── 전체 NPC 목록 (DialogueManager 거리 검사용) ─────────
        public static readonly List<NpcInteractable> AllNpcs = new List<NpcInteractable>();

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

        /// <summary>플레이어가 상호작용할 수 있는 상태인지 반환.</summary>
        public bool CanInteract() => repeatable || !hasSpoken;

        // ── Unity 생명주기 ────────────────────────────────────────

        private void Awake()
        {
            if (interactHint != null)
                interactHint.SetActive(false);
        }

        private void OnEnable()
        {
            if (!AllNpcs.Contains(this))
                AllNpcs.Add(this);
        }

        private void OnDisable()
        {
            AllNpcs.Remove(this);
            if (interactHint != null)
                interactHint.SetActive(false);
        }

        // ── DialogueManager 호출용 공개 API ──────────────────────

        /// <summary>상호작용 힌트 표시/숨김.</summary>
        public void SetHintActive(bool active)
        {
            if (interactHint != null)
                interactHint.SetActive(active);
        }

        /// <summary>DialogueManager가 대화 종료 후 호출한다.</summary>
        public void OnDialogueFinished()
        {
            hasSpoken = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 상호작용 범위 시각화 (1.2 유닛)
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, 1.2f);

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

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CardAdventure
{
    /// <summary>
    /// 어드벤처 씬 대화 시스템 싱글턴.
    /// - NpcInteractable이 플레이어 진입/이탈 시 RegisterNpc / UnregisterNpc 호출
    /// - Space 입력 시: 대화 미활성 → 대화 시작 / 활성 → 타이핑 스킵 또는 다음 줄 진행
    /// - 대화 중 PlayerController 이동 차단, 종료 시 해제
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        // ── 싱글턴 ─────────────────────────────────────────────
        public static DialogueManager Instance { get; private set; }

        // ── Inspector 직렬화 ───────────────────────────────────
        [Header("UI 참조")]
        [SerializeField] private DialogueView dialogueView;

        // ── 런타임 상태 ────────────────────────────────────────
        private DialogueData     currentDialogue;
        private NpcInteractable  currentNpc;
        private NpcInteractable  pendingNpc;      // 범위 안에 있는 NPC
        private PlayerController activePlayer;
        private int              lineIndex;
        private bool             isDialogueActive;

        // ── 공개 프로퍼티 ──────────────────────────────────────
        public bool IsDialogueActive => isDialogueActive;

        /// <summary>대화가 완전히 종료될 때 발행</summary>
        public event Action OnDialogueEnded;

        // ══════════════════════════════════════════════════════
        //  Unity 생명주기
        // ══════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // 씬 내 PlayerController 자동 탐색
            activePlayer = FindFirstObjectByType<PlayerController>();

            if (dialogueView == null)
                dialogueView = FindFirstObjectByType<DialogueView>();

            if (dialogueView == null)
                Debug.LogWarning("[DialogueManager] DialogueView를 찾을 수 없습니다. Inspector에서 직접 연결하거나 씬에 배치하세요.", this);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            bool spaceDown = Keyboard.current.spaceKey.wasPressedThisFrame;
            if (!spaceDown) return;

            if (isDialogueActive)
            {
                HandleSpaceDuringDialogue();
            }
            else if (pendingNpc != null)
            {
                BeginDialogue(pendingNpc);
            }
        }

        // ══════════════════════════════════════════════════════
        //  NPC 등록 (NpcInteractable이 호출)
        // ══════════════════════════════════════════════════════

        public void RegisterNpc(NpcInteractable npc)
        {
            pendingNpc = npc;
        }

        public void UnregisterNpc(NpcInteractable npc)
        {
            if (pendingNpc == npc)
                pendingNpc = null;
        }

        // ══════════════════════════════════════════════════════
        //  대화 시작 / 진행 / 종료
        // ══════════════════════════════════════════════════════

        private void BeginDialogue(NpcInteractable npc)
        {
            if (npc == null) return;
            DialogueData data = npc.DialogueData;
            if (data == null || data.lines == null || data.lines.Length == 0)
            {
                Debug.LogWarning($"[DialogueManager] '{npc.gameObject.name}'에 DialogueData가 없거나 대사가 비어 있습니다.", npc);
                return;
            }

            currentDialogue = data;
            currentNpc      = npc;
            lineIndex       = 0;
            isDialogueActive = true;

            // 플레이어 이동 차단
            activePlayer?.SetInputEnabled(false);

            // NPC 쪽 바라보게 회전 (PlayerController 확장 없이 간단 처리)
            if (activePlayer != null)
                FacePlayerTowardNpc(npc);

            // 대화창 표시
            if (dialogueView != null)
                dialogueView.Show(data.speakerName, data.lines[0]);
        }

        private void HandleSpaceDuringDialogue()
        {
            if (dialogueView != null && dialogueView.IsTyping)
            {
                // 타이핑 진행 중 → 전체 텍스트 즉시 표시
                dialogueView.SkipTypewriter();
            }
            else
            {
                // 타이핑 완료 → 다음 줄 또는 대화 종료
                AdvanceLine();
            }
        }

        private void AdvanceLine()
        {
            lineIndex++;

            if (lineIndex >= currentDialogue.lines.Length)
            {
                EndDialogue();
            }
            else
            {
                dialogueView?.ShowLine(currentDialogue.lines[lineIndex]);
            }
        }

        private void EndDialogue()
        {
            isDialogueActive = false;

            dialogueView?.Hide();

            // NPC에 종료 알림 (repeatable 플래그 처리)
            currentNpc?.OnDialogueFinished();

            currentDialogue = null;
            currentNpc      = null;

            // 플레이어 이동 복원
            activePlayer?.SetInputEnabled(true);

            OnDialogueEnded?.Invoke();
        }

        // ══════════════════════════════════════════════════════
        //  헬퍼
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// 플레이어 스프라이트를 NPC 방향으로 돌린다.
        /// PlayerController.SetInputEnabled(false) 이후에 호출해야 한다.
        /// </summary>
        private void FacePlayerTowardNpc(NpcInteractable npc)
        {
            if (activePlayer == null || npc == null) return;

            Vector2 dir = npc.transform.position - activePlayer.transform.position;

            // PlayerController에 FaceDirection 공개 메서드가 생기면 그것을 사용.
            // 현재는 SpriteRenderer flipX만 처리한다.
            SpriteRenderer sr = activePlayer.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && Mathf.Abs(dir.x) > 0.1f)
                sr.flipX = dir.x < 0f;
        }

        // ══════════════════════════════════════════════════════
        //  외부 주입 (씬 빌더 등에서 사용)
        // ══════════════════════════════════════════════════════

        public void SetDialogueView(DialogueView view) => dialogueView = view;
        public void SetActivePlayer(PlayerController player) => activePlayer = player;
    }
}

using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 전직관 NPC의 타일 정렬, 방향 전환, 직업 선택 UI 연동을 처리하는 스크립트.
    ///
    /// 동작 흐름:
    ///   1. DialogueManager가 대화를 시작할 때 FaceToward(playerPos) 호출
    ///   2. FaceToward 내부에서 DialogueManager.OnDialogueEnded를 구독
    ///   3. 대화가 끝나면 JobSelectionUI.Open() 호출
    ///   4. 플레이어가 직업을 결정하면 OnJobConfirmed 콜백으로 GameDataManager 업데이트
    ///      및 PlayerController 비주얼 갱신 요청
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class JobChangerNpc : MonoBehaviour
    {
        [Header("Tile Alignment")]
        [SerializeField] private bool snapToNearestTileOnStart = true;
        [SerializeField] private float moveUnitSize = 1f;
        [SerializeField] private BoxCollider2D footCollider;

        [Header("Visual")]
        [SerializeField] private bool alignVisualToReferenceHeight = true;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("직업 선택 UI")]
        [Tooltip("씬에 배치된 JobChangeUIController 참조. 설정하지 않으면 씬에서 자동 탐색.")]
        [SerializeField] private JobChangeUIController jobChangeUI;
        [Tooltip("직업 변경 확정 직후 출력할 후속 대화.")]
        [SerializeField] private DialogueData afterJobChangeDialogue;

        private Animator  animator;
        private Rigidbody2D rb;
        private bool isWaitingForDialogueEnd = false;

        private void Awake()
        {
            animator    = GetComponent<Animator>();
            rb          = GetComponent<Rigidbody2D>();
            footCollider = footCollider != null ? footCollider : GetComponent<BoxCollider2D>();
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();

            rb.gravityScale  = 0f;
            rb.freezeRotation = true;
            rb.bodyType       = RigidbodyType2D.Kinematic;
            rb.interpolation  = RigidbodyInterpolation2D.Interpolate;

            AlignVisualToReferenceHeight();
            ConfigureTileCollider();
        }

        private void Start()
        {
            SnapToNearestTile();

            // JobChangeUIController 자동 탐색 (씬에 있는 경우)
            if (jobChangeUI == null)
                jobChangeUI = FindFirstObjectByType<JobChangeUIController>(FindObjectsInactive.Include);
        }

        private void OnDestroy()
        {
            UnsubscribeFromDialogue();
        }

        // ══════════════════════════════════════════════════════
        //  공개 API
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// DialogueManager가 대화를 시작할 때 호출한다.
        /// NPC를 플레이어 방향으로 돌리고, 대화가 끝나면 직업 선택 UI를 열도록 예약한다.
        /// </summary>
        public void FaceToward(Vector2 targetPosition)
        {
            if (animator == null) return;

            Vector2 dir = targetPosition - (Vector2)transform.position;
            if (dir.sqrMagnitude < 0.01f) return;

            // 가장 멀리 떨어진 축 기준 4방향 결정
            Vector2 faceDir;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                faceDir = dir.x > 0 ? Vector2.right : Vector2.left;
            else
                faceDir = dir.y > 0 ? Vector2.up : Vector2.down;

            animator.SetFloat("DirectionX", faceDir.x);
            animator.SetFloat("DirectionY", faceDir.y);

            // 대화 종료 구독 (중복 구독 방지)
            SubscribeToDialogueEnd();
        }

        // ══════════════════════════════════════════════════════
        //  내부: 타일 정렬 및 비주얼
        // ══════════════════════════════════════════════════════

        private void SnapToNearestTile()
        {
            if (!snapToNearestTileOnStart) return;
            AdventureGridUtility.SnapOwnerFootToNearestCell(transform, rb, footCollider, moveUnitSize);
        }

        private void AlignVisualToReferenceHeight()
        {
            if (!alignVisualToReferenceHeight || spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            float visualScale = AdventureGridUtility.GetVisualScaleForReferenceHeight(spriteRenderer.sprite);
            transform.localScale = new Vector3(visualScale, visualScale, transform.localScale.z);
        }

        private void ConfigureTileCollider()
        {
            if (footCollider == null) return;

            Vector2 cellSize = AdventureGridUtility.GetCellSize(moveUnitSize);
            AdventureGridUtility.ConfigureFootCollider(footCollider, transform, spriteRenderer, cellSize);
            AdventureGridUtility.DisableSolidCircles(gameObject);
        }

        // ══════════════════════════════════════════════════════
        //  내부: 대화 종료 → 직업 선택 UI 연동
        // ══════════════════════════════════════════════════════

        private void SubscribeToDialogueEnd()
        {
            if (isWaitingForDialogueEnd) return;
            if (DialogueManager.Instance == null) return;

            isWaitingForDialogueEnd = true;
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
        }

        private void UnsubscribeFromDialogue()
        {
            if (!isWaitingForDialogueEnd) return;
            isWaitingForDialogueEnd = false;
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        }

        private void HandleDialogueEnded()
        {
            UnsubscribeFromDialogue();
            OpenJobSelectionUI();
        }

        private void OpenJobSelectionUI()
        {
            if (jobChangeUI == null)
            {
                Debug.LogWarning("[JobChangerNpc] JobChangeUIController를 찾지 못했습니다. " +
                    "씬의 JobChangeCanvas에 JobChangeUIController 컴포넌트를 추가하거나 Inspector에서 직접 할당하세요.", this);
                return;
            }

            jobChangeUI.OnJobConfirmed -= HandleJobConfirmed;
            jobChangeUI.OnCancelled    -= HandleJobCancelled;
            jobChangeUI.OnJobConfirmed += HandleJobConfirmed;
            jobChangeUI.OnCancelled    += HandleJobCancelled;

            jobChangeUI.Open(0);
        }

        private void HandleJobConfirmed(JobClassInfo selectedJob)
        {
            jobChangeUI.OnJobConfirmed -= HandleJobConfirmed;
            jobChangeUI.OnCancelled    -= HandleJobCancelled;

            if (selectedJob == null) return;

            // GameDataManager에 선택된 직업 및 덱/HP 갱신
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.UpdateJob(selectedJob);
                Debug.Log($"[JobChangerNpc] 직업 변경 프로세스 완료: {selectedJob.displayName}");
            }

            // PlayerController 비주얼 갱신 (직업 변경 후 키 기준 재적용)
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                player.ApplyJobVisual(selectedJob);

            if (afterJobChangeDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.BeginDialogue(afterJobChangeDialogue);
            }
        }

        private void HandleJobCancelled()
        {
            jobChangeUI.OnJobConfirmed -= HandleJobConfirmed;
            jobChangeUI.OnCancelled    -= HandleJobCancelled;
            Debug.Log("[JobChangerNpc] 직업 변경 취소됨.");
        }
    }
}

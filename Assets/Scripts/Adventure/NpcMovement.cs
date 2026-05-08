using System.Collections;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// NPC 타일 기반 랜덤 배회 컴포넌트.
    ///
    /// 동작 규칙:
    ///   - Start() 시점 위치를 홈(home)으로 기억한다.
    ///   - 매 waitTime(기본 2초)마다 상하좌우 중 하나를 랜덤으로 골라 한 칸 이동한다.
    ///   - 이동 목표 타일이 홈에서 wanderRadius 초과이면 방향을 건너뛴다.
    ///   - 이동 목표에 장애물(솔리드 콜라이더)이 있으면 방향을 건너뛴다.
    ///   - 이동 중에는 방향 변경 없이 목표 타일까지 완주한다 (PlayerController와 동일 규칙).
    ///   - 대화 중(DialogueManager.IsDialogueActive)에는 대기 타이머를 멈춘다.
    ///
    /// 충돌 규칙:
    ///   - Rigidbody2D(Kinematic) + 솔리드 BoxCollider2D 로 물리 밀침 없이
    ///     OverlapBox 검사를 통해 플레이어/벽과 상호 차단한다.
    ///   - 대화 판정은 NpcInteractable이 발밑 BoxCollider2D 기준으로 계산한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class NpcMovement : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────
        [Header("이동 설정")]
        [SerializeField] private float moveSpeed    = 4f;
        [SerializeField] private float waitTime     = 2f;
        [SerializeField] private float wanderRadius = 3f;
        [SerializeField] private float moveUnitSize = 1f;

        [Header("충돌 설정")]
        [Tooltip("장애물로 취급할 레이어 (기본: Ignore Raycast 제외 전체)")]
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Tile Alignment")]
        [SerializeField] private BoxCollider2D footCollider;
        [SerializeField] private bool alignVisualToTile = true;
        [SerializeField] private float maxVisualWidthInTiles = 1f;
        [SerializeField] private float maxVisualHeightInTiles = 2f;

        [Header("비주얼 (선택)")]
        [Tooltip("좌우 이동 시 flipX로 방향 반전할 SpriteRenderer")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        // ── 런타임 상태 ────────────────────────────────────────
        private Rigidbody2D rb;
        private Vector2     homePosition;
        private Vector2     targetPosition;
        private bool        isMoving;

        private static readonly Vector2[] CardinalDirs =
            { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        // ══════════════════════════════════════════════════════
        //  Unity 생명주기
        // ══════════════════════════════════════════════════════

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale  = 0f;
            rb.freezeRotation = true;
            rb.bodyType      = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (obstacleLayer.value == 0)
                obstacleLayer = ~LayerMask.GetMask("Ignore Raycast");

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            ResolveMoveUnitSize();
            AlignVisualToTile();
            ConfigureTileColliders();
        }

        private void ResolveMoveUnitSize()
        {
            Grid grid = FindFirstObjectByType<Grid>();
            if (grid == null) return;

            Vector3 cellSize = grid.cellSize;
            float resolvedSize = Mathf.Abs(cellSize.x) > 0.001f ? Mathf.Abs(cellSize.x) : Mathf.Abs(cellSize.y);
            if (resolvedSize > 0.001f)
            {
                moveUnitSize = resolvedSize;
            }
        }

        private void ConfigureTileColliders()
        {
            if (footCollider == null)
            {
                footCollider = GetComponent<BoxCollider2D>();
            }

            if (footCollider == null)
            {
                footCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            Vector2 cellSize = AdventureGridUtility.GetCellSize(moveUnitSize);
            AdventureGridUtility.ConfigureFootCollider(footCollider, transform, spriteRenderer, cellSize);
            AdventureGridUtility.DisableSolidCircles(gameObject);
        }

        private void AlignVisualToTile()
        {
            if (!alignVisualToTile || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            Vector2 cellSize = AdventureGridUtility.GetCellSize(moveUnitSize);
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            float scale = AdventureGridUtility.GetVisualScaleForReferenceHeight(spriteRenderer.sprite);

            if (spriteRenderer.transform == transform)
            {
                Vector3 localScale = transform.localScale;
                transform.localScale = new Vector3(scale, scale, localScale.z);
                return;
            }

            spriteRenderer.transform.localScale = new Vector3(scale, scale, 1f);
            float finalHeight = spriteSize.y * scale;
            Vector3 localPos = spriteRenderer.transform.localPosition;
            localPos.y = (finalHeight - cellSize.y) * 0.5f;
            spriteRenderer.transform.localPosition = localPos;
        }

        private void Start()
        {
            Vector2 snappedFootPosition = SnapToUnit(GetFootCenter(transform.position));
            homePosition = snappedFootPosition;
            targetPosition = GetRootPositionForFootCenter(snappedFootPosition);
            transform.position = targetPosition;
            rb.position = targetPosition;

            // 초기 위치 타일 예약
            GridOccupancy.TryReserve(snappedFootPosition, moveUnitSize);

            StartCoroutine(WanderLoop());
        }

        private void OnDestroy()
        {
            GridOccupancy.Release(GetFootCenter(targetPosition), moveUnitSize);
        }

        private void FixedUpdate()
        {
            if (!isMoving) return;

            Vector2 next = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);

            if (Vector2.Distance(next, targetPosition) < 0.001f)
            {
                rb.MovePosition(targetPosition);
                isMoving = false;
            }
        }

        // ══════════════════════════════════════════════════════
        //  배회 루프
        // ══════════════════════════════════════════════════════

        private IEnumerator WanderLoop()
        {
            while (true)
            {
                // ── 2초 대기 (대화 중에는 타이머 정지) ──────────
                float elapsed = 0f;
                while (elapsed < waitTime)
                {
                    if (!IsDialogueActive())
                        elapsed += Time.deltaTime;
                    yield return null;
                }

                // ── 이동 중이면 완료 대기 ─────────────────────
                while (isMoving)
                    yield return null;

                // ── 대화 중이면 해제될 때까지 대기 ──────────────
                while (IsDialogueActive())
                    yield return null;

                // ── 이동 시도 ─────────────────────────────────
                TryWander();

                // ── 이동 완료 대기 ────────────────────────────
                while (isMoving)
                    yield return null;
            }
        }

        // ══════════════════════════════════════════════════════
        //  이동 선택
        // ══════════════════════════════════════════════════════

        private void TryWander()
        {
            Vector2[] dirs = Shuffled(CardinalDirs);

            foreach (Vector2 dir in dirs)
            {
                Vector2 currentFootCenter = GetFootCenter(targetPosition);
                Vector2 candidateFootCenter = currentFootCenter + AdventureGridUtility.GetCardinalStep(dir, moveUnitSize);

                // 홈 반경 초과 → 건너뜀
                if (Vector2.Distance(candidateFootCenter, homePosition) > wanderRadius + 0.01f)
                    continue;

                // 1단계: 물리 콜라이더 검사 (벽, 솔리드 오브젝트)
                Vector2 checkSize = AdventureGridUtility.GetCollisionProbeSize(moveUnitSize);
                
                // 자신의 콜라이더를 잠시 끄고 검사하여 자기 자신을 장애물로 인식하는 것을 방지
                var colliders = GetComponents<Collider2D>();
                foreach(var col in colliders) col.enabled = false;
                
                Collider2D hit = Physics2D.OverlapBox(candidateFootCenter, checkSize, 0f, obstacleLayer);
                
                foreach(var col in colliders) col.enabled = true;

                if (hit != null && !hit.isTrigger)
                    continue;

                // 2단계: 타일 예약 검사 (동시 이동 충돌 방지)
                if (!GridOccupancy.TryReserve(candidateFootCenter, moveUnitSize))
                    continue;

                GridOccupancy.Release(currentFootCenter, moveUnitSize);

                // 이동 확정
                targetPosition = GetRootPositionForFootCenter(candidateFootCenter);
                isMoving       = true;
                UpdateFacing(dir);
                return;
            }
            // 이동 가능 방향 없음 → 이번 틱 제자리 대기
        }

        // ══════════════════════════════════════════════════════
        //  헬퍼
        // ══════════════════════════════════════════════════════

        private void UpdateFacing(Vector2 dir)
        {
            if (spriteRenderer == null || dir.x == 0f) return;
            spriteRenderer.flipX = dir.x < 0f;
        }

        private static bool IsDialogueActive() =>
            DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;

        private Vector2 SnapToUnit(Vector2 pos)
        {
            return AdventureGridUtility.SnapToCellCenter(pos, moveUnitSize);
        }

        private Vector2 GetFootCenter(Vector2 rootPosition)
        {
            return AdventureGridUtility.GetFootCenter(rootPosition, transform, footCollider);
        }

        private Vector2 GetRootPositionForFootCenter(Vector2 footCenter)
        {
            return AdventureGridUtility.GetRootPositionForFootCenter(footCenter, transform, footCollider);
        }

        /// <summary>Fisher-Yates 셔플로 새 배열 반환</summary>
        private static Vector2[] Shuffled(Vector2[] source)
        {
            Vector2[] arr = (Vector2[])source.Clone();
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return arr;
        }
    }
}

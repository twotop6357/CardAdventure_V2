using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace CardAdventure
{
    /// <summary>
    /// Top-down adventure player movement on a 1x1 grid.
    /// Controls a simple SpriteRenderer/Animator visual child.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4f;
        [Tooltip("Shift를 누른 채 이동 시 적용되는 속도 배율.")]
        [SerializeField] private float sprintMultiplier = 2f;
        [Tooltip("이동을 차단할 Tilemap 목록 (예: Wall_Tilemap, Water_Tilemap). 비워두면 런타임에 자동으로 탐색합니다.")]
        [SerializeField] private Tilemap[] blockingTilemaps;
        [SerializeField] private float moveUnitSize = 1f;
        [SerializeField] private bool useGridCellSize = true;
        [Tooltip("Input shorter than this only turns the player without stepping to the next tile.")]
        [SerializeField] private float moveHoldThreshold = 0.06f;
        [Tooltip("Layers that block movement. Defaults to every layer except Player and Ignore Raycast.")]
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Tile Alignment")]
        [SerializeField] private BoxCollider2D footCollider;
        [Header("Visual Alignment")]
        public Transform visualTransform;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [Tooltip("Visual scale used by the smaller walk sprite sheets.")]
        [SerializeField] private Vector3 walkVisualScale = Vector3.one;
        [Tooltip("Idle sheets are larger than walk sheets, so idle is scaled down to match visual size.")]
        [SerializeField] private float idleVisualScaleMultiplier = 0.267f;

        private Rigidbody2D rb;
        private PlayerInput playerInput;
        private bool isMoving;
        private bool isSprinting;
        private bool inputEnabled = true;

        private InputAction moveAction;
        private Vector2 targetPosition;
        private Vector2 facingDirection = Vector2.down;
        public Vector2 FacingDirection => facingDirection;
        private Vector2 currentMoveDirection;
        private Vector2 heldDirection;
        private float heldDirectionTime;
        private string currentAnimationState;

        private const string IdleFront = "Player_IdleFront";
        private const string IdleBack  = "Player_IdleBack";
        private const string IdleSide  = "Player_IdleSide";
        private const string WalkFront = "Player_WalkFront";
        private const string WalkBack  = "Player_WalkBack";
        private const string WalkSide  = "Player_WalkSide";
        private const string RunFront  = "Player_RunFront";
        private const string RunBack   = "Player_RunBack";
        private const string RunSide   = "Player_RunSide";

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            ResolveMoveUnitSize();
            ResolveWallTilemap();
            ConfigureFootCollider();

            if (obstacleLayer.value == 0)
            {
                obstacleLayer = ~(LayerMask.GetMask("Player", "Ignore Raycast"));
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (spriteRenderer != null && walkVisualScale == Vector3.zero)
            {
                walkVisualScale = spriteRenderer.transform.localScale;
            }

            playerInput = GetComponent<PlayerInput>();
            ResolveInputAction();
        }

        private void OnEnable()
        {
            if (inputEnabled)
            {
                moveAction?.Enable();
            }
        }

        private void OnDisable()
        {
            moveAction?.Disable();
        }

        private void Start()
        {
            // GameDataManager에서 상태 복구 시도
            if (GameDataManager.Instance != null)
            {
                // 1. 비주얼 복구 (직업)
                if (GameDataManager.Instance.SelectedJobInfo != null)
                {
                    ApplyJobVisual(GameDataManager.Instance.SelectedJobInfo);
                }

                // 2. 위치 복구
                if (GameDataManager.Instance.HasSavedPosition)
                {
                    transform.position = GameDataManager.Instance.SavedPosition;
                    facingDirection = GameDataManager.Instance.SavedFacingDirection;
                    // 저장된 위치 사용 후 플래그 리셋 (다음 진입 시 새로 저장하기 위함)
                    GameDataManager.Instance.HasSavedPosition = false;
                }
            }

            Vector2 snappedFootPosition = SnapToMoveUnit(GetFootCenter(transform.position));
            targetPosition = GetRootPositionForFootCenter(snappedFootPosition);
            transform.position = targetPosition;
            rb.position = targetPosition;
            AlignVisualToTile();

            // 초기 위치 타일 예약
            GridOccupancy.TryReserve(snappedFootPosition, moveUnitSize);

            PlayDirectionalAnimation(false, false);
        }

        private void OnDestroy()
        {
            GridOccupancy.Release(GetFootCenter(targetPosition), moveUnitSize);
        }

private void FixedUpdate()
        {
            if (!inputEnabled) return;

            // Shift 입력 감지 (Left / Right Shift 모두 허용)
            isSprinting = Keyboard.current != null &&
                          (Keyboard.current.leftShiftKey.isPressed ||
                           Keyboard.current.rightShiftKey.isPressed);
            float effectiveSpeed = isSprinting ? moveSpeed * sprintMultiplier : moveSpeed;

            Vector2 inputDirection = ReadCardinalDirection();
            UpdateHeldDirection(inputDirection);

            if (!isMoving)
            {
                if (inputDirection != Vector2.zero)
                {
                    UpdateFacingDirection(inputDirection);
                    if (heldDirectionTime >= moveHoldThreshold)
                        TryStartMove(inputDirection);
                }
                else
                {
                    PlayDirectionalAnimation(false, false);
                }
            }
            else
            {
                Vector2 currentPos = rb.position;
                Vector2 newPos = Vector2.MoveTowards(currentPos, targetPosition, effectiveSpeed * Time.fixedDeltaTime);
                rb.MovePosition(newPos);

                if (Vector2.Distance(newPos, targetPosition) < 0.001f)
                {
                    rb.MovePosition(targetPosition);
                    isMoving = false;
                    currentMoveDirection = Vector2.zero;

                    if (inputDirection != Vector2.zero)
                    {
                        UpdateFacingDirection(inputDirection);
                        if (!TryStartMove(inputDirection))
                            PlayDirectionalAnimation(false, false);
                    }
                    else
                    {
                        PlayDirectionalAnimation(false, false);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            NormalizeVisualToReferenceHeight();

            if (spriteRenderer != null)
            {
                // 발밑(bounds.min.y) 기준으로 정렬 순서 결정.
                // Unity의 sortingOrder는 클수록 앞에 보이므로, -100을 곱해 낮은 Y값이 큰 order를 갖게 함.
                spriteRenderer.sortingOrder = 10000 + (int)(spriteRenderer.bounds.min.y * -100);
            }
        }

        private void ResolveMoveUnitSize()
        {
            if (!useGridCellSize)
            {
                return;
            }

            Grid grid = FindFirstObjectByType<Grid>();
            if (grid == null)
            {
                return;
            }

            Vector3 cellSize = grid.cellSize;
            float resolvedSize = Mathf.Abs(cellSize.x) > 0.001f ? Mathf.Abs(cellSize.x) : Mathf.Abs(cellSize.y);
            if (resolvedSize > 0.001f)
            {
                moveUnitSize = resolvedSize;
            }
        }

        /// <summary>
        /// blockingTilemaps가 비어있으면 'Wall_Tilemap'과 'Water_Tilemap'을 자동으로 탐색합니다.
        /// </summary>
        private void ResolveWallTilemap()
        {
            if (blockingTilemaps != null && blockingTilemaps.Length > 0)
            {
                // 배열에 null이 없으면 이미 할당된 것으로 간주
                bool hasAny = false;
                for (int i = 0; i < blockingTilemaps.Length; i++)
                {
                    if (blockingTilemaps[i] != null) { hasAny = true; break; }
                }
                if (hasAny) return;
            }

            // 자동 탐색: 이름으로 찾아 배열 구성
            string[] autoNames = { "Wall_Tilemap", "Water_Tilemap" };
            var found = new System.Collections.Generic.List<Tilemap>();
            for (int i = 0; i < autoNames.Length; i++)
            {
                var obj = GameObject.Find(autoNames[i]);
                if (obj != null)
                {
                    var tm = obj.GetComponent<Tilemap>();
                    if (tm != null) found.Add(tm);
                }
            }
            blockingTilemaps = found.ToArray();
        }

        /// <summary>
        /// 월드 좌표 <paramref name="worldPos"/>에 이동 차단 타일맵의 타일이 하나라도 있으면 true.
        /// blockingTilemaps가 비어있으면 false를 반환해 이동을 허용합니다.
        /// </summary>
        private bool HasBlockingTileAt(Vector2 worldPos)
        {
            if (blockingTilemaps == null || blockingTilemaps.Length == 0)
            {
                return false;
            }

            Vector3 world3 = new Vector3(worldPos.x, worldPos.y, 0f);
            for (int i = 0; i < blockingTilemaps.Length; i++)
            {
                Tilemap tm = blockingTilemaps[i];
                if (tm == null) continue;
                Vector3Int cell = tm.WorldToCell(world3);
                if (tm.HasTile(cell)) return true;
            }
            return false;
        }

        private Vector2 SnapToMoveUnit(Vector2 position)
        {
            if (moveUnitSize <= 0.001f)
            {
                return position;
            }

            return AdventureGridUtility.SnapToCellCenter(position, moveUnitSize);
        }

        private Vector2 GetFootCenter(Vector2 rootPosition)
        {
            return AdventureGridUtility.GetFootCenter(rootPosition, transform, footCollider);
        }

        private Vector2 GetRootPositionForFootCenter(Vector2 footCenter)
        {
            return AdventureGridUtility.GetRootPositionForFootCenter(footCenter, transform, footCollider);
        }

        private void ConfigureFootCollider()
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
            AdventureGridUtility.ConfigureFootCollider(footCollider, transform, cellSize);
            AdventureGridUtility.DisableSolidCircles(gameObject);
        }

        private void AlignVisualToTile()
        {
            NormalizeVisualToReferenceHeight();
        }

        public void RefreshVisualAlignment()
        {
            AlignVisualToTile();
            ApplyVisualScale(isMoving);
            currentAnimationState = null;
            PlayDirectionalAnimation(isMoving, isSprinting);
        }

        public void SetVisual(SpriteRenderer newSpriteRenderer, Animator newAnimator, float newIdleVisualScaleMultiplier)
        {
            spriteRenderer = newSpriteRenderer;
            animator = newAnimator;
            idleVisualScaleMultiplier = Mathf.Max(0.001f, newIdleVisualScaleMultiplier);
            RefreshVisualAlignment();
        }

        public void ApplyJobVisual(JobClassInfo jobInfo)
        {
            if (jobInfo == null)
            {
                return;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            Sprite nextSprite = jobInfo.playerIdleSprite != null ? jobInfo.playerIdleSprite : jobInfo.previewSprite;
            if (spriteRenderer != null && nextSprite != null)
            {
                spriteRenderer.sprite = nextSprite;
            }

            if (animator != null && jobInfo.playerAnimatorController != null)
            {
                animator.runtimeAnimatorController = jobInfo.playerAnimatorController;
            }

            idleVisualScaleMultiplier = Mathf.Max(0.001f, jobInfo.playerIdleVisualScaleMultiplier);
            RefreshVisualAlignment();

            // 방향 강제 업데이트 (직업별 반전 설정이 다를 수 있으므로)
            UpdateFacingDirection(facingDirection);
        }

        private void UpdateHeldDirection(Vector2 inputDirection)
        {
            if (inputDirection == Vector2.zero)
            {
                heldDirection = Vector2.zero;
                heldDirectionTime = 0f;
                return;
            }

            if (inputDirection != heldDirection)
            {
                heldDirection = inputDirection;
                heldDirectionTime = 0f;
                return;
            }

            heldDirectionTime += Time.fixedDeltaTime;
        }

        private bool TryStartMove(Vector2 direction)
        {
            Vector2 nextTarget = targetPosition + AdventureGridUtility.GetCardinalStep(direction, moveUnitSize);
            Vector2 nextFootCenter = GetFootCenter(nextTarget);
            Vector2 collisionSize = AdventureGridUtility.GetCollisionProbeSize(moveUnitSize);

            // 1단계: 물리 콜라이더 검사 (벽, 솔리드 오브젝트)
            Collider2D[] hits = Physics2D.OverlapBoxAll(nextFootCenter, collisionSize, 0f, obstacleLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];
                if (hit == null || hit.isTrigger || IsSelfCollider(hit))
                {
                    continue;
                }

                PlayDirectionalAnimation(false, false);
                return false;
            }

            // 2단계: 차단 Tilemap 타일 검사 (Wall_Tilemap, Water_Tilemap 등)
            if (HasBlockingTileAt(nextFootCenter))
            {
                PlayDirectionalAnimation(false, false);
                return false;
            }

            // 3단계: 타일 예약 검사 (동시 이동 충돌 방지)
            if (!GridOccupancy.TryReserve(nextFootCenter, moveUnitSize))
            {
                PlayDirectionalAnimation(false, false);
                return false;
            }

            GridOccupancy.Release(GetFootCenter(targetPosition), moveUnitSize);
            targetPosition = nextTarget;
            isMoving = true;
            currentMoveDirection = direction;
            PlayDirectionalAnimation(true, isSprinting);
            return true;
        }

        private bool IsSelfCollider(Collider2D hit)
        {
            if (hit == null)
            {
                return false;
            }

            if (rb != null && hit.attachedRigidbody == rb)
            {
                return true;
            }

            Transform hitTransform = hit.transform;
            return hitTransform == transform || hitTransform.IsChildOf(transform);
        }


        private Vector2 ReadCardinalDirection()
        {
            Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector2 direction = Vector2.zero;

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                direction.x = Mathf.Sign(input.x);
            }
            else if (Mathf.Abs(input.y) > 0.1f)
            {
                direction.y = Mathf.Sign(input.y);
            }

            return direction;
        }

        private void UpdateFacingDirection(Vector2 direction)
        {
            facingDirection = direction;

            if (spriteRenderer != null && direction.x != 0f)
            {
                bool baseFlip = direction.x > 0f;
                // 직업 데이터에 따라 flipX 로직 반전 여부 결정
                if (GameDataManager.Instance != null && GameDataManager.Instance.SelectedJobInfo != null)
                {
                    if (GameDataManager.Instance.SelectedJobInfo.invertVisualFlip)
                        baseFlip = !baseFlip;
                }
                spriteRenderer.flipX = baseFlip;
            }
        }

        /// <summary>
        /// 특정 월드 좌표를 바라보도록 스프라이트 방향을 설정한다.
        /// </summary>
        public void FaceToward(Vector2 targetPosition)
        {
            Vector2 dir = targetPosition - (Vector2)transform.position;
            if (Mathf.Abs(dir.x) > 0.1f)
            {
                facingDirection = dir.x > 0 ? Vector2.right : Vector2.left;
                if (spriteRenderer != null)
                {
                    bool baseFlip = dir.x > 0f;
                    if (GameDataManager.Instance != null && GameDataManager.Instance.SelectedJobInfo != null)
                    {
                        if (GameDataManager.Instance.SelectedJobInfo.invertVisualFlip)
                            baseFlip = !baseFlip;
                    }
                    spriteRenderer.flipX = baseFlip;
                }
            }
            else if (Mathf.Abs(dir.y) > 0.1f)
            {
                facingDirection = dir.y > 0 ? Vector2.up : Vector2.down;
            }

            PlayDirectionalAnimation(false, false);
        }

private void PlayDirectionalAnimation(bool moving, bool sprinting)
        {
            if (animator == null) return;

            ApplyVisualScale(moving);

            string stateName;
            if (!moving)
            {
                if (Mathf.Abs(facingDirection.x) > 0f) stateName = IdleSide;
                else if (facingDirection.y > 0f)       stateName = IdleBack;
                else                                   stateName = IdleFront;
            }
            else if (sprinting)
            {
                if (Mathf.Abs(facingDirection.x) > 0f) stateName = RunSide;
                else if (facingDirection.y > 0f)       stateName = RunBack;
                else                                   stateName = RunFront;
            }
            else
            {
                if (Mathf.Abs(facingDirection.x) > 0f) stateName = WalkSide;
                else if (facingDirection.y > 0f)       stateName = WalkBack;
                else                                   stateName = WalkFront;
            }

            if (currentAnimationState == stateName) return;
            currentAnimationState = stateName;
            animator.Play(stateName);

            // Idle: 첫 프레임 정지 (부동자세)
            animator.speed = stateName.Contains("Idle") ? 0f : 1f;
        }

        private void ApplyVisualScale(bool moving)
        {
            NormalizeVisualToReferenceHeight();
        }

        private void NormalizeVisualToReferenceHeight()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            float scale = AdventureGridUtility.GetVisualScaleForReferenceHeight(spriteRenderer.sprite);
            spriteRenderer.transform.localScale = new Vector3(scale, scale, 1f);
            walkVisualScale = spriteRenderer.transform.localScale;

            Vector2 cellSize = AdventureGridUtility.GetCellSize(moveUnitSize);
            float finalHeight = spriteRenderer.sprite.bounds.size.y * scale;
            Vector3 localPos = spriteRenderer.transform.localPosition;
            localPos.y = (finalHeight - cellSize.y) * 0.5f;
            spriteRenderer.transform.localPosition = localPos;
        }

        private void ResolveInputAction()
        {
            if (playerInput != null && playerInput.actions != null)
            {
                moveAction = playerInput.actions.FindAction("Move", throwIfNotFound: false);
                if (moveAction != null)
                {
                    return;
                }
            }

#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            if (asset != null)
            {
                moveAction = asset.FindAction("Player/Move", throwIfNotFound: false);
            }
#endif
        }

        /// <summary>Locks or unlocks input for dialogue, shops, and scene transitions.</summary>
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (playerInput != null)
            {
                playerInput.enabled = enabled;
            }

            if (enabled)
            {
                moveAction?.Enable();
            }
            else
            {
                moveAction?.Disable();
                isSprinting = false;
            }

            if (!enabled)
            {
                isMoving = false;
                currentMoveDirection = Vector2.zero;
                heldDirection = Vector2.zero;
                heldDirectionTime = 0f;

                if (rb != null)
                {
                    Vector2 snappedFootPosition = SnapToMoveUnit(GetFootCenter(rb.position));
                    Vector2 snappedRootPosition = GetRootPositionForFootCenter(snappedFootPosition);

                    GridOccupancy.Release(GetFootCenter(targetPosition), moveUnitSize);
                    GridOccupancy.TryReserve(snappedFootPosition, moveUnitSize);
                    targetPosition = snappedRootPosition;
                    transform.position = snappedRootPosition;
                    rb.position = snappedRootPosition;
                }

                PlayDirectionalAnimation(false, false);
            }
        }

        public void SetMoveSpeed(float speed) => moveSpeed = speed;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (spriteRenderer != null && !Application.isPlaying)
            {
                NormalizeVisualToReferenceHeight();
            }

            if (footCollider != null && !Application.isPlaying)
            {
                Vector2 cellSize = AdventureGridUtility.GetCellSize(moveUnitSize);
                AdventureGridUtility.ConfigureFootCollider(footCollider, transform, cellSize);
            }
        }
#endif
    }
}

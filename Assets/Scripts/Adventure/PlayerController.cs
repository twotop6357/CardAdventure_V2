using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private float moveUnitSize = 1f;
        [SerializeField] private bool useGridCellSize = true;
        [Tooltip("Input shorter than this only turns the player without stepping to the next tile.")]
        [SerializeField] private float moveHoldThreshold = 0.06f;
        [Tooltip("Layers that block movement. Defaults to every layer except Player and Ignore Raycast.")]
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [Tooltip("Visual scale used by the smaller walk sprite sheets.")]
        [SerializeField] private Vector3 walkVisualScale = Vector3.one;
        [Tooltip("Idle sheets are larger than walk sheets, so idle is scaled down to match visual size.")]
        [SerializeField] private float idleVisualScaleMultiplier = 0.267f;

        private Rigidbody2D rb;
        private bool isMoving;
        private bool inputEnabled = true;

        private InputAction moveAction;
        private Vector2 targetPosition;
        private Vector2 facingDirection = Vector2.down;
        private Vector2 currentMoveDirection;
        private Vector2 heldDirection;
        private float heldDirectionTime;
        private string currentAnimationState;

        private const string IdleFront = "Player_IdleFront";
        private const string IdleBack = "Player_IdleBack";
        private const string IdleSide = "Player_IdleSide";
        private const string WalkFront = "Player_WalkFront";
        private const string WalkBack = "Player_WalkBack";
        private const string WalkSide = "Player_WalkSide";

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            ResolveMoveUnitSize();

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

            ResolveInputAction();
        }

        private void OnEnable()
        {
            moveAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
        }

        private void Start()
        {
            Vector3 pos = transform.position;
            targetPosition = SnapToMoveUnit(pos);
            transform.position = targetPosition;

            // 초기 위치 타일 예약
            GridOccupancy.TryReserve(targetPosition, moveUnitSize);

            PlayDirectionalAnimation(false);
        }

        private void OnDestroy()
        {
            GridOccupancy.Release(targetPosition, moveUnitSize);
        }

        private void FixedUpdate()
        {
            if (!inputEnabled)
            {
                return;
            }

            Vector2 inputDirection = ReadCardinalDirection();
            UpdateHeldDirection(inputDirection);

            if (!isMoving)
            {
                if (inputDirection != Vector2.zero)
                {
                    UpdateFacingDirection(inputDirection);

                    if (heldDirectionTime >= moveHoldThreshold)
                    {
                        TryStartMove(inputDirection);
                    }
                }
                else
                {
                    PlayDirectionalAnimation(false);
                }
            }
            else
            {
                // 이동 중 방향 전환: 스프라이트는 즉시 바라보는 방향을 바꾸되
                // 실제 이동 목표는 현재 타일 도착 후 변경한다 (포켓몬 스타일).
                if (inputDirection != Vector2.zero && inputDirection != currentMoveDirection)
                {
                    UpdateFacingDirection(inputDirection);
                }

                Vector2 currentPos = rb.position;
                Vector2 newPos = Vector2.MoveTowards(currentPos, targetPosition, moveSpeed * Time.fixedDeltaTime);
                rb.MovePosition(newPos);

                if (Vector2.Distance(newPos, targetPosition) < 0.001f)
                {
                    rb.MovePosition(targetPosition);
                    isMoving = false;
                    currentMoveDirection = Vector2.zero;

                    // 타일 도착 시점의 입력 방향으로 다음 이동 즉시 시작
                    if (inputDirection != Vector2.zero)
                    {
                        UpdateFacingDirection(inputDirection);
                        if (!TryStartMove(inputDirection))
                        {
                            PlayDirectionalAnimation(false);
                        }
                    }
                    else
                    {
                        PlayDirectionalAnimation(false);
                    }
                }
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

        private Vector2 SnapToMoveUnit(Vector2 position)
        {
            if (moveUnitSize <= 0.001f)
            {
                return position;
            }

            return new Vector2(
                Mathf.Round(position.x / moveUnitSize) * moveUnitSize,
                Mathf.Round(position.y / moveUnitSize) * moveUnitSize);
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
            Vector2 nextTarget = targetPosition + direction * moveUnitSize;
            Vector2 collisionSize = Vector2.one * (moveUnitSize * 0.8f);

            // 1단계: 물리 콜라이더 검사 (벽, 솔리드 오브젝트)
            Collider2D hit = Physics2D.OverlapBox(nextTarget, collisionSize, 0f, obstacleLayer);
            if (hit != null && !hit.isTrigger)
            {
                PlayDirectionalAnimation(false);
                return false;
            }

            // 2단계: 타일 예약 검사 (동시 이동 충돌 방지)
            if (!GridOccupancy.TryReserve(nextTarget, moveUnitSize))
            {
                PlayDirectionalAnimation(false);
                return false;
            }

            GridOccupancy.Release(targetPosition, moveUnitSize);
            targetPosition = nextTarget;
            isMoving = true;
            currentMoveDirection = direction;
            PlayDirectionalAnimation(true);
            return true;
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
                spriteRenderer.flipX = direction.x < 0f;
            }
        }

        private void PlayDirectionalAnimation(bool moving)
        {
            if (animator == null)
            {
                return;
            }

            ApplyVisualScale(moving);

            string stateName;
            if (Mathf.Abs(facingDirection.x) > 0f)
            {
                stateName = moving ? WalkSide : IdleSide;
            }
            else if (facingDirection.y > 0f)
            {
                stateName = moving ? WalkBack : IdleBack;
            }
            else
            {
                stateName = moving ? WalkFront : IdleFront;
            }

            if (currentAnimationState == stateName)
            {
                return;
            }

            currentAnimationState = stateName;
            animator.Play(stateName);
        }

        private void ApplyVisualScale(bool moving)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            float scaleMultiplier = moving ? 1f : idleVisualScaleMultiplier;
            spriteRenderer.transform.localScale = walkVisualScale * scaleMultiplier;
        }

        private void ResolveInputAction()
        {
            var playerInput = GetComponent<PlayerInput>();
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
            if (!enabled)
            {
                isMoving = false;
                currentMoveDirection = Vector2.zero;
                heldDirection = Vector2.zero;
                heldDirectionTime = 0f;

                // rb가 아직 초기화되지 않았거나 이미 파괴된 경우를 방어한다.
                if (rb != null)
                {
                    Vector2 snapped = SnapToMoveUnit(rb.position);
                    // 기존 예약 타일(이동 도중이었을 경우 목적지)을 해제하고 실제 위치를 재예약
                    GridOccupancy.Release(targetPosition, moveUnitSize);
                    GridOccupancy.TryReserve(snapped, moveUnitSize);
                    targetPosition = snapped;
                    rb.position    = snapped;
                }

                PlayDirectionalAnimation(false);
            }
        }

        public void SetMoveSpeed(float speed) => moveSpeed = speed;
    }
}

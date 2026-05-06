using UnityEngine;
using UnityEngine.InputSystem;

namespace CardAdventure
{
    /// <summary>
    /// 탑다운 어드벤처 씬의 플레이어 이동 및 SPUM 애니메이션 컨트롤러.
    /// Input System의 Move 액션을 직접 참조하는 방식으로 동작한다.
    /// PlayerInput.notificationBehavior에 의존하지 않으므로 가장 안정적이다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("이동")]
        [SerializeField] private float moveSpeed = 4f;

        [Header("SPUM 참조")]
        [Tooltip("SPUM_Prefabs 컴포넌트가 있는 하위 오브젝트 (없으면 자동 탐색)")]
        [SerializeField] private SPUM_Prefabs spumPrefabs;

        // ── 내부 ───────────────────────────────────────────────
        private Rigidbody2D    rb;
        private Vector2        moveInput;
        private bool           isMoving;
        private bool           inputEnabled = true;
        private SpriteRenderer spriteRenderer;

        // Input System — Move 액션 직접 참조
        private InputAction moveAction;

        private const string ANIM_IDLE = "IDLE";
        private const string ANIM_MOVE = "MOVE";

        // ── 라이프사이클 ───────────────────────────────────────

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale           = 0f;
            rb.freezeRotation         = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // SPUM 자동 탐색
            if (spumPrefabs == null)
                spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();

            if (spumPrefabs != null)
                spriteRenderer = spumPrefabs.GetComponentInChildren<SpriteRenderer>();

            // Input Action 직접 참조
            ResolveInputAction();
        }

        private void OnEnable()
        {
            if (moveAction != null)
                moveAction.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null)
                moveAction.Disable();
        }

        private void Start()
        {
            PlayAnimation(ANIM_IDLE);
        }

        private void FixedUpdate()
        {
            if (!inputEnabled)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // 액션에서 직접 읽기 (폴링 방식 — 가장 신뢰성 높음)
            if (moveAction != null)
                moveInput = moveAction.ReadValue<Vector2>();

            rb.linearVelocity = moveInput * moveSpeed;

            bool nowMoving = moveInput.sqrMagnitude > 0.01f;
            if (nowMoving != isMoving)
            {
                isMoving = nowMoving;
                PlayAnimation(isMoving ? ANIM_MOVE : ANIM_IDLE);
            }

            if (isMoving && spriteRenderer != null)
                spriteRenderer.flipX = moveInput.x < 0f;
        }

        // ── Input Action 탐색 ──────────────────────────────────

        private void ResolveInputAction()
        {
            // 1순위: PlayerInput 컴포넌트
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                moveAction = playerInput.actions.FindAction("Move", throwIfNotFound: false);
                if (moveAction != null)
                {
                    Debug.Log("[PlayerController] Move action 연결 완료 (PlayerInput).");
                    return;
                }
            }

            // 2순위: 프로젝트의 기본 InputActionAsset 로드
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            if (asset != null)
            {
                moveAction = asset.FindAction("Player/Move", throwIfNotFound: false);
                if (moveAction != null)
                {
                    Debug.Log("[PlayerController] Move action 연결 완료 (Asset 직접 로드).");
                    return;
                }
            }
#endif
            Debug.LogWarning("[PlayerController] Move 액션을 찾을 수 없습니다. " +
                             "PlayerInput.actions에 InputSystem_Actions를 연결하세요.");
        }

        // ── SPUM 애니메이션 ────────────────────────────────────

        private void PlayAnimation(string stateName)
        {
            if (spumPrefabs == null) return;
            try { spumPrefabs._anim.Play(stateName); }
            catch { /* 애니메이터 없는 경우 무시 */ }
        }

        // ── 공개 API ───────────────────────────────────────────

        /// <summary>입력 잠금/해제 (컷씬, 상점 진입 등).</summary>
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                moveInput = Vector2.zero;
                rb.linearVelocity = Vector2.zero;
                PlayAnimation(ANIM_IDLE);
            }
        }

        public void SetMoveSpeed(float speed) => moveSpeed = speed;
    }
}

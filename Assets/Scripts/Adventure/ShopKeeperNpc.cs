using UnityEngine;

namespace CardAdventure
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class ShopKeeperNpc : MonoBehaviour
    {
        [Header("Tile Alignment")]
        [SerializeField] private bool snapToNearestTileOnStart = true;
        [SerializeField] private float moveUnitSize = 1f;
        [SerializeField] private BoxCollider2D footCollider;

        [Header("Visual")]
        [SerializeField] private bool alignVisualToReferenceHeight = true;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private bool invertVisualFlip;

        [Header("Shop UI")]
        [SerializeField] private ShopUIController shopUI;

        private Animator animator;
        private Rigidbody2D rb;
        private Vector2 currentFacingDir = Vector2.down;
        private bool isWaitingForDialogueEnd;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            footCollider = footCollider != null ? footCollider : GetComponent<BoxCollider2D>();
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            ResolveMoveUnitSize();
            RefreshVisualAlignment();
            ConfigureCollider();
        }

        private void Start()
        {
            if (snapToNearestTileOnStart)
            {
                SnapToNearestTile();
            }

            // NpcTileAlignment와 동일: Animator를 speed=0으로 얼려 첫 프레임에 고정
            if (animator != null)
            {
                animator.speed = 0f;
            }

            if (shopUI == null)
            {
                shopUI = FindFirstObjectByType<ShopUIController>(FindObjectsInactive.Include);
            }
        }

        private void LateUpdate()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 10000 + (int)(spriteRenderer.bounds.min.y * -100);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromDialogue();
        }

        public void FaceToward(Vector2 targetPosition)
        {
            Vector2 dir = targetPosition - (Vector2)transform.position;
            if (dir.sqrMagnitude >= 0.01f)
            {
                // 더 큰 축 기준 4방향 결정
                if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                    currentFacingDir = dir.x > 0f ? Vector2.right : Vector2.left;
                else
                    currentFacingDir = dir.y > 0f ? Vector2.up : Vector2.down;
            }

            // 좌우(Side) 방향이면 flipX로 좌우 반전
            if (currentFacingDir.x != 0f && spriteRenderer != null)
            {
                bool shouldFlip = currentFacingDir.x > 0f;
                if (invertVisualFlip) shouldFlip = !shouldFlip;
                spriteRenderer.flipX = shouldFlip;
            }

            // Animator가 있으면 방향에 맞는 Idle 애니메이션 재생
            PlayIdleAnim();

            SubscribeToDialogueEnd();
        }

        // NpcChaser.PlayIdleAnim()과 동일한 패턴 — 표준 상태명 직접 사용
        private void PlayIdleAnim()
        {
            if (animator == null) return;
            if      (currentFacingDir.y < 0f) animator.Play("MaleNPC_IdleDown", 0);
            else if (currentFacingDir.y > 0f) animator.Play("MaleNPC_IdleBack", 0);
            else                               animator.Play("MaleNPC_IdleSide", 0);
        }

        public void SnapToNearestTile()
        {
            AdventureGridUtility.SnapOwnerFootToNearestCell(transform, rb, footCollider, moveUnitSize);
        }

        public void RefreshVisualAlignment()
        {
            if (!alignVisualToReferenceHeight || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            float visualScale = AdventureGridUtility.GetVisualScaleForReferenceHeight(spriteRenderer.sprite);
            transform.localScale = new Vector3(visualScale, visualScale, transform.localScale.z);
        }

        private void ResolveMoveUnitSize()
        {
            moveUnitSize = AdventureGridUtility.GetMoveUnitSize(moveUnitSize);
        }

        private void ConfigureCollider()
        {
            if (footCollider == null)
            {
                return;
            }

            AdventureGridUtility.ConfigureFootCollider(
                footCollider,
                transform,
                spriteRenderer,
                AdventureGridUtility.GetCellSize(moveUnitSize));
            AdventureGridUtility.DisableSolidCircles(gameObject);
        }

        private void SubscribeToDialogueEnd()
        {
            if (isWaitingForDialogueEnd || DialogueManager.Instance == null)
            {
                return;
            }

            isWaitingForDialogueEnd = true;
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
        }

        private void UnsubscribeFromDialogue()
        {
            if (!isWaitingForDialogueEnd)
            {
                return;
            }

            isWaitingForDialogueEnd = false;
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
            }
        }

        private void HandleDialogueEnded()
        {
            UnsubscribeFromDialogue();

            if (shopUI == null)
            {
                shopUI = FindFirstObjectByType<ShopUIController>(FindObjectsInactive.Include);
            }

            if (shopUI == null)
            {
                Debug.LogWarning("[ShopKeeperNpc] ShopUIController를 찾지 못했습니다.", this);
                return;
            }

            shopUI.Open();
        }
    }
}

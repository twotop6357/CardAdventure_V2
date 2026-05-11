using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// Stationary NPC grid alignment and collider setup.
    /// Moving NPCs should use NpcMovement, which applies the same foot-center rules.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class NpcTileAlignment : MonoBehaviour
    {
        [Header("Tile Alignment")]
        [SerializeField] private bool snapToNearestTileOnStart = true;
        [SerializeField] private float moveUnitSize = 1f;
        [SerializeField] private BoxCollider2D footCollider;

        [Header("Visual")]
        [SerializeField] private bool alignVisualToReferenceHeight = true;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("방향 판정 시 flipX 로직을 반전시킵니다. (스프라이트 시트 기본 방향이 다를 경우 사용)")]
        public bool invertVisualFlip = false;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            footCollider = footCollider != null ? footCollider : GetComponent<BoxCollider2D>();
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            ResolveMoveUnitSize();
            RefreshVisualAlignment();
            ConfigureCollider();
        }

        private void LateUpdate()
        {
            if (spriteRenderer != null)
            {
                // 발밑(bounds.min.y) 기준으로 정렬 순서 결정.
                // Unity의 sortingOrder는 클수록 앞에 보이므로, -100을 곱해 낮은 Y값이 큰 order를 갖게 함.
                spriteRenderer.sortingOrder = 10000 + (int)(spriteRenderer.bounds.min.y * -100);
            }
        }

        private void Start()
        {
            if (snapToNearestTileOnStart)
            {
                SnapToNearestTile();
            }

            // 부동자세를 위해 Animator 속도를 0으로 설정 (첫 프레임 고정)
            // 단, 전직관 NPC(JobChangerNpc)는 제외한다.
            if (GetComponent<JobChangerNpc>() == null)
            {
                Animator anim = GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.speed = 0f;
                }
            }
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

        /// <summary>
        /// 특정 월드 좌표를 바라보도록 스프라이트 방향을 설정한다.
        /// </summary>
        public void FaceToward(Vector2 targetPosition)
        {
            Vector2 dir = targetPosition - (Vector2)transform.position;
            if (Mathf.Abs(dir.x) > 0.1f)
            {
                if (spriteRenderer != null)
                {
                    bool baseFlip = dir.x > 0f;
                    if (invertVisualFlip) baseFlip = !baseFlip;
                    spriteRenderer.flipX = baseFlip;
                }
            }
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
    }
}

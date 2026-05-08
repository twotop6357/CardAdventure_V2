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

        private void Start()
        {
            if (snapToNearestTileOnStart)
            {
                SnapToNearestTile();
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

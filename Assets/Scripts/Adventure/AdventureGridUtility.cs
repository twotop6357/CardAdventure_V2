using UnityEngine;

namespace CardAdventure
{
    public static class AdventureGridUtility
    {
        public const float ReferenceCharacterVisualHeight = 1.3304521f;

        public static Grid FindGrid()
        {
            return Object.FindFirstObjectByType<Grid>();
        }

        public static Vector2 GetCellSize(float fallbackUnitSize = 1f)
        {
            Grid grid = FindGrid();
            if (grid == null)
            {
                return Vector2.one * Mathf.Max(0.001f, fallbackUnitSize);
            }

            Vector3 cellSize = grid.cellSize;
            float width = Mathf.Abs(cellSize.x) > 0.001f ? Mathf.Abs(cellSize.x) : fallbackUnitSize;
            float height = Mathf.Abs(cellSize.y) > 0.001f ? Mathf.Abs(cellSize.y) : fallbackUnitSize;
            return new Vector2(width, height);
        }

        public static float GetMoveUnitSize(float fallbackUnitSize = 1f)
        {
            Vector2 cellSize = GetCellSize(fallbackUnitSize);
            return Mathf.Abs(cellSize.x) > 0.001f ? Mathf.Abs(cellSize.x) : Mathf.Abs(cellSize.y);
        }

        public static Vector2 SnapToCellCenter(Vector2 worldPosition, float fallbackUnitSize = 1f)
        {
            Grid grid = FindGrid();
            if (grid != null)
            {
                Vector3Int cell = grid.WorldToCell(worldPosition);
                return grid.GetCellCenterWorld(cell);
            }

            float unit = Mathf.Max(0.001f, fallbackUnitSize);
            return new Vector2(
                Mathf.Floor(worldPosition.x / unit) * unit + unit * 0.5f,
                Mathf.Floor(worldPosition.y / unit) * unit + unit * 0.5f);
        }

        public static Vector2 GetCardinalStep(Vector2 direction, float fallbackUnitSize = 1f)
        {
            Vector2 cellSize = GetCellSize(fallbackUnitSize);
            if (Mathf.Abs(direction.x) > 0f)
            {
                return new Vector2(Mathf.Sign(direction.x) * cellSize.x, 0f);
            }

            if (Mathf.Abs(direction.y) > 0f)
            {
                return new Vector2(0f, Mathf.Sign(direction.y) * cellSize.y);
            }

            return Vector2.zero;
        }

        public static Vector2 GetCollisionProbeSize(float fallbackUnitSize = 1f)
        {
            Vector2 cellSize = GetCellSize(fallbackUnitSize);
            return new Vector2(
                Mathf.Max(0.001f, cellSize.x * 0.5f),
                Mathf.Max(0.001f, cellSize.y * 0.5f));
        }

        public static float GetVisualScaleForReferenceHeight(Sprite sprite)
        {
            if (sprite == null || sprite.bounds.size.y <= 0.001f)
            {
                return 1f;
            }

            return ReferenceCharacterVisualHeight / sprite.bounds.size.y;
        }

        public static Vector2 GetColliderWorldOffset(Transform owner, BoxCollider2D footCollider)
        {
            if (owner == null || footCollider == null)
            {
                return Vector2.zero;
            }

            Vector3 worldOffset = owner.TransformVector(footCollider.offset);
            return new Vector2(worldOffset.x, worldOffset.y);
        }

        public static Vector2 GetFootCenter(Vector2 rootPosition, Transform owner, BoxCollider2D footCollider)
        {
            return rootPosition + GetColliderWorldOffset(owner, footCollider);
        }

        public static Vector2 GetRootPositionForFootCenter(Vector2 footCenter, Transform owner, BoxCollider2D footCollider)
        {
            return footCenter - GetColliderWorldOffset(owner, footCollider);
        }

        public static Vector2 SnapFootCenterToCell(
            Vector2 rootPosition,
            Transform owner,
            BoxCollider2D footCollider,
            float fallbackUnitSize = 1f)
        {
            return SnapToCellCenter(GetFootCenter(rootPosition, owner, footCollider), fallbackUnitSize);
        }

        public static Vector2 SnapOwnerFootToNearestCell(
            Transform owner,
            Rigidbody2D rb,
            BoxCollider2D footCollider,
            float fallbackUnitSize = 1f)
        {
            if (owner == null)
            {
                return Vector2.zero;
            }

            Vector2 snappedFootCenter = SnapFootCenterToCell(owner.position, owner, footCollider, fallbackUnitSize);
            Vector2 snappedRootPosition = GetRootPositionForFootCenter(snappedFootCenter, owner, footCollider);
            owner.position = snappedRootPosition;

            if (rb != null)
            {
                rb.position = snappedRootPosition;
            }

            return snappedFootCenter;
        }

        public static void ConfigureFootCollider(BoxCollider2D collider, Transform owner, Vector2 worldSize)
        {
            if (collider == null || owner == null)
            {
                return;
            }

            collider.isTrigger = false;

            if (worldSize.x <= 0.001f || worldSize.y <= 0.001f)
            {
                return;
            }

            Vector3 lossyScale = owner.lossyScale;
            float scaleX = Mathf.Abs(lossyScale.x) > 0.001f ? Mathf.Abs(lossyScale.x) : 1f;
            float scaleY = Mathf.Abs(lossyScale.y) > 0.001f ? Mathf.Abs(lossyScale.y) : 1f;
            collider.size = new Vector2(worldSize.x / scaleX, worldSize.y / scaleY);
        }

        public static void ConfigureFootCollider(
            BoxCollider2D collider,
            Transform owner,
            SpriteRenderer spriteRenderer,
            Vector2 worldSize)
        {
            ConfigureFootCollider(collider, owner, worldSize);

            if (collider == null || owner == null || spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            Bounds visualBounds = spriteRenderer.bounds;
            Vector3 footWorldPosition = new Vector3(
                visualBounds.center.x,
                visualBounds.min.y,
                owner.position.z);
            Vector3 footLocalPosition = owner.InverseTransformPoint(footWorldPosition);
            collider.offset = new Vector2(footLocalPosition.x, footLocalPosition.y);
        }

        public static void DisableSolidCircles(GameObject owner)
        {
            if (owner == null)
            {
                return;
            }

            CircleCollider2D[] circles = owner.GetComponents<CircleCollider2D>();
            for (int i = 0; i < circles.Length; i++)
            {
                if (circles[i] != null && !circles[i].isTrigger)
                {
                    circles[i].enabled = false;
                }
            }
        }
    }
}

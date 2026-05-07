using UnityEngine;

namespace CardAdventure
{
    public static class AdventureGridUtility
    {
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

        public static void ConfigureFootCollider(BoxCollider2D collider, Transform owner, Vector2 worldSize)
        {
            if (collider == null || owner == null)
            {
                return;
            }

            collider.isTrigger = false;
            // 사용자가 에디터에서 설정한 축소된 발밑 콜라이더 크기와 오프셋을 런타임에 1x1로 덮어쓰지 않도록 제거.
        }

        public static void DisableSolidCircles(GameObject owner)
        {
            // 사용자가 의도적으로 CircleCollider2D를 충돌용으로 사용할 수 있도록 강제 비활성화 로직 제거.
        }
    }
}

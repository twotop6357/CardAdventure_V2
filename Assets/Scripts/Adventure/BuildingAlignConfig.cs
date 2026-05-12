using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// BuildingAlignerEditor가 Building 레이어 오브젝트를 처리할 때 사용할 per-building 설정.
    /// 이 컴포넌트가 없으면 에디터의 전역 설정이 사용됩니다.
    /// </summary>
    [AddComponentMenu("CardAdventure/Building/Building Align Config")]
    public class BuildingAlignConfig : MonoBehaviour, IBuildingBoundsProvider
    {
        [Header("Bounds Source")]
        [Tooltip("콜라이더/중앙 계산에 쓸 bounds 기준.\nAutoDetect: 자식 Tilemap → SpriteRenderer → Manual 순으로 자동 선택")]
        public BuildingBoundsSource boundsSource = BuildingBoundsSource.AutoDetect;

        [Header("Manual Bounds  (boundsSource = Manual 일 때)")]
        [Tooltip("로컬 좌표 기준 offset (로컬 원점에서의 거리)")]
        public Vector2 manualBoundsOffset = Vector2.zero;
        [Tooltip("월드 단위 크기 (그리드 셀 수를 입력하면 편함)")]
        public Vector2 manualBoundsSize = new Vector2(2f, 2f);

        [Header("Collider Padding")]
        [Tooltip("계산된 bounds에 추가로 더할 가로 여백 (월드 단위)")]
        public float paddingX;
        [Tooltip("계산된 bounds에 추가로 더할 세로 여백 (월드 단위)")]
        public float paddingY;

        [Header("Grid Snap")]
        [Tooltip("bounds 중앙을 Grid 셀에 스냅할지 여부")]
        public bool snapToGrid = true;
        [Tooltip("짝수 칸(2x2, 4x4 등) 건물: 셀 중앙이 아니라 셀 코너(정수 좌표)에 정렬")]
        public bool alignToCorner;

        // IBuildingBoundsProvider: Manual 모드일 때만 직접 bounds를 반환.
        // 다른 모드는 default(size==0)를 반환해 editor의 auto-detect로 위임.
        public Bounds GetBuildingBounds()
        {
            if (boundsSource != BuildingBoundsSource.Manual)
                return default;

            Vector3 worldCenter = transform.TransformPoint(
                new Vector3(manualBoundsOffset.x, manualBoundsOffset.y, 0f));
            return new Bounds(worldCenter,
                new Vector3(manualBoundsSize.x, manualBoundsSize.y, 0.1f));
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (boundsSource != BuildingBoundsSource.Manual) return;
            Bounds b = GetBuildingBounds();
            UnityEditor.Handles.color = new Color(0f, 1f, 1f, 0.85f);
            UnityEditor.Handles.DrawWireCube(b.center, b.size);
        }
#endif
    }

    // ─── 열거형 ──────────────────────────────────────────────────────────

    public enum BuildingBoundsSource
    {
        AutoDetect,       // 자식 Tilemap → SpriteRenderer → 실패 순으로 자동 탐색
        TilemapChildren,  // 자식 Tilemap들의 합산 bounds만 사용
        SpriteRenderer,   // 루트 SpriteRenderer.bounds만 사용
        Manual,           // manualBoundsSize / manualBoundsOffset 직접 지정
    }

    // ─── 확장 인터페이스 ──────────────────────────────────────────────────

    /// <summary>
    /// 커스텀 bounds를 제공하고 싶은 컴포넌트가 구현하는 인터페이스.
    /// 반환 Bounds의 size가 0에 가까우면 BuildingAlignerEditor의 auto-detect로 위임됩니다.
    /// </summary>
    public interface IBuildingBoundsProvider
    {
        Bounds GetBuildingBounds();
    }
}

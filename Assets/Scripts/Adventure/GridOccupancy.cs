using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 타일 기반 이동 시 동시 타일 진입을 방지하는 정적 예약 시스템.
    ///
    /// 사용 규칙:
    ///   - 이동 시작 전 TryReserve(목적 타일) → 실패하면 이동 포기
    ///   - TryReserve 성공 후 Release(출발 타일)
    ///   - 오브젝트 파괴 시 Release(현재 타일)
    /// </summary>
    public static class GridOccupancy
    {
        private static readonly HashSet<Vector2Int> _reserved = new HashSet<Vector2Int>();

        private static Vector2Int ToCell(Vector2 worldPos, float unitSize)
        {
            return unitSize > 0.001f
                ? new Vector2Int(Mathf.RoundToInt(worldPos.x / unitSize),
                                 Mathf.RoundToInt(worldPos.y / unitSize))
                : new Vector2Int(Mathf.RoundToInt(worldPos.x),
                                 Mathf.RoundToInt(worldPos.y));
        }

        /// <summary>타일 예약 시도. 이미 예약된 타일이면 false 반환.</summary>
        public static bool TryReserve(Vector2 worldPos, float unitSize = 1f)
        {
            return _reserved.Add(ToCell(worldPos, unitSize));
        }

        /// <summary>타일 예약 해제.</summary>
        public static void Release(Vector2 worldPos, float unitSize = 1f)
        {
            _reserved.Remove(ToCell(worldPos, unitSize));
        }

        /// <summary>씬 로드 전 전체 초기화 (PlayMode 반복 진입 시 잔여 예약 제거).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Reset()
        {
            _reserved.Clear();
        }
    }
}

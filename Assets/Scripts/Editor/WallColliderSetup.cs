using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CardAdventure.Editor
{
    /// <summary>
    /// Wall_Tilemap에 TilemapCollider2D + CompositeCollider2D를 추가하는 일회성 유틸리티.
    /// Menu: CardAdventure/Map/Setup Wall Collider
    /// </summary>
    public static class WallColliderSetup
    {
        [MenuItem("CardAdventure/Map/Setup Wall Collider")]
        public static void SetupWallCollider()
        {
            // 1. Wall_Tilemap 탐색
            var wallObj = GameObject.Find("Wall_Tilemap");
            if (wallObj == null)
            {
                Debug.LogError("[WallColliderSetup] Wall_Tilemap을 찾을 수 없습니다.");
                return;
            }

            if (wallObj.GetComponent<Tilemap>() == null)
            {
                Debug.LogError("[WallColliderSetup] Wall_Tilemap에 Tilemap 컴포넌트가 없습니다.");
                return;
            }

            Undo.RecordObject(wallObj, "Setup Wall Collider");

            // 2. TilemapCollider2D 추가 (없으면)
            var tilemapCol = wallObj.GetComponent<TilemapCollider2D>();
            if (tilemapCol == null)
            {
                tilemapCol = Undo.AddComponent<TilemapCollider2D>(wallObj);
                Debug.Log("[WallColliderSetup] TilemapCollider2D 추가 완료");
            }
            else
            {
                Debug.Log("[WallColliderSetup] TilemapCollider2D 이미 존재");
            }

            // 3. CompositeCollider2D가 필요하므로 compositeOperation = Merge 설정
            tilemapCol.compositeOperation = Collider2D.CompositeOperation.Merge;
            tilemapCol.isTrigger = false;
            EditorUtility.SetDirty(tilemapCol);

            // 4. CompositeCollider2D 추가 (없으면) — Rigidbody2D도 자동 추가됨
            var composite = wallObj.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = Undo.AddComponent<CompositeCollider2D>(wallObj);
                Debug.Log("[WallColliderSetup] CompositeCollider2D 추가 완료");
            }
            // Polygons = 채워진 솔리드 영역, Outlines = 빈 외곽선
            // OverlapBoxAll로 타일 내부를 감지하려면 반드시 Polygons 사용
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            EditorUtility.SetDirty(composite);

            // 5. Rigidbody2D → Static
            var rb = wallObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Undo.RecordObject(rb, "Setup Wall Collider Rigidbody");
                rb.bodyType    = RigidbodyType2D.Static;
                rb.gravityScale = 0f;
                EditorUtility.SetDirty(rb);
                Debug.Log("[WallColliderSetup] Rigidbody2D bodyType → Static, gravityScale → 0");
            }

            // 6. 씬 저장 표시
            EditorUtility.SetDirty(wallObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                wallObj.scene);

            Debug.Log("[WallColliderSetup] 완료. Wall_Tilemap 콜라이더 설정 성공.");
            Debug.Log("  TilemapCollider2D.compositeOperation = Merge");
            Debug.Log("  CompositeCollider2D.geometryType = Polygons  ← 솔리드 충돌");
            Debug.Log("  Rigidbody2D.bodyType = Static");
        }

        /// <summary>
        /// 메뉴 항목 활성화 조건 — Wall_Tilemap이 씬에 있을 때만 활성화
        /// </summary>
        [MenuItem("CardAdventure/Map/Setup Wall Collider", true)]
        public static bool ValidateSetupWallCollider()
        {
            return GameObject.Find("Wall_Tilemap") != null;
        }
    }
}

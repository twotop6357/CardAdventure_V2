using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;

namespace CardAdventure.Editor
{
    public class CameraSetupHelper : EditorWindow
    {
        [MenuItem("CardAdventure/Setup Camera Confiner")]
        public static void SetupConfiner()
        {
            // 1. Cinemachine 카메라 찾기
            var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
            if (vcam == null)
            {
                Debug.LogError("CinemachineCamera를 찾을 수 없습니다.");
                return;
            }

            // 2. "Wall"이라는 이름이 포함된 모든 타일맵 찾기
            var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            var wallTilemaps = new System.Collections.Generic.List<Tilemap>();
            foreach (var tm in tilemaps)
            {
                if (tm.name.Contains("Wall")) wallTilemaps.Add(tm);
            }

            if (wallTilemaps.Count == 0)
            {
                Debug.LogError("'Wall'이 이름에 포함된 타일맵을 찾을 수 없습니다.");
                return;
            }

            // 3. Camera Confiner 오브젝트 설정
            GameObject confinerObj = GameObject.Find("Camera Confiner");
            if (confinerObj == null) confinerObj = new GameObject("Camera Confiner");
            var poly = confinerObj.GetComponent<PolygonCollider2D>();
            if (poly == null) poly = confinerObj.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;

            // 4. 모든 Wall 타일맵의 범위를 합쳐서 최외곽 범위를 계산
            Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, 0);
            Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, 0);
            bool foundAny = false;

            foreach (var wall in wallTilemaps)
            {
                BoundsInt wallBounds = wall.cellBounds;
                for (int x = wallBounds.xMin; x < wallBounds.xMax; x++)
                {
                    for (int y = wallBounds.yMin; y < wallBounds.yMax; y++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, 0);
                        if (wall.HasTile(pos))
                        {
                            // 고립된 타일은 무시 (확장성 및 실수 방지)
                            bool hasWallNeighbor = 
                                wall.HasTile(pos + Vector3Int.up) || wall.HasTile(pos + Vector3Int.down) || 
                                wall.HasTile(pos + Vector3Int.left) || wall.HasTile(pos + Vector3Int.right);

                            if (hasWallNeighbor)
                            {
                                min.x = Mathf.Min(min.x, pos.x);
                                min.y = Mathf.Min(min.y, pos.y);
                                max.x = Mathf.Max(max.x, pos.x);
                                max.y = Mathf.Max(max.y, pos.y);
                                foundAny = true;
                            }
                        }
                    }
                }
            }

            if (foundAny)
            {
                // 계산된 최외곽 좌표를 폴리곤 경로로 설정
                Vector2[] points = new Vector2[]
                {
                    new Vector2(min.x, min.y),
                    new Vector2(max.x + 1, min.y),
                    new Vector2(max.x + 1, max.y + 1),
                    new Vector2(min.x, max.y + 1)
                };
                poly.pathCount = 1;
                poly.SetPath(0, points);

                // 5. Confiner 컴포넌트 연결 및 설정
                var confiner = vcam.GetComponent<CinemachineConfiner2D>();
                if (confiner == null) confiner = vcam.gameObject.AddComponent<CinemachineConfiner2D>();
                
                confiner.BoundingShape2D = poly;
                confiner.Damping = 0;
                confiner.SlowingDistance = 0;
                confiner.enabled = true;
                confinerObj.SetActive(true);

                Selection.activeGameObject = confinerObj;
                Debug.Log($"카메라 경계가 벽 타일맵 외곽({min} ~ {max})으로 고정되었습니다. 이제 맵 밖으로 나가지 않습니다.");
            }
        }

        [MenuItem("CardAdventure/Fix Camera Follow")]
        public static void FixCamera()
        {
            var brain = Object.FindAnyObjectByType<CinemachineBrain>();
            var vcam = Object.FindAnyObjectByType<CinemachineCamera>();
            var player = GameObject.Find("Player");

            if (vcam != null && player != null)
            {
                vcam.gameObject.SetActive(true);
                vcam.enabled = true;
                vcam.Follow = player.transform;
                vcam.LookAt = null;
                vcam.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
                vcam.Lens.OrthographicSize = 5;
                vcam.transform.rotation = Quaternion.identity;
                
                var confiner = vcam.GetComponent<CinemachineConfiner2D>();
                if (confiner != null)
                {
                    confiner.Damping = 0;
                    confiner.SlowingDistance = 0;
                    confiner.enabled = true;
                }
                
                if (brain != null)
                {
                    brain.enabled = true;
                    brain.DefaultBlend.Time = 0;
                }
                Debug.Log("카메라 추적 설정이 초기화되었습니다.");
            }
            else
            {
                Debug.LogError("VCam 또는 Player를 찾을 수 없습니다.");
            }
        }
    }
}

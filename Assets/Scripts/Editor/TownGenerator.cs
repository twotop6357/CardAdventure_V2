using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

namespace CardAdventure.Editor
{
    public class TownGenerator : EditorWindow
    {
        private TileBase groundTile;
        private TileBase pathTile;
        private TileBase wallTile;

        [MenuItem("CardAdventure/Map/Generate Town (4 Houses)")]
        public static void ShowWindow()
        {
            GetWindow<TownGenerator>("Town Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("마을 타일맵 생성기 (InteliMap 에셋 활용)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("InteliMap Pro에는 완성된 '집' 스프라이트가 없으므로, 잔디(Plains)와 던전 벽(Dungeon) 타일을 조합해 4개의 건물 형태를 그립니다.", MessageType.Info);

            groundTile = (TileBase)EditorGUILayout.ObjectField("Ground Tile (Grass)", groundTile, typeof(TileBase), false);
            pathTile = (TileBase)EditorGUILayout.ObjectField("Path Tile (Dirt)", pathTile, typeof(TileBase), false);
            wallTile = (TileBase)EditorGUILayout.ObjectField("Wall Tile (House)", wallTile, typeof(TileBase), false);

            if (GUILayout.Button("자동 타일 할당 (InteliMap 샘플)"))
            {
                AutoAssignTiles();
            }

            if (GUILayout.Button("마을 생성 (AdventureScene)"))
            {
                GenerateTown();
            }
        }

        private void AutoAssignTiles()
        {
            // InteliMap Pro 예제 타일들을 강제로 불러와 할당
            string plainsPath1 = "Assets/Assets/InteliMap Pro/Examples/Tilemaps/Plains/PlainsTilemap_0.asset"; // 풀
            string plainsPath2 = "Assets/Assets/InteliMap Pro/Examples/Tilemaps/Plains/PlainsTilemap_48.asset"; // 흙길
            string dungeonPath = "Assets/Assets/InteliMap Pro/Examples/Tilemaps/Orthographic Dungeon/orthographic dungeon_15.asset"; // 벽

            groundTile = AssetDatabase.LoadAssetAtPath<TileBase>(plainsPath1);
            pathTile = AssetDatabase.LoadAssetAtPath<TileBase>(plainsPath2);
            wallTile = AssetDatabase.LoadAssetAtPath<TileBase>(dungeonPath);

            // 못 찾았을 경우 대비 첫 번째 에셋으로 대체
            if (groundTile == null) groundTile = LoadFirstTile("Assets/Assets/InteliMap Pro/Examples/Tilemaps/Plains");
            if (wallTile == null) wallTile = LoadFirstTile("Assets/Assets/InteliMap Pro/Examples/Tilemaps/Orthographic Dungeon");

            Debug.Log("타일 할당 완료.");
        }

        private TileBase LoadFirstTile(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:TileBase", new[] { folderPath });
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<TileBase>(path);
            }
            return null;
        }

        private void GenerateTown()
        {
            if (groundTile == null || wallTile == null)
            {
                Debug.LogError("타일이 할당되지 않았습니다. '자동 타일 할당'을 먼저 눌러주세요.");
                return;
            }

            GameObject gridObj = GameObject.Find("Grid");
            if (gridObj == null)
            {
                Debug.LogError("씬에 'Grid' 오브젝트가 없습니다. AdventureSceneBuilder로 먼저 씬을 구축하세요.");
                return;
            }

            Tilemap groundMap = null;
            Tilemap wallsMap = null;

            foreach (Transform child in gridObj.transform)
            {
                if (child.name == "Ground") groundMap = child.GetComponent<Tilemap>();
                if (child.name == "Walls") wallsMap = child.GetComponent<Tilemap>();
            }

            if (groundMap == null || wallsMap == null)
            {
                Debug.LogError("Ground 또는 Walls 타일맵을 찾을 수 없습니다.");
                return;
            }

            Undo.RecordObject(groundMap, "Generate Town Ground");
            Undo.RecordObject(wallsMap, "Generate Town Walls");

            groundMap.ClearAllTiles();
            wallsMap.ClearAllTiles();

            // 1. 바닥(잔디) 생성 (30x30)
            for (int x = -15; x <= 15; x++)
            {
                for (int y = -15; y <= 15; y++)
            {
                    groundMap.SetTile(new Vector3Int(x, y, 0), groundTile);
                }
            }

            // 2. 십자 형태의 길 생성
            TileBase actualPathTile = pathTile != null ? pathTile : groundTile;
            for (int i = -15; i <= 15; i++)
            {
                groundMap.SetTile(new Vector3Int(i, 0, 0), actualPathTile);
                groundMap.SetTile(new Vector3Int(i, -1, 0), actualPathTile);
                groundMap.SetTile(new Vector3Int(0, i, 0), actualPathTile);
                groundMap.SetTile(new Vector3Int(-1, i, 0), actualPathTile);
            }

            // 3. 4개의 집 (벽돌 건물 모양) 배치
            // 왼쪽 위
            BuildHouse(wallsMap, new Vector2Int(-8, 5), 5, 4);
            // 오른쪽 위
            BuildHouse(wallsMap, new Vector2Int(4, 5), 6, 5);
            // 왼쪽 아래
            BuildHouse(wallsMap, new Vector2Int(-10, -8), 5, 5);
            // 오른쪽 아래
            BuildHouse(wallsMap, new Vector2Int(5, -9), 4, 4);

            Debug.Log("마을 타일맵(집 4개) 생성이 완료되었습니다!");
        }

        private void BuildHouse(Tilemap wallsMap, Vector2Int bottomLeft, int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 테두리만 벽으로 칠하고 입구(아래쪽 중앙)는 비움
                    bool isBorder = (x == 0 || x == width - 1 || y == 0 || y == height - 1);
                    bool isDoor = (y == 0 && x == width / 2);

                    if (isBorder && !isDoor)
                    {
                        wallsMap.SetTile(new Vector3Int(bottomLeft.x + x, bottomLeft.y + y, 0), wallTile);
                    }
                }
            }
        }
    }
}

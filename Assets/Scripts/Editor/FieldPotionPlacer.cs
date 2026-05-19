using UnityEngine;
using UnityEditor;
using CardAdventure;

namespace CardAdventure.Editor
{
    /// <summary>
    /// 모험 씬 상에 포션을 정밀하게 배치하여 영구 저장해 주는 에디터 유틸리티 메뉴입니다.
    /// </summary>
    public static class FieldPotionPlacer
    {
        [MenuItem("CardAdventure/Place Field Potions")]
        public static void PlacePotions()
        {
            // 1. Potion Sprite 찾기
            string potionPath = "Assets/Assets/Sprites/Items/Potion.png";
            Sprite potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(potionPath);
            if (potionSprite == null)
            {
                // Texture2D로 로드한 뒤 Sprite로 가져오기
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(potionPath);
                if (tex != null)
                {
                    object[] assets = AssetDatabase.LoadAllAssetsAtPath(potionPath);
                    foreach (var asset in assets)
                    {
                        if (asset is Sprite)
                        {
                            potionSprite = (Sprite)asset;
                            break;
                        }
                    }
                }
            }

            if (potionSprite == null)
            {
                Debug.LogError("[FieldPotionPlacer] Potion Sprite를 로드하지 못했습니다.");
                return;
            }

            // 2. Player 위치 찾기
            GameObject playerGo = GameObject.Find("Player");
            Vector3 playerPos = playerGo != null ? playerGo.transform.position : Vector3.zero;

            // 3. 포션 생성 위치 정의 (플레이어 근처 2개, 조금 떨어진 평원 2개)
            Vector3[] spawnPositions = new Vector3[]
            {
                playerPos + new Vector3(3f, 2f, 0f),   // 우상단
                playerPos + new Vector3(-4f, 1f, 0f),  // 좌상단
                playerPos + new Vector3(8f, -3f, 0f),  // 멀리 우하단
                playerPos + new Vector3(-6f, -5f, 0f)  // 멀리 좌하단
            };

            int spawnCount = 0;
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                string potionName = "FieldPotion_" + (i + 1);
                if (GameObject.Find(potionName) != null)
                {
                    Debug.LogWarning($"[FieldPotionPlacer] {potionName}이 이미 존재하여 생성을 건너뜜.");
                    continue;
                }

                GameObject potionGo = new GameObject(potionName);
                potionGo.transform.position = spawnPositions[i];
                
                // 스프라이트 렌더러 추가 및 설정
                SpriteRenderer sr = potionGo.AddComponent<SpriteRenderer>();
                sr.sprite = potionSprite;
                sr.sortingLayerName = "Characters";
                sr.sortingOrder = 10;
                
                // 콜라이더 추가 및 트리거 설정
                BoxCollider2D bc = potionGo.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(0.8f, 0.8f);
                
                // FieldPotionItem 컴포넌트 추가
                potionGo.AddComponent<FieldPotionItem>();
                
                // Y-sorting을 위해 부모 설정 (Player와 동일한 부모)
                if (playerGo != null)
                {
                    potionGo.transform.SetParent(playerGo.transform.parent);
                }
                
                // 에디터 변경 사항 기록
                Undo.RegisterCreatedObjectUndo(potionGo, "Place Field Potion");
                spawnCount++;
            }

            if (spawnCount > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                Debug.Log($"[FieldPotionPlacer] {spawnCount}개의 필드 포션 아이템이 성공적으로 생성 및 배치되었습니다. 플레이어 좌표: {playerPos}");
            }
            else
            {
                Debug.Log("[FieldPotionPlacer] 이미 모든 포션이 배치되어 있습니다.");
            }
        }
    }
}

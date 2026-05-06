using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace CardAdventure
{
    /// <summary>
    /// AdventureScene 기본 계층을 자동으로 구성하는 에디터 도구.
    /// 메뉴: CardAdventure > Build Adventure Scene
    /// </summary>
    public static class AdventureSceneBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/AdventureScene.unity";
        private const string SPUM_UNIT_PATH =
            "Assets/Assets/SPUM/Resources/Addons/Legacy/2_Prefab/SPUM_20250915183854408.prefab";

        [MenuItem("CardAdventure/Build Adventure Scene")]
        public static void BuildAdventureScene()
        {
            // ── 씬 생성/로드 ────────────────────────────────────
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            // ── GameManagers 오브젝트 ────────────────────────────
            GameObject managers = new GameObject("GameManagers");
            managers.AddComponent<CardAdventure.GameDataManager>();
            managers.AddComponent<CardAdventure.SceneLoader>();

            // ── 카메라 (URP 2D) ───────────────────────────────────
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 5f;
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.09f, 0.13f, 0.17f, 1f);
            cam.nearClipPlane    = -100f;
            cam.farClipPlane     = 100f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();

            // ── Tilemap Grid ─────────────────────────────────────
            GameObject gridGo    = new GameObject("Grid");
            Grid grid            = gridGo.AddComponent<Grid>();
            grid.cellSize        = new Vector3(1f, 1f, 0f);

            // Ground 레이어
            GameObject groundGo  = new GameObject("Ground");
            groundGo.transform.SetParent(gridGo.transform);
            Tilemap groundTm     = groundGo.AddComponent<Tilemap>();
            TilemapRenderer groundTr = groundGo.AddComponent<TilemapRenderer>();
            groundTr.sortingLayerName = "Default";
            groundTr.sortingOrder     = 0;

            // Wall 레이어
            GameObject wallGo    = new GameObject("Walls");
            wallGo.transform.SetParent(gridGo.transform);
            Tilemap wallTm       = wallGo.AddComponent<Tilemap>();
            TilemapRenderer wallTr = wallGo.AddComponent<TilemapRenderer>();
            wallTr.sortingLayerName = "Default";
            wallTr.sortingOrder     = 1;
            // 벽 충돌용 콜라이더
            TilemapCollider2D wallCol = wallGo.AddComponent<TilemapCollider2D>();
            // Unity 6: compositeOperation으로 CompositeCollider2D와 결합
            wallCol.compositeOperation = Collider2D.CompositeOperation.Merge;
            CompositeCollider2D composite = wallGo.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
            Rigidbody2D wallRb = wallGo.GetComponent<Rigidbody2D>();
            if (wallRb == null) wallRb = wallGo.AddComponent<Rigidbody2D>();
            wallRb.bodyType = RigidbodyType2D.Static;

            // ── 플레이어 ────────────────────────────────────────
            GameObject playerGo = new GameObject("Player");
            playerGo.tag   = "Player";
            playerGo.layer = LayerMask.NameToLayer("Default");
            playerGo.transform.position = Vector3.zero;

            // Rigidbody2D + Collider
            Rigidbody2D playerRb        = playerGo.AddComponent<Rigidbody2D>();
            playerRb.gravityScale       = 0f;
            playerRb.freezeRotation     = true;
            playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D playerCol  = playerGo.AddComponent<CircleCollider2D>();
            playerCol.radius            = 0.3f;

            // PlayerController
            PlayerController pc = playerGo.AddComponent<PlayerController>();

            // Input System — PlayerInput 컴포넌트
            PlayerInput pi = playerGo.AddComponent<PlayerInput>();

            // SPUM 캐릭터 프리팹 자식으로 추가
            GameObject spumPrefabAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(SPUM_UNIT_PATH);
            if (spumPrefabAsset != null)
            {
                GameObject spumGo = (GameObject)PrefabUtility.InstantiatePrefab(
                    spumPrefabAsset, playerGo.transform);
                spumGo.transform.localPosition = Vector3.zero;
                spumGo.transform.localScale    = Vector3.one * 0.5f;

                // PlayerController에 SPUM 연결
                SerializedObject pcSo = new SerializedObject(pc);
                SPUM_Prefabs spumComp = spumGo.GetComponent<SPUM_Prefabs>();
                if (spumComp != null)
                    pcSo.FindProperty("spumPrefabs").objectReferenceValue = spumComp;
                pcSo.ApplyModifiedProperties();
            }
            else
            {
                // SPUM이 없으면 임시 스프라이트 렌더러
                SpriteRenderer sr = playerGo.AddComponent<SpriteRenderer>();
                sr.color = new Color(0.3f, 0.6f, 1f, 1f);
                Debug.LogWarning("[AdventureSceneBuilder] SPUM 프리팹을 찾을 수 없습니다. 임시 스프라이트 사용.");
            }

            // ── Cinemachine 카메라 팔로우 ─────────────────────────
            GameObject cmCamGo = new GameObject("CinemachineCamera");
            CinemachineCamera cmCam = cmCamGo.AddComponent<CinemachineCamera>();
            cmCam.Follow = playerGo.transform;
            cmCam.Lens.OrthographicSize = 5f;

            // PositionComposer 추가 (팔로우 감쇠)
            CinemachinePositionComposer composer =
                cmCamGo.AddComponent<CinemachinePositionComposer>();
            composer.Damping = new Vector3(0.5f, 0.5f, 0f);

            // CinemachineBrain은 Main Camera에
            camGo.AddComponent<CinemachineBrain>();

            // ── 샘플 배틀 입구 (베르데 슬라임) ─────────────────────
            EnemyData slime = AssetDatabase.LoadAssetAtPath<EnemyData>(
                "Assets/ScriptableObjects/Enemies/Enemy_Verde_Slime.asset");

            if (slime != null)
            {
                GameObject entranceGo = new GameObject("BattleEntrance_Slime");
                entranceGo.transform.position = new Vector3(3f, 0f, 0f);

                CircleCollider2D entranceCol = entranceGo.AddComponent<CircleCollider2D>();
                entranceCol.isTrigger = true;
                entranceCol.radius    = 0.8f;

                BattleEntrance be = entranceGo.AddComponent<BattleEntrance>();

                // 적 비주얼 (임시 스프라이트)
                GameObject visGo = new GameObject("EnemyVisual");
                visGo.transform.SetParent(entranceGo.transform);
                visGo.transform.localPosition = Vector3.zero;
                SpriteRenderer visSr = visGo.AddComponent<SpriteRenderer>();
                visSr.color = new Color(1f, 0.4f, 0.4f, 1f);

                // BattleEntrance 필드 연결
                SerializedObject beSo = new SerializedObject(be);
                beSo.FindProperty("enemyData").objectReferenceValue   = slime;
                beSo.FindProperty("enemyVisual").objectReferenceValue = visGo;
                beSo.ApplyModifiedProperties();
            }

            // ── 씬 저장 ─────────────────────────────────────────
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();

            Debug.Log("[AdventureSceneBuilder] ✅ AdventureScene 생성 완료: " + SCENE_PATH);
        }
    }
}

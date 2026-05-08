using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace CardAdventure
{
    /// <summary>
    /// Editor utility that rebuilds the basic AdventureScene.
    /// Menu: CardAdventure > Build Adventure Scene
    /// </summary>
    public static class AdventureSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/AdventureScene.unity";
        private const string PlayerControllerPath = "Assets/Animations/Player/Player_Warrior.controller";
        private const string PlayerIdleSpritePath =
            "Assets/Assets/Sprites/Character/Warrior/Idle/Warrior_IdleFront.png";

        [MenuItem("CardAdventure/Build Adventure Scene")]
        public static void BuildAdventureScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject managers = new GameObject("GameManagers");
            GameDataManager gameDataManager = managers.AddComponent<GameDataManager>();
            managers.AddComponent<SceneLoader>();
            ConfigureStarterDeck(gameDataManager);

            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.13f, 0.17f, 1f);
            cam.nearClipPlane = -100f;
            cam.farClipPlane = 100f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();

            GameObject gridGo = new GameObject("Grid");
            Grid grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            GameObject groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(gridGo.transform);
            groundGo.AddComponent<Tilemap>();
            TilemapRenderer groundTr = groundGo.AddComponent<TilemapRenderer>();
            groundTr.sortingLayerName = "Default";
            groundTr.sortingOrder = 0;

            GameObject wallGo = new GameObject("Walls");
            wallGo.transform.SetParent(gridGo.transform);
            wallGo.AddComponent<Tilemap>();
            TilemapRenderer wallTr = wallGo.AddComponent<TilemapRenderer>();
            wallTr.sortingLayerName = "Default";
            wallTr.sortingOrder = 1;
            TilemapCollider2D wallCol = wallGo.AddComponent<TilemapCollider2D>();
            wallCol.compositeOperation = Collider2D.CompositeOperation.Merge;
            CompositeCollider2D composite = wallGo.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
            Rigidbody2D wallRb = wallGo.GetComponent<Rigidbody2D>();
            if (wallRb == null)
            {
                wallRb = wallGo.AddComponent<Rigidbody2D>();
            }
            wallRb.bodyType = RigidbodyType2D.Static;

            GameObject playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.layer = LayerMask.NameToLayer("Default");
            playerGo.transform.position = Vector3.zero;

            Rigidbody2D playerRb = playerGo.AddComponent<Rigidbody2D>();
            playerRb.gravityScale = 0f;
            playerRb.freezeRotation = true;
            playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            playerRb.interpolation = RigidbodyInterpolation2D.Interpolate;

            BoxCollider2D playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = Vector2.one;
            playerCol.offset = new Vector2(0f, -0.5f);

            PlayerController pc = playerGo.AddComponent<PlayerController>();

            PlayerInput pi = playerGo.AddComponent<PlayerInput>();
            InputActionAsset inputActions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (inputActions != null)
            {
                pi.actions = inputActions;
                pi.defaultActionMap = "Player";
                pi.notificationBehavior = PlayerNotifications.SendMessages;
            }

            CreatePlayerVisual(playerGo.transform, pc);

            GameObject cmCamGo = new GameObject("CinemachineCamera");
            CinemachineCamera cmCam = cmCamGo.AddComponent<CinemachineCamera>();
            cmCam.Follow = playerGo.transform;
            cmCam.Lens.OrthographicSize = 5f;

            CinemachinePositionComposer composer = cmCamGo.AddComponent<CinemachinePositionComposer>();
            composer.Damping = new Vector3(0.5f, 0.5f, 0f);
            camGo.AddComponent<CinemachineBrain>();

            EnemyData slime = AssetDatabase.LoadAssetAtPath<EnemyData>(
                "Assets/ScriptableObjects/Enemies/Enemy_Verde_Slime.asset");

            if (slime != null)
            {
                GameObject entranceGo = new GameObject("BattleEntrance_Slime");
                entranceGo.transform.position = new Vector3(3f, 0f, 0f);

                CircleCollider2D entranceCol = entranceGo.AddComponent<CircleCollider2D>();
                entranceCol.isTrigger = true;
                entranceCol.radius = 0.8f;

                BattleEntrance be = entranceGo.AddComponent<BattleEntrance>();

                GameObject visGo = new GameObject("EnemyVisual");
                visGo.transform.SetParent(entranceGo.transform);
                visGo.transform.localPosition = Vector3.zero;
                SpriteRenderer visSr = visGo.AddComponent<SpriteRenderer>();
                visSr.color = new Color(1f, 0.4f, 0.4f, 1f);

                SerializedObject beSo = new SerializedObject(be);
                beSo.FindProperty("enemyData").objectReferenceValue = slime;
                beSo.FindProperty("enemyVisual").objectReferenceValue = visGo;
                beSo.ApplyModifiedProperties();
            }

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[AdventureSceneBuilder] AdventureScene build complete: " + ScenePath);
        }

        [MenuItem("CardAdventure/Setup Adventure Starter Deck")]
        public static void SetupAdventureStarterDeck()
        {
            EditorSceneManager.OpenScene(ScenePath);
            GameDataManager gameDataManager = Object.FindFirstObjectByType<GameDataManager>();
            if (gameDataManager == null)
            {
                Debug.LogError("[AdventureSceneBuilder] GameDataManager를 찾을 수 없습니다.");
                return;
            }

            ConfigureStarterDeck(gameDataManager);
            EditorSceneManager.SaveScene(gameDataManager.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[AdventureSceneBuilder] Adventure starter deck setup complete.");
        }

        private static void CreatePlayerVisual(Transform parent, PlayerController playerController)
        {
            GameObject visualGo = new GameObject("PlayerVisual");
            visualGo.transform.SetParent(parent);
            visualGo.transform.localPosition =
                new Vector3(0f, (AdventureGridUtility.ReferenceCharacterVisualHeight - 1f) * 0.5f, 0f);
            visualGo.transform.localScale = Vector3.one;

            SpriteRenderer spriteRenderer = visualGo.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 2;
            spriteRenderer.sprite = LoadSpriteByName(PlayerIdleSpritePath, "Warrior_IdleFront_0");
            float idleScale = AdventureGridUtility.GetVisualScaleForReferenceHeight(spriteRenderer.sprite);
            visualGo.transform.localScale = new Vector3(idleScale, idleScale, 1f);

            Animator animator = visualGo.AddComponent<Animator>();
            animator.runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);

            SerializedObject pcSo = new SerializedObject(playerController);
            pcSo.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            pcSo.FindProperty("animator").objectReferenceValue = animator;
            pcSo.FindProperty("moveUnitSize").floatValue = 1f;
            pcSo.FindProperty("useGridCellSize").boolValue = true;
            pcSo.FindProperty("moveHoldThreshold").floatValue = 0.06f;
            float idleMultiplier = 0.267f;
            pcSo.FindProperty("walkVisualScale").vector3Value =
                new Vector3(idleScale / idleMultiplier, idleScale / idleMultiplier, 1f);
            pcSo.FindProperty("idleVisualScaleMultiplier").floatValue = idleMultiplier;
            pcSo.ApplyModifiedProperties();
        }

        private static void ConfigureStarterDeck(GameDataManager gameDataManager)
        {
            string[] cardPaths =
            {
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Strike.asset",
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Strike.asset",
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Strike.asset",
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Defend.asset",
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Defend.asset",
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_ShieldBash.asset",
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Rage.asset",
                "Assets/ScriptableObjects/Cards/Warrior/Card_Warrior_Taunt.asset"
            };

            SerializedObject so = new SerializedObject(gameDataManager);
            SerializedProperty starterDeck = so.FindProperty("starterDeck");
            starterDeck.ClearArray();
            for (int i = 0; i < cardPaths.Length; i++)
            {
                starterDeck.InsertArrayElementAtIndex(i);
                starterDeck.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<CardData>(cardPaths[i]);
            }
            so.ApplyModifiedProperties();
        }

        private static Sprite LoadSpriteByName(string assetPath, string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardAdventure
{
    public static class WarriorPlayerVisualSetup
    {
        private const string AnimationFolder = "Assets/Animations/Player";
        private const string ScenePath = "Assets/Scenes/AdventureScene.unity";
        private const float FrameTime = 0.25f;

        [MenuItem("CardAdventure/Setup Warrior Player Visual")]
        public static void Setup()
        {
            SetupWarrior();
        }

        [MenuItem("CardAdventure/Player Visual/Setup All Class Visuals")]
        public static void SetupAll()
        {
            SetupClass(GetWarriorConfig(), applyToScene: false);
            SetupClass(GetMagicianConfig(), applyToScene: false);
            SetupClass(GetRogueConfig(), applyToScene: false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[WarriorPlayerVisualSetup] All class player visuals generated.");
        }

        [MenuItem("CardAdventure/Player Visual/Apply Warrior")]
        public static void SetupWarrior()
        {
            SetupClass(GetWarriorConfig(), applyToScene: true);
        }

        [MenuItem("CardAdventure/Player Visual/Apply Magician")]
        public static void SetupMagician()
        {
            SetupClass(GetMagicianConfig(), applyToScene: true);
        }

        [MenuItem("CardAdventure/Player Visual/Apply Rogue")]
        public static void SetupRogue()
        {
            SetupClass(GetRogueConfig(), applyToScene: true);
        }

        private static void SetupClass(CharacterVisualConfig config, bool applyToScene)
        {
            EnsureFolders();

            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>
            {
                ["Player_IdleFront"] = CreateOrUpdateClip(
                    config,
                    "Player_IdleFront",
                    $"{config.SpriteRoot}/Idle/{config.SpritePrefix}_IdleFront.png",
                    $"{config.SpritePrefix}_IdleFront",
                    new[] { 0, 2, 4, 2 }),
                ["Player_IdleBack"] = CreateOrUpdateClip(
                    config,
                    "Player_IdleBack",
                    $"{config.SpriteRoot}/Idle/{config.SpritePrefix}_IdleBack.png",
                    $"{config.SpritePrefix}_IdleBack",
                    new[] { 0, 2, 4, 2 }),
                ["Player_IdleSide"] = CreateOrUpdateClip(
                    config,
                    "Player_IdleSide",
                    $"{config.SpriteRoot}/Idle/{config.SpritePrefix}_IdleBeside.png",
                    $"{config.SpritePrefix}_IdleBeside",
                    new[] { 0, 2, 4, 2 }),
                ["Player_WalkFront"] = CreateOrUpdateClip(
                    config,
                    "Player_WalkFront",
                    $"{config.SpriteRoot}/Walk/{config.SpritePrefix}_WalkFront.png",
                    $"{config.SpritePrefix}_WalkFront",
                    new[] { 0, 2, 4, 6 }),
                ["Player_WalkBack"] = CreateOrUpdateClip(
                    config,
                    "Player_WalkBack",
                    $"{config.SpriteRoot}/Walk/{config.SpritePrefix}_WalkBack.png",
                    $"{config.SpritePrefix}_WalkBack",
                    new[] { 0, 2, 4, 6 }),
                ["Player_WalkSide"] = CreateOrUpdateClip(
                    config,
                    "Player_WalkSide",
                    $"{config.SpriteRoot}/Walk/{config.SpritePrefix}_WalkBeside.png",
                    $"{config.SpritePrefix}_WalkBeside",
                    new[] { 0, 2, 4, 6 })
            };

            AnimatorController controller = CreateOrUpdateController(config, clips);
            if (applyToScene)
            {
                ApplyToAdventureScene(config, controller);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[WarriorPlayerVisualSetup] {config.DisplayName} player visual setup complete.");
        }

        private static CharacterVisualConfig GetWarriorConfig()
        {
            return new CharacterVisualConfig(
                "Warrior",
                "Warrior",
                "Assets/Assets/Sprites/Character/Warrior",
                AnimationFolder,
                AnimationFolder + "/Player_Warrior.controller",
                0.267f);
        }

        private static CharacterVisualConfig GetMagicianConfig()
        {
            return new CharacterVisualConfig(
                "Magician",
                "Magician",
                "Assets/Assets/Sprites/Character/Magician",
                AnimationFolder + "/Magician",
                AnimationFolder + "/Player_Magician.controller",
                0.85f);
        }

        private static CharacterVisualConfig GetRogueConfig()
        {
            return new CharacterVisualConfig(
                "Rogue",
                "Rogue",
                "Assets/Assets/Sprites/Character/Rogue",
                AnimationFolder + "/Rogue",
                AnimationFolder + "/Player_Rogue.controller",
                0.74f);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }

            if (!AssetDatabase.IsValidFolder(AnimationFolder))
            {
                AssetDatabase.CreateFolder("Assets/Animations", "Player");
            }
        }

        private static AnimationClip CreateOrUpdateClip(
            CharacterVisualConfig config,
            string clipName,
            string spritePath,
            string spritePrefix,
            IReadOnlyList<int> frameIndexes)
        {
            EnsureClassFolder(config);

            string clipPath = config.ClipFolder + "/" + clipName + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.name = clipName;
            clip.frameRate = 1f / FrameTime;

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            }

            EditorCurveBinding spriteBinding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frameIndexes.Count];
            for (int i = 0; i < frameIndexes.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i * FrameTime,
                    value = LoadSpriteByName(spritePath, spritePrefix + "_" + frameIndexes[i])
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void EnsureClassFolder(CharacterVisualConfig config)
        {
            if (config.ClipFolder == AnimationFolder || AssetDatabase.IsValidFolder(config.ClipFolder))
            {
                return;
            }

            AssetDatabase.CreateFolder(AnimationFolder, config.DisplayName);
        }

        private static AnimatorController CreateOrUpdateController(CharacterVisualConfig config, Dictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(config.ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(config.ControllerPath);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            ChildAnimatorState[] existingStates = stateMachine.states;
            foreach (ChildAnimatorState state in existingStates)
            {
                stateMachine.RemoveState(state.state);
            }

            AnimatorState defaultState = null;
            foreach (KeyValuePair<string, AnimationClip> pair in clips)
            {
                AnimatorState state = stateMachine.AddState(pair.Key);
                state.motion = pair.Value;

                if (pair.Key == "Player_IdleFront")
                {
                    defaultState = state;
                }
            }

            stateMachine.defaultState = defaultState;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ApplyToAdventureScene(CharacterVisualConfig config, AnimatorController controller)
        {
            EditorSceneManager.OpenScene(ScenePath);
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                throw new InvalidOperationException("Player object not found in AdventureScene.");
            }

            for (int i = player.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(player.transform.GetChild(i).gameObject);
            }

            GameObject visual = new GameObject("PlayerVisual");
            visual.transform.SetParent(player.transform);
            visual.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            visual.transform.localScale = Vector3.one;

            SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 2;
            spriteRenderer.sprite = LoadSpriteByName(
                $"{config.SpriteRoot}/Idle/{config.SpritePrefix}_IdleFront.png",
                $"{config.SpritePrefix}_IdleFront_0");

            Animator animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                throw new InvalidOperationException("PlayerController not found on Player.");
            }

            CircleCollider2D playerCollider = player.GetComponent<CircleCollider2D>();
            if (playerCollider != null)
            {
                playerCollider.radius = 0.225f;
                playerCollider.offset = new Vector2(0f, -0.45f);
                EditorUtility.SetDirty(playerCollider);
            }

            SerializedObject playerControllerSo = new SerializedObject(playerController);
            playerControllerSo.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            playerControllerSo.FindProperty("animator").objectReferenceValue = animator;
            playerControllerSo.FindProperty("moveUnitSize").floatValue = 1f;
            playerControllerSo.FindProperty("useGridCellSize").boolValue = true;
            playerControllerSo.FindProperty("moveHoldThreshold").floatValue = 0.06f;
            playerControllerSo.FindProperty("walkVisualScale").vector3Value = Vector3.one;
            playerControllerSo.FindProperty("idleVisualScaleMultiplier").floatValue = config.IdleVisualScaleMultiplier;
            playerControllerSo.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(player.scene);
        }

        private static Sprite LoadSpriteByName(string assetPath, string spriteName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            throw new InvalidOperationException("Sprite not found: " + spriteName);
        }

        private readonly struct CharacterVisualConfig
        {
            public CharacterVisualConfig(
                string displayName,
                string spritePrefix,
                string spriteRoot,
                string clipFolder,
                string controllerPath,
                float idleVisualScaleMultiplier)
            {
                DisplayName = displayName;
                SpritePrefix = spritePrefix;
                SpriteRoot = spriteRoot;
                ClipFolder = clipFolder;
                ControllerPath = controllerPath;
                IdleVisualScaleMultiplier = idleVisualScaleMultiplier;
            }

            public string DisplayName { get; }
            public string SpritePrefix { get; }
            public string SpriteRoot { get; }
            public string ClipFolder { get; }
            public string ControllerPath { get; }
            public float IdleVisualScaleMultiplier { get; }
        }
    }
}

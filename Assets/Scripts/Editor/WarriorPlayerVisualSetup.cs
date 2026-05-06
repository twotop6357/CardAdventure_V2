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
        private const string ControllerPath = AnimationFolder + "/Player_Warrior.controller";
        private const string ScenePath = "Assets/Scenes/AdventureScene.unity";
        private const float FrameTime = 0.25f;

        [MenuItem("CardAdventure/Setup Warrior Player Visual")]
        public static void Setup()
        {
            EnsureFolders();

            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>
            {
                ["Player_IdleFront"] = CreateOrUpdateClip(
                    "Player_IdleFront",
                    "Assets/Assets/Sprites/Character/Warrior/Idle/Warrior_IdleFront.png",
                    "Warrior_IdleFront",
                    new[] { 0, 2, 4, 2 }),
                ["Player_IdleBack"] = CreateOrUpdateClip(
                    "Player_IdleBack",
                    "Assets/Assets/Sprites/Character/Warrior/Idle/Warrior_IdleBack.png",
                    "Warrior_IdleBack",
                    new[] { 0, 2, 4, 2 }),
                ["Player_IdleSide"] = CreateOrUpdateClip(
                    "Player_IdleSide",
                    "Assets/Assets/Sprites/Character/Warrior/Idle/Warrior_IdleBeside.png",
                    "Warrior_IdleBeside",
                    new[] { 0, 2, 4, 2 }),
                ["Player_WalkFront"] = CreateOrUpdateClip(
                    "Player_WalkFront",
                    "Assets/Assets/Sprites/Character/Warrior/Walk/Warrior_WalkFront.png",
                    "Warrior_WalkFront",
                    new[] { 0, 2, 4, 6 }),
                ["Player_WalkBack"] = CreateOrUpdateClip(
                    "Player_WalkBack",
                    "Assets/Assets/Sprites/Character/Warrior/Walk/Warrior_WalkBack.png",
                    "Warrior_WalkBack",
                    new[] { 0, 2, 4, 6 }),
                ["Player_WalkSide"] = CreateOrUpdateClip(
                    "Player_WalkSide",
                    "Assets/Assets/Sprites/Character/Warrior/Walk/Warrior_WalkBeside.png",
                    "Warrior_WalkBeside",
                    new[] { 0, 2, 4, 6 })
            };

            AnimatorController controller = CreateOrUpdateController(clips);
            ApplyToAdventureScene(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[WarriorPlayerVisualSetup] Warrior player visual setup complete.");
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
            string clipName,
            string spritePath,
            string spritePrefix,
            IReadOnlyList<int> frameIndexes)
        {
            string clipPath = AnimationFolder + "/" + clipName + ".anim";
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

        private static AnimatorController CreateOrUpdateController(Dictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
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

        private static void ApplyToAdventureScene(AnimatorController controller)
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
                "Assets/Assets/Sprites/Character/Warrior/Idle/Warrior_IdleFront.png",
                "Warrior_IdleFront_0");

            Animator animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                throw new InvalidOperationException("PlayerController not found on Player.");
            }

            SerializedObject playerControllerSo = new SerializedObject(playerController);
            playerControllerSo.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            playerControllerSo.FindProperty("animator").objectReferenceValue = animator;
            playerControllerSo.FindProperty("moveUnitSize").floatValue = 1f;
            playerControllerSo.FindProperty("useGridCellSize").boolValue = true;
            playerControllerSo.FindProperty("moveHoldThreshold").floatValue = 0.06f;
            playerControllerSo.FindProperty("walkVisualScale").vector3Value = Vector3.one;
            playerControllerSo.FindProperty("idleVisualScaleMultiplier").floatValue = 0.267f;
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
    }
}

using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardAdventure.Editor
{
    /// <summary>
    /// Builds FemaleNPC animation clips and controller from FemaleNPC_Sprites.png.
    /// </summary>
    public static class FemaleNpcAnimationSetup
    {
        private const string SpritePath     = "Assets/Assets/Sprites/NPCs/FemaleNPC_Sprites.png";
        private const string ControllerPath = "Assets/Animations/NPC/NPC_FemaleChaser.controller";
        private const string EnemyPath      = "Assets/ScriptableObjects/Enemies/Enemy_MagicDeer_Female.asset";
        private const string ClipDir        = "Assets/Animations/NPC";
        private const float  FrameRate      = 6f;
        private static readonly Vector2 FemaleChaserFootCell = new(6.5f, 8f);

        private static readonly (int start, int count, string name, bool loop)[] Clips =
        {
            ( 0, 7, "FemaleNPC_WalkFront", true),
            ( 7, 7, "FemaleNPC_WalkSide",  true),
            (21, 7, "FemaleNPC_WalkBack",  true),
            ( 1, 1, "FemaleNPC_IdleDown",  false),
            ( 8, 1, "FemaleNPC_IdleSide",  false),
            (22, 1, "FemaleNPC_IdleBack",  false),
        };

        [MenuItem("CardAdventure/NPC/Setup FemaleNPC Animations")]
        public static void SetupAll()
        {
            Sprite[] sprites = LoadOrderedSprites(SpritePath);
            if (sprites.Length == 0)
            {
                Debug.LogError($"[FemaleNpcSetup] Sprite not found: {SpritePath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(ClipDir))
                AssetDatabase.CreateFolder("Assets/Animations", "NPC");

            var clipMap = new System.Collections.Generic.Dictionary<string, AnimationClip>();
            int created = 0;
            int updated = 0;

            foreach (var (start, count, name, loop) in Clips)
            {
                if (start + count > sprites.Length)
                {
                    Debug.LogWarning($"[FemaleNpcSetup] {name}: not enough frames.");
                    continue;
                }

                string path = $"{ClipDir}/{name}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                bool isNew = clip == null;
                if (isNew) clip = new AnimationClip { name = name };

                BuildClip(clip, sprites, start, count, loop);

                if (isNew)
                {
                    AssetDatabase.CreateAsset(clip, path);
                    created++;
                }
                else
                {
                    EditorUtility.SetDirty(clip);
                    updated++;
                }

                clipMap[name] = clip;
            }

            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null)
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            foreach (var (_, _, name, _) in Clips)
            {
                if (clipMap.TryGetValue(name, out AnimationClip clip))
                    AddOrUpdateState(sm, name, clip);
            }

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FemaleNpcSetup] Done. Created {created}, updated {updated}. Controller: {ControllerPath}");
        }

        [MenuItem("CardAdventure/NPC/Configure FemaleChaser Scene Object")]
        public static void ConfigureSceneObject()
        {
            GameObject npc = GameObject.Find("NPC_FemaleChaser");
            if (npc == null)
            {
                Debug.LogError("[FemaleNpcSetup] NPC_FemaleChaser not found in the active scene.");
                return;
            }

            Sprite[] sprites = LoadOrderedSprites(SpritePath);
            Sprite idleDown = sprites.FirstOrDefault(s => s.name == "FemaleNPC_Sprites_1") ?? sprites.FirstOrDefault();
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            EnemyData enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyPath);

            var renderer = npc.GetComponent<SpriteRenderer>();
            var animator = npc.GetComponent<Animator>();
            var collider = npc.GetComponent<BoxCollider2D>();
            var rb = npc.GetComponent<Rigidbody2D>();
            var chaser = npc.GetComponent<NpcChaser>();

            if (renderer != null)
                renderer.sprite = idleDown;

            if (animator != null)
                animator.runtimeAnimatorController = controller;

            if (chaser != null)
            {
                var serialized = new SerializedObject(chaser);
                SetString(serialized, "animWalkFront", "FemaleNPC_WalkFront");
                SetString(serialized, "animWalkSide", "FemaleNPC_WalkSide");
                SetString(serialized, "animWalkBack", "FemaleNPC_WalkBack");
                SetString(serialized, "animIdleDown", "FemaleNPC_IdleDown");
                SetString(serialized, "animIdleSide", "FemaleNPC_IdleSide");
                SetString(serialized, "animIdleBack", "FemaleNPC_IdleBack");
                SerializedProperty enemyProp = serialized.FindProperty("battleEnemyData");
                if (enemyProp != null)
                    enemyProp.objectReferenceValue = enemyData;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            if (renderer != null && renderer.sprite != null)
            {
                float scale = AdventureGridUtility.GetVisualScaleForReferenceHeight(renderer.sprite);
                npc.transform.localScale = new Vector3(scale, scale, npc.transform.localScale.z);
            }

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            AdventureGridUtility.ConfigureFootCollider(collider, npc.transform, renderer, Vector2.one);
            Vector2 root = AdventureGridUtility.GetRootPositionForFootCenter(FemaleChaserFootCell, npc.transform, collider);
            npc.transform.position = new Vector3(root.x, root.y, npc.transform.position.z);
            if (rb != null)
                rb.position = root;

            EditorUtility.SetDirty(npc);
            if (renderer != null) EditorUtility.SetDirty(renderer);
            if (animator != null) EditorUtility.SetDirty(animator);
            if (collider != null) EditorUtility.SetDirty(collider);
            if (rb != null) EditorUtility.SetDirty(rb);
            if (chaser != null) EditorUtility.SetDirty(chaser);
            EditorSceneManager.MarkSceneDirty(npc.scene);
            EditorSceneManager.SaveScene(npc.scene);
            Debug.Log("[FemaleNpcSetup] NPC_FemaleChaser configured and scene saved.");
        }

        private static Sprite[] LoadOrderedSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => ExtractTrailingNumber(s.name))
                .ToArray();
        }

        private static int ExtractTrailingNumber(string name)
        {
            Match match = Regex.Match(name, @"(\d+)$");
            return match.Success ? int.Parse(match.Value) : 0;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.stringValue = value;
        }

        private static void BuildClip(AnimationClip clip, Sprite[] sprites, int startIdx, int frameCount, bool loop)
        {
            clip.frameRate = FrameRate;
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / FrameRate,
                    value = sprites[startIdx + i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static void AddOrUpdateState(AnimatorStateMachine sm, string stateName, AnimationClip clip)
        {
            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state.name == stateName)
                {
                    child.state.motion = clip;
                    EditorUtility.SetDirty(child.state);
                    return;
                }
            }

            sm.AddState(stateName).motion = clip;
        }
    }
}

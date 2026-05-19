using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CardAdventure.Editor
{
    public static class ExaminerNpcSetup
    {
        private const string SpritePath = "Assets/Assets/Sprites/NPCs/Examiner_Sprites.png";
        private const string AnimFolder = "Assets/Animations/NPCs/Examiner";
        private const string ControllerPath = AnimFolder + "/Examiner_Controller.controller";
        private const string PrefabPath = "Assets/Prefabs/NPCs/NPC_Examiner.prefab";
        private const string QuestionDialoguePath = "Assets/ScriptableObjects/Dialogues/NPC_ExaminerQuestion_Dialogue.asset";
        private const string IntoBattleDialoguePath = "Assets/ScriptableObjects/Dialogues/NPC_ExaminerIntoBattle_Dialogue.asset";
        private const string BattleEndDialoguePath = "Assets/ScriptableObjects/Dialogues/NPC_ExaminerBattleEnd_Dialogue.asset";
        private const string BossEnemyPath = "Assets/ScriptableObjects/Enemies/Enemy_MagicCrow.asset";
        private const string BattleIntroPath = "Assets/ScriptableObjects/BattleIntros/BattleIntro_Examiner.asset";

        [MenuItem("CardAdventure/Setup Examiner NPC")]
        public static void Setup()
        {
            List<Sprite> sprites = LoadSprites();
            if (sprites.Count == 0)
            {
                Debug.LogError($"[ExaminerNpcSetup] No sprites found at {SpritePath}. Import it as Multiple Sprite.");
                return;
            }

            EnsureFolder("Assets/Animations");
            EnsureFolder("Assets/Animations/NPCs");
            EnsureFolder(AnimFolder);
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/NPCs");

            int framesPerDir = sprites.Count / 4;
            if (framesPerDir <= 0)
            {
                Debug.LogError("[ExaminerNpcSetup] Not enough sprites to create directional clips.");
                return;
            }

            AnimationClip front = CreateOrUpdateClip(sprites.GetRange(0, framesPerDir), AnimFolder + "/Examiner_IdleFront.anim");
            AnimationClip left = CreateOrUpdateClip(sprites.GetRange(framesPerDir, framesPerDir), AnimFolder + "/Examiner_IdleLeft.anim");
            AnimationClip right = CreateOrUpdateClip(sprites.GetRange(framesPerDir * 2, framesPerDir), AnimFolder + "/Examiner_IdleRight.anim");
            AnimationClip back = CreateOrUpdateClip(sprites.GetRange(framesPerDir * 3, framesPerDir), AnimFolder + "/Examiner_IdleBack.anim");

            AnimatorController controller = RebuildController(front, back, left, right);
            SavePrefab(sprites[0], controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ExaminerNpcSetup] Setup complete. Prefab saved to {PrefabPath}");
        }

        private static List<Sprite> LoadSprites()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SpritePath);
            List<Sprite> sprites = new List<Sprite>();
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort((a, b) => GetSpriteNumber(a.name).CompareTo(GetSpriteNumber(b.name)));
            return sprites;
        }

        private static AnimatorController RebuildController(AnimationClip front, AnimationClip back, AnimationClip left, AnimationClip right)
        {
            AssetDatabase.DeleteAsset(ControllerPath);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("DirectionX", AnimatorControllerParameterType.Float);
            controller.AddParameter("DirectionY", AnimatorControllerParameterType.Float);

            BlendTree blendTree;
            AnimatorState state = controller.layers[0].stateMachine.AddState("Idle");
            controller.CreateBlendTreeInController("IdleTree", out blendTree);
            state.motion = blendTree;

            blendTree.blendType = BlendTreeType.SimpleDirectional2D;
            blendTree.blendParameter = "DirectionX";
            blendTree.blendParameterY = "DirectionY";
            blendTree.AddChild(front, new Vector2(0f, -1f));
            blendTree.AddChild(back, new Vector2(0f, 1f));
            blendTree.AddChild(left, new Vector2(-1f, 0f));
            blendTree.AddChild(right, new Vector2(1f, 0f));

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void SavePrefab(Sprite firstSprite, RuntimeAnimatorController controller)
        {
            GameObject npc = new GameObject("NPC_Examiner");

            SpriteRenderer spriteRenderer = npc.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = firstSprite;
            spriteRenderer.sortingOrder = 1;

            float visualScale = AdventureGridUtility.GetVisualScaleForReferenceHeight(firstSprite);
            npc.transform.localScale = new Vector3(visualScale, visualScale, 1f);

            Animator animator = npc.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            BoxCollider2D boxCollider = npc.AddComponent<BoxCollider2D>();
            AdventureGridUtility.ConfigureFootCollider(boxCollider, npc.transform, spriteRenderer, AdventureGridUtility.GetCellSize(1f));

            Rigidbody2D rigidbody = npc.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            npc.AddComponent<NpcTileAlignment>();

            NpcInteractable interactable = npc.AddComponent<NpcInteractable>();
            DialogueData questionDialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(QuestionDialoguePath);
            interactable.SetDialogueData(questionDialogue);

            ExaminerNpc examiner = npc.AddComponent<ExaminerNpc>();
            SerializedObject examinerSo = new SerializedObject(examiner);
            examinerSo.FindProperty("questionDialogue").objectReferenceValue = questionDialogue;
            examinerSo.FindProperty("intoBattleDialogue").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogueData>(IntoBattleDialoguePath);
            examinerSo.FindProperty("battleEndDialogue").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogueData>(BattleEndDialoguePath);
            examinerSo.FindProperty("bossEnemyData").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<EnemyData>(BossEnemyPath);
            examinerSo.FindProperty("battleIntroData").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<BattleIntroData>(BattleIntroPath);
            examinerSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(npc, PrefabPath);
            Object.DestroyImmediate(npc);
        }

        private static AnimationClip CreateOrUpdateClip(List<Sprite> sprites, string path)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.frameRate = 8f;
            ApplyClipFrames(clip, sprites);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void ApplyClipFrames(AnimationClip clip, List<Sprite> sprites)
        {
            EditorCurveBinding spriteBinding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Count + 1];
            for (int i = 0; i < sprites.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / clip.frameRate,
                    value = sprites[i]
                };
            }

            keyframes[sprites.Count] = new ObjectReferenceKeyframe
            {
                time = sprites.Count / clip.frameRate,
                value = sprites[sprites.Count - 1]
            };

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyframes);
        }

        private static int GetSpriteNumber(string name)
        {
            int underscoreIndex = name.LastIndexOf('_');
            if (underscoreIndex >= 0 && underscoreIndex < name.Length - 1)
            {
                if (int.TryParse(name.Substring(underscoreIndex + 1), out int result))
                {
                    return result;
                }
            }

            return 0;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int lastSlash = path.LastIndexOf('/');
            string parent = path.Substring(0, lastSlash);
            string folder = path.Substring(lastSlash + 1);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}

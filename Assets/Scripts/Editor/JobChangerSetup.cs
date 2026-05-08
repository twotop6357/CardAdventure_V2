using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

namespace CardAdventure.Editor
{
    public static class JobChangerSetup
    {
        [MenuItem("CardAdventure/Fix Job Changer Animation Clips")]
        public static void FixAnimationClips()
        {
            string spritePath = "Assets/Assets/Sprites/NPCs/JobChanger_Sprite.png";
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            List<Sprite> sprites = new List<Sprite>();
            foreach (var asset in assets)
            {
                if (asset is Sprite s)
                {
                    sprites.Add(s);
                }
            }

            if (sprites.Count == 0)
            {
                Debug.LogError($"[JobChangerSetup] No sprites found at {spritePath}. Is the file correctly imported as a Multiple Sprite?");
                return;
            }

            sprites.Sort((a, b) =>
            {
                int aNum = GetSpriteNumber(a.name);
                int bNum = GetSpriteNumber(b.name);
                return aNum.CompareTo(bNum);
            });

            int framesPerDir = sprites.Count / 4;
            if (framesPerDir <= 0)
            {
                Debug.LogError("[JobChangerSetup] Not enough sprites to create directional clips.");
                return;
            }

            string animPath = "Assets/Animations/NPCs/JobChanger";

            // Sheet row order: front, left, right, back.
            ApplyClipFrames(sprites.GetRange(0, framesPerDir), animPath + "/IdleFront.anim");
            ApplyClipFrames(sprites.GetRange(framesPerDir, framesPerDir), animPath + "/IdleLeft.anim");
            ApplyClipFrames(sprites.GetRange(framesPerDir * 2, framesPerDir), animPath + "/IdleRight.anim");
            ApplyClipFrames(sprites.GetRange(framesPerDir * 3, framesPerDir), animPath + "/IdleBack.anim");

            AssetDatabase.SaveAssets();
            Debug.Log("[JobChangerSetup] JobChanger animation clips fixed.");
        }

        [MenuItem("CardAdventure/Setup Job Changer")]
        public static void Setup()
        {
            string spritePath = "Assets/Assets/Sprites/NPCs/JobChanger_Sprite.png";
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            List<Sprite> sprites = new List<Sprite>();
            foreach (var asset in assets)
            {
                if (asset is Sprite s) sprites.Add(s);
            }
            
            if (sprites.Count == 0)
            {
                Debug.LogError($"[JobChangerSetup] No sprites found at {spritePath}. Is the file correctly imported as a Multiple Sprite?");
                return;
            }

            // Sort by name (JobChanger_Sprite_0, 1, 2...)
            sprites.Sort((a, b) => 
            {
                int aNum = GetSpriteNumber(a.name);
                int bNum = GetSpriteNumber(b.name);
                return aNum.CompareTo(bNum);
            });

            // Ensure folders exist
            EnsureFolder("Assets/Animations");
            EnsureFolder("Assets/Animations/NPCs");
            EnsureFolder("Assets/Animations/NPCs/JobChanger");

            string animPath = "Assets/Animations/NPCs/JobChanger";

            // Sheet row order: front, left, right, back.
            int framesPerDir = sprites.Count / 4;
            
            AnimationClip front = CreateClip(sprites.GetRange(0, Mathf.Min(framesPerDir, sprites.Count)), animPath + "/IdleFront.anim");
            AnimationClip left = CreateClip(sprites.GetRange(Mathf.Min(framesPerDir, sprites.Count), Mathf.Min(framesPerDir, sprites.Count - framesPerDir)), animPath + "/IdleLeft.anim");
            AnimationClip right = CreateClip(sprites.GetRange(Mathf.Min(framesPerDir * 2, sprites.Count), Mathf.Min(framesPerDir, sprites.Count - framesPerDir * 2)), animPath + "/IdleRight.anim");
            AnimationClip back = CreateClip(sprites.GetRange(Mathf.Min(framesPerDir * 3, sprites.Count), Mathf.Min(framesPerDir, sprites.Count - framesPerDir * 3)), animPath + "/IdleBack.anim");

            // Create Animator Controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(animPath + "/JobChanger_Controller.controller");
            controller.AddParameter("DirectionX", AnimatorControllerParameterType.Float);
            controller.AddParameter("DirectionY", AnimatorControllerParameterType.Float);

            // Create BlendTree
            BlendTree blendTree;
            AnimatorState state = controller.layers[0].stateMachine.AddState("Idle");
            controller.CreateBlendTreeInController("IdleTree", out blendTree);
            state.motion = blendTree;
            blendTree.blendType = BlendTreeType.SimpleDirectional2D;
            blendTree.blendParameter = "DirectionX";
            blendTree.blendParameterY = "DirectionY";

            blendTree.AddChild(front, new Vector2(0, -1)); // Down
            blendTree.AddChild(back, new Vector2(0, 1));   // Up
            blendTree.AddChild(left, new Vector2(-1, 0));  // Left
            blendTree.AddChild(right, new Vector2(1, 0));  // Right

            // Create Prefab
            GameObject prefab = new GameObject("NPC_JobChanger");
            SpriteRenderer sr = prefab.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            sr.sortingOrder = 1; // Default sorting for NPC
            float visualScale = AdventureGridUtility.GetVisualScaleForReferenceHeight(sr.sprite);
            prefab.transform.localScale = new Vector3(visualScale, visualScale, 1f);

            Animator anim = prefab.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            // Add Colliders
            BoxCollider2D box = prefab.AddComponent<BoxCollider2D>();
            box.offset = Vector2.zero;
            AdventureGridUtility.ConfigureFootCollider(box, prefab.transform, sr, AdventureGridUtility.GetCellSize(1f));

            Rigidbody2D rb = prefab.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Add scripts
            prefab.AddComponent<NpcTileAlignment>();
            prefab.AddComponent<JobChangerNpc>();
            prefab.AddComponent<NpcInteractable>();

            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/NPCs");
            
            string prefabPath = "Assets/Prefabs/NPCs/NPC_JobChanger.prefab";
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            Object.DestroyImmediate(prefab);

            Debug.Log($"[JobChangerSetup] Setup complete! Prefab saved to {prefabPath}");
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
            if (!AssetDatabase.IsValidFolder(path))
            {
                int lastSlash = path.LastIndexOf('/');
                string parent = path.Substring(0, lastSlash);
                string folder = path.Substring(lastSlash + 1);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static AnimationClip CreateClip(List<Sprite> sprites, string path)
        {
            AnimationClip clip = new AnimationClip();
            clip.frameRate = 8; // 8 fps is usually smooth enough for pixel idle

            ApplyClipFrames(clip, sprites);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void ApplyClipFrames(List<Sprite> sprites, string path)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                Debug.LogError($"[JobChangerSetup] AnimationClip not found: {path}");
                return;
            }

            ApplyClipFrames(clip, sprites);
            EditorUtility.SetDirty(clip);
        }

        private static void ApplyClipFrames(AnimationClip clip, List<Sprite> sprites)
        {
            if (clip == null || sprites == null || sprites.Count == 0)
            {
                return;
            }

            EditorCurveBinding spriteBinding = new EditorCurveBinding();
            spriteBinding.type = typeof(SpriteRenderer);
            spriteBinding.path = "";
            spriteBinding.propertyName = "m_Sprite";

            ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[sprites.Count + 1];
            for (int i = 0; i < sprites.Count; i++)
            {
                spriteKeyFrames[i] = new ObjectReferenceKeyframe();
                spriteKeyFrames[i].time = i / clip.frameRate;
                spriteKeyFrames[i].value = sprites[i];
            }
            
            // Add a loop frame at the very end to properly loop the animation
            spriteKeyFrames[sprites.Count] = new ObjectReferenceKeyframe();
            spriteKeyFrames[sprites.Count].time = sprites.Count / clip.frameRate;
            spriteKeyFrames[sprites.Count].value = sprites[sprites.Count - 1]; // Hold the last frame or loop back to first depending on need, but Unity expects a keyframe at the end of loop.

            AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyFrames);
        }
    }
}

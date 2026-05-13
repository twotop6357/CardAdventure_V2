using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CardAdventure.Editor
{
    /// <summary>
    /// MaleNPC_Sprites.png 에서 Walk / Idle 애니메이션 클립을 생성하고
    /// Assets/Animations/NPC/NPC_MaleChaser.controller 에 상태를 등록합니다.
    ///
    /// Menu: CardAdventure/NPC/Setup MaleNPC Animations
    ///
    /// 스프라이트 행 구성 (7프레임 / 행):
    ///   행 0 (sprites  0- 6): Down  → WalkFront / IdleDown
    ///   행 1 (sprites  7-13): Left  → WalkSide  / IdleSide  (flipX 로 Right 처리)
    ///   행 2 (sprites 14-20): Right → 미사용 (flipX 대체)
    ///   행 3 (sprites 21-27): Up    → WalkBack  / IdleBack
    /// </summary>
    public static class MaleNpcAnimationSetup
    {
        private const string SpritePath     = "Assets/Assets/Sprites/NPCs/MaleNPC_Sprites.png";
        private const string ControllerPath = "Assets/Animations/NPC/NPC_MaleChaser.controller";
        private const string ClipDir        = "Assets/Animations/NPC";
        private const float  FrameRate      = 6f;

        // (시작 인덱스, 프레임 수, 상태 이름, 루프 여부)
        private static readonly (int start, int count, string name, bool loop)[] Clips =
        {
            ( 0, 7, "MaleNPC_WalkFront", true),
            ( 7, 7, "MaleNPC_WalkSide",  true),
            (21, 7, "MaleNPC_WalkBack",  true),
            ( 1, 1, "MaleNPC_IdleDown",  false),
            ( 8, 1, "MaleNPC_IdleSide",  false),
            (22, 1, "MaleNPC_IdleBack",  false),
        };

        [MenuItem("CardAdventure/NPC/Setup MaleNPC Animations")]
        public static void SetupAll()
        {
            Sprite[] sprites = LoadOrderedSprites(SpritePath);
            if (sprites.Length == 0)
            {
                Debug.LogError($"[MaleNpcSetup] 스프라이트를 찾을 수 없습니다: {SpritePath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(ClipDir))
                AssetDatabase.CreateFolder("Assets/Animations", "NPC");

            var clipMap = new System.Collections.Generic.Dictionary<string, AnimationClip>();
            int created = 0, updated = 0;

            foreach (var (start, count, name, loop) in Clips)
            {
                if (start + count > sprites.Length)
                {
                    Debug.LogWarning($"[MaleNpcSetup] {name}: 프레임 부족 (필요 {start + count}, 보유 {sprites.Length})");
                    continue;
                }

                string path = $"{ClipDir}/{name}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                bool isNew = clip == null;
                if (isNew) clip = new AnimationClip { name = name };

                BuildClip(clip, sprites, start, count, loop);

                if (isNew) { AssetDatabase.CreateAsset(clip, path); created++; }
                else        { EditorUtility.SetDirty(clip); updated++; }

                clipMap[name] = clip;
            }

            // AnimatorController 생성 또는 갱신
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null)
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            var sm = ctrl.layers[0].stateMachine;
            foreach (var (_, _, name, _) in Clips)
            {
                if (clipMap.TryGetValue(name, out var clip))
                    AddOrUpdateState(sm, name, clip);
            }

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MaleNpcSetup] 완료 — 신규 {created}개, 갱신 {updated}개 / 컨트롤러: {ControllerPath}");
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
            var m = Regex.Match(name, @"(\d+)$");
            return m.Success ? int.Parse(m.Value) : 0;
        }

        private static void BuildClip(AnimationClip clip, Sprite[] sprites,
                                      int startIdx, int frameCount, bool loop)
        {
            clip.frameRate = FrameRate;
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys    = new ObjectReferenceKeyframe[frameCount];
            for (int i = 0; i < frameCount; i++)
                keys[i] = new ObjectReferenceKeyframe
                    { time = i / FrameRate, value = sprites[startIdx + i] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static void AddOrUpdateState(AnimatorStateMachine sm, string stateName, AnimationClip clip)
        {
            foreach (var child in sm.states)
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

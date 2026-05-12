using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CardAdventure.Editor
{
    /// <summary>
    /// 각 직업의 _Run 스프라이트 시트에서 Run 애니메이션 클립을 자동 생성하고
    /// AnimatorController에 Player_RunFront / Player_RunBack / Player_RunSide 상태를 추가합니다.
    ///
    /// Menu: CardAdventure/Player/Setup Run Animations
    ///
    /// 스프라이트 시트 행 구성 (RPGMK 표준, 위→아래, 행당 6프레임):
    ///   행 0 (sprites  0- 5): Down  → Player_RunFront
    ///   행 1 (sprites  6-11): Left  → Player_RunSide  (Walk과 동일한 왼쪽 기본 방향, flipX로 오른쪽 처리)
    ///   행 2 (sprites 12-17): Right → 사용 안 함       (flipX로 대체)
    ///   행 3 (sprites 18-23): Up    → Player_RunBack
    /// </summary>
    public static class PlayerRunAnimationSetup
    {
        // (직업명, AnimatorController 경로, 애니메이션 클립 저장 폴더)
        private static readonly (string job, string controllerPath, string clipDir)[] Jobs =
        {
            ("Warrior", "Assets/Animations/Player/Player_Warrior.controller",
                        "Assets/Animations/Player"),
            ("Magician", "Assets/Animations/Player/Player_Magician.controller",
                        "Assets/Animations/Player/Magician"),
            ("Rogue",   "Assets/Animations/Player/Player_Rogue.controller",
                        "Assets/Animations/Player/Rogue"),
        };

        // (시트에서 시작 프레임, 프레임 수, Animator 상태 이름)
        private static readonly (int start, int count, string stateName)[] RunRows =
        {
            ( 0, 6, "Player_RunFront"),  // 행 0: Down
            ( 6, 6, "Player_RunSide"),   // 행 1: Left (Walk과 동일 flip 로직 적용)
            (18, 6, "Player_RunBack"),   // 행 3: Up
        };

        private const float FrameRate = 8f; // Walk(6fps)보다 빠른 8fps

        [MenuItem("CardAdventure/Player/Setup Run Animations")]
        public static void SetupAll()
        {
            int created = 0, updated = 0;

            foreach (var (job, controllerPath, clipDir) in Jobs)
            {
                // 1. Run 스프라이트 시트 로드
                string spritePath = $"Assets/Assets/Sprites/Character/{job}/Walk/{job}_Run.png";
                Sprite[] sprites = LoadOrderedSprites(spritePath);
                if (sprites.Length == 0)
                {
                    Debug.LogWarning($"[RunSetup] {job}_Run.png 스프라이트를 찾지 못했습니다. 경로: {spritePath}");
                    continue;
                }

                // 2. AnimatorController 로드
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (controller == null)
                {
                    Debug.LogWarning($"[RunSetup] AnimatorController 없음: {controllerPath}");
                    continue;
                }

                var stateMachine = controller.layers[0].stateMachine;

                // 3. 행별 클립 생성 + 상태 등록
                foreach (var (start, count, stateName) in RunRows)
                {
                    if (start + count > sprites.Length)
                    {
                        Debug.LogWarning($"[RunSetup] {job}/{stateName}: 시트 프레임 부족 (필요 {start+count}, 보유 {sprites.Length})");
                        continue;
                    }

                    string clipPath = $"{clipDir}/{stateName}.anim";

                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    bool isNew = clip == null;
                    if (isNew)
                        clip = new AnimationClip { name = stateName };

                    BuildClip(clip, sprites, start, count);

                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(clip, clipPath);
                        created++;
                    }
                    else
                    {
                        EditorUtility.SetDirty(clip);
                        updated++;
                    }

                    AddOrUpdateState(stateMachine, stateName, clip);
                }

                EditorUtility.SetDirty(controller);
                Debug.Log($"[RunSetup] {job} 완료  (clips: {clipDir})");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RunSetup] 전체 완료 — 신규 {created}개, 갱신 {updated}개");
        }

        // ─── 헬퍼 ────────────────────────────────────────────────────────

        /// <summary>스프라이트 시트에서 서브 스프라이트를 번호 순으로 불러옵니다.</summary>
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

        /// <summary>AnimationClip에 스프라이트 프레임을 설정하고 루프를 활성화합니다.</summary>
        private static void BuildClip(AnimationClip clip, Sprite[] sprites, int startIdx, int frameCount)
        {
            clip.frameRate = FrameRate;

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frameCount];
            for (int i = 0; i < frameCount; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / FrameRate, value = sprites[startIdx + i] };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        /// <summary>StateMachine에 상태를 추가하거나 기존 상태의 모션을 교체합니다.</summary>
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
            var state = sm.AddState(stateName);
            state.motion = clip;
        }
    }
}

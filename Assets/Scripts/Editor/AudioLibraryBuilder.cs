using System.Collections.Generic;
using System.IO;
using CardAdventure.Audio;
using UnityEditor;
using UnityEngine;

namespace CardAdventure.EditorTools
{
    /// <summary>
    /// `Assets/Assets/Audios/` 폴더의 모든 AudioClip을 스캔해
    /// `Assets/Resources/AudioLibrary.asset` 을 생성/갱신한다.
    ///
    /// 분류 규칙
    /// - 파일명이 `_BGM` 으로 끝나면 BGM 카테고리
    /// - 파일명이 `_SFX` 으로 끝나면 SFX 카테고리
    /// - 두 접미사 모두 없으면 무시 (경고 로그)
    ///
    /// 씬→BGM 매핑은 기본 시드값을 채워준다. 이미 있는 매핑은 보존한다.
    /// </summary>
    public static class AudioLibraryBuilder
    {
        private const string SourceFolder = "Assets/Assets/Audios";
        private const string ResourcesFolder = "Assets/Resources";
        private const string AssetPath = "Assets/Resources/AudioLibrary.asset";

        // 시드 매핑 — 신규 자산 생성 시에만 채운다 (이미 매핑이 있는 씬은 건드리지 않음).
        // LobbyScene 은 의도적으로 매핑하지 않아 BGM 미사용 정책을 유지한다.
        private static readonly (string scene, string bgmKey)[] DefaultSeedMappings =
        {
            ("AdventureScene", "AdventureScene_BGM"),
            ("BattleScene",    "NormalEnemyBattleScene_BGM"),
            ("BattleTest",     "NormalEnemyBattleScene_BGM"),
        };

        [MenuItem("CardAdventure/Audio/Build AudioLibrary From Folder")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(SourceFolder))
            {
                EditorUtility.DisplayDialog("AudioLibraryBuilder",
                    $"오디오 폴더를 찾을 수 없습니다: {SourceFolder}", "확인");
                return;
            }

            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            AudioLibrary library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(AssetPath);
            bool isNew = library == null;
            if (isNew)
            {
                library = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(library, AssetPath);
            }

            List<AudioLibrary.ClipEntry> bgm = new List<AudioLibrary.ClipEntry>();
            List<AudioLibrary.ClipEntry> sfx = new List<AudioLibrary.ClipEntry>();

            string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { SourceFolder });
            for (int i = 0; i < clipGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(clipGuids[i]);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                string key = Path.GetFileNameWithoutExtension(path);
                AudioLibrary.ClipEntry entry = new AudioLibrary.ClipEntry { key = key, clip = clip };

                if (key.EndsWith("_BGM"))
                {
                    bgm.Add(entry);
                }
                else if (key.EndsWith("_SFX"))
                {
                    sfx.Add(entry);
                }
                else
                {
                    Debug.LogWarning($"[AudioLibraryBuilder] '{key}' 는 _BGM/_SFX 접미사가 없어 분류에서 제외됨: {path}");
                }
            }

            bgm.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
            sfx.Sort((a, b) => string.CompareOrdinal(a.key, b.key));

            // 기존 매핑은 보존하고, 시드값은 신규 자산일 때만 채운다.
            List<AudioLibrary.SceneBgmEntry> mappings = new List<AudioLibrary.SceneBgmEntry>(library.SceneBgmMappings);
            if (isNew && mappings.Count == 0)
            {
                for (int i = 0; i < DefaultSeedMappings.Length; i++)
                {
                    mappings.Add(new AudioLibrary.SceneBgmEntry
                    {
                        sceneName = DefaultSeedMappings[i].scene,
                        bgmKey    = DefaultSeedMappings[i].bgmKey,
                    });
                }
            }

            library.EditorOnly_SetEntries(bgm, sfx, mappings);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AudioLibraryBuilder] BGM {bgm.Count}개 / SFX {sfx.Count}개 등록 완료 → {AssetPath}");
            EditorGUIUtility.PingObject(library);
        }
    }
}

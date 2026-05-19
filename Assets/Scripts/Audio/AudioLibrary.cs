using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure.Audio
{
    /// <summary>
    /// 프로젝트 전역 오디오 라이브러리.
    /// - BGM/SFX 클립을 키 기반으로 검색
    /// - 씬 이름 → BGM 키 매핑 테이블 보유 (씬 로드 시 자동 전환)
    /// - 매핑에 없는 씬은 BGM을 정지(예: LobbyScene)
    ///
    /// 사용 규약
    /// - 파일명 접미사가 `_BGM`이면 BGM 카테고리, `_SFX`이면 SFX 카테고리로 분류한다.
    /// - 키는 확장자를 제외한 파일명을 그대로 사용한다. (예: `Interaction_SFX`)
    /// - 자산 위치는 `Assets/Resources/AudioLibrary.asset` 으로 고정한다 (Resources.Load 대상).
    /// </summary>
    [CreateAssetMenu(menuName = "CardAdventure/Audio/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        public const string ResourcePath = "AudioLibrary";

        [Serializable]
        public struct ClipEntry
        {
            [Tooltip("재생 시 사용할 키. 파일명(확장자 제외)을 그대로 사용한다. 예) Interaction_SFX")]
            public string key;
            public AudioClip clip;
        }

        [Serializable]
        public struct SceneBgmEntry
        {
            [Tooltip("Unity 씬 이름 (예: AdventureScene)")]
            public string sceneName;
            [Tooltip("위 씬에서 자동 재생할 BGM 키 (예: AdventureScene_BGM). 비워두면 BGM 정지로 처리됨.")]
            public string bgmKey;
        }

        [Header("BGM 클립")]
        [SerializeField] private List<ClipEntry> bgmClips = new List<ClipEntry>();

        [Header("SFX 클립")]
        [SerializeField] private List<ClipEntry> sfxClips = new List<ClipEntry>();

        [Header("씬 → BGM 매핑 (이 표에 없는 씬은 BGM 정지)")]
        [SerializeField] private List<SceneBgmEntry> sceneBgmMappings = new List<SceneBgmEntry>();

        [Header("기본 볼륨 (0~1)")]
        [Range(0f, 1f)][SerializeField] private float defaultBgmVolume = 0.7f;
        [Range(0f, 1f)][SerializeField] private float defaultSfxVolume = 1.0f;

        public float DefaultBgmVolume => defaultBgmVolume;
        public float DefaultSfxVolume => defaultSfxVolume;

        public IReadOnlyList<ClipEntry> BgmClips => bgmClips;
        public IReadOnlyList<ClipEntry> SfxClips => sfxClips;
        public IReadOnlyList<SceneBgmEntry> SceneBgmMappings => sceneBgmMappings;

        /// <summary>BGM 키로 클립 찾기. 없으면 null.</summary>
        public AudioClip GetBgm(string key)
        {
            return FindClip(bgmClips, key);
        }

        /// <summary>SFX 키로 클립 찾기. 없으면 null.</summary>
        public AudioClip GetSfx(string key)
        {
            return FindClip(sfxClips, key);
        }

        /// <summary>씬 이름에 매핑된 BGM 키. 매핑이 없으면 null(=BGM 정지).</summary>
        public string GetBgmKeyForScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;
            for (int i = 0; i < sceneBgmMappings.Count; i++)
            {
                if (string.Equals(sceneBgmMappings[i].sceneName, sceneName, StringComparison.Ordinal))
                {
                    return sceneBgmMappings[i].bgmKey;
                }
            }
            return null;
        }

        private static AudioClip FindClip(List<ClipEntry> list, string key)
        {
            if (list == null || string.IsNullOrEmpty(key)) return null;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].key, key, StringComparison.Ordinal))
                {
                    return list[i].clip;
                }
            }
            return null;
        }

#if UNITY_EDITOR
        /// <summary>에디터 도구가 라이브러리를 채울 때 사용 (런타임 미사용).</summary>
        public void EditorOnly_SetEntries(
            List<ClipEntry> newBgm,
            List<ClipEntry> newSfx,
            List<SceneBgmEntry> newSceneMappings)
        {
            if (newBgm != null) bgmClips = newBgm;
            if (newSfx != null) sfxClips = newSfx;
            if (newSceneMappings != null) sceneBgmMappings = newSceneMappings;
        }
#endif
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardAdventure.Audio
{
    /// <summary>
    /// CardAdventure 오디오 시스템 싱글턴.
    ///
    /// 책임
    /// - BGM 재생/정지 (씬 로드 시 `AudioLibrary.GetBgmKeyForScene` 결과로 자동 전환)
    /// - SFX 재생: 같은 SFX는 채널 안에서 재시작(덮어쓰기), 서로 다른 SFX는 채널 분리로 동시 재생
    /// - LobbyScene 처럼 매핑이 없는 씬은 BGM 자동 정지
    ///
    /// 부트스트랩
    /// - `Resources/AudioLibrary.asset` 을 자동 로드한다.
    /// - `RuntimeInitializeOnLoadMethod`로 첫 씬 로드 이후 1회 인스턴스화되며 `DontDestroyOnLoad`로 유지된다.
    /// - 외부 코드는 `AudioManager.Instance.PlaySfx("Interaction_SFX")` 형태로 호출한다.
    ///
    /// 확장
    /// - 새 BGM/SFX 추가는 `AudioLibrary.asset` 의 리스트만 갱신하면 코드 수정 없이 동작.
    /// - 씬별 BGM은 `AudioLibrary.SceneBgmMappings` 에 (씬 이름, BGM 키) 쌍으로 등록.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        /// <summary>전역 SFX 키 상수. 호출부 오타 방지용.</summary>
        public static class SfxKeys
        {
            public const string Interaction = "Interaction_SFX";
            public const string CardUse     = "CardUse_SFX";
            public const string Hit         = "Hit_SFX";
        }

        /// <summary>전역 BGM 키 상수.</summary>
        public static class BgmKeys
        {
            public const string Adventure          = "AdventureScene_BGM";
            public const string NormalEnemyBattle  = "NormalEnemyBattleScene_BGM";
            public const string BossBattle         = "BossBattleScene_BGM";
            public const string Win                = "Win_BGM";
            public const string Lose               = "Lose_BGM";
        }

        /// <summary>PlayerPrefs 영속화 키. 외부(설정 UI)에서 동일 키를 참조해도 안전하도록 노출.</summary>
        public const string PrefsBgmVolumeKey = "Audio.BgmVolume";
        public const string PrefsSfxVolumeKey = "Audio.SfxVolume";

        private AudioLibrary library;
        private AudioSource bgmSource;
        // 같은 클립을 같은 채널(=동일 AudioSource)에서 재시작하기 위해 클립별로 AudioSource를 풀링한다.
        // 서로 다른 SFX 키는 각자의 AudioSource를 갖게 되어 동시 재생이 가능하다.
        private readonly Dictionary<string, AudioSource> sfxSources = new Dictionary<string, AudioSource>();

        // 씬에 AudioListener가 하나도 없을 때 AudioManager 자체에 부착해 두는 폴백 리스너.
        // 씬의 카메라에 이미 AudioListener가 있으면 비활성화해 "두 개" 경고를 피한다.
        private AudioListener fallbackListener;

        private float bgmVolume = 0.7f;
        private float sfxVolume = 1.0f;
        private float masterVolume = 1.0f;

        private string currentBgmKey;

        /// <summary>현재 BGM 볼륨 (0~1). 설정 UI 초기화에 사용.</summary>
        public float BgmVolume => bgmVolume;
        /// <summary>현재 SFX 볼륨 (0~1). 설정 UI 초기화에 사용.</summary>
        public float SfxVolume => sfxVolume;
        /// <summary>현재 마스터 볼륨 (0~1).</summary>
        public float MasterVolume => masterVolume;

        // ── 부트스트랩 ─────────────────────────────────────────
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            GameObject go = new GameObject("AudioManager");
            Instance = go.AddComponent<AudioManager>();
            DontDestroyOnLoad(go);

            // 최초 부트스트랩 시점에 이미 로드된 씬에도 AudioListener 보장 + BGM 정책을 적용한다.
            Instance.EnsureAudioListener();
            Instance.ApplySceneBgm(SceneManager.GetActiveScene().name);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadLibrary();
            EnsureBgmSource();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        private void LoadLibrary()
        {
            if (library != null) return;
            library = Resources.Load<AudioLibrary>(AudioLibrary.ResourcePath);
            if (library == null)
            {
                Debug.LogWarning($"[AudioManager] Resources/{AudioLibrary.ResourcePath}.asset 을 찾지 못했습니다. " +
                                 "에디터 메뉴 `CardAdventure/Audio/Build AudioLibrary From Folder` 를 실행해 주세요.");
                LoadVolumesFromPrefs(0.7f, 1.0f);
                return;
            }
            LoadVolumesFromPrefs(library.DefaultBgmVolume, library.DefaultSfxVolume);
        }

        /// <summary>
        /// PlayerPrefs 에 저장된 볼륨이 있으면 그 값을, 없으면 라이브러리 기본값을 사용한다.
        /// 세션 간 사용자 설정이 자동으로 복원되도록 보장.
        /// </summary>
        private void LoadVolumesFromPrefs(float bgmDefault, float sfxDefault)
        {
            bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefsBgmVolumeKey, bgmDefault));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefsSfxVolumeKey, sfxDefault));
            if (bgmSource != null) bgmSource.volume = bgmVolume * masterVolume;
            foreach (var kv in sfxSources)
            {
                if (kv.Value != null) kv.Value.volume = sfxVolume * masterVolume;
            }
        }

        private void EnsureBgmSource()
        {
            if (bgmSource != null) return;
            GameObject bgmGo = new GameObject("BgmSource");
            bgmGo.transform.SetParent(transform, false);
            bgmSource = bgmGo.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;     // 2D
            bgmSource.priority = 64;
            bgmSource.volume = bgmVolume * masterVolume;
        }

        // ══════════════════════════════════════════════════════
        //  씬 로드 시 BGM 자동 전환
        // ══════════════════════════════════════════════════════
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureAudioListener();
            ApplySceneBgm(scene.name);
        }

        /// <summary>
        /// 씬에 AudioListener가 하나도 없으면 AudioManager 자신에게 폴백 리스너를 부착한다.
        /// 씬의 카메라 등 다른 곳에 이미 리스너가 있으면 폴백을 비활성화해 "여러 개" 경고를 방지한다.
        /// </summary>
        private void EnsureAudioListener()
        {
            // 비활성 오브젝트까지 포함해 실제로 활성/사용 가능한 AudioListener가 있는지 본다.
            AudioListener[] all = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool sceneHasOtherListener = false;
            for (int i = 0; i < all.Length; i++)
            {
                AudioListener l = all[i];
                if (l == null) continue;
                if (l == fallbackListener) continue;
                if (!l.isActiveAndEnabled) continue;
                sceneHasOtherListener = true;
                break;
            }

            if (sceneHasOtherListener)
            {
                // 씬이 자체 리스너를 제공하면 폴백은 끈다.
                if (fallbackListener != null) fallbackListener.enabled = false;
                return;
            }

            // 씬에 리스너가 없으면 AudioManager 자신에 부착(or 재활성화).
            if (fallbackListener == null)
            {
                fallbackListener = gameObject.GetComponent<AudioListener>();
                if (fallbackListener == null)
                {
                    fallbackListener = gameObject.AddComponent<AudioListener>();
                }
            }
            fallbackListener.enabled = true;
        }

        private void ApplySceneBgm(string sceneName)
        {
            if (library == null) LoadLibrary();
            if (library == null) return;

            string bgmKey = library.GetBgmKeyForScene(sceneName);
            if (string.IsNullOrEmpty(bgmKey))
            {
                // 매핑이 없는 씬(예: LobbyScene)은 BGM을 끈다.
                StopBgm();
                return;
            }
            PlayBgm(bgmKey);
        }

        // ══════════════════════════════════════════════════════
        //  BGM API
        // ══════════════════════════════════════════════════════
        public void PlayBgm(string key)
        {
            if (string.IsNullOrEmpty(key)) { StopBgm(); return; }
            if (library == null) LoadLibrary();
            if (library == null) return;

            // 같은 BGM이 이미 재생 중이면 그대로 둔다(씬 전환 시 매끄러운 연속 재생).
            if (string.Equals(currentBgmKey, key) && bgmSource != null && bgmSource.isPlaying)
            {
                return;
            }

            AudioClip clip = library.GetBgm(key);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM 키 '{key}' 에 해당하는 클립이 AudioLibrary에 없습니다.");
                return;
            }

            EnsureBgmSource();
            bgmSource.clip = clip;
            bgmSource.volume = bgmVolume * masterVolume;
            bgmSource.Play();
            currentBgmKey = key;
        }

        public void StopBgm()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }
            currentBgmKey = null;
        }

        // ══════════════════════════════════════════════════════
        //  SFX API
        // ══════════════════════════════════════════════════════
        /// <summary>
        /// SFX 재생.
        /// 같은 키의 SFX가 이미 재생 중이면 그 채널을 Stop 후 같은 위치(0초)에서 재시작한다.
        /// 다른 키의 SFX는 별도 채널에서 동시 재생된다.
        /// </summary>
        public void PlaySfx(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (library == null) LoadLibrary();
            if (library == null) return;

            AudioClip clip = library.GetSfx(key);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX 키 '{key}' 에 해당하는 클립이 AudioLibrary에 없습니다.");
                return;
            }

            AudioSource src = GetOrCreateSfxSource(key);
            // 덮어쓰기 규칙: 같은 클립이 진행 중이어도 즉시 처음부터 다시 재생.
            src.Stop();
            src.clip = clip;
            src.volume = sfxVolume * masterVolume;
            src.Play();
        }

        private AudioSource GetOrCreateSfxSource(string key)
        {
            if (sfxSources.TryGetValue(key, out AudioSource existing) && existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject($"SfxSource_{key}");
            go.transform.SetParent(transform, false);
            AudioSource src = go.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D
            src.priority = 128;
            sfxSources[key] = src;
            return src;
        }

        // ══════════════════════════════════════════════════════
        //  볼륨 API (설정 UI에서 호출)
        // ══════════════════════════════════════════════════════
        public void SetMasterVolume(float v)
        {
            masterVolume = Mathf.Clamp01(v);
            ApplyVolumes();
        }

        public void SetBgmVolume(float v)
        {
            bgmVolume = Mathf.Clamp01(v);
            if (bgmSource != null) bgmSource.volume = bgmVolume * masterVolume;
            PlayerPrefs.SetFloat(PrefsBgmVolumeKey, bgmVolume);
            // PlayerPrefs.Save() 는 빈번한 디스크 IO를 막기 위해 호출하지 않는다.
            // SettingsUIController.Hide() 가 닫힐 때 한 번 Save() 한다.
        }

        public void SetSfxVolume(float v)
        {
            sfxVolume = Mathf.Clamp01(v);
            // 다음 PlaySfx에서 반영됨. 이미 재생 중인 SFX 채널은 즉시 갱신.
            foreach (var kv in sfxSources)
            {
                if (kv.Value != null) kv.Value.volume = sfxVolume * masterVolume;
            }
            PlayerPrefs.SetFloat(PrefsSfxVolumeKey, sfxVolume);
        }

        private void ApplyVolumes()
        {
            if (bgmSource != null) bgmSource.volume = bgmVolume * masterVolume;
            foreach (var kv in sfxSources)
            {
                if (kv.Value != null) kv.Value.volume = sfxVolume * masterVolume;
            }
        }

        // ══════════════════════════════════════════════════════
        //  호출부에서 인스턴스가 비어있을 때 안전하게 쓰기 위한 헬퍼
        // ══════════════════════════════════════════════════════
        /// <summary>인스턴스가 없으면 무시. 호출부 코드 간결화용.</summary>
        public static void PlaySfxSafe(string key)
        {
            Instance?.PlaySfx(key);
        }

        public static void PlayBgmSafe(string key)
        {
            Instance?.PlayBgm(key);
        }

        public static void StopBgmSafe()
        {
            Instance?.StopBgm();
        }
    }
}

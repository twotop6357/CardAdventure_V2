using CardAdventure.Audio;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace CardAdventure.UI
{
    /// <summary>
    /// 로비/인게임 메뉴의 설정 패널 컨트롤러.
    /// BGM/SFX 두 슬라이더는 각각 독립적으로 동작하며, 값은 `AudioManager` 에 즉시 반영되고
    /// PlayerPrefs(`Audio.BgmVolume` / `Audio.SfxVolume`)로 영속화된다.
    /// </summary>
    public class SettingsUIController : MonoBehaviour
    {
        [Header("Audio Settings")]
        [Tooltip("선택 사항 — AudioMixer 를 추가로 쓰고 싶을 때만 할당. 현재 프로젝트는 AudioManager 기반이므로 비워둬도 정상 동작한다.")]
        public AudioMixer mainMixer;
        public Slider bgmSlider;
        public Slider sfxSlider;

        [Header("UI")]
        public GameObject settingsPanel;
        public Button closeButton;

        private System.Action onCloseCallback;

        private void Start()
        {
            InitializeSliders();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() =>
                {
                    Hide();
                    onCloseCallback?.Invoke();
                });
            }
        }

        /// <summary>
        /// AudioManager 의 현재 볼륨을 기준으로 슬라이더 초기값을 채운다.
        /// `SetValueWithoutNotify` 를 사용해 초기 할당이 onValueChanged 를 트리거하지 않도록 한다.
        /// </summary>
        private void InitializeSliders()
        {
            // AudioManager 가 부팅되었다면 그 값을 사용하고, 아직이면 PlayerPrefs 폴백.
            float bgmInitial = AudioManager.Instance != null
                ? AudioManager.Instance.BgmVolume
                : PlayerPrefs.GetFloat(AudioManager.PrefsBgmVolumeKey, 0.7f);
            float sfxInitial = AudioManager.Instance != null
                ? AudioManager.Instance.SfxVolume
                : PlayerPrefs.GetFloat(AudioManager.PrefsSfxVolumeKey, 1.0f);

            if (bgmSlider != null)
            {
                bgmSlider.minValue = 0f;
                bgmSlider.maxValue = 1f;
                bgmSlider.SetValueWithoutNotify(bgmInitial);
                // 기존 리스너 제거 후 새로 등록 (Setup 툴 재실행 등에서 중복 부착 방지)
                bgmSlider.onValueChanged.RemoveListener(SetBgmVolume);
                bgmSlider.onValueChanged.AddListener(SetBgmVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.minValue = 0f;
                sfxSlider.maxValue = 1f;
                sfxSlider.SetValueWithoutNotify(sfxInitial);
                sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);
            }
        }

        public void Show(System.Action onClose = null)
        {
            onCloseCallback = onClose;
            if (settingsPanel != null) settingsPanel.SetActive(true);

            // 패널을 다시 열 때마다 현재 AudioManager 볼륨에 슬라이더 위치를 동기화한다.
            // (다른 씬의 SettingsUIController 가 변경했을 가능성에 대비)
            SyncSlidersFromAudioManager();
        }

        public void Hide()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            PlayerPrefs.Save();
        }

        private void SyncSlidersFromAudioManager()
        {
            if (AudioManager.Instance == null) return;
            if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(AudioManager.Instance.BgmVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
        }

        private void SetBgmVolume(float value)
        {
            // AudioManager 에 우선 반영 (PlayerPrefs 까지 처리됨).
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBgmVolume(value);
            }
            else
            {
                // AudioManager 가 아직 없으면 최소한 PlayerPrefs 만이라도 기록.
                PlayerPrefs.SetFloat(AudioManager.PrefsBgmVolumeKey, Mathf.Clamp01(value));
            }

            // AudioMixer 를 추가로 쓰는 경우의 호환 경로 — 기본 프로젝트에서는 null 이라 건너뛴다.
            if (mainMixer != null)
            {
                float dB = value > 0.001f ? Mathf.Log10(value) * 20f : -80f;
                mainMixer.SetFloat("BGMVolume", dB);
            }
        }

        private void SetSfxVolume(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSfxVolume(value);
            }
            else
            {
                PlayerPrefs.SetFloat(AudioManager.PrefsSfxVolumeKey, Mathf.Clamp01(value));
            }

            if (mainMixer != null)
            {
                float dB = value > 0.001f ? Mathf.Log10(value) * 20f : -80f;
                mainMixer.SetFloat("SFXVolume", dB);
            }
        }
    }
}

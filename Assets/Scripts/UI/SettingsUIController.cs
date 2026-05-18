using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace CardAdventure.UI
{
    public class SettingsUIController : MonoBehaviour
    {
        [Header("Audio Settings")]
        public AudioMixer mainMixer;
        public Slider bgmSlider;
        public Slider sfxSlider;
        
        [Header("UI")]
        public GameObject settingsPanel;
        public Button closeButton;

        private System.Action onCloseCallback;

        private void Start()
        {
            if (bgmSlider != null)
            {
                bgmSlider.onValueChanged.AddListener(SetBgmVolume);
                // Load saved volume if any, otherwise default to 0.75
                bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 0.75f);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);
                sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => 
                {
                    Hide();
                    onCloseCallback?.Invoke();
                });
            }
        }

        public void Show(System.Action onClose = null)
        {
            onCloseCallback = onClose;
            settingsPanel.SetActive(true);
        }

        public void Hide()
        {
            settingsPanel.SetActive(false);
            PlayerPrefs.Save();
        }

        private void SetBgmVolume(float value)
        {
            if (mainMixer != null)
            {
                // Convert linear 0-1 to dB (-80 to 0)
                float dB = value > 0.001f ? Mathf.Log10(value) * 20f : -80f;
                mainMixer.SetFloat("BGMVolume", dB);
            }
            PlayerPrefs.SetFloat("BGMVolume", value);
        }

        private void SetSfxVolume(float value)
        {
            if (mainMixer != null)
            {
                float dB = value > 0.001f ? Mathf.Log10(value) * 20f : -80f;
                mainMixer.SetFloat("SFXVolume", dB);
            }
            PlayerPrefs.SetFloat("SFXVolume", value);
        }
    }
}

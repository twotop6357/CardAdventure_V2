using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CardAdventure.Audio
{
    /// <summary>
    /// 씬 내의 모든 UnityEngine.UI.Button 에 ButtonAudioHook 컴포넌트를 동적으로 부착하여
    /// Interaction_SFX 재생을 자동화한다.
    ///
    /// 동작 원칙
    /// - `GlobalButtonHighlighter` 와 동일한 부트스트랩 패턴 사용 (RuntimeInitializeOnLoadMethod + DontDestroyOnLoad).
    /// - 씬 로드 시 스캔 1회 + 0.4초 주기로 재스캔 (런타임에 동적으로 켜지는 팝업 버튼도 커버).
    /// - AddListener 대신 IPointerClickHandler 및 ISubmitHandler를 구현한 컴포넌트 방식을 활용하여,
    ///   기타 UI 스크립트에서 onClick.RemoveAllListeners()를 수행하더라도 클릭 사운드가 무력화되지 않도록 보장한다.
    /// </summary>
    public class ButtonClickAudioInjector : MonoBehaviour
    {
        private static ButtonClickAudioInjector instance;
        private float scanTimer;
        private const float ScanInterval = 0.4f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;

            GameObject go = new GameObject("ButtonClickAudioInjector");
            instance = go.AddComponent<ButtonClickAudioInjector>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HookAllButtons();
        }

        private void Update()
        {
            scanTimer += Time.unscaledDeltaTime;
            if (scanTimer >= ScanInterval)
            {
                scanTimer = 0f;
                HookAllButtons();
            }
        }

        private void HookAllButtons()
        {
            // 비활성 상태의 팝업 버튼까지 포함해서 스캔 (직업 변경 UI 등).
            Button[] all = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < all.Length; i++)
            {
                Button btn = all[i];
                if (btn == null) continue;

                // 프리팹 에셋(씬에 인스턴스화되지 않은 것)은 건너뛴다.
                if (string.IsNullOrEmpty(btn.gameObject.scene.name)) continue;

                if (btn.gameObject.GetComponent<ButtonAudioHook>() == null)
                {
                    btn.gameObject.AddComponent<ButtonAudioHook>();
                }
            }
        }
    }

    /// <summary>
    /// 버튼 컴포넌트의 클릭 및 선택 제출(Submit) 이벤트를 수신하여 Interaction_SFX 효과음을 재생하는 훅 컴포넌트.
    /// </summary>
    public class ButtonAudioHook : MonoBehaviour, IPointerDownHandler, ISubmitHandler
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PlayClickSound();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            PlayClickSound();
        }

        private void PlayClickSound()
        {
            if (button != null && button.interactable)
            {
                AudioManager.PlaySfxSafe(AudioManager.SfxKeys.Interaction);
            }
        }
    }
}

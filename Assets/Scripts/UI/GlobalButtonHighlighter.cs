using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace CardAdventure
{
    /// <summary>
    /// 씬 내의 모든 UI Button에 대해 ESC 메뉴 스타일의 마우스 오버 하이라이트 효과(ColorTint)를 자동으로 이식해주는 전역 헬퍼입니다.
    /// 동적으로 켜지는 팝업 및 직업 변경 UI 내의 버튼들도 감지하여 실시간으로 완벽히 보정합니다.
    /// </summary>
    public class GlobalButtonHighlighter : MonoBehaviour
    {
        private static GlobalButtonHighlighter instance;
        private readonly HashSet<Button> initializedButtons = new HashSet<Button>();
        private float scanTimer = 0f;
        private const float SCAN_INTERVAL = 0.4f; // 0.4초마다 동적으로 켜진 버튼들을 스캔

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnLoad()
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GlobalButtonHighlighter");
                instance = go.AddComponent<GlobalButtonHighlighter>();
                DontDestroyOnLoad(go);
            }
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
            initializedButtons.Clear();
            ApplyHighlightToAllButtons();
        }

        private void Update()
        {
            // 런타임에 동적으로 열리는 다이얼로그나 직업 변경 UI 등의 버튼을 처리하기 위해 주기적 탐색 실행
            scanTimer += Time.deltaTime;
            if (scanTimer >= SCAN_INTERVAL)
            {
                scanTimer = 0f;
                ApplyHighlightToAllButtons();
            }
        }

        /// <summary>
        /// 현재 활성화된 씬 내의 모든 버튼들을 탐색하여 ESC 메뉴 스타일의 마우스 오버 피드백을 주입합니다.
        /// </summary>
        public void ApplyHighlightToAllButtons()
        {
            // 비활성화된 오브젝트 내의 버튼도 모두 가져옴 (직업 변경 UI 등 대기중인 팝업 포함)
            Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();

            foreach (var btn in allButtons)
            {
                if (btn == null) continue;
                
                // 에디터 에셋(프리팹 소스 등)은 제외하고 실제 씬 내에 존재하는 버튼만 타겟으로 삼음
                if (btn.gameObject.scene.name == null) continue;

                if (initializedButtons.Contains(btn)) continue;

                // ESC 스타일의 ColorBlock 하이라이트 구성
                btn.transition = Selectable.Transition.ColorTint;
                ColorBlock cb = btn.colors;

                // 원래 버튼이 가지고 있던 알파 및 컬러 요소를 존중하면서 하이라이트 피드백 구현
                Color baseColor = cb.normalColor;
                
                // 만약 너무 어두운 검정 계열이거나 디폴트 회색 계열이면 명확한 그레이 하이라이트를 이식
                if (baseColor.r < 0.3f && baseColor.g < 0.3f && baseColor.b < 0.3f)
                {
                    cb.normalColor = new Color(0.15f, 0.15f, 0.15f, baseColor.a > 0.1f ? baseColor.a : 0.9f);
                    cb.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
                    cb.pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                    cb.selectedColor = cb.normalColor;
                }
                else
                {
                    // 일반적인 컬러풀/밝은 버튼의 경우, 마우스 오버 시 화사하게 밝아지거나 색상 대비가 나타나게 디자인
                    float hR = Mathf.Min(baseColor.r * 1.3f, 1f);
                    float hG = Mathf.Min(baseColor.g * 1.3f, 1f);
                    float hB = Mathf.Min(baseColor.b * 1.3f, 1f);
                    
                    // 만약 이미 흰색 계열(1,1,1)에 가까우면 마우스 오버 시 대비가 보이도록 살짝 어둡게 피드백을 주거나
                    // 픽셀 버튼의 느낌을 강조하기 위해 알파 및 밝기 조절
                    if (baseColor.r > 0.85f && baseColor.g > 0.85f && baseColor.b > 0.85f)
                    {
                        cb.highlightedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                        cb.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                    }
                    else
                    {
                        cb.highlightedColor = new Color(hR, hG, hB, 1f);
                        cb.pressedColor = new Color(baseColor.r * 0.7f, baseColor.g * 0.7f, baseColor.b * 0.7f, 1f);
                    }
                    cb.selectedColor = baseColor;
                }

                cb.fadeDuration = 0.1f;
                btn.colors = cb;

                // 버튼 내의 텍스트가 있을 경우 가독성을 확보하고 마우스 오버 피드백을 추가로 강화
                TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    // 필요 시 텍스트 컬러나 레이아웃도 무결하게 유지
                }

                initializedButtons.Add(btn);
            }
        }
    }
}

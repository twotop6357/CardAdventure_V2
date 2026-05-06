using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// DOTween 페이드 씬 전환 유틸리티.
    /// DontDestroyOnLoad 싱글턴. 페이드 마스크(Canvas/Image)를 씬 위에 올려
    /// 페이드 아웃 → 씬 로드 → 페이드 인 순서로 부드럽게 전환한다.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        // ── 싱글턴 ────────────────────────────────────────────────
        public static SceneLoader Instance { get; private set; }

        [Header("페이드 설정")]
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private Color fadeColor    = Color.black;

        // ── 내부 ───────────────────────────────────────────────
        private CanvasGroup  canvasGroup;
        private bool         isLoading;

        // ── 라이프사이클 ───────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildFadeCanvas();
        }

        // ── 페이드 캔버스 생성 ──────────────────────────────────

        private void BuildFadeCanvas()
        {
            // Canvas
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // 최상단

            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            // 페이드 Image
            GameObject imgGo = new GameObject("FadeImage");
            imgGo.transform.SetParent(transform, false);

            Image img = imgGo.AddComponent<Image>();
            img.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            img.raycastTarget = false;

            RectTransform rt = imgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // CanvasGroup
            canvasGroup = imgGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha          = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable   = false;
        }

        // ── 공개 API ───────────────────────────────────────────

        /// <summary>씬 이름으로 페이드 전환한다.</summary>
        public void LoadScene(string sceneName)
        {
            if (isLoading) return;
            isLoading = true;

            DOTween.KillAll();

            // 페이드 아웃
            canvasGroup.blocksRaycasts = true;
            canvasGroup.DOFade(1f, fadeDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    SceneManager.LoadSceneAsync(sceneName).completed += _ =>
                    {
                        // 페이드 인
                        canvasGroup.DOFade(0f, fadeDuration)
                            .SetEase(Ease.OutQuad)
                            .SetUpdate(true)
                            .OnComplete(() =>
                            {
                                canvasGroup.blocksRaycasts = false;
                                isLoading = false;
                            });
                    };
                });
        }

        /// <summary>
        /// 어드벤처 씬에서 전투 씬으로 진입.
        /// GameDataManager에 적 데이터를 세팅하고 BattleScene을 로드한다.
        /// </summary>
        // 배틀 씬 이름 — 현재 BattleTest.unity 사용. 정식 BattleScene 완성 후 변경.
        private const string BATTLE_SCENE_NAME = "BattleTest";

        public void EnterBattle(EnemyData enemy, string returnScene = "AdventureScene")
        {
            if (GameDataManager.Instance != null)
                GameDataManager.Instance.PrepareBattle(enemy, returnScene);

            LoadScene(BATTLE_SCENE_NAME);
        }

        /// <summary>
        /// 전투 결과를 반영하고 어드벤처 씬으로 복귀.
        /// </summary>
        public void ReturnFromBattle(int remainingHp, CardData rewardCard = null)
        {
            if (GameDataManager.Instance != null)
                GameDataManager.Instance.ApplyBattleResult(remainingHp, rewardCard);

            string returnScene = GameDataManager.Instance?.ReturnSceneName ?? "AdventureScene";
            LoadScene(returnScene);
        }
    }
}

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
        [SerializeField] private float irisCloseDuration = 0.65f;
        [SerializeField] private Color fadeColor    = Color.black;

        // ── 내부 ───────────────────────────────────────────────
        private CanvasGroup  canvasGroup;
        private CanvasGroup  irisCanvasGroup;
        private IrisTransitionGraphic irisGraphic;
        private bool         isLoading;

        public bool IsLoading => isLoading;

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

            GameObject irisGo = new GameObject("IrisTransition");
            irisGo.transform.SetParent(transform, false);

            irisGo.AddComponent<CanvasRenderer>();
            irisGraphic = irisGo.AddComponent<IrisTransitionGraphic>();
            irisGraphic.color = fadeColor;
            irisGraphic.raycastTarget = true;
            irisGraphic.HoleRadiusNormalized = 1.15f;

            RectTransform irisRt = irisGo.GetComponent<RectTransform>();
            irisRt.anchorMin = Vector2.zero;
            irisRt.anchorMax = Vector2.one;
            irisRt.offsetMin = Vector2.zero;
            irisRt.offsetMax = Vector2.zero;

            irisCanvasGroup = irisGo.AddComponent<CanvasGroup>();
            irisCanvasGroup.alpha = 0f;
            irisCanvasGroup.blocksRaycasts = false;
            irisCanvasGroup.interactable = false;
        }

        // ── 공개 API ───────────────────────────────────────────

        /// <summary>씬 이름으로 페이드 전환한다.</summary>
        public void LoadScene(string sceneName)
        {
            LoadScene(sceneName, false);
        }

        private void LoadScene(string sceneName, bool useIrisTransition)
        {
            if (isLoading) return;
            isLoading = true;

            DOTween.KillAll();

            if (useIrisTransition && irisGraphic != null && irisCanvasGroup != null)
            {
                PlayIrisTransition(sceneName);
                return;
            }

            canvasGroup.blocksRaycasts = true;
            canvasGroup.DOFade(1f, fadeDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    SceneManager.LoadSceneAsync(sceneName).completed += _ =>
                    {
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

        private void PlayIrisTransition(string sceneName)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;

            irisGraphic.HoleRadiusNormalized = 1.15f;
            irisCanvasGroup.alpha = 1f;
            irisCanvasGroup.blocksRaycasts = true;

            DOTween.To(
                    () => irisGraphic.HoleRadiusNormalized,
                    value => irisGraphic.HoleRadiusNormalized = value,
                    0f,
                    irisCloseDuration)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    SceneManager.LoadSceneAsync(sceneName).completed += _ =>
                    {
                        irisCanvasGroup.DOFade(0f, fadeDuration)
                            .SetEase(Ease.OutQuad)
                            .SetUpdate(true)
                            .OnComplete(() =>
                            {
                                irisGraphic.HoleRadiusNormalized = 1.15f;
                                irisCanvasGroup.blocksRaycasts = false;
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
            if (isLoading)
            {
                StartCoroutine(EnterBattleWhenReady(enemy, null, returnScene));
                return;
            }

            LockAdventurePlayerInput();

            if (GameDataManager.Instance != null)
                GameDataManager.Instance.PrepareBattle(enemy, returnScene);

            LoadScene(BATTLE_SCENE_NAME, true);
        }

        public void EnterBattle(EnemyData enemy, BattleIntroData introData, string returnScene = "AdventureScene")
        {
            if (isLoading)
            {
                StartCoroutine(EnterBattleWhenReady(enemy, introData, returnScene));
                return;
            }

            LockAdventurePlayerInput();

            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.PrepareBattle(enemy, returnScene);
                GameDataManager.Instance.PendingIntroData = introData;
            }

            LoadScene(BATTLE_SCENE_NAME, true);
        }

        private System.Collections.IEnumerator EnterBattleWhenReady(EnemyData enemy, BattleIntroData introData, string returnScene)
        {
            while (isLoading)
            {
                yield return null;
            }

            if (introData != null)
            {
                EnterBattle(enemy, introData, returnScene);
            }
            else
            {
                EnterBattle(enemy, returnScene);
            }
        }


        public void EnterBossBattle(EnemyData enemy, string returnScene = "AdventureScene")
        {
            if (isLoading) return;

            LockAdventurePlayerInput();

            if (GameDataManager.Instance != null)
                GameDataManager.Instance.PrepareBattle(enemy, returnScene);

            LoadScene(BATTLE_SCENE_NAME, true);
        }

        private void PlayBossBattleTransition(string sceneName)
        {
            isLoading = true;
            DOTween.KillAll();

            canvasGroup.blocksRaycasts = true;
            Transform overlay = canvasGroup.transform;
            overlay.localScale = new Vector3(0.96f, 0.96f, 1f);

            DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, 0.22f).SetEase(Ease.InQuad).SetUpdate(true))
                .Join(overlay.DOScale(Vector3.one, 0.22f).SetEase(Ease.OutBack).SetUpdate(true))
                .AppendInterval(0.08f)
                .OnComplete(() =>
                {
                    SceneManager.LoadSceneAsync(sceneName).completed += _ =>
                    {
                        overlay.localScale = Vector3.one;
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

        private static void LockAdventurePlayerInput()
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.SetInputEnabled(false);
            }
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

using UnityEngine;
using System.Collections;

namespace CardAdventure
{
    /// <summary>
    /// 게임의 해상도 종횡비를 16:9로 강제 고정하는 클래스입니다.
    /// 16:9가 아닌 해상도 환경에서는 자동으로 검은색 레터박스(위아래) 또는 필러박스(좌우)를 생성합니다.
    /// [RuntimeInitializeOnLoadMethod]를 통해 게임 시작 시 자동 구동되므로 별도로 씬에 배치할 필요가 없습니다.
    /// </summary>
    public class AspectRatioEnforcer : MonoBehaviour
    {
        private const float TargetAspect = 16f / 9f; // 1.777777...
        private static AspectRatioEnforcer _instance;

        private Camera _backgroundCamera;
        private int _lastWidth;
        private int _lastHeight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("AspectRatioEnforcer");
                _instance = go.AddComponent<AspectRatioEnforcer>();
                DontDestroyOnLoad(go);
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // 창 모드인 경우 16:9 기준 해상도로 우선 설정 (예: 1280x720)
            if (!Screen.fullScreen)
            {
                int targetHeight = Mathf.Min(Screen.currentResolution.height - 100, 720);
                int targetWidth = Mathf.RoundToInt(targetHeight * TargetAspect);
                Screen.SetResolution(targetWidth, targetHeight, false);
            }

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            CreateBackgroundCamera();
            ApplyAspectRatio();
        }

        private void Start()
        {
            // 다른 카메라가 스타트 시점에 생성되는 경우가 있으므로 한 번 더 보정
            ApplyAspectRatio();
        }

        private void Update()
        {
            // 화면 해상도가 실시간으로 변경되었는지 체크
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            {
                _lastWidth = Screen.width;
                _lastHeight = Screen.height;
                ApplyAspectRatio();
            }
        }

        /// <summary>
        /// 레터박스/필러박스 영역을 투명하게 두지 않고 깔끔한 검은색으로 지워줄 전용 카메라를 생성합니다.
        /// </summary>
        private void CreateBackgroundCamera()
        {
            if (_backgroundCamera != null) return;

            GameObject camGo = new GameObject("BackgroundBlackCamera");
            camGo.transform.SetParent(transform);
            
            _backgroundCamera = camGo.AddComponent<Camera>();
            _backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            _backgroundCamera.backgroundColor = Color.black;
            _backgroundCamera.cullingMask = 0; // 아무것도 그리지 않음
            _backgroundCamera.depth = -100; // 모든 카메라보다 먼저 그려짐
            _backgroundCamera.useOcclusionCulling = false;
            _backgroundCamera.allowMSAA = false;
            _backgroundCamera.allowHDR = false;
        }

        /// <summary>
        /// 씬 내 모든 활성 카메라의 Viewport Rect를 16:9 종횡비에 맞춰 강제 조정합니다.
        /// </summary>
        public void ApplyAspectRatio()
        {
            float windowAspect = (float)Screen.width / (float)Screen.height;
            float scaleHeight = windowAspect / TargetAspect;

            Rect targetRect;

            if (scaleHeight < 1.0f)
            {
                // 레터박스 (위아래 검은 띠가 필요한 세로가 상대적으로 긴 해상도, 예: 16:10, 4:3)
                targetRect = new Rect(0, (1.0f - scaleHeight) / 2.0f, 1.0f, scaleHeight);
            }
            else
            {
                // 필러박스 (좌우 검은 띠가 필요한 가로가 상대적으로 긴 해상도, 예: 21:9)
                float scaleWidth = 1.0f / scaleHeight;
                targetRect = new Rect((1.0f - scaleWidth) / 2.0f, 0, scaleWidth, 1.0f);
            }

            // 씬 내의 모든 활성 카메라를 찾아서 뷰포트를 설정합니다. (단, 백그라운드 카메라는 제외)
            Camera[] cameras = Camera.allCameras;
            foreach (Camera cam in cameras)
            {
                if (cam == _backgroundCamera) continue;

                // URP Overlay 카메라는 스택 구조로 동작하므로 기본 Viewport 변경 대상에서 제외될 수 있지만,
                // 일반적인 카메라는 모두 Viewport Rect를 일체형으로 고정합니다.
                cam.rect = targetRect;
            }
        }

        private void OnEnable()
        {
            // 씬이 변경되거나 카메라 구조가 바뀔 때 자동 감지하여 뷰포트 갱신을 보장하기 위해 이벤트 핸들러 등록
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene loadedScene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // 새로운 씬이 로드되면 잠시 후 카메라들을 모두 다시 조율합니다.
            StartCoroutine(ApplyDelayed());
        }

        private IEnumerator ApplyDelayed()
        {
            yield return null; // 1프레임 지연 대기
            CreateBackgroundCamera();
            ApplyAspectRatio();
        }
    }
}

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardAdventure
{
    public static class AdventurePlayModeVerifier
    {
        private const string AdventureScenePath = "Assets/Scenes/AdventureScene.unity";
        private const string BattleSceneName = "BattleTest";
        private const double TimeoutSeconds = 6.0;

        private static double startedAt;
        private static bool triggerSent;

        [MenuItem("CardAdventure/Verify Adventure Battle Entrance")]
        public static void Verify()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[AdventurePlayModeVerifier] PlayMode가 이미 실행 중입니다.");
                return;
            }

            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene(AdventureScenePath);

            triggerSent = false;
            startedAt = EditorApplication.timeSinceStartup;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            startedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
            {
                Fail("검증 시간이 초과되었습니다.");
                return;
            }

            if (SceneManager.GetActiveScene().name == BattleSceneName)
            {
                VerifyBattleScene();
                return;
            }

            if (triggerSent)
            {
                return;
            }

            GameObject player = GameObject.FindWithTag("Player");
            BattleEntrance entrance = Object.FindFirstObjectByType<BattleEntrance>();
            if (player == null || entrance == null)
            {
                return;
            }

            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider == null)
            {
                Fail("Player Collider2D를 찾을 수 없습니다.");
                return;
            }

            player.transform.position = entrance.transform.position;
            entrance.SendMessage("OnTriggerEnter2D", playerCollider);
            triggerSent = true;
        }

        private static void VerifyBattleScene()
        {
            BattleManager battleManager = Object.FindFirstObjectByType<BattleManager>();
            if (battleManager == null || battleManager.Player == null || battleManager.Enemy == null)
            {
                return;
            }

            if (GameDataManager.Instance == null)
            {
                Fail("GameDataManager가 유지되지 않았습니다.");
                return;
            }

            if (GameDataManager.Instance.PendingEnemy == null)
            {
                Fail("PendingEnemy가 설정되지 않았습니다.");
                return;
            }

            if (GameDataManager.Instance.ReadOnlyDeck.Count == 0)
            {
                Fail("GameDataManager 덱이 비어 있습니다.");
                return;
            }

            if (battleManager.Player.CardPiles.Hand.Count == 0)
            {
                Fail("BattleManager 손패가 비어 있습니다.");
                return;
            }

            if (battleManager.Enemy.Data != GameDataManager.Instance.PendingEnemy)
            {
                Fail("BattleManager 적 데이터가 PendingEnemy와 다릅니다.");
                return;
            }

            Debug.Log("[AdventurePlayModeVerifier] PASS: BattleEntrance -> BattleTest 전환 및 전투 데이터 연결 확인.");
            Cleanup();
        }

        private static void Fail(string message)
        {
            Debug.LogError("[AdventurePlayModeVerifier] FAIL: " + message);
            Cleanup();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }
    }
}

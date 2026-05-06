using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// BattleScene 진입 시 GameDataManager → BattleManager 설정을 연결하고,
    /// 전투 종료 시 SceneLoader를 통해 어드벤처 씬으로 복귀한다.
    ///
    /// BattleScene의 BattleManager와 같은 오브젝트(또는 자식)에 추가한다.
    /// </summary>
    [RequireComponent(typeof(BattleManager))]
    public class BattleSceneConnector : MonoBehaviour
    {
        [Header("독립 실행 설정 (BattleScene 직접 시작 시)")]
        [Tooltip("GameDataManager가 없을 때 사용할 기본 적 데이터")]
        [SerializeField] private EnemyData fallbackEnemy;

        private BattleManager battleManager;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            ConfigureBattle();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Start()
        {
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        // ── 배틀 설정 ──────────────────────────────────────────

        private void ConfigureBattle()
        {
            GameDataManager gd = GameDataManager.Instance;

            if (gd == null)
            {
                // 독립 실행: BattleManager 인스펙터 설정 그대로 사용
                Debug.Log("[BattleSceneConnector] GameDataManager 없음 — 인스펙터 설정 사용.");
                return;
            }

            EnemyData enemy = gd.PendingEnemy ?? fallbackEnemy;
            if (enemy == null)
            {
                Debug.LogWarning("[BattleSceneConnector] 적 데이터 없음 — 인스펙터 설정 사용.");
                return;
            }

            battleManager.Configure(
                newPlayerName  : gd.PlayerName,
                newPlayerMaxHp : gd.MaxHp,
                newDeck        : gd.ReadOnlyDeck,
                newEnemyData   : enemy
            );
        }

        // ── 이벤트 구독 ────────────────────────────────────────

        private void SubscribeEvents()
        {
            if (battleManager != null)
                battleManager.BattleEnded += OnBattleEnded;
        }

        private void UnsubscribeEvents()
        {
            if (battleManager != null)
                battleManager.BattleEnded -= OnBattleEnded;
        }

        // ── 전투 종료 처리 ─────────────────────────────────────

        private void OnBattleEnded(BattleManager manager, BattlePhase phase)
        {
            // GameDataManager 없으면 복귀하지 않음 (독립 실행)
            if (GameDataManager.Instance == null) return;
            if (SceneLoader.Instance == null) return;

            if (phase == BattlePhase.Won)
            {
                int remainingHp = manager.Player?.Combatant?.CurrentHp ?? 0;
                // 보상 카드는 추후 보상 UI에서 처리 — 여기선 null
                Invoke(nameof(ReturnToAdventure), 1.5f); // 결과 패널 잠시 표시
                _remainingHp = remainingHp;
            }
            else if (phase == BattlePhase.Lost)
            {
                // 패배 시 게임 오버 처리 (현재는 어드벤처 씬으로 복귀)
                Invoke(nameof(ReturnToAdventure), 1.5f);
                _remainingHp = 0;
            }
        }

        private int _remainingHp;

        private void ReturnToAdventure()
        {
            SceneLoader.Instance.ReturnFromBattle(_remainingHp);
        }
    }
}

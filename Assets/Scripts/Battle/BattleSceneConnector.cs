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

        [Header("전투 보상 UI")]
        [Tooltip("승리 후 카드 보상 선택 UI. 설정 시 승리 결과 패널 대신 이 UI가 표시됩니다.")]
        [SerializeField] private BattleRewardUIController rewardUI;

        private BattleManager battleManager;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            ConfigureBattle();
            ConfigureIntroDirector();
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

        // ── 배틀 인트로 연출 설정 ─────────────────────────────

        /// <summary>
        /// GameDataManager.PendingEnemy.introData를 BattleIntroDirector에 주입한다.
        /// 어드벤처 씬 → 배틀 씬 전환 시 적별 전용 인트로 데이터가 자동 적용된다.
        /// </summary>
        private void ConfigureIntroDirector()
        {
            BattleIntroDirector director =
                FindFirstObjectByType<BattleIntroDirector>();

            if (director == null) return;

            GameDataManager gd = GameDataManager.Instance;
            if (gd != null)
            {
                if (gd.PendingIntroData != null)
                {
                    director.SetIntroData(gd.PendingIntroData);
                    return;
                }

                EnemyData enemy = gd.PendingEnemy;
                if (enemy?.introData != null)
                {
                    director.SetIntroData(enemy.introData);
                }
            }
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
            if (GameDataManager.Instance == null) return;
            if (phase == BattlePhase.Won)
            {
                if (GameDataManager.Instance.ExaminerBattlePending)
                {
                    GameDataManager.Instance.MarkExaminerBattleCompleted();
                }

                int remainingHp = manager.Player?.Combatant?.CurrentHp ?? 0;
                _remainingHp = remainingHp;

                if (rewardUI != null)
                {
                    rewardUI.Show(manager);
                }
                else
                {
                    CardAdventure.Audio.AudioManager.PlayBgmSafe(CardAdventure.Audio.AudioManager.BgmKeys.Win);
                    Invoke(nameof(ReturnToAdventure), 1.5f);
                }
            }
            else if (phase == BattlePhase.Lost)
            {
                _remainingHp = 0;

                if (rewardUI != null)
                {
                    rewardUI.ShowDefeat(manager);
                }
                else
                {
                    CardAdventure.Audio.AudioManager.PlayBgmSafe(CardAdventure.Audio.AudioManager.BgmKeys.Lose);
                    Invoke(nameof(ReturnToAdventure), 1.5f);
                }
            }
        }

        private int _remainingHp;

        private void ReturnToAdventure()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.ReturnFromBattle(_remainingHp);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 씬 간 플레이어 데이터(덱, 골드, 챕터 진행도)를 유지하는 싱글턴.
    /// DontDestroyOnLoad로 게임 전체 생존.
    /// </summary>
    public class GameDataManager : MonoBehaviour
    {
        // ── 싱글턴 ────────────────────────────────────────────────
        public static GameDataManager Instance { get; private set; }

        // ── 플레이어 기본 설정 ─────────────────────────────────
        [Header("플레이어 설정")]
        [SerializeField] private string playerName    = "플레이어";
        [SerializeField] private int    playerMaxHp   = 50;
        [SerializeField] private JobClassInfo defaultJobInfo;
        [SerializeField] private List<CardData> starterDeck = new List<CardData>();

        // ── 런타임 데이터 ──────────────────────────────────────
        /// <summary>현재 플레이어 HP (배틀 간 유지).</summary>
        public int CurrentHp   { get; set; }
        public int MaxHp       { get; private set; }

        /// <summary>보유 골드.</summary>
        public int Gold { get; set; }

        /// <summary>현재 덱 (배틀 보상으로 카드 추가됨).</summary>
        public List<CardData> Deck { get; private set; } = new List<CardData>();

        /// <summary>챕터 진행도 (0 = 미시작, 1 = 챕터1 진행 중 …).</summary>
        public int ChapterProgress { get; set; }

        /// <summary>마지막으로 진입한 전투의 적 데이터.</summary>
        public EnemyData PendingEnemy { get; set; }

        /// <summary>배틀에서 돌아올 어드벤처 씬 이름.</summary>
        public string ReturnSceneName { get; set; } = "AdventureScene";

        private readonly HashSet<string> completedChaserNpcIds = new HashSet<string>();

        public string PendingChaserNpcId { get; private set; }

        /// <summary>현재 선택된 직업 정보. null이면 기본값(전사)으로 간주.</summary>
        public JobClassInfo SelectedJobInfo { get; set; }

        /// <summary>배틀 진입 전 어드벤처 씬에서의 플레이어 위치.</summary>
        public Vector2 SavedPosition { get; set; }
        /// <summary>배틀 진입 전 플레이어가 바라보던 방향.</summary>
        public Vector2 SavedFacingDirection { get; set; } = Vector2.down;
        /// <summary>위치 정보가 저장되어 있는지 여부.</summary>
        public bool HasSavedPosition { get; set; }

        /// <summary>현재 선택된 직업의 CardClass 값. SelectedJobInfo가 null이면 Warrior 반환.</summary>
        public CardClass SelectedJobClass => SelectedJobInfo != null ? SelectedJobInfo.cardClass : CardClass.Warrior;

        public string PlayerName => playerName;

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

            InitDefaults();
        }

        // ── 초기화 ─────────────────────────────────────────────

        private void InitDefaults()
        {
            MaxHp     = playerMaxHp;
            CurrentHp = playerMaxHp;
            Gold      = 0;
            ChapterProgress = 0;
            HasSavedPosition = false;
            PendingChaserNpcId = null;
            completedChaserNpcIds.Clear();

            if (defaultJobInfo != null)
            {
                SelectedJobInfo = defaultJobInfo;
            }

            Deck.Clear();
            // starterDeck이 설정되어 있으면 그것을 쓰고, 아니면 직업 기본 덱을 씀
            if (starterDeck.Count > 0)
            {
                foreach (CardData card in starterDeck)
                {
                    if (card != null) Deck.Add(card);
                }
            }
            else if (SelectedJobInfo != null && SelectedJobInfo.starterCards != null)
            {
                foreach (CardData card in SelectedJobInfo.starterCards)
                {
                    if (card != null) Deck.Add(card);
                }
            }
        }

        // ── 공개 API ───────────────────────────────────────────

        /// <summary>
        /// BattleScene 진입 직전 호출.
        /// BattleManager.Configure()에 넘길 데이터를 세팅한다.
        /// </summary>
        public void PrepareBattle(EnemyData enemy, string returnScene)
        {
            PendingEnemy    = enemy;
            ReturnSceneName = returnScene;

            // 플레이어 현재 상태 저장
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                SavedPosition = player.transform.position;
                SavedFacingDirection = player.FacingDirection;
                HasSavedPosition = true;
                Debug.Log($"[GameDataManager] 플레이어 위치 저장: {SavedPosition}");
            }
        }

        public void SetPendingChaserNpc(string npcId)
        {
            PendingChaserNpcId = npcId;
        }

        public bool IsChaserNpcBattleCompleted(string npcId)
        {
            return !string.IsNullOrEmpty(npcId) && completedChaserNpcIds.Contains(npcId);
        }

        /// <summary>
        /// 배틀 결과 반영 (승리 시 카드 보상, HP 동기화 등).
        /// BattleScene 종료 시 호출한다.
        /// </summary>
        public void ApplyBattleResult(int remainingHp, CardData rewardCard = null)
        {
            CurrentHp = Mathf.Clamp(remainingHp, 0, MaxHp);

            if (rewardCard != null && !Deck.Contains(rewardCard))
                Deck.Add(rewardCard);

            if (!string.IsNullOrEmpty(PendingChaserNpcId))
            {
                completedChaserNpcIds.Add(PendingChaserNpcId);
                PendingChaserNpcId = null;
            }
        }

        /// <summary>
        /// 카드를 덱에 추가한다 (상점/보물상자 등).
        /// </summary>
        public void AddCardToDeck(CardData card)
        {
            if (card != null) Deck.Add(card);
        }

        /// <summary>
        /// 골드 변경. 음수 허용 안 함.
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        public void EarnGold(int amount) => Gold += Mathf.Max(0, amount);

        /// <summary>
        /// 데이터를 초기 상태로 리셋 (뉴 게임).
        /// </summary>
        public void ResetForNewGame()
        {
            InitDefaults();
        }

        /// <summary>
        /// 직업을 변경하고 관련 데이터(HP, 덱)를 갱신한다.
        /// </summary>
        public void UpdateJob(JobClassInfo newJob)
        {
            if (newJob == null) return;

            SelectedJobInfo = newJob;

            // HP 갱신 (비율 유지)
            int oldMax = MaxHp;
            MaxHp = newJob.baseMaxHp;
            if (oldMax > 0)
            {
                float ratio = (float)CurrentHp / oldMax;
                CurrentHp = Mathf.RoundToInt(MaxHp * ratio);
            }
            else
            {
                CurrentHp = MaxHp;
            }

            // 덱 교체
            Deck.Clear();
            if (newJob.starterCards != null)
            {
                foreach (CardData card in newJob.starterCards)
                {
                    if (card != null) Deck.Add(card);
                }
            }

            Debug.Log($"[GameDataManager] 직업 변경 완료: {newJob.displayName}, 덱 크기: {Deck.Count}");
        }

        /// <summary>
        /// 현재 덱의 읽기 전용 뷰.
        /// </summary>
        public IReadOnlyList<CardData> ReadOnlyDeck => Deck;
    }
}

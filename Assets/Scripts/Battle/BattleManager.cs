using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// Coordinates the single-enemy card battle loop for Phase 1.
    /// </summary>
    public sealed class BattleManager : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private string playerName = "Player";
        [SerializeField] private int playerMaxHp = 50;
        [SerializeField] private List<CardData> startingDeck = new List<CardData>();

        [Header("기본 직업 (GameDataManager 없을 때 폴백 / 배틀 테스트용)")]
        [Tooltip("어드벤처 씬에서 배틀로 넘어올 때 JobClassInfo가 없으면 이 값을 사용한다. Job_Warrior 할당 권장.")]
        [SerializeField] private JobClassInfo defaultJob;

        [Header("Enemy")]
        [SerializeField] private EnemyData enemyData;

        [Header("Rules")]
        [SerializeField] private bool startOnAwake;
        [Tooltip("매 턴 시작 시 드로우할 카드 수. 기본 1장 (손패 유지 방식).")]
        [SerializeField] private int cardsDrawnPerTurn = 1;

        [Header("전투 인트로 연출")]
        [Tooltip("true이면 StartBattle()에서 BeginPlayerTurn()을 즉시 호출하지 않는다.\n" +
                 "BattleIntroDirector가 연출 완료 후 직접 BeginPlayerTurn()을 호출한다.\n" +
                 "BattleIntroDirector 컴포넌트가 씬에 있을 때 반드시 true로 설정한다.")]
        [SerializeField] private bool waitForIntroDirector = false;

        public event Action<BattleManager> BattleStarted;
        public event Action<BattleManager> StateChanged;
        public event Action<BattleManager, BattleRuntimeCard> CardPlayed;
        public event Action<BattleManager, int> PlayerTriggeredDamageResolved;
        public event Action<BattleManager, EnemyAction> EnemyIntentSelected;
        public event Action<BattleManager, BattleCombatantState, BattleStatusTurnResult> TurnStartStatusResolved;
        public event Action<BattleManager, BattlePhase> BattleEnded;
        /// <summary>적이 실제로 행동하기 직전 발생. UI 애니메이션 재생에 사용한다.</summary>
        public event Action<BattleManager, EnemyAction> EnemyActionExecuting;
        /// <summary>플레이어가 적 공격을 회피했을 때 발생.</summary>
        public event Action<BattleManager> DodgeSucceeded;
        /// <summary>플레이어 카드 효과로 적에게 상태이상이 부여될 때 발생 (StatusEffectData는 null일 수 있음).</summary>
        public event Action<BattleManager, StatusEffectType, StatusEffectData> EnemyStatusEffectApplied;

        private readonly List<BattleTriggeredDamageRequest> pendingTriggeredDamageRequests =
            new List<BattleTriggeredDamageRequest>();

        private bool deferTriggeredDamage;

        // ── 전투 기록 ──────────────────────────────────────────────
        public List<BattleTurnSummary> TurnLog { get; private set; } = new List<BattleTurnSummary>();
        private BattleTurnSummary currentTurnSummary;

        /// <summary>가장 최근 발생한 대미지가 치명타(Crit)인지 여부.</summary>
        public bool IsLastDamageCritical { get; set; }

        private string lastPlayedCardName = null;

        public BattlePlayerState Player { get; private set; }

        public BattleEnemyState Enemy { get; private set; }

        public BattlePhase Phase { get; private set; } = BattlePhase.NotStarted;

        public int PlayerTurnCount { get; private set; }

        public bool IsBattleActive => Phase == BattlePhase.PlayerTurn || Phase == BattlePhase.EnemyTurn;

        /// <summary>
        /// 이번 전투에서 실제로 사용 중인 직업 정보.
        /// GameDataManager.SelectedJobInfo → 없으면 defaultJob 순으로 결정.
        /// </summary>
        public JobClassInfo ActiveJob { get; private set; }

        private void Start()
        {
            // 데미지 텍스트 팝업 제어기 자동 장착
            if (gameObject.GetComponent<BattleDamageTextController>() == null)
            {
                gameObject.AddComponent<BattleDamageTextController>();
            }

            AutoConfigureFromGameData();

            if (startOnAwake)
            {
                StartBattle();
            }
        }

        /// <summary>
        /// GameDataManager가 있으면 해당 데이터로, 없으면 defaultJob(전사 기본)으로
        /// playerName / playerMaxHp / startingDeck / ActiveJob을 자동 설정한다.
        /// </summary>
        private void AutoConfigureFromGameData()
        {
            GameDataManager gdm = GameDataManager.Instance;

            if (gdm != null)
            {
                // ── GameDataManager가 존재하는 경우 ───────────────────
                JobClassInfo job = gdm.SelectedJobInfo;
                ActiveJob  = job ?? defaultJob;
                playerName = gdm.PlayerName;

                // HP: 직업 baseMaxHp 우선, 없으면 GameDataManager.MaxHp
                playerMaxHp = (job != null) ? job.baseMaxHp : Mathf.Max(1, gdm.MaxHp);

                // 덱: GameDataManager.Deck(진행 중 덱) 우선,
                //     비어있으면 직업 starterCards, 그것도 없으면 Inspector 값 유지
                if (gdm.Deck != null && gdm.Deck.Count > 0)
                {
                    startingDeck = new List<CardData>(gdm.Deck);
                }
                else if (ActiveJob != null
                         && ActiveJob.starterCards != null
                         && ActiveJob.starterCards.Count > 0)
                {
                    startingDeck = new List<CardData>(ActiveJob.starterCards);
                }
                // else: Inspector에서 지정한 startingDeck 그대로 사용
            }
            else if (defaultJob != null)
            {
                // ── GameDataManager 없음 → defaultJob(전사 기본값) 사용 ─
                ActiveJob   = defaultJob;
                playerMaxHp = defaultJob.baseMaxHp;

                if (defaultJob.starterCards != null && defaultJob.starterCards.Count > 0)
                    startingDeck = new List<CardData>(defaultJob.starterCards);
                // else: Inspector startingDeck 유지
            }
            // else: GameDataManager도 defaultJob도 없으면 Inspector 값 그대로
        }

        public void Configure(string newPlayerName, int newPlayerMaxHp, IEnumerable<CardData> newDeck, EnemyData newEnemyData)
        {
            playerName = string.IsNullOrWhiteSpace(newPlayerName) ? playerName : newPlayerName;
            playerMaxHp = Mathf.Max(1, newPlayerMaxHp);
            enemyData = newEnemyData;

            startingDeck.Clear();
            if (newDeck == null)
            {
                return;
            }

            foreach (CardData cardData in newDeck)
            {
                if (cardData != null)
                {
                    startingDeck.Add(cardData);
                }
            }
        }

        public void StartBattle()
        {
            Player = new BattlePlayerState(playerName, playerMaxHp, startingDeck);
            Enemy = new BattleEnemyState(enemyData);
            PlayerTurnCount = 0;
            TurnLog = new List<BattleTurnSummary>();
            currentTurnSummary = null;

            Player.DrawStartingHand();
            BattleStarted?.Invoke(this);

            PlayBattleBgm();

            // waitForIntroDirector = true이면 BattleIntroDirector가
            // 연출 완료 후 BeginPlayerTurn()을 직접 호출한다.
            if (!waitForIntroDirector)
            {
                BeginPlayerTurn();
            }
        }

        private void PlayBattleBgm()
        {
            string bgmKey = CardAdventure.Audio.AudioManager.BgmKeys.NormalEnemyBattle;
            if (enemyData != null)
            {
                if (enemyData.enemyName == "벨카르온" || enemyData.enemyName == "Velkarion" || enemyData.isBoss)
                {
                    bgmKey = CardAdventure.Audio.AudioManager.BgmKeys.BossBattle;
                }
            }
            CardAdventure.Audio.AudioManager.PlayBgmSafe(bgmKey);
        }

        public void BeginPlayerTurn()
        {
            if (Player == null || Enemy == null)
            {
                return;
            }

            Phase = BattlePhase.PlayerTurn;
            PlayerTurnCount++;
            currentTurnSummary = new BattleTurnSummary { TurnNumber = PlayerTurnCount };
            TurnLog.Add(currentTurnSummary);
            Player.StartTurn();
            ResolveTurnStartStatuses(Player.Combatant);

            if (ResolveBattleEndOrNotify())
            {
                return;
            }

            if (PlayerTurnCount > 1)
            {
                Player.CardPiles.Draw(cardsDrawnPerTurn);
            }

            Enemy.SelectIntent();
            EnemyIntentSelected?.Invoke(this, Enemy.CurrentIntent);
            StateChanged?.Invoke(this);
        }

        /// <summary>카드 사용 가능 여부만 확인한다 (효과 미적용).</summary>
        public bool CanPlayCard(BattleRuntimeCard card)
        {
            if (Phase != BattlePhase.PlayerTurn || Player == null) return false;
            if (card == null || card.Data == null) return false;
            if (!Player.CardPiles.HasHandCard(card)) return false;
            if (!Player.CanPayEnergy(card)) return false;
            return true;
        }

        public void SetTriggeredDamageDeferred(bool deferred)
        {
            deferTriggeredDamage = deferred;
            if (!deferred)
            {
                pendingTriggeredDamageRequests.Clear();
            }
        }

        public List<BattleTriggeredDamageRequest> ConsumePendingTriggeredDamageRequests()
        {
            List<BattleTriggeredDamageRequest> requests = new List<BattleTriggeredDamageRequest>(pendingTriggeredDamageRequests);
            pendingTriggeredDamageRequests.Clear();
            return requests;
        }

        public int ResolveTriggeredDamage(BattleTriggeredDamageRequest request)
        {
            if (Enemy == null || Enemy.Combatant.IsDefeated) return 0;

            int damageDealt = DealPlayerDamageToEnemy(
                request.BaseDamage,
                request.IncludeAttackBonuses,
                request.TriggerDamageRewards);

            // 히트당 상태이상 획득 (MultiHitAndGainStrength 등)
            if (request.HasPlayerStatusGain && damageDealt > 0 && Player != null)
                Player.Combatant.ApplyStatus(request.PlayerGainStatus, request.PlayerGainStatusStacks, 0);

            if (damageDealt > 0)
                PlayerTriggeredDamageResolved?.Invoke(this, damageDealt);

            ResolveBattleEndOrNotify();
            return damageDealt;
        }

        public BattleCardPlayResult PlayCard(BattleRuntimeCard card)
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.BattleNotActive);
            }

            if (card != null && card.Data != null)
            {
                lastPlayedCardName = card.Data.cardName;
            }

            if (card == null || card.Data == null)
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.InvalidCard);
            }

            if (!Player.CardPiles.HasHandCard(card))
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.CardNotInHand);
            }

            bool needsEnemyTarget = card.Data.effectType == CardEffectType.BasicAttack
                                 || card.Data.effectType == CardEffectType.ShieldBash
                                 || card.Data.effectType == CardEffectType.DoubleStrike
                                 || card.Data.effectType == CardEffectType.BerserkerAttack
                                 || card.Data.effectType == CardEffectType.AttackAndDefend
                                 || card.Data.effectType == CardEffectType.AttackAndApplyStatus
                                 || card.Data.effectType == CardEffectType.AttackAndGainBlockEqualDamage
                                 || card.Data.effectType == CardEffectType.ConsumeBlockToDealDamage
                                 || card.Data.effectType == CardEffectType.DamageAndApplyStatus
                                 || card.Data.effectType == CardEffectType.GrantEnemyStrengthAndRetaliateNext
                                 || card.Data.effectType == CardEffectType.MultiHitAttack
                                 || card.Data.effectType == CardEffectType.MultiHitAndGainStrength
                                 || card.Data.effectType == CardEffectType.FreezeEnemyNextAction
                                 || card.Data.effectType == CardEffectType.PlayHandRandomly
                                 || card.Data.effectType == CardEffectType.ApplyStatusToEnemy
                                 || card.Data.effectType == CardEffectType.ConsumeAllEnergyAndAttack
                                 || card.Data.effectType == CardEffectType.AttackAndGainDodge
                                 || card.Data.effectType == CardEffectType.MultiHitWithCritFromDodge
                                 || card.Data.effectType == CardEffectType.PoisonAndDetonateAllPoison
                                 || card.Data.effectType == CardEffectType.AttackAndShuffleBackToDeck;
            if (needsEnemyTarget && (Enemy == null || Enemy.Combatant.IsDefeated))
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.TargetRequired);
            }

            bool cardWasFree = card.EnergyCost == 0;
            if (!Player.SpendEnergy(card))
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.NotEnoughEnergy);
            }

            Player.CardPiles.RemoveHandCard(card);

            ApplyCardEffect(card);
            Player.ConsumeFirstAttackFreeIfNeeded(card);
            currentTurnSummary?.CardsUsed.Add(card.Data.cardName);

            if (cardWasFree)
            {
                TriggerFreeCardPlayedEffects();
            }

            MoveResolvedCardToDestination(card);

            CardPlayed?.Invoke(this, card);
            CardAdventure.Audio.AudioManager.PlaySfxSafe(CardAdventure.Audio.AudioManager.SfxKeys.CardUse);
            ResolveBattleEndOrNotify();
            return BattleCardPlayResult.Succeeded();
        }

        public void EndPlayerTurn()
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                return;
            }

            Player.Combatant.TickStatusDurations();
            StartCoroutine(ExecuteEnemyTurnRoutine());
        }

        // 하위 호환성을 위해 공개 유지 (즉시 코루틴 시작)
        public void ExecuteEnemyTurn() => StartCoroutine(ExecuteEnemyTurnRoutine());

        private IEnumerator ExecuteEnemyTurnRoutine()
        {
            if (!IsBattleActive || Enemy == null || Enemy.Combatant.IsDefeated)
            {
                ResolveBattleEndOrNotify();
                yield break;
            }

            Phase = BattlePhase.EnemyTurn;
            StateChanged?.Invoke(this); // 버튼·손패 비활성화 즉시 처리

            // 플레이어 상태이상 틱 (독 등)
            ResolveTurnStartStatuses(Enemy.Combatant);
            if (ResolveBattleEndOrNotify()) yield break;

            yield return new WaitForSeconds(0.35f);

            EnemyAction action = Enemy.CurrentIntent ?? Enemy.SelectIntent();
            bool skipped = Enemy.ConsumeSkipNextAction();
            if (skipped && currentTurnSummary != null)
            {
                currentTurnSummary.EnemySkipped = true;
                currentTurnSummary.EnemyActionDesc = "행동 없음 (동결됨)";
            }
            if (!skipped)
            {
                // UI가 행동 예고 애니메이션을 재생할 수 있도록 이벤트 발행
                EnemyActionExecuting?.Invoke(this, action);

                yield return new WaitForSeconds(0.55f); // 적 전진 애니메이션 대기

                ApplyEnemyAction(action);
                StateChanged?.Invoke(this); // HP 변화 → HUD·VFX 갱신

                yield return new WaitForSeconds(0.3f); // 피격 이펙트가 보일 시간
            }

            Enemy.AdvanceTurn();
            Enemy.Combatant.TickStatusDurations();

            if (ResolveBattleEndOrNotify()) yield break;

            BeginPlayerTurn();
        }

        private void ApplyCardEffect(BattleRuntimeCard card)
        {
            CardData data = card.Data;

            if (data.effectType == CardEffectType.None)
            {
                Debug.LogWarning($"[BattleManager] 카드 '{data.cardName}'의 effectType이 None입니다. " +
                                 "CardData Inspector에서 effectType을 설정해 주세요.");
                return;
            }

            switch (data.effectType)
            {
                // ── 공격 ──────────────────────────────────────────
                case CardEffectType.BasicAttack:
                {
                    DealPlayerDamageToEnemy(data.effectValue, true, true);
                    break;
                }
                case CardEffectType.ShieldBash:
                {
                    int baseDamage = data.effectValue + Player.Combatant.Block;
                    DealPlayerDamageToEnemy(baseDamage, true, true);
                    break;
                }

                // ── 방어 ──────────────────────────────────────────
                case CardEffectType.BasicDefense:
                    AddPlayerBlock(data.effectValue);
                    break;

                // ── 스킬 ──────────────────────────────────────────
                case CardEffectType.Rage:
                    Player.AddAttackBonusGainedPerAttackForTurn(data.effectValue);
                    break;

                case CardEffectType.Taunt:
                    AddPlayerBlock(data.effectValue);
                    if (data.statusEffect != null)
                        ApplyAndNotifyEnemyStatus(data.statusEffect, Mathf.Max(1, data.statusEffect.defaultStacks));
                    else
                        ApplyAndNotifyEnemyStatus(StatusEffectType.Weak, 1, 1);
                    break;

                // ── 상태이상 부여 ─────────────────────────────────
                case CardEffectType.ApplyStatusToEnemy:
                    if (data.statusEffect != null)
                        ApplyAndNotifyEnemyStatus(data.statusEffect,
                            Mathf.Max(1, data.effectValue > 0 ? data.effectValue : data.statusEffect.defaultStacks));
                    else
                        Debug.LogWarning($"[BattleManager] 카드 '{data.cardName}': ApplyStatusToEnemy인데 statusEffect가 없습니다.");
                    break;

                case CardEffectType.ApplyStatusToPlayer:
                    if (data.statusEffect != null)
                        Player.Combatant.ApplyStatus(data.statusEffect,
                            Mathf.Max(1, data.effectValue > 0 ? data.effectValue : data.statusEffect.defaultStacks));
                    else
                        Debug.LogWarning($"[BattleManager] 카드 '{data.cardName}': ApplyStatusToPlayer인데 statusEffect가 없습니다.");
                    break;

                // ── 공격 확장 ─────────────────────────────────────
                case CardEffectType.DoubleStrike:
                {
                    for (int i = 0; i < 2; i++)
                        DealOrQueueTriggeredDamage(data.effectValue, true, true, isMultiHit: true);
                    break;
                }
                case CardEffectType.BerserkerAttack:
                {
                    int selfDmg = Mathf.Max(0, data.secondaryValue);
                    Player.Combatant.ReceiveDamage(selfDmg);
                    DealPlayerDamageToEnemy(data.effectValue, true, true);
                    break;
                }
                case CardEffectType.AttackAndDefend:
                {
                    DealPlayerDamageToEnemy(data.effectValue, true, true);
                    AddPlayerBlock(data.secondaryValue);
                    break;
                }
                case CardEffectType.AttackAndApplyStatus:
                {
                    DealPlayerDamageToEnemy(data.effectValue, true, true);
                    if (data.statusEffect != null)
                    {
                        int stacks = data.secondaryValue > 0 ? data.secondaryValue : data.statusEffect.defaultStacks;
                        ApplyAndNotifyEnemyStatus(data.statusEffect, Mathf.Max(1, stacks));
                    }
                    else
                        Debug.LogWarning($"[BattleManager] 카드 '{data.cardName}': AttackAndApplyStatus인데 statusEffect가 없습니다.");
                    break;
                }
                case CardEffectType.AttackAndGainBlockEqualDamage:
                {
                    int damageDealt = DealPlayerDamageToEnemy(data.effectValue, true, true);
                    AddPlayerBlock(damageDealt);
                    break;
                }
                case CardEffectType.ConsumeBlockToDealDamage:
                {
                    int blockToConsume = Player.Combatant.Block;
                    Player.Combatant.ClearBlock();
                    DealPlayerDamageToEnemy(blockToConsume, true, true);
                    break;
                }
                case CardEffectType.DamageAndApplyStatus:
                {
                    DealPlayerDamageToEnemy(data.secondaryValue, true, true);
                    if (data.statusEffect != null)
                    {
                        int stacks = data.effectValue > 0 ? data.effectValue : data.statusEffect.defaultStacks;
                        ApplyAndNotifyEnemyStatus(data.statusEffect, Mathf.Max(1, stacks));
                    }
                    else
                        Debug.LogWarning($"[BattleManager] 카드 '{data.cardName}': DamageAndApplyStatus인데 statusEffect가 없습니다.");
                    break;
                }
                case CardEffectType.MultiHitAttack:
                {
                    int hitCount = Mathf.Max(1, data.secondaryValue);
                    for (int i = 0; i < hitCount; i++)
                        DealOrQueueTriggeredDamage(data.effectValue, true, true, isMultiHit: true);
                    break;
                }
                case CardEffectType.MultiHitAndGainStrength:
                {
                    const int hitCount = 3;
                    int strengthPerHit = Mathf.Max(1, data.secondaryValue);
                    for (int i = 0; i < hitCount; i++)
                        DealOrQueueTriggeredDamage(data.effectValue, true, true,
                            isMultiHit: true,
                            playerGainStatus: StatusEffectType.Strength,
                            playerGainStatusStacks: strengthPerHit);
                    break;
                }

                // ── 방어 확장 ─────────────────────────────────────
                case CardEffectType.DefenseAndDraw:
                    AddPlayerBlock(data.effectValue);
                    if (data.secondaryValue > 0)
                        Player.CardPiles.Draw(data.secondaryValue);
                    break;
                case CardEffectType.DrawAndDefense:
                    if (data.effectValue > 0)
                        Player.CardPiles.Draw(data.effectValue);
                    AddPlayerBlock(data.secondaryValue);
                    break;

                // ── 스킬 확장 ─────────────────────────────────────
                case CardEffectType.DrawCards:
                    if (data.effectValue > 0)
                        Player.CardPiles.Draw(data.effectValue);
                    break;

                case CardEffectType.GainStrength:
                    Player.Combatant.ApplyStatus(StatusEffectType.Strength,
                        Mathf.Max(1, data.effectValue), 0);
                    break;
                case CardEffectType.DealDamageWhenBlockGained:
                    Player.AddDamageDealtPerBlockGainedForTurn(Mathf.Max(1, data.effectValue));
                    break;
                case CardEffectType.GainBlockWhenDamageDealt:
                    Player.AddBlockGainedPerDamageDealtForTurn(Mathf.Max(1, data.effectValue));
                    break;
                case CardEffectType.GainStrengthEqualCurrentBlock:
                    if (Player.Combatant.Block > 0)
                    {
                        Player.Combatant.ApplyStatus(StatusEffectType.Strength, Player.Combatant.Block, 0);
                    }
                    break;
                case CardEffectType.DrawCardWhenBlockGained:
                    Player.AddCardsDrawnPerBlockGainForTurn(Mathf.Max(1, data.effectValue));
                    break;
                case CardEffectType.GrantEnemyStrengthAndRetaliateNext:
                    ApplyAndNotifyEnemyStatus(StatusEffectType.Strength, Mathf.Max(1, data.effectValue), 0);
                    Player.PrepareGainStrengthFromNextEnemyDamage();
                    break;
                case CardEffectType.GainEnergyThisTurn:
                    Player.AddEnergy(data.effectValue);
                    break;
                case CardEffectType.BlockAndNextTurnEnergy:
                    AddPlayerBlock(data.effectValue);
                    Player.AddNextTurnEnergyBonus(data.secondaryValue);
                    break;
                case CardEffectType.ApplyPoisonWhenDamageDealt:
                    Player.AddPoisonAppliedPerDamageDealtForTurn(Mathf.Max(1, data.effectValue));
                    break;
                case CardEffectType.FreezeEnemyNextAction:
                    Enemy.FreezeNextAction();
                    break;
                case CardEffectType.MakeFirstAttackFreeThisTurn:
                    Player.MakeFirstAttackFreeThisTurn();
                    break;
                case CardEffectType.PlayHandRandomly:
                    PlayOtherHandCardsRandomly(card);
                    break;

                // ── 도적 전용 ─────────────────────────────────────
                case CardEffectType.ConsumeAllEnergyAndAttack:
                {
                    int energyConsumed = Player.ConsumeAllEnergy();
                    int damage = data.effectValue + energyConsumed * data.secondaryValue;
                    DealPlayerDamageToEnemy(damage, true, true);
                    break;
                }
                case CardEffectType.AttackAndGainDodge:
                {
                    DealPlayerDamageToEnemy(data.effectValue, true, true);
                    Player.Combatant.ApplyStatus(StatusEffectType.Dodge, Mathf.Max(1, data.secondaryValue), 0);
                    break;
                }
                case CardEffectType.GainDodgeWhenPlayingFreeCards:
                    Player.SetDodgePerFreeCardPlayed(Mathf.Max(1, data.effectValue));
                    break;
                case CardEffectType.MultiHitWithCritFromDodge:
                {
                    int dodgeStacks = Player.Combatant.GetStatusStacks(StatusEffectType.Dodge);
                    float critChance = Mathf.Clamp01(dodgeStacks * 0.01f);
                    for (int i = 0; i < 3; i++)
                    {
                        // 크리티컬 여부를 큐잉 시점에 미리 결정해 저장
                        int hitDamage = data.effectValue;
                        if (UnityEngine.Random.value < critChance)
                        {
                            hitDamage = Mathf.RoundToInt(hitDamage * data.secondaryValue);
                            IsLastDamageCritical = true; // 치명타 플래그 세팅!
                        }
                        DealOrQueueTriggeredDamage(hitDamage, true, true, isMultiHit: true);
                    }
                    break;
                }
                case CardEffectType.MultiplyDodgeStacks:
                    Player.Combatant.MultiplyStatusStacks(StatusEffectType.Dodge, Mathf.Max(2, data.effectValue));
                    break;
                case CardEffectType.DrawCardWhenPlayingFreeCards:
                    Player.SetDrawPerFreeCardPlayed(Mathf.Max(1, data.effectValue));
                    break;
                case CardEffectType.PoisonAndDetonateAllPoison:
                {
                    ApplyAndNotifyEnemyStatus(StatusEffectType.Poison, Mathf.Max(1, data.effectValue), 0);
                    int removedPoison = Enemy.Combatant.RemoveStatus(StatusEffectType.Poison);
                    int detonationDamage = data.effectValue + removedPoison;
                    DealPlayerDamageToEnemy(detonationDamage, false, true);
                    break;
                }
                case CardEffectType.AttackAndShuffleBackToDeck:
                    DealPlayerDamageToEnemy(data.effectValue, true, true);
                    break;
                case CardEffectType.DrawCardsGainDodgeOnFreeDraw:
                {
                    List<BattleRuntimeCard> drawn = Player.CardPiles.Draw(Mathf.Max(0, data.effectValue));
                    int dodgePerFree = Mathf.Max(0, data.secondaryValue);
                    if (dodgePerFree > 0)
                    {
                        foreach (BattleRuntimeCard drawnCard in drawn)
                        {
                            if (drawnCard?.EnergyCost == 0)
                                Player.Combatant.ApplyStatus(StatusEffectType.Dodge, dodgePerFree, 0);
                        }
                    }
                    break;
                }

                default:
                    Debug.LogWarning($"[BattleManager] 카드 '{data.cardName}'의 effectType({data.effectType})에 대한 처리가 없습니다.");
                    break;
            }
        }

        private void TriggerFreeCardPlayedEffects()
        {
            if (Player == null)
                return;

            if (Player.DodgePerFreeCardPlayed > 0)
                Player.Combatant.ApplyStatus(StatusEffectType.Dodge, Player.DodgePerFreeCardPlayed, 0);

            if (Player.DrawPerFreeCardPlayed > 0)
                Player.CardPiles.Draw(Player.DrawPerFreeCardPlayed);
        }

        private void AddPlayerBlock(int amount, bool triggerBlockGainEffects = true)
        {
            int blockAmount = Mathf.Max(0, amount);
            if (blockAmount <= 0 || Player == null)
            {
                return;
            }

            Player.Combatant.AddBlock(blockAmount);

            if (!triggerBlockGainEffects)
            {
                return;
            }

            if (Player.DamageDealtPerBlockGained > 0)
            {
                int damage = blockAmount * Player.DamageDealtPerBlockGained;
                DealOrQueueTriggeredDamage(damage, false, true);
            }

            if (Player.CardsDrawnPerBlockGain > 0)
            {
                // "방어막을 획득할 때마다 N장 드로우"는 획득량이 아닌 이벤트 단위
                Player.CardPiles.Draw(Player.CardsDrawnPerBlockGain);
            }
        }

        private int DealPlayerDamageToEnemy(int baseDamage, bool includeAttackBonuses, bool triggerDamageRewards)
        {
            if (Enemy == null || Enemy.Combatant == null)
            {
                return 0;
            }

            int damage = Mathf.Max(0, baseDamage);
            if (includeAttackBonuses)
            {
                damage = Player.GetAttackDamage(damage);
            }

            damage = ApplyVulnerableDamageModifier(Enemy.Combatant, damage);
            int hpBeforeDamage = Enemy.Combatant.CurrentHp;
            Enemy.Combatant.ReceiveDamage(damage);
            int damageDealt = Mathf.Max(0, hpBeforeDamage - Enemy.Combatant.CurrentHp);
            if (currentTurnSummary != null) currentTurnSummary.DamageDealtToEnemy += damageDealt;

            if (damageDealt > 0 && !string.IsNullOrEmpty(lastPlayedCardName))
            {
                if (GameDataManager.Instance != null)
                {
                    GameDataManager.Instance.TrackCardDamage(lastPlayedCardName, damageDealt);
                }
            }

            if (triggerDamageRewards && damageDealt > 0 && Player.BlockGainedPerDamageDealt > 0)
            {
                AddPlayerBlock(damageDealt * Player.BlockGainedPerDamageDealt, true);
            }

            if (triggerDamageRewards && damageDealt > 0 && Player.PoisonAppliedPerDamageDealt > 0)
                ApplyAndNotifyEnemyStatus(StatusEffectType.Poison, Player.PoisonAppliedPerDamageDealt, 0);

            return damageDealt;
        }

        /// <summary>적에게 StatusEffectData 기반 상태이상을 부여하고 이벤트를 발생시킨다.</summary>
        private void ApplyAndNotifyEnemyStatus(StatusEffectData data, int stacks)
        {
            if (data == null || stacks <= 0) return;
            Enemy.Combatant.ApplyStatus(data, stacks);
            EnemyStatusEffectApplied?.Invoke(this, data.effectType, data);
        }

        /// <summary>적에게 타입 기반 상태이상을 부여하고 이벤트를 발생시킨다.</summary>
        private void ApplyAndNotifyEnemyStatus(StatusEffectType type, int stacks, int duration = 0)
        {
            if (stacks <= 0) return;
            Enemy.Combatant.ApplyStatus(type, stacks, duration);
            EnemyStatusEffectApplied?.Invoke(this, type, null);
        }

        private void DealOrQueueTriggeredDamage(
            int baseDamage, bool includeAttackBonuses, bool triggerDamageRewards,
            bool isMultiHit = false,
            StatusEffectType playerGainStatus = default,
            int playerGainStatusStacks = 0)
        {
            if (deferTriggeredDamage)
            {
                pendingTriggeredDamageRequests.Add(new BattleTriggeredDamageRequest(
                    baseDamage, includeAttackBonuses, triggerDamageRewards,
                    isMultiHit, playerGainStatus, playerGainStatusStacks));
                return;
            }

            int dealt = DealPlayerDamageToEnemy(baseDamage, includeAttackBonuses, triggerDamageRewards);
            if (playerGainStatusStacks > 0 && dealt > 0 && Player != null)
                Player.Combatant.ApplyStatus(playerGainStatus, playerGainStatusStacks, 0);
        }

        private void PlayOtherHandCardsRandomly(BattleRuntimeCard sourceCard)
        {
            if (Player == null || Enemy == null)
            {
                return;
            }

            List<BattleRuntimeCard> cardsToPlay = new List<BattleRuntimeCard>();
            foreach (BattleRuntimeCard handCard in Player.CardPiles.Hand)
            {
                if (handCard != null && handCard != sourceCard)
                {
                    cardsToPlay.Add(handCard);
                }
            }

            for (int i = cardsToPlay.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (cardsToPlay[i], cardsToPlay[swapIndex]) = (cardsToPlay[swapIndex], cardsToPlay[i]);
            }

            foreach (BattleRuntimeCard card in cardsToPlay)
            {
                if (Phase != BattlePhase.PlayerTurn || Enemy.Combatant.IsDefeated || !Player.CardPiles.HasHandCard(card))
                {
                    break;
                }

                Player.CardPiles.RemoveHandCard(card);

                ApplyCardEffect(card);
                Player.ConsumeFirstAttackFreeIfNeeded(card);

                MoveResolvedCardToDestination(card);

                CardPlayed?.Invoke(this, card);

                if (ResolveBattleEndOrNotify())
                {
                    break;
                }
            }
        }

        private void MoveResolvedCardToDestination(BattleRuntimeCard card)
        {
            if (card == null || card.Data == null || Player == null)
            {
                return;
            }

            if (card.Data.effectType == CardEffectType.AttackAndShuffleBackToDeck)
            {
                Player.CardPiles.AddToDrawPile(card);
            }
            else if (card.IsExhaust)
            {
                Player.CardPiles.AddToExhaust(card);
            }
            else
            {
                Player.CardPiles.AddToDiscard(card);
            }
        }

        private void ApplyEnemyAction(EnemyAction action)
        {
            if (action == null)
            {
                return;
            }

            if (currentTurnSummary != null)
                currentTurnSummary.EnemyActionDesc = action.GetIntentDescription();

            switch (action.actionType)
            {
                case EnemyActionType.Attack:
                {
                    int attackDamage = GetEnemyAttackDamage(action.value);
                    int dodgeStacks  = Player.Combatant.GetStatusStacks(StatusEffectType.Dodge);
                    float dodgeChance = Mathf.Clamp01(dodgeStacks * 0.01f);

                    if (dodgeStacks > 0 && UnityEngine.Random.value < dodgeChance)
                    {
                        // 회피 성공: 피해 무효, 스택 절반으로 감소
                        Player.Combatant.HalveStatusStacks(StatusEffectType.Dodge);
                        DodgeSucceeded?.Invoke(this);
                        if (currentTurnSummary != null) currentTurnSummary.PlayerDodged = true;
                    }
                    else
                    {
                        int damageTaken = Player.Combatant.ReceiveDamage(attackDamage);
                        Player.ResolveGainStrengthFromEnemyDamage(damageTaken);
                        if (currentTurnSummary != null) currentTurnSummary.DamageTakenByPlayer += damageTaken;
                    }
                    break;
                }
                case EnemyActionType.Defend:
                    Enemy.Combatant.AddBlock(action.value);
                    break;
                case EnemyActionType.Buff:
                    Enemy.Combatant.ApplyStatus(action.statusEffect, action.statusEffectStacks);
                    break;
                case EnemyActionType.DebuffPlayer:
                    Player.Combatant.ApplyStatus(action.statusEffect, action.statusEffectStacks);
                    break;
                case EnemyActionType.HealSelf:
                    Enemy.Combatant.Heal(action.value);
                    break;
            }
        }

        private void ResolveTurnStartStatuses(BattleCombatantState combatant)
        {
            if (combatant == null || combatant.IsDefeated)
            {
                return;
            }

            BattleStatusTurnResult result = combatant.ApplyTurnStartStatusEffects();

            if (result.HasAnyEffect)
            {
                TurnStartStatusResolved?.Invoke(this, combatant, result);
            }
        }

        private int GetEnemyAttackDamage(int baseDamage)
        {
            int damage = Mathf.Max(0, baseDamage + Enemy.Combatant.GetStatusStacks(StatusEffectType.Strength));

            if (Enemy.Combatant.HasStatus(StatusEffectType.Weak))
            {
                damage = Mathf.FloorToInt(damage * 0.75f);
            }

            return ApplyVulnerableDamageModifier(Player.Combatant, damage);
        }

        private static int ApplyVulnerableDamageModifier(BattleCombatantState target, int damage)
        {
            if (target != null && target.HasStatus(StatusEffectType.Vulnerable))
            {
                return Mathf.CeilToInt(damage * 1.5f);
            }

            return damage;
        }

        private bool ResolveBattleEndOrNotify()
        {
            if (Enemy != null && Enemy.Combatant.IsDefeated)
            {
                Phase = BattlePhase.Won;
                BattleEnded?.Invoke(this, Phase);
                StateChanged?.Invoke(this);
                return true;
            }

            if (Player != null && Player.Combatant.IsDefeated)
            {
                Phase = BattlePhase.Lost;
                BattleEnded?.Invoke(this, Phase);
                StateChanged?.Invoke(this);
                return true;
            }

            StateChanged?.Invoke(this);
            return false;
        }
    }
}

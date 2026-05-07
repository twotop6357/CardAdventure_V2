using System;
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

        [Header("Enemy")]
        [SerializeField] private EnemyData enemyData;

        [Header("Rules")]
        [SerializeField] private bool startOnAwake;
        [SerializeField] private int cardsDrawnPerTurn = 5;

        public event Action<BattleManager> BattleStarted;
        public event Action<BattleManager> StateChanged;
        public event Action<BattleManager, BattleRuntimeCard> CardPlayed;
        public event Action<BattleManager, EnemyAction> EnemyIntentSelected;
        public event Action<BattleManager, BattleCombatantState, BattleStatusTurnResult> TurnStartStatusResolved;
        public event Action<BattleManager, BattlePhase> BattleEnded;

        public BattlePlayerState Player { get; private set; }

        public BattleEnemyState Enemy { get; private set; }

        public BattlePhase Phase { get; private set; } = BattlePhase.NotStarted;

        public int PlayerTurnCount { get; private set; }

        public bool IsBattleActive => Phase == BattlePhase.PlayerTurn || Phase == BattlePhase.EnemyTurn;

        private void Start()
        {
            if (startOnAwake)
            {
                StartBattle();
            }
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

            Player.DrawStartingHand();
            BattleStarted?.Invoke(this);
            BeginPlayerTurn();
        }

        public void BeginPlayerTurn()
        {
            if (Player == null || Enemy == null)
            {
                return;
            }

            Phase = BattlePhase.PlayerTurn;
            PlayerTurnCount++;
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

        public BattleCardPlayResult PlayCard(BattleRuntimeCard card)
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.BattleNotActive);
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
                                 || card.Data.effectType == CardEffectType.ApplyStatusToEnemy;
            if (needsEnemyTarget && (Enemy == null || Enemy.Combatant.IsDefeated))
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.TargetRequired);
            }

            if (!Player.SpendEnergy(card))
            {
                return BattleCardPlayResult.Failed(BattleCardPlayFailureReason.NotEnoughEnergy);
            }

            ApplyCardEffect(card);

            if (card.IsExhaust)
            {
                Player.CardPiles.MoveHandCardToExhaust(card);
            }
            else
            {
                Player.CardPiles.MoveHandCardToDiscard(card);
            }

            CardPlayed?.Invoke(this, card);
            ResolveBattleEndOrNotify();
            return BattleCardPlayResult.Succeeded();
        }

        public void EndPlayerTurn()
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                return;
            }

            Player.CardPiles.DiscardHand();
            Player.Combatant.TickStatusDurations();
            ExecuteEnemyTurn();
        }

        public void ExecuteEnemyTurn()
        {
            if (!IsBattleActive || Enemy == null || Enemy.Combatant.IsDefeated)
            {
                ResolveBattleEndOrNotify();
                return;
            }

            Phase = BattlePhase.EnemyTurn;
            ResolveTurnStartStatuses(Enemy.Combatant);

            if (ResolveBattleEndOrNotify())
            {
                return;
            }

            EnemyAction action = Enemy.CurrentIntent ?? Enemy.SelectIntent();
            ApplyEnemyAction(action);
            Enemy.AdvanceTurn();
            Enemy.Combatant.TickStatusDurations();

            if (ResolveBattleEndOrNotify())
            {
                return;
            }

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
                    int damage = Player.GetAttackDamage(data.effectValue);
                    damage = ApplyVulnerableDamageModifier(Enemy.Combatant, damage);
                    Enemy.Combatant.ReceiveDamage(damage);
                    break;
                }
                case CardEffectType.ShieldBash:
                {
                    int baseDamage = data.effectValue + Player.Combatant.Block;
                    int damage = Player.GetAttackDamage(baseDamage);
                    damage = ApplyVulnerableDamageModifier(Enemy.Combatant, damage);
                    Enemy.Combatant.ReceiveDamage(damage);
                    break;
                }

                // ── 방어 ──────────────────────────────────────────
                case CardEffectType.BasicDefense:
                    Player.Combatant.AddBlock(data.effectValue);
                    break;

                // ── 스킬 ──────────────────────────────────────────
                case CardEffectType.Rage:
                    Player.AddAttackBonusGainedPerAttackForTurn(data.effectValue);
                    break;

                case CardEffectType.Taunt:
                    Player.Combatant.AddBlock(data.effectValue);
                    Enemy.Combatant.ApplyStatus(StatusEffectType.Weak, 1, 1);
                    break;

                // ── 상태이상 부여 ─────────────────────────────────
                case CardEffectType.ApplyStatusToEnemy:
                    if (data.statusEffect != null)
                        Enemy.Combatant.ApplyStatus(data.statusEffect,
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
                    int dmg = Player.GetAttackDamage(data.effectValue);
                    dmg = ApplyVulnerableDamageModifier(Enemy.Combatant, dmg);
                    Enemy.Combatant.ReceiveDamage(dmg);
                    Enemy.Combatant.ReceiveDamage(dmg);
                    break;
                }
                case CardEffectType.BerserkerAttack:
                {
                    int selfDmg = Mathf.Max(0, data.secondaryValue);
                    Player.Combatant.ReceiveDamage(selfDmg);
                    int dmg = Player.GetAttackDamage(data.effectValue);
                    dmg = ApplyVulnerableDamageModifier(Enemy.Combatant, dmg);
                    Enemy.Combatant.ReceiveDamage(dmg);
                    break;
                }
                case CardEffectType.AttackAndDefend:
                {
                    int dmg = Player.GetAttackDamage(data.effectValue);
                    dmg = ApplyVulnerableDamageModifier(Enemy.Combatant, dmg);
                    Enemy.Combatant.ReceiveDamage(dmg);
                    Player.Combatant.AddBlock(data.secondaryValue);
                    break;
                }
                case CardEffectType.AttackAndApplyStatus:
                {
                    int dmg = Player.GetAttackDamage(data.effectValue);
                    dmg = ApplyVulnerableDamageModifier(Enemy.Combatant, dmg);
                    Enemy.Combatant.ReceiveDamage(dmg);
                    if (data.statusEffect != null)
                    {
                        int stacks = data.secondaryValue > 0 ? data.secondaryValue : data.statusEffect.defaultStacks;
                        Enemy.Combatant.ApplyStatus(data.statusEffect, Mathf.Max(1, stacks));
                    }
                    else
                        Debug.LogWarning($"[BattleManager] 카드 '{data.cardName}': AttackAndApplyStatus인데 statusEffect가 없습니다.");
                    break;
                }

                // ── 방어 확장 ─────────────────────────────────────
                case CardEffectType.DefenseAndDraw:
                    Player.Combatant.AddBlock(data.effectValue);
                    if (data.secondaryValue > 0)
                        Player.CardPiles.Draw(data.secondaryValue);
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

                default:
                    Debug.LogWarning($"[BattleManager] 카드 '{data.cardName}'의 effectType({data.effectType})에 대한 처리가 없습니다.");
                    break;
            }
        }

        private void ApplyEnemyAction(EnemyAction action)
        {
            if (action == null)
            {
                return;
            }

            switch (action.actionType)
            {
                case EnemyActionType.Attack:
                    Player.Combatant.ReceiveDamage(GetEnemyAttackDamage(action.value));
                    break;
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

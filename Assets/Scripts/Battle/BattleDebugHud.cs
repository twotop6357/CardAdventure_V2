using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// Temporary IMGUI debug HUD for manually testing the Phase 1 battle loop in Play Mode.
    /// </summary>
    public sealed class BattleDebugHud : MonoBehaviour
    {
        [SerializeField] private BattleManager battleManager;

        private readonly List<string> logLines = new List<string>();
        private Vector2 logScroll;

        private void Awake()
        {
            ResolveBattleManager();
        }

        private void OnEnable()
        {
            ResolveBattleManager();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnGUI()
        {
            ResolveBattleManager();

            Rect panelRect = new Rect(16f, 16f, 520f, Screen.height - 32f);
            GUILayout.BeginArea(panelRect, GUI.skin.box);

            GUILayout.Label("CardAdventure Battle Test");

            if (battleManager == null)
            {
                GUILayout.Label("BattleManager not found.");
                GUILayout.EndArea();
                return;
            }

            DrawBattleState();
            GUILayout.Space(8f);
            DrawHandControls();
            GUILayout.Space(8f);
            DrawTurnControls();
            GUILayout.Space(8f);
            DrawLog();

            GUILayout.EndArea();
        }

        private void DrawBattleState()
        {
            GUILayout.Label("Phase: " + battleManager.Phase);

            if (battleManager.Player != null)
            {
                BattleCombatantState player = battleManager.Player.Combatant;
                GUILayout.Label(
                    $"Player HP {player.CurrentHp}/{player.MaxHp} | Block {player.Block} | Energy {battleManager.Player.CurrentEnergy}/{battleManager.Player.MaxEnergy}");
                GUILayout.Label("Player Status: " + FormatStatuses(player));
            }

            if (battleManager.Enemy != null)
            {
                BattleCombatantState enemy = battleManager.Enemy.Combatant;
                string intent = battleManager.Enemy.CurrentIntent != null
                    ? battleManager.Enemy.CurrentIntent.GetIntentDescription()
                    : "None";
                GUILayout.Label($"Enemy HP {enemy.CurrentHp}/{enemy.MaxHp} | Block {enemy.Block} | Intent {intent}");
                GUILayout.Label("Enemy Status: " + FormatStatuses(enemy));
            }
        }

        private void DrawHandControls()
        {
            GUILayout.Label("Hand");

            if (battleManager.Player == null)
            {
                GUILayout.Label("No player state.");
                return;
            }

            IReadOnlyList<BattleRuntimeCard> hand = battleManager.Player.CardPiles.Hand;
            if (hand.Count == 0)
            {
                GUILayout.Label("(empty)");
                return;
            }

            for (int i = 0; i < hand.Count; i++)
            {
                BattleRuntimeCard card = hand[i];
                string label = $"{card.DisplayName} ({card.EnergyCost}) - {card.Data.GetFormattedDescription()}";
                bool canPlay = battleManager.Phase == BattlePhase.PlayerTurn && battleManager.Player.CanPayEnergy(card);

                GUI.enabled = canPlay;
                if (GUILayout.Button(label, GUILayout.Height(32f)))
                {
                    BattleCardPlayResult result = battleManager.PlayCard(card);
                    if (!result.Success)
                    {
                        AddLog("Failed to play " + card.DisplayName + ": " + result.FailureReason);
                    }
                }

                GUI.enabled = true;
            }
        }

        private void DrawTurnControls()
        {
            GUILayout.BeginHorizontal();

            GUI.enabled = battleManager.Phase == BattlePhase.PlayerTurn;
            if (GUILayout.Button("End Turn", GUILayout.Height(34f)))
            {
                battleManager.EndPlayerTurn();
            }

            GUI.enabled = true;
            if (GUILayout.Button("Restart Battle", GUILayout.Height(34f)))
            {
                battleManager.StartBattle();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawLog()
        {
            GUILayout.Label("Log");
            logScroll = GUILayout.BeginScrollView(logScroll, GUILayout.Height(180f));

            foreach (string line in logLines)
            {
                GUILayout.Label(line);
            }

            GUILayout.EndScrollView();
        }

        private void Subscribe()
        {
            if (battleManager == null)
            {
                return;
            }

            battleManager.BattleStarted += OnBattleStarted;
            battleManager.CardPlayed += OnCardPlayed;
            battleManager.EnemyIntentSelected += OnEnemyIntentSelected;
            battleManager.TurnStartStatusResolved += OnTurnStartStatusResolved;
            battleManager.BattleEnded += OnBattleEnded;
        }

        private void Unsubscribe()
        {
            if (battleManager == null)
            {
                return;
            }

            battleManager.BattleStarted -= OnBattleStarted;
            battleManager.CardPlayed -= OnCardPlayed;
            battleManager.EnemyIntentSelected -= OnEnemyIntentSelected;
            battleManager.TurnStartStatusResolved -= OnTurnStartStatusResolved;
            battleManager.BattleEnded -= OnBattleEnded;
        }

        private void ResolveBattleManager()
        {
            if (battleManager != null)
            {
                return;
            }

            battleManager = FindObjectOfType<BattleManager>();
        }

        private void OnBattleStarted(BattleManager manager)
        {
            AddLog("Battle started.");
        }

        private void OnCardPlayed(BattleManager manager, BattleRuntimeCard card)
        {
            AddLog("Played: " + card.DisplayName);
        }

        private void OnEnemyIntentSelected(BattleManager manager, EnemyAction intent)
        {
            AddLog("Enemy intent: " + (intent != null ? intent.GetIntentDescription() : "None"));
        }

        private void OnTurnStartStatusResolved(
            BattleManager manager,
            BattleCombatantState combatant,
            BattleStatusTurnResult result)
        {
            if (result.PoisonDamage > 0)
            {
                AddLog(combatant.DisplayName + " poison damage: " + result.PoisonDamage);
            }

            if (result.RegenerationHealing > 0)
            {
                AddLog(combatant.DisplayName + " regeneration: " + result.RegenerationHealing);
            }
        }

        private void OnBattleEnded(BattleManager manager, BattlePhase phase)
        {
            AddLog("Battle ended: " + phase);
        }

        private void AddLog(string line)
        {
            logLines.Add(line);

            if (logLines.Count > 30)
            {
                logLines.RemoveAt(0);
            }

            logScroll.y = float.MaxValue;
        }

        private static string FormatStatuses(BattleCombatantState combatant)
        {
            if (combatant == null || combatant.Statuses.Count == 0)
            {
                return "None";
            }

            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < combatant.Statuses.Count; i++)
            {
                BattleStatusInstance status = combatant.Statuses[i];
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(status.EffectType);
                builder.Append(" ");
                builder.Append(status.Stacks);

                if (status.HasTimedDuration)
                {
                    builder.Append(" (");
                    builder.Append(status.RemainingDuration);
                    builder.Append("T)");
                }
            }

            return builder.ToString();
        }
    }
}

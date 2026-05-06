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
        [SerializeField] private Texture2D backgroundTexture;
        [SerializeField] private Texture2D cardFrameTexture;
        [SerializeField] private Texture2D fightIconTexture;
        [SerializeField] private Texture2D shieldIconTexture;
        [SerializeField] private Texture2D manaIconTexture;
        [SerializeField] private Texture2D endTurnButtonTexture;
        [SerializeField] private Texture2D enemyPanelTexture;

        private readonly List<string> logLines = new List<string>();
        private Vector2 logScroll;
        private BattleRuntimeCard pendingTargetCard;

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

            DrawBackground();
            DrawBattlefield();

            Rect panelRect = new Rect(16f, 16f, 560f, Screen.height - 32f);
            GUILayout.BeginArea(panelRect, MakePanelStyle());

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

            DrawTargetArrow();
        }

        private void DrawBackground()
        {
            if (backgroundTexture != null)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), backgroundTexture, ScaleMode.ScaleAndCrop);
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0.08f, 0.11f, 0.14f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawBattlefield()
        {
            float centerY = Screen.height * 0.42f;
            Rect playerRect = new Rect(Screen.width * 0.08f, centerY - 70f, 180f, 140f);
            Rect enemyRect = GetEnemyRect();

            DrawUnitPanel(playerRect, "전사", battleManager != null ? battleManager.Player?.Combatant : null, new Color(0.22f, 0.55f, 0.75f, 0.9f));
            DrawUnitPanel(enemyRect, "적", battleManager != null ? battleManager.Enemy?.Combatant : null, new Color(0.75f, 0.25f, 0.24f, 0.9f));

            if (pendingTargetCard != null)
            {
                Color previousColor = GUI.color;
                GUI.color = new Color(1f, 0.15f, 0.12f, 1f);
                GUI.Label(new Rect(enemyRect.x - 10f, enemyRect.y - 36f, enemyRect.width + 20f, 30f), "TARGET", MakeCenteredLabelStyle(18));
                GUI.color = previousColor;
            }
        }

        private void DrawUnitPanel(Rect rect, string title, BattleCombatantState combatant, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, enemyPanelTexture != null ? enemyPanelTexture : Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;

            GUI.Label(new Rect(rect.x, rect.y + 12f, rect.width, 28f), title, MakeCenteredLabelStyle(20));

            if (combatant == null)
            {
                GUI.Label(new Rect(rect.x, rect.y + 52f, rect.width, 24f), "No Data", MakeCenteredLabelStyle(14));
                return;
            }

            GUI.Label(
                new Rect(rect.x, rect.y + 48f, rect.width, 24f),
                $"HP {combatant.CurrentHp}/{combatant.MaxHp}",
                MakeCenteredLabelStyle(16));
            GUI.Label(
                new Rect(rect.x, rect.y + 74f, rect.width, 24f),
                $"Block {combatant.Block}",
                MakeCenteredLabelStyle(16));
            GUI.Label(
                new Rect(rect.x, rect.y + 100f, rect.width, 24f),
                FormatStatuses(combatant),
                MakeCenteredLabelStyle(12));
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

            if (pendingTargetCard != null)
            {
                GUILayout.Label("공격 대상 선택 중: " + pendingTargetCard.DisplayName);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Enemy 선택", GUILayout.Height(32f)))
                {
                    PlaySelectedTargetCard();
                }

                if (GUILayout.Button("취소", GUILayout.Height(32f)))
                {
                    pendingTargetCard = null;
                }

                GUILayout.EndHorizontal();
            }

            for (int i = 0; i < hand.Count; i++)
            {
                BattleRuntimeCard card = hand[i];
                string icon = card.Data.cardType == CardType.Attack ? "ATK" : card.Data.cardType == CardType.Defense ? "DEF" : "SKL";
                string label = $"{icon} {card.DisplayName} ({card.EnergyCost}) - {card.Data.GetFormattedDescription()}";
                bool canPlay = battleManager.Phase == BattlePhase.PlayerTurn && battleManager.Player.CanPayEnergy(card);

                GUI.enabled = canPlay;
                if (GUILayout.Button(label, MakeCardButtonStyle(card), GUILayout.Height(42f)))
                {
                    if (RequiresEnemySelection(card))
                    {
                        pendingTargetCard = card;
                        AddLog("Select enemy target for " + card.DisplayName);
                    }
                    else
                    {
                        PlayCard(card);
                    }
                }

                GUI.enabled = true;
            }
        }

        private void DrawTurnControls()
        {
            GUILayout.BeginHorizontal();

            GUI.enabled = battleManager.Phase == BattlePhase.PlayerTurn;
            if (GUILayout.Button("End Turn", MakeEndTurnButtonStyle(), GUILayout.Height(42f)))
            {
                pendingTargetCard = null;
                battleManager.EndPlayerTurn();
            }

            GUI.enabled = true;
            if (GUILayout.Button("Restart Battle", GUILayout.Height(34f)))
            {
                pendingTargetCard = null;
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

        private void DrawTargetArrow()
        {
            if (pendingTargetCard == null)
            {
                return;
            }

            Rect enemyRect = GetEnemyRect();
            Vector2 start = new Vector2(Screen.width * 0.48f, Screen.height * 0.72f);
            Vector2 end = new Vector2(enemyRect.center.x, enemyRect.center.y);
            DrawArrow(start, end, new Color(1f, 0.08f, 0.05f, 0.92f));

            Rect targetButtonRect = new Rect(enemyRect.x, enemyRect.yMax + 10f, enemyRect.width, 42f);
            if (GUI.Button(targetButtonRect, "이 적을 공격", MakeEndTurnButtonStyle()))
            {
                PlaySelectedTargetCard();
            }
        }

        private void PlaySelectedTargetCard()
        {
            if (pendingTargetCard == null)
            {
                return;
            }

            BattleRuntimeCard card = pendingTargetCard;
            pendingTargetCard = null;
            PlayCard(card);
        }

        private void PlayCard(BattleRuntimeCard card)
        {
            BattleCardPlayResult result = battleManager.PlayCard(card);
            if (!result.Success)
            {
                AddLog("Failed to play " + card.DisplayName + ": " + result.FailureReason);
            }
        }

        private static bool RequiresEnemySelection(BattleRuntimeCard card)
        {
            return card != null
                && card.Data != null
                && (card.Data.cardType == CardType.Attack || card.Data.statusEffect != null);
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

            battleManager = FindFirstObjectByType<BattleManager>();
        }

        private void OnBattleStarted(BattleManager manager)
        {
            pendingTargetCard = null;
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

        private static Rect GetEnemyRect()
        {
            return new Rect(Screen.width * 0.72f, Screen.height * 0.34f, 190f, 145f);
        }

        private static void DrawArrow(Vector2 start, Vector2 end, Color color)
        {
            Vector2 direction = end - start;
            float length = direction.magnitude;
            if (length <= 0.01f)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - 10f, length - 34f, 20f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(start.x + length - 44f, start.y - 24f, 44f, 48f), Texture2D.whiteTexture);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private GUIStyle MakePanelStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = Texture2D.whiteTexture;
            style.normal.textColor = Color.white;
            style.padding = new RectOffset(12, 12, 12, 12);
            Color previousColor = GUI.color;
            GUI.color = new Color(0.02f, 0.03f, 0.04f, 0.78f);
            GUI.color = previousColor;
            return style;
        }

        private GUIStyle MakeCardButtonStyle(BattleRuntimeCard card)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.alignment = TextAnchor.MiddleLeft;
            style.fontSize = 13;
            style.padding = new RectOffset(12, 12, 4, 4);
            style.normal.background = cardFrameTexture != null ? cardFrameTexture : style.normal.background;
            style.hover.background = cardFrameTexture != null ? cardFrameTexture : style.hover.background;
            style.normal.textColor = Color.white;
            style.hover.textColor = Color.cyan;
            return style;
        }

        private GUIStyle MakeEndTurnButtonStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.fontSize = 18;
            style.fontStyle = FontStyle.Bold;
            style.normal.background = endTurnButtonTexture != null ? endTurnButtonTexture : style.normal.background;
            style.hover.background = endTurnButtonTexture != null ? endTurnButtonTexture : style.hover.background;
            style.normal.textColor = Color.white;
            style.hover.textColor = Color.yellow;
            return style;
        }

        private static GUIStyle MakeCenteredLabelStyle(int fontSize)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = fontSize;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            return style;
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

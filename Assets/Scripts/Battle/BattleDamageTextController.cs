using System.Collections;
using TMPro;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// BattleManager의 대미지, 회복, 회피 이벤트를 구독해
    /// 각 컴뱃턴트 머리 위에 TextAnimator가 가미된 대미지 텍스트 팝업을 동적 생성해주는 컨트롤러.
    /// </summary>
    public sealed class BattleDamageTextController : MonoBehaviour
    {
        [Header("팝업 설정")]
        [Tooltip("대미지 텍스트 팝업 프리팹 (생략 시 런타임에 기본 TMP로 생성)")]
        [SerializeField] private BattleDamageTextPopup popupPrefab;
        [Tooltip("사용할 TMP 폰트 에셋 (생략 시 기본 폰트 사용)")]
        [SerializeField] private TMP_FontAsset fontAsset;

        [Header("이펙트 스폰 위치")]
        [SerializeField] private RectTransform playerAvatarRect;
        [SerializeField] private RectTransform enemyAreaRect;

        [Header("텍스트 연출 크기")]
        [SerializeField] private float defaultFontSize = 5f;
        [SerializeField] private float heavyDamageFontSize = 6.8f;

        private BattleManager bm;
        private int prevEnemyHp;
        private int prevPlayerHp;
        private int prevEnemyBlock;
        private int prevPlayerBlock;

        // 동시 팝업 스태킹 방지를 위한 대상별 활성 팝업 리스트
        private readonly System.Collections.Generic.List<BattleDamageTextPopup> activePlayerPopups = new System.Collections.Generic.List<BattleDamageTextPopup>();
        private readonly System.Collections.Generic.List<BattleDamageTextPopup> activeEnemyPopups = new System.Collections.Generic.List<BattleDamageTextPopup>();

        private void Awake()
        {
            bm = GetComponent<BattleManager>();
            if (bm == null) bm = Object.FindFirstObjectByType<BattleManager>();
        }

        private void Start()
        {
            // playerAvatarRect나 enemyAreaRect가 null인 경우 BattleVfxController에서 리플렉션으로 자동 획득
            if (playerAvatarRect == null || enemyAreaRect == null)
            {
                var vfx = GetComponent<BattleVfxController>();
                if (vfx == null) vfx = Object.FindFirstObjectByType<BattleVfxController>();
                
                if (vfx != null)
                {
                    var type = vfx.GetType();
                    var playerField = type.GetField("playerAvatarRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var enemyField = type.GetField("enemyAreaRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (playerField != null && playerAvatarRect == null)
                    {
                        playerAvatarRect = playerField.GetValue(vfx) as RectTransform;
                    }
                    if (enemyField != null && enemyAreaRect == null)
                    {
                        enemyAreaRect = enemyField.GetValue(vfx) as RectTransform;
                    }
                }
            }
        }

        private void OnEnable()
        {
            if (bm == null) return;
            bm.BattleStarted += HandleBattleStarted;
            bm.CardPlayed += HandleCardPlayed;
            bm.PlayerTriggeredDamageResolved += HandlePlayerTriggeredDamageResolved;
            bm.TurnStartStatusResolved += HandleTurnStartStatus;
            bm.StateChanged += HandleStateChanged;
            bm.DodgeSucceeded += HandleDodgeSucceeded;
        }

        private void OnDisable()
        {
            if (bm == null) return;
            bm.BattleStarted -= HandleBattleStarted;
            bm.CardPlayed -= HandleCardPlayed;
            bm.PlayerTriggeredDamageResolved -= HandlePlayerTriggeredDamageResolved;
            bm.TurnStartStatusResolved -= HandleTurnStartStatus;
            bm.StateChanged -= HandleStateChanged;
            bm.DodgeSucceeded -= HandleDodgeSucceeded;
        }

        private void HandleBattleStarted(BattleManager m) => SnapshotHpAndBlock(m);

        private void HandleDodgeSucceeded(BattleManager m)
        {
            // 플레이어 회피 성공: Miss! 표시 (과하지 않게 좌우로 약하게 한 번 움직임 효과)
            SpawnText(
                "Miss!",
                GetWorldPos(playerAvatarRect) + new Vector3(0f, 0.8f, 0f),
                new Color(0.25f, 0.8f, 1.0f), // 청명한 하늘색
                defaultFontSize,
                true, // isHeavyOrCrit (좌우 한번 살짝 움직임 적용)
                true // isPlayerPopup
            );
            prevPlayerHp = m.Player?.Combatant.CurrentHp ?? prevPlayerHp;
            prevPlayerBlock = m.Player?.Combatant.Block ?? prevPlayerBlock;
        }

        private void HandleCardPlayed(BattleManager m, BattleRuntimeCard card)
        {
            int newEnemyHp = m.Enemy?.Combatant.CurrentHp ?? 0;
            int newPlayerHp = m.Player?.Combatant.CurrentHp ?? 0;
            int newEnemyBlock = m.Enemy?.Combatant.Block ?? 0;
            int newPlayerBlock = m.Player?.Combatant.Block ?? 0;

            // 1. 적 방어도 감소 감지
            int enemyBlockDamage = prevEnemyBlock - newEnemyBlock;
            if (enemyBlockDamage > 0)
            {
                SpawnBlockDamageText(enemyBlockDamage, GetWorldPos(enemyAreaRect) + new Vector3(0f, 0.8f, 0f), false);
            }

            // 2. 적 HP 피해 감지
            int enemyDamage = prevEnemyHp - newEnemyHp;
            if (enemyDamage > 0)
            {
                SpawnDamageText(enemyDamage, GetWorldPos(enemyAreaRect) + new Vector3(0f, 0.8f, 0f), false);
            }

            // 3. 플레이어 방어도 감소 감지
            int playerBlockDamage = prevPlayerBlock - newPlayerBlock;
            if (playerBlockDamage > 0)
            {
                SpawnBlockDamageText(playerBlockDamage, GetWorldPos(playerAvatarRect) + new Vector3(0f, 0.8f, 0f), true);
            }

            // 4. 플레이어 HP 피해 감지
            int selfDamage = prevPlayerHp - newPlayerHp;
            if (selfDamage > 0)
            {
                SpawnDamageText(selfDamage, GetWorldPos(playerAvatarRect) + new Vector3(0f, 0.8f, 0f), true);
            }

            SnapshotHpAndBlock(m);
        }

        private void HandlePlayerTriggeredDamageResolved(BattleManager m, int damage)
        {
            int newEnemyHp = m.Enemy?.Combatant.CurrentHp ?? 0;
            int newEnemyBlock = m.Enemy?.Combatant.Block ?? 0;

            // 적 방어도 감소 감지
            int enemyBlockDamage = prevEnemyBlock - newEnemyBlock;
            if (enemyBlockDamage > 0)
            {
                SpawnBlockDamageText(enemyBlockDamage, GetWorldPos(enemyAreaRect) + new Vector3(0f, 0.8f, 0f), false);
            }

            // 적 HP 피해 감지
            int enemyHpDamage = prevEnemyHp - newEnemyHp;
            if (enemyHpDamage > 0)
            {
                SpawnDamageText(enemyHpDamage, GetWorldPos(enemyAreaRect) + new Vector3(0f, 0.8f, 0f), false);
            }

            SnapshotHpAndBlock(m);
        }

        private void HandleTurnStartStatus(BattleManager m, BattleCombatantState combatant, BattleStatusTurnResult result)
        {
            bool isEnemy = (m.Enemy?.Combatant == combatant);
            Vector3 pos = isEnemy ? GetWorldPos(enemyAreaRect) : GetWorldPos(playerAvatarRect);
            pos += new Vector3(0f, 0.8f, 0f);
            bool isPlayerPopup = !isEnemy;

            // 독 또는 화상 상태이상 데미지 연출 (가벼운 wiggle 효과 적용)
            if (result.PoisonDamage > 0)
            {
                SpawnText(
                    $"<wiggle>-{result.PoisonDamage}</wiggle>",
                    pos,
                    new Color(0.65f, 0.35f, 0.85f), // 독보랏빛 색상
                    defaultFontSize,
                    false,
                    isPlayerPopup
                );
            }
            else if (result.BurnDamage > 0)
            {
                SpawnText(
                    $"<wiggle>-{result.BurnDamage}</wiggle>",
                    pos,
                    new Color(1.0f, 0.45f, 0.15f), // 이글거리는 불꽃주황
                    defaultFontSize,
                    false,
                    isPlayerPopup
                );
            }

            // 재생 치료 연출 (부드러운 float 플로팅만 적용)
            if (result.RegenerationHealing > 0)
            {
                SpawnText(
                    $"+{result.RegenerationHealing}",
                    pos,
                    new Color(0.2f, 0.85f, 0.35f), // 생기 넘치는 에메랄드 녹색
                    defaultFontSize,
                    false,
                    isPlayerPopup
                );
            }

            SnapshotHpAndBlock(m);
        }

        private void HandleStateChanged(BattleManager m)
        {
            // 적 턴 동안 플레이어가 입은 대미지 감지
            if (m.Phase != BattlePhase.EnemyTurn) return;

            int newPlayerHp = m.Player?.Combatant.CurrentHp ?? 0;
            int newPlayerBlock = m.Player?.Combatant.Block ?? 0;

            // 플레이어 방어도 감소 감지
            int playerBlockDamage = prevPlayerBlock - newPlayerBlock;
            if (playerBlockDamage > 0)
            {
                SpawnBlockDamageText(playerBlockDamage, GetWorldPos(playerAvatarRect) + new Vector3(0f, 0.8f, 0f), true);
            }

            // 플레이어 HP 피해 감지
            int damage = prevPlayerHp - newPlayerHp;
            if (damage > 0)
            {
                SpawnDamageText(damage, GetWorldPos(playerAvatarRect) + new Vector3(0f, 0.8f, 0f), true);
            }

            prevPlayerHp = newPlayerHp;
            prevPlayerBlock = newPlayerBlock;
        }

        private void SnapshotHpAndBlock(BattleManager m)
        {
            prevEnemyHp = m.Enemy?.Combatant.CurrentHp ?? 0;
            prevPlayerHp = m.Player?.Combatant.CurrentHp ?? 0;
            prevEnemyBlock = m.Enemy?.Combatant.Block ?? 0;
            prevPlayerBlock = m.Player?.Combatant.Block ?? 0;
        }

        /// <summary>
        /// 방어도를 깎는 공격에 대한 팝업 텍스트 소환.
        /// 몇 대미지가 들어가든 관계없이 무조건 10 미만 일반 대미지의 효과(isHeavyOrCrit = false)를 가짐.
        /// </summary>
        private void SpawnBlockDamageText(int amount, Vector3 position, bool isPlayer)
        {
            string rawText = $"-{amount} Block";
            Color color = new Color(0.45f, 0.7f, 1.0f); // 맑고 투명한 하늘색/파란색 톤
            float size = defaultFontSize;

            // 약간의 가로 오프셋 랜덤 부여로 연타 피해 겹침 방지
            position += new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.05f, 0.1f), 0f);

            // 방어도는 무조건 10 미만의 부드러운 float 플로팅만 적용
            SpawnText(rawText, position, color, size, false, isPlayer);
        }

        /// <summary>
        /// 대미지 수치 종류(치명타 여부, 플레이어/적 여부)에 따른 팝업 소환.
        /// 사용자의 피드백(10 미만 가만히 플로팅 후 소멸, 10 이상 좌우 진동 후 멈춰 소멸, 치명타 시 강제 붉은색 강타 연출)을 완벽 구현.
        /// </summary>
        private void SpawnDamageText(int amount, Vector3 position, bool isPlayer)
        {
            // 치명타 여부 수신 및 리셋
            bool isCritical = bm != null && bm.IsLastDamageCritical;
            if (isCritical)
            {
                bm.IsLastDamageCritical = false;
            }

            // 10 이상의 대미지 혹은 치명타일 때 좌우 진동 효과(isHeavyOrCrit) 적용
            bool isHeavyOrCrit = (amount >= 10) || isCritical;

            string rawText;
            Color color;
            float size;

            if (isPlayer)
            {
                // 플레이어가 받은 대미지: 붉은 계열 텍스트
                // 상하 움직임 방지를 위해 10 미만 일반 대미지는 얌전한 문자열로 구성
                rawText = $"-{amount}";
                color = isHeavyOrCrit ? new Color(1.0f, 0.15f, 0.15f) : new Color(0.9f, 0.35f, 0.35f);
                size = isHeavyOrCrit ? heavyDamageFontSize : defaultFontSize;
            }
            else
            {
                // 적이 받은 대미지
                if (isCritical)
                {
                    // 치명타 발생 시: 몇 데미지던지 상관없이 무조건 붉은색 강한 텍스트
                    rawText = $"-{amount}";
                    color = new Color(1.0f, 0.1f, 0.1f); // 강렬한 붉은색
                    size = heavyDamageFontSize;
                }
                else
                {
                    // 일반적인 적 타격 (10 이상은 주황/노랑 좌우진동, 10 미만은 부드러운 노랑 둥실 플로팅)
                    rawText = $"-{amount}";
                    color = isHeavyOrCrit ? new Color(1.0f, 0.75f, 0.0f) : new Color(0.95f, 0.85f, 0.15f);
                    size = isHeavyOrCrit ? heavyDamageFontSize : defaultFontSize;
                }
            }

            // 약간의 가로 오프셋 랜덤 부여로 연타 피해 겹침 방지
            position += new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.05f, 0.1f), 0f);

            SpawnText(rawText, position, color, size, isHeavyOrCrit, isPlayer);
        }

        /// <summary>
        /// 최종 팝업 스폰 및 설정 수행.
        /// 여러 개가 동시에 뜰 때 겹치지 않고 기존 텍스트 밑으로 수직 정렬해 겹침 현상을 완벽 방지.
        /// </summary>
        private void SpawnText(string text, Vector3 position, Color color, float size, bool isHeavyOrCrit, bool isPlayerPopup)
        {
            // 동시에 여러 개의 텍스트가 뜰 경우 기존 활성 텍스트들의 밑에 오프셋을 두어 정렬
            var activeList = isPlayerPopup ? activePlayerPopups : activeEnemyPopups;
            int activeCount = activeList.Count;
            float yOffset = activeCount * 0.32f; // 겹치지 않는 수직 간격 오프셋
            position.y -= yOffset;

            BattleDamageTextPopup popup;

            if (popupPrefab != null)
            {
                popup = Instantiate(popupPrefab, position, Quaternion.identity);
            }
            else
            {
                // 프리팹이 할당되지 않은 경우 런타임에 동적으로 컴포넌트 조합
                GameObject go = new GameObject("DamageTextPopup");
                go.transform.position = position;
                
                var tmp = go.AddComponent<TextMeshPro>();
                if (fontAsset != null)
                {
                    tmp.font = fontAsset;
                }

                popup = go.AddComponent<BattleDamageTextPopup>();
            }

            // 활성 목록에 등록 및 소멸 시 자동 청소 Action 위임
            activeList.Add(popup);
            popup.OnPopupDestroyed += (p) => {
                activeList.Remove(p);
            };

            popup.Setup(text, color, size, isHeavyOrCrit);
        }

        // ── Canvas 좌표의 월드 z=0 변환 헬퍼 ──
        private static Vector3 GetWorldPos(RectTransform rect)
        {
            if (rect == null || Camera.main == null) return Vector3.zero;

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                            ? canvas.worldCamera
                            : null;

            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, rect.position);
            float dist = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, dist));
            world.z = 0f;
            return world;
        }
    }
}

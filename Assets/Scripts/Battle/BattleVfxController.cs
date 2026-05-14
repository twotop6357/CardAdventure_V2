using System.Collections;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 배틀 씬의 타격·상태이상 파티클 이펙트를 관리한다.
    /// BattleManager 이벤트를 구독해 피해량·효과 종류에 맞는 프리팹을 자동 소환/파괴한다.
    /// </summary>
    public class BattleVfxController : MonoBehaviour
    {
        // ── 타격 이펙트 (피해량 기준) ────────────────────────────
        [Header("타격 이펙트 (피해량 기준)")]
        [Tooltip("1~3 피해: Basic Hit")]
        [SerializeField] private GameObject hitSmall;
        [Tooltip("4~9 피해: Basic Hit 2")]
        [SerializeField] private GameObject hitMedium;
        [Tooltip("10+ 피해: Basic Hit 7")]
        [SerializeField] private GameObject hitLarge;

        // ── 상태이상 부여 이펙트 ─────────────────────────────────
        [Header("상태이상 부여 이펙트")]
        [SerializeField] private GameObject fxPoison;
        [SerializeField] private GameObject fxBurn;
        [SerializeField] private GameObject fxWeak;
        [SerializeField] private GameObject fxVulnerable;
        [SerializeField] private GameObject fxStrength;
        [SerializeField] private GameObject fxRegen;
        [SerializeField] private GameObject fxDodge;

        // ── 턴 시작 틱 이펙트 ────────────────────────────────────
        [Header("턴 시작 틱 이펙트")]
        [Tooltip("독·화상 틱 피해")]
        [SerializeField] private GameObject fxPoisonTick;
        [Tooltip("재생 틱 회복")]
        [SerializeField] private GameObject fxRegenTick;

        // ── 위치 기준 RectTransform ───────────────────────────────
        [Header("이펙트 스폰 위치")]
        [SerializeField] private RectTransform playerAvatarRect;
        [SerializeField] private RectTransform enemyAreaRect;

        // ── 이펙트 크기 / 수명 ───────────────────────────────────
        [Header("이펙트 크기 / 수명")]
        [Tooltip("스폰 시 이펙트 전체 스케일 배율")]
        [SerializeField] private float effectScale    = 3f;
        [SerializeField] private float effectLifetime = 2f;

        // ── 내부 상태 ────────────────────────────────────────────
        private BattleManager bm;
        private int prevEnemyHp;
        private int prevPlayerHp;

        // ── 피해량 구간 ──────────────────────────────────────────
        private const int DmgMediumThreshold = 4;
        private const int DmgLargeThreshold  = 10;

        // ════════════════════════════════════════════════════════
        //  Unity 생명주기
        // ════════════════════════════════════════════════════════

        private void Awake()
        {
            bm = GetComponent<BattleManager>();
            if (bm == null) bm = FindFirstObjectByType<BattleManager>();
        }

        private void OnEnable()
        {
            if (bm == null) return;
            bm.BattleStarted             += HandleBattleStarted;
            bm.CardPlayed                += HandleCardPlayed;
            bm.TurnStartStatusResolved   += HandleTurnStartStatus;
            bm.StateChanged              += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (bm == null) return;
            bm.BattleStarted             -= HandleBattleStarted;
            bm.CardPlayed                -= HandleCardPlayed;
            bm.TurnStartStatusResolved   -= HandleTurnStartStatus;
            bm.StateChanged              -= HandleStateChanged;
        }

        // ════════════════════════════════════════════════════════
        //  이벤트 핸들러
        // ════════════════════════════════════════════════════════

        private void HandleBattleStarted(BattleManager m) => SnapshotHp(m);

        private void HandleCardPlayed(BattleManager m, BattleRuntimeCard card)
        {
            int newEnemyHp  = m.Enemy?.Combatant.CurrentHp ?? 0;
            int newPlayerHp = m.Player?.Combatant.CurrentHp ?? 0;

            // 적에게 입힌 피해
            int enemyDamage = prevEnemyHp - newEnemyHp;
            if (enemyDamage > 0)
                SpawnHitByDamage(enemyDamage, GetWorldPos(enemyAreaRect));

            // 자해 피해 (BerserkerAttack 등)
            int selfDamage = prevPlayerHp - newPlayerHp;
            if (selfDamage > 0)
                SpawnHitByDamage(selfDamage, GetWorldPos(playerAvatarRect));

            // 카드 종류에 따른 상태이상 이펙트
            SpawnStatusEffectForCard(card.Data, m);

            SnapshotHp(m);
        }

        private void HandleTurnStartStatus(BattleManager m, BattleCombatantState combatant, BattleStatusTurnResult result)
        {
            bool isEnemy = (m.Enemy?.Combatant == combatant);
            Vector3 pos  = isEnemy ? GetWorldPos(enemyAreaRect) : GetWorldPos(playerAvatarRect);

            if (result.PoisonDamage > 0 || result.BurnDamage > 0)
                Spawn(fxPoisonTick, pos);

            if (result.RegenerationHealing > 0)
                Spawn(fxRegenTick, pos);

            // 상태 처리 후 HP 스냅샷 갱신 (이후 적 공격과 혼동 방지)
            SnapshotHp(m);
        }

        private void HandleStateChanged(BattleManager m)
        {
            // 적 턴에 플레이어가 입은 피해만 감지
            if (m.Phase != BattlePhase.EnemyTurn) return;

            int newPlayerHp = m.Player?.Combatant.CurrentHp ?? 0;
            int damage      = prevPlayerHp - newPlayerHp;
            if (damage > 0)
                SpawnHitByDamage(damage, GetWorldPos(playerAvatarRect));

            prevPlayerHp = newPlayerHp;
        }

        // ════════════════════════════════════════════════════════
        //  상태이상 이펙트 선택
        // ════════════════════════════════════════════════════════

        private void SpawnStatusEffectForCard(CardData data, BattleManager m)
        {
            if (data == null) return;

            // 어느 쪽에 상태이상을 부여하는지 판별
            bool toEnemy = IsEnemyTargetEffect(data.effectType);
            Vector3 pos  = toEnemy ? GetWorldPos(enemyAreaRect) : GetWorldPos(playerAvatarRect);

            StatusEffectType? statusType = ResolveStatusType(data);
            if (statusType == null) return;

            GameObject prefab = statusType.Value switch
            {
                StatusEffectType.Poison       => fxPoison,
                StatusEffectType.Burn         => fxBurn,
                StatusEffectType.Weak         => fxWeak,
                StatusEffectType.Vulnerable   => fxVulnerable,
                StatusEffectType.Strength     => fxStrength,
                StatusEffectType.Regeneration => fxRegen,
                StatusEffectType.Dodge        => fxDodge,
                _                             => null
            };

            Spawn(prefab, pos);
        }

        private static StatusEffectType? ResolveStatusType(CardData data)
        {
            // statusEffect ScriptableObject가 연결된 경우 우선 사용
            if (data.statusEffect != null)
                return data.statusEffect.effectType;

            // effectType으로 추론
            return data.effectType switch
            {
                CardEffectType.GainStrength
                or CardEffectType.GainStrengthEqualCurrentBlock
                or CardEffectType.GrantEnemyStrengthAndRetaliateNext
                or CardEffectType.MultiHitAndGainStrength          => StatusEffectType.Strength,

                CardEffectType.ApplyPoisonWhenDamageDealt          => StatusEffectType.Poison,

                CardEffectType.AttackAndGainDodge
                or CardEffectType.GainDodgeWhenPlayingFreeCards
                or CardEffectType.MultiplyDodgeStacks
                or CardEffectType.DrawCardsGainDodgeOnFreeDraw     => StatusEffectType.Dodge,

                _ => null
            };
        }

        private static bool IsEnemyTargetEffect(CardEffectType t) =>
            t is CardEffectType.ApplyStatusToEnemy
              or CardEffectType.AttackAndApplyStatus
              or CardEffectType.DamageAndApplyStatus
              or CardEffectType.PoisonAndDetonateAllPoison
              or CardEffectType.Taunt
              or CardEffectType.GrantEnemyStrengthAndRetaliateNext;

        // ════════════════════════════════════════════════════════
        //  이펙트 소환 헬퍼
        // ════════════════════════════════════════════════════════

        private void SpawnHitByDamage(int damage, Vector3 pos)
        {
            GameObject prefab = damage >= DmgLargeThreshold  ? hitLarge
                              : damage >= DmgMediumThreshold ? hitMedium
                              :                                hitSmall;
            Spawn(prefab, pos);
        }

        private void Spawn(GameObject prefab, Vector3 pos)
        {
            if (prefab == null) return;
            GameObject go = Instantiate(prefab, pos, prefab.transform.rotation);
            go.transform.localScale = prefab.transform.localScale * effectScale;
            StartCoroutine(AutoDestroy(go, effectLifetime));
        }

        private IEnumerator AutoDestroy(GameObject go, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (go != null) Destroy(go);
        }

        private void SnapshotHp(BattleManager m)
        {
            prevEnemyHp  = m.Enemy?.Combatant.CurrentHp  ?? 0;
            prevPlayerHp = m.Player?.Combatant.CurrentHp ?? 0;
        }

        // ════════════════════════════════════════════════════════
        //  Canvas UI 위치 → 월드 좌표 변환
        //  Screen Space Overlay / Screen Space Camera 양쪽 지원
        // ════════════════════════════════════════════════════════

        private static Vector3 GetWorldPos(RectTransform rect)
        {
            if (rect == null || Camera.main == null) return Vector3.zero;

            // 렌더 모드에 따라 올바른 카메라로 스크린 좌표를 구한다
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCam  = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                            ? canvas.worldCamera
                            : null;

            // rect.position → 스크린 픽셀 좌표
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, rect.position);

            // 카메라 ~ z=0 사이의 거리를 depth로 사용 → 월드 z=0에 소환
            float dist  = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, dist));
            world.z = 0f;
            return world;
        }
    }
}

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 적의 HP 바, 방어막, 상태이상, 다음 행동 의도를 표시하는 UI 컴포넌트.
    /// 피격 시 흔들림·붉은 플래시 DOTween 연출을 포함한다.
    /// </summary>
    public class BattleEnemyView : MonoBehaviour
    {
        [Header("기본 정보")]
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("HP")]
        [SerializeField] private Slider          hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Image           hpFillImage;
        [SerializeField] private Color           hpFullColor  = new Color(0.20f, 0.75f, 0.30f);
        [SerializeField] private Color           hpLowColor   = new Color(0.85f, 0.20f, 0.20f);

        [Header("방어막")]
        [SerializeField] private GameObject      blockPanel;
        [SerializeField] private TextMeshProUGUI blockText;

        [Header("상태이상")]
        [SerializeField] private RectTransform   statusContainer;
        [SerializeField] private BattleStatusIconView statusIconPrefab;

        [Header("인텐트 (의도)")]
        [SerializeField] private GameObject      intentPanel;
        [SerializeField] private Image           intentIcon;
        [SerializeField] private TextMeshProUGUI intentText;
        [SerializeField] private Sprite          attackIntentSprite;
        [SerializeField] private Sprite          defendIntentSprite;
        [SerializeField] private Sprite          buffIntentSprite;
        [SerializeField] private Sprite          debuffIntentSprite;
        [SerializeField] private Sprite          healIntentSprite;
        [SerializeField] private Sprite          unknownIntentSprite;

        [Header("스프라이트")]
        [SerializeField] private Image           enemyImage;

        [Header("피격 연출")]
        [SerializeField] private float shakeDuration  = 0.3f;
        [SerializeField] private float shakeStrength  = 18f;
        [SerializeField] private int   shakeVibrato   = 20;
        [SerializeField] private float flashDuration  = 0.15f;

        // ── 캐시 ──────────────────────────────────────────────────
        private int lastHp = -1;

        // ── 공개 메서드 ────────────────────────────────────────────

        /// <summary>적 상태를 UI에 반영한다.</summary>
        public void Refresh(BattleEnemyState enemy)
        {
            if (enemy == null || enemy.Data == null) return;

            EnemyData data   = enemy.Data;
            BattleCombatantState c = enemy.Combatant;

            // 이름
            if (nameText != null) nameText.text = data.enemyName;

            // 스프라이트
            if (enemyImage != null && data.enemySprite != null)
            {
                enemyImage.sprite = data.enemySprite;
            }

            // HP
            int currentHp = c.CurrentHp;
            if (hpSlider != null)
            {
                hpSlider.maxValue = c.MaxHp;
                DOTween.To(() => hpSlider.value, v => hpSlider.value = v, currentHp, 0.3f)
                       .SetEase(Ease.OutQuad);
            }
            if (hpText != null) hpText.text = $"{currentHp} / {c.MaxHp}";

            // HP 색상
            if (hpFillImage != null)
            {
                float ratio = (float)currentHp / Mathf.Max(1, c.MaxHp);
                hpFillImage.color = Color.Lerp(hpLowColor, hpFullColor, ratio);
            }

            // 피격 연출
            if (lastHp >= 0 && currentHp < lastHp)
            {
                PlayHitAnimation();
            }
            lastHp = currentHp;

            // 방어막
            bool hasBlock = c.Block > 0;
            if (blockPanel != null) blockPanel.SetActive(hasBlock);
            if (blockText  != null) blockText.text = c.Block.ToString();

            // 상태이상
            RefreshStatusIcons(c);

            // 의도
            RefreshIntent(enemy.CurrentIntent);
        }

        /// <summary>전투 시작 시 초기화 (피격 플래시 캐시 리셋).</summary>
        public void ResetForBattle(BattleEnemyState enemy)
        {
            lastHp = enemy?.Combatant?.CurrentHp ?? -1;
            Refresh(enemy);
        }

        // ── 내부 ───────────────────────────────────────────────────

        private void RefreshIntent(EnemyAction intent)
        {
            if (intentPanel == null) return;

            if (intent == null)
            {
                intentPanel.SetActive(false);
                return;
            }

            intentPanel.SetActive(true);
            if (intentText != null) intentText.text = intent.GetIntentDescription();
            if (intentIcon != null) intentIcon.sprite = GetIntentSprite(intent.actionType);
        }

        private Sprite GetIntentSprite(EnemyActionType type) => type switch
        {
            EnemyActionType.Attack      => attackIntentSprite,
            EnemyActionType.Defend      => defendIntentSprite,
            EnemyActionType.Buff        => buffIntentSprite,
            EnemyActionType.DebuffPlayer => debuffIntentSprite,
            EnemyActionType.HealSelf    => healIntentSprite,
            _                           => unknownIntentSprite,
        };

        private void RefreshStatusIcons(BattleCombatantState combatant)
        {
            if (statusContainer == null) return;

            foreach (Transform child in statusContainer)
            {
                Destroy(child.gameObject);
            }

            if (statusIconPrefab == null || combatant == null) return;

            foreach (BattleStatusInstance status in combatant.Statuses)
            {
                if (status.IsExpired) continue;
                BattleStatusIconView icon = Instantiate(statusIconPrefab, statusContainer);
                icon.Bind(status);
            }
        }

        /// <summary>적 행동 직전 호출 — 행동 타입에 맞는 예고 애니메이션을 재생한다.</summary>
        public void PlayActionAnimation(EnemyActionType actionType)
        {
            if (enemyImage == null) return;
            DOTween.Kill(enemyImage.rectTransform);

            switch (actionType)
            {
                case EnemyActionType.Attack:
                    // 플레이어 쪽(왼쪽)으로 전진했다가 복귀
                    enemyImage.rectTransform
                        .DOLocalMoveX(enemyImage.rectTransform.localPosition.x - 80f, 0.2f)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                            enemyImage.rectTransform
                                .DOLocalMoveX(enemyImage.rectTransform.localPosition.x + 80f, 0.25f)
                                .SetEase(Ease.OutBounce));
                    break;

                case EnemyActionType.Defend:
                    // 파란 빛 펄스 + 살짝 커졌다 복귀
                    DOTween.Sequence()
                        .Append(enemyImage.DOColor(new Color(0.4f, 0.7f, 1f), 0.15f))
                        .Append(enemyImage.DOColor(Color.white, 0.25f));
                    enemyImage.rectTransform
                        .DOScale(Vector3.one * 1.1f, 0.2f).SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                            enemyImage.rectTransform.DOScale(Vector3.one, 0.2f));
                    break;

                case EnemyActionType.Buff:
                    // 황금빛 펄스
                    DOTween.Sequence()
                        .Append(enemyImage.DOColor(new Color(1f, 0.85f, 0.1f), 0.2f))
                        .Append(enemyImage.DOColor(Color.white, 0.3f));
                    enemyImage.rectTransform
                        .DOScale(Vector3.one * 1.15f, 0.25f).SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                            enemyImage.rectTransform.DOScale(Vector3.one, 0.2f));
                    break;

                case EnemyActionType.HealSelf:
                    // 초록빛 펄스
                    DOTween.Sequence()
                        .Append(enemyImage.DOColor(new Color(0.3f, 1f, 0.4f), 0.2f))
                        .Append(enemyImage.DOColor(Color.white, 0.3f));
                    break;

                case EnemyActionType.DebuffPlayer:
                    // 보라빛 진동
                    DOTween.Sequence()
                        .Append(enemyImage.DOColor(new Color(0.8f, 0.3f, 1f), 0.2f))
                        .Append(enemyImage.DOColor(Color.white, 0.3f));
                    enemyImage.rectTransform
                        .DOShakePosition(0.4f, 6f, 12, 90f, false, true);
                    break;
            }
        }

        private void PlayHitAnimation()
        {
            if (enemyImage != null)
            {
                enemyImage.rectTransform
                    .DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true)
                    .SetEase(Ease.OutQuad);

                DOTween.Sequence()
                    .Append(enemyImage.DOColor(Color.red,   flashDuration))
                    .Append(enemyImage.DOColor(Color.white, flashDuration));
            }
        }
    }
}

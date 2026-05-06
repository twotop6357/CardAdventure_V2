using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 어드벤처 씬에서 플레이어가 접촉하면 지정된 적과의 전투 씬으로 전환하는 트리거.
    /// Collider2D를 IsTrigger=true로 설정하고 이 컴포넌트를 추가한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BattleEntrance : MonoBehaviour
    {
        [Header("전투 설정")]
        [SerializeField] private EnemyData enemyData;

        [Header("비주얼")]
        [Tooltip("이미 클리어한 전투는 비활성화할 스프라이트/오브젝트들")]
        [SerializeField] private GameObject enemyVisual;

        [Header("진행 상태")]
        [SerializeField] private bool isDefeated;

        private void Start()
        {
            // 이미 처치한 적은 비주얼 숨기고 트리거 비활성화
            if (isDefeated)
                SetDefeated();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isDefeated) return;
            if (!other.CompareTag("Player")) return;
            if (SceneLoader.Instance == null) return;
            if (enemyData == null)
            {
                Debug.LogWarning("[BattleEntrance] EnemyData가 연결되지 않았습니다.");
                return;
            }

            // 현재 씬 이름을 복귀 씬으로 설정
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            SceneLoader.Instance.EnterBattle(enemyData, currentScene);
        }

        // ── 공개 API ───────────────────────────────────────────

        /// <summary>
        /// 배틀 승리 후 이 입구를 영구적으로 비활성화한다.
        /// BattleResultHandler 등에서 호출.
        /// </summary>
        public void MarkDefeated()
        {
            isDefeated = true;
            SetDefeated();
        }

        private void SetDefeated()
        {
            if (enemyVisual != null) enemyVisual.SetActive(false);
            // 트리거 콜라이더 비활성화
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        // ── 에디터 기즈모 ──────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (isDefeated) return;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Collider2D col = GetComponent<Collider2D>();
            if (col is CircleCollider2D circle)
                Gizmos.DrawSphere(transform.position, circle.radius);
            else
                Gizmos.DrawCube(transform.position, Vector3.one);

            // 적 이름 레이블
            if (enemyData != null)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.6f,
                    enemyData.enemyName);
            }
        }
#endif
    }
}

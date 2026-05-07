using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// 전직관 NPC의 시점을 플레이어 방향으로 맞춰주는 스크립트.
    /// 플레이어와 가장 멀리 떨어진 축(가로 또는 세로)을 기준으로 4방향 중 하나를 바라보도록 설정합니다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class JobChangerNpc : MonoBehaviour
    {
        private Animator animator;
        private Transform playerTransform;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null || animator == null)
            {
                return;
            }

            Vector2 dir = playerTransform.position - transform.position;
            
            // 거리가 너무 가까우면 회전하지 않음 (선택적)
            if (dir.sqrMagnitude < 0.01f)
            {
                return;
            }

            // 플레이어가 가장 멀리 떨어진 축을 찾아 해당 방향을 결정
            Vector2 faceDir = Vector2.down; // 기본값: 아래
            
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                faceDir = dir.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                faceDir = dir.y > 0 ? Vector2.up : Vector2.down;
            }

            // 애니메이터의 파라미터 업데이트 (블렌드 트리에 맞게)
            animator.SetFloat("DirectionX", faceDir.x);
            animator.SetFloat("DirectionY", faceDir.y);
            
            // 만약 NPC의 비주얼이 스프라이트를 뒤집어야 한다면 여기서 처리할 수 있습니다.
            // 하지만 보통 전후좌우 4방향 애니메이션이 개별적으로 있다면 FlipX가 필요 없습니다.
            // 필요 시 아래 주석 해제.
            // if (faceDir.x != 0) 
            // {
            //     SpriteRenderer sr = GetComponent<SpriteRenderer>();
            //     if (sr != null) sr.flipX = faceDir.x < 0;
            // }
        }
    }
}

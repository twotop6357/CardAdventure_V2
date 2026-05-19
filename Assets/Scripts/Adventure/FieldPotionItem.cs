using UnityEngine;
using TMPro;
using DG.Tweening;

namespace CardAdventure
{
    /// <summary>
    /// 모험 필드 상에 배치되어 플레이어가 밟았을 때 획득하고 
    /// 프리미엄 애니메이션(통통 튀기 + 페이드) 및 플로팅 텍스트를 띄우는 컴포넌트입니다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class FieldPotionItem : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("획득 시 플로팅 텍스트가 표시될 Y축 오프셋")]
        [SerializeField] private float textSpawnOffsetY = 0.5f;

        private SpriteRenderer spriteRenderer;
        private Collider2D itemCollider;
        private bool isPickedUp = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            itemCollider = GetComponent<Collider2D>();
            
            // 트리거 충돌 활성화 보장
            itemCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 플레이어 태그 또는 PlayerController 컴포넌트로 플레이어 검증
            if (isPickedUp) return;

            // PlayerController가 붙어있는지 확인
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null)
            {
                player = other.GetComponent<PlayerController>();
            }

            if (player != null)
            {
                PickUp();
            }
        }

        private void PickUp()
        {
            isPickedUp = true;
            itemCollider.enabled = false; // 추가 충돌 방지

            // 1. 포션 개수 증가
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.PotionCount += 1;
                Debug.Log($"[FieldPotionItem] 포션을 획득했습니다! 현재 포션 개수: {GameDataManager.Instance.PotionCount}");
            }

            // 2. 프리미엄 획득 연출 (통통 튀며 페이드아웃)
            transform.DOMoveY(transform.position.y + 0.8f, 0.4f).SetEase(Ease.OutQuad);
            spriteRenderer.DOFade(0f, 0.4f).OnComplete(() =>
            {
                Destroy(gameObject);
            });

            // 3. 플로팅 텍스트 동적 생성
            CreateFloatingText();
        }

        private void CreateFloatingText()
        {
            // 월드 스페이스 TextMeshPro 오브젝트 동적 빌드
            GameObject textGo = new GameObject("FloatingPotionText");
            textGo.transform.position = transform.position + new Vector3(0f, textSpawnOffsetY, 0f);

            TextMeshPro tmp = textGo.AddComponent<TextMeshPro>();
            tmp.text = "+1 포션 획득!";
            tmp.fontSize = 5.5f;
            tmp.color = new Color(0.98f, 0.84f, 0.3f); // 골드 옐로우 색상
            tmp.alignment = TextAlignmentOptions.Center;
            
            // 한국어 폰트가 훼손되지 않도록 기본 폰트 또는 TextMeshPro 에셋 적용 시도
            // (TMP 기본 설정이 되어 있을 것이므로 특별히 강제 지정하지 않아도 잘 렌더링됩니다)
            tmp.fontStyle = FontStyles.Bold;

            // Sorting Layer 설정 (캐릭터보다 위에 오도록)
            MeshRenderer meshRenderer = tmp.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingLayerName = "UI"; 
                meshRenderer.sortingOrder = 500;
            }

            // 플로팅 애니메이션
            textGo.transform.DOMoveY(textGo.transform.position.y + 0.7f, 0.6f).SetEase(Ease.OutCubic);
            
            // 텍스트 서서히 흐려짐 연출
            DOTween.To(() => tmp.color, x => tmp.color = x, new Color(tmp.color.r, tmp.color.g, tmp.color.b, 0f), 0.6f)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    Destroy(textGo);
                });
        }
    }
}

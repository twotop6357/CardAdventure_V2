using DG.Tweening;
using Febucci.UI;
using TMPro;
using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// Febucci TextAnimator 및 TextMeshPro를 활용해
    /// 대미지, 회복, 상태이상 틱, 회피 등의 수치를 시각적으로 출력하고 소멸시키는 팝업 텍스트 컴포넌트.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class BattleDamageTextPopup : MonoBehaviour
    {
        private TextMeshPro tmpText;
        private TextAnimator_TMP textAnimator;

        private void Awake()
        {
            tmpText = GetComponent<TextMeshPro>();
            
            // TextAnimator가 동적으로 활성화되기 위해 컴포넌트가 없을 경우 자동 탑재
            if (!TryGetComponent(out textAnimator))
            {
                textAnimator = gameObject.AddComponent<TextAnimator_TMP>();
            }

            // 월드 스페이스 3D 텍스트 설정
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontSize = 5.5f; // 적당한 기본 폰트 크기
            
            // 머티리얼 렌더링 우선순위를 위해 앞쪽으로 당김
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 100; // 파티클이나 캐릭터 스프라이트보다 앞에 렌더링
            }
        }

        public System.Action<BattleDamageTextPopup> OnPopupDestroyed;

        /// <summary>
        /// 텍스트 연출 데이터 설정 및 팝업 애니메이션 시작.
        /// 사용자의 피드백을 수용하여 과하지 않고 극도로 고급스럽고 절제된 흐름으로 구현됨.
        /// </summary>
        /// <param name="rawText">TextAnimator 태그가 포함된 리치 텍스트 (예: "-15")</param>
        /// <param name="color">폰트 기본 색상</param>
        /// <param name="fontSize">폰트 크기 배율</param>
        /// <param name="isHeavyOrCrit">10 이상의 대미지 혹은 치명타 여부 (좌우 진동 연출용)</param>
        public void Setup(string rawText, Color color, float fontSize = 5.5f, bool isHeavyOrCrit = false)
        {
            tmpText.color = color;
            tmpText.fontSize = fontSize;

            // TextAnimator_TMP에 텍스트 주입
            textAnimator.SetText(rawText);

            // 초기 스케일 및 오프셋 초기화
            transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();
            
            // 1. 크기가 부드럽게 나타남 (Fade/Scale Ease-out)
            seq.Append(transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad));
            
            // 2. 대미지 성격에 따른 연출 분기
            if (isHeavyOrCrit)
            {
                // [강한 피해 / 치명타]: 텍스트 전체가 좌우로 한 번 약하게 움직인(1회 왕복) 후 제자리에 멈춰 서서히 페이드 아웃
                float startX = transform.position.x;
                
                // 오른쪽으로 약하게 (0.08초) -> 왼쪽으로 살짝 (0.12초) -> 원래 위치로 (0.08초)
                seq.Append(transform.DOMoveX(startX + 0.12f, 0.08f).SetEase(Ease.OutQuad));
                seq.Append(transform.DOMoveX(startX - 0.08f, 0.12f).SetEase(Ease.InOutQuad));
                seq.Append(transform.DOMoveX(startX, 0.08f).SetEase(Ease.InQuad));
                
                // 좌우 왕복 종료 후 아주 미세하게만 둥실 떠오르며(0.35 유닛) 대기하다 사라짐
                seq.Append(transform.DOMoveY(transform.position.y + 0.35f, 0.62f).SetEase(Ease.OutQuad));
            }
            else
            {
                // [일반 피해 (10 미만)]: 튀거나 흔들리는 움직임 전혀 없이, 
                // 가만히 플로팅(둥실 떠오름 - 0.45 유닛)하고 있다가 서서히 페이드 아웃
                seq.Append(transform.DOMoveY(transform.position.y + 0.45f, 0.9f).SetEase(Ease.OutQuad));
            }
            
            // 3. 서서히 페이드 아웃 (0.5초 시점부터 자연스럽게 사라짐)
            seq.Insert(0.55f, tmpText.DOFade(0f, 0.45f));
            
            // 4. 완료 시 자동 삭제
            seq.OnComplete(() => Destroy(gameObject));
        }

        private void OnDestroy()
        {
            OnPopupDestroyed?.Invoke(this);
        }
    }
}

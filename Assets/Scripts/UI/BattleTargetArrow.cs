using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// 공격 카드 선택 시 카드 위치에서 마우스 포인터까지 이어지는
    /// 베지어 곡선 화살표 UI.
    ///
    /// 사용법:
    ///   arrow.Show(startWorldPos);   // 화살표 활성화 + 시작점 설정
    ///   arrow.Hide();                // 화살표 비활성화
    ///   (Update는 내부에서 자동으로 마우스를 추적)
    ///
    /// 씬 구조:
    ///   Canvas > TargetArrow (이 컴포넌트)
    ///     ├─ Segment_0 (Image)   ← arrowSegments[0]
    ///     ├─ Segment_1 (Image)
    ///     ├─ ...
    ///     └─ ArrowHead (Image)   ← arrowHead
    /// </summary>
    public class BattleTargetArrow : MonoBehaviour
    {
        [Header("세그먼트 설정")]
        [Tooltip("화살표를 구성하는 세그먼트 Image 배열 (순서대로 시작→끝)")]
        [SerializeField] private RectTransform[] segments;
        [Tooltip("화살 끝 삼각형 Image")]
        [SerializeField] private RectTransform arrowHead;

        [Header("곡선 설정")]
        [Tooltip("세그먼트 두께 (픽셀)")]
        [SerializeField] private float segmentWidth = 28f;
        [Tooltip("베지어 제어점 높이 오프셋 (중간에서 얼마나 호가 올라가는지)")]
        [SerializeField] private float curveHeight = 120f;
        [Tooltip("화살촉 크기 배율")]
        [SerializeField] private float headScale = 1.6f;

        [Header("색상 / 펄스")]
        [SerializeField] private Color arrowColor     = new Color(0.85f, 0.10f, 0.10f, 1f);
        [SerializeField] private Color arrowColorFade = new Color(0.85f, 0.10f, 0.10f, 0.3f);
        [Tooltip("세그먼트가 시작→끝으로 흘러가는 펄스 주기 (초)")]
        [SerializeField] private float pulsePeriod    = 0.5f;

        [Header("Canvas 참조")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera uiCamera;      // Screen Space Camera 모드면 필요

        // ── 내부 ───────────────────────────────────────────────────
        private Vector2 startScreenPos;
        private bool    isActive;
        private float   pulseTimer;

        // ── 공개 API ───────────────────────────────────────────────

        /// <summary>화살표를 보이게 하고 시작점을 설정한다.</summary>
        public void Show(Vector3 startWorldPos)
        {
            // isActive를 먼저 설정해야 한다.
            // TargetArrow가 Inspector에서 비활성으로 시작하면 SetActive(true) 시점에
            // Awake()가 처음 실행되는데, Awake() 내부의 SetActive(false) 호출을
            // isActive 플래그로 차단해야 첫 클릭에도 화살표가 표시된다.
            isActive = true;
            pulseTimer = 0f;
            gameObject.SetActive(true);

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = uiCamera != null ? uiCamera : canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

            startScreenPos = RectTransformUtility.WorldToScreenPoint(cam, startWorldPos);

            SetSegmentVisibility(true);
            UpdateCurve(startScreenPos, Input.mousePosition);
            UpdatePulse();
        }

        /// <summary>화살표를 숨긴다.</summary>
        public void Hide()
        {
            isActive = false;
            gameObject.SetActive(false);
        }

        // ── 라이프사이클 ───────────────────────────────────────────

        private void Awake()
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
            // Canvas가 없으면 부모에서 검색
            if (canvas == null) canvas = GetComponentInParent<Canvas>();

            // Show()가 이미 isActive=true로 설정한 경우(Inspector 비활성 오브젝트의 첫 활성화)
            // SetActive(false)를 호출하면 화살표가 즉시 숨겨지므로 건너뛴다.
            if (!isActive)
                gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!isActive || segments == null || segments.Length == 0) return;

            Vector2 mouseScreen = Input.mousePosition;
            UpdateCurve(startScreenPos, mouseScreen);
            UpdatePulse();
        }

        // ── 곡선 업데이트 ──────────────────────────────────────────

        private void UpdateCurve(Vector2 p0Screen, Vector2 p2Screen)
        {
            // 베지어 제어점: 두 점의 중간에서 수직으로 curveHeight 올린 위치
            Vector2 mid = (p0Screen + p2Screen) * 0.5f;
            Vector2 dir = (p2Screen - p0Screen).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x); // 수직 방향
            Vector2 p1Screen = mid + perp * curveHeight;

            int count = segments.Length;
            for (int i = 0; i < count; i++)
            {
                if (segments[i] == null) continue;

                // 이 세그먼트의 베지어 t 값 (중간 t)
                float t0 = (float)i / count;
                float t1 = (float)(i + 1) / count;
                float tMid = (t0 + t1) * 0.5f;

                Vector2 posScreen = Bezier(p0Screen, p1Screen, p2Screen, tMid);
                Vector2 tangent   = BezierTangent(p0Screen, p1Screen, p2Screen, tMid).normalized;

                // 스크린 좌표 → Canvas(로컬) 좌표
                Vector2 localPos = ScreenToCanvasLocal(posScreen);
                segments[i].localPosition = localPos;

                // 회전: tangent 방향으로
                float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                segments[i].localRotation = Quaternion.Euler(0f, 0f, angle);

                // 크기: 세그먼트 사이 간격에 맞게 길이 조정
                Vector2 posA = Bezier(p0Screen, p1Screen, p2Screen, t0);
                Vector2 posB = Bezier(p0Screen, p1Screen, p2Screen, t1);
                float   segLen = Vector2.Distance(posA, posB);
                segments[i].sizeDelta = new Vector2(segLen + 2f, segmentWidth);
            }

            // 화살촉
            if (arrowHead != null)
            {
                Vector2 headScreen = p2Screen;
                Vector2 headLocal  = ScreenToCanvasLocal(headScreen);
                arrowHead.localPosition = headLocal;

                Vector2 tailTangent = BezierTangent(p0Screen, p1Screen, p2Screen, 1f).normalized;
                float headAngle = Mathf.Atan2(tailTangent.y, tailTangent.x) * Mathf.Rad2Deg;
                arrowHead.localRotation = Quaternion.Euler(0f, 0f, headAngle);
                arrowHead.localScale    = Vector3.one * headScale;
            }
        }

        // ── 펄스 연출 ──────────────────────────────────────────────

        private void UpdatePulse()
        {
            pulseTimer += Time.deltaTime;
            float phase = (pulseTimer % pulsePeriod) / pulsePeriod; // 0..1

            int count = segments == null ? 0 : segments.Length;
            for (int i = 0; i < count; i++)
            {
                if (segments[i] == null) continue;
                Image img = segments[i].GetComponent<Image>();
                if (img == null) continue;

                // 각 세그먼트마다 phase 오프셋을 두어 시작→끝으로 흐르는 효과
                float segPhase = ((float)i / count + phase) % 1f;
                float alpha = Mathf.Sin(segPhase * Mathf.PI); // 0→1→0
                img.color = Color.Lerp(arrowColorFade, arrowColor, alpha);
            }

            // 화살촉은 항상 진한 색
            if (arrowHead != null)
            {
                Image img = arrowHead.GetComponent<Image>();
                if (img != null) img.color = arrowColor;
            }
        }

        private void SetSegmentVisibility(bool visible)
        {
            if (segments != null)
            {
                foreach (RectTransform seg in segments)
                {
                    if (seg != null) seg.gameObject.SetActive(visible);
                }
            }
            if (arrowHead != null) arrowHead.gameObject.SetActive(visible);
        }

        // ── 좌표 변환 ──────────────────────────────────────────────

        private Vector2 ScreenToCanvasLocal(Vector2 screenPos)
        {
            if (canvas == null) return screenPos;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null) return screenPos;

            Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : uiCamera ?? Camera.main;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, cam, out Vector2 localPoint);
            return localPoint;
        }

        // ── 베지어 수식 ────────────────────────────────────────────

        private static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private static Vector2 BezierTangent(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            // d/dt of quadratic bezier
            return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
        }
    }
}

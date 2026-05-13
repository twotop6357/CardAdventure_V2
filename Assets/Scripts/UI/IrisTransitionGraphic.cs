using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    /// <summary>
    /// Fullscreen black overlay with a circular transparent hole.
    /// HoleRadiusNormalized is relative to half of the screen diagonal.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class IrisTransitionGraphic : Graphic
    {
        [SerializeField, Range(0f, 1.5f)] private float holeRadiusNormalized = 1.15f;
        [SerializeField, Range(16, 192)] private int segments = 96;
        [SerializeField] private Vector2 centerNormalized = new Vector2(0.5f, 0.5f);

        public float HoleRadiusNormalized
        {
            get => holeRadiusNormalized;
            set
            {
                float clamped = Mathf.Clamp(value, 0f, 1.5f);
                if (Mathf.Approximately(holeRadiusNormalized, clamped)) return;
                holeRadiusNormalized = clamped;
                SetVerticesDirty();
            }
        }

        public Vector2 CenterNormalized
        {
            get => centerNormalized;
            set
            {
                centerNormalized = new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y));
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            int segmentCount = Mathf.Max(16, segments);
            Vector2 center = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, centerNormalized.x),
                Mathf.Lerp(rect.yMin, rect.yMax, centerNormalized.y));

            float halfDiagonal = Mathf.Sqrt(rect.width * rect.width + rect.height * rect.height) * 0.5f;
            float innerRadius = Mathf.Max(0f, holeRadiusNormalized * halfDiagonal);
            float outerRadius = halfDiagonal * 3f;
            Color32 vertexColor = color;

            for (int i = 0; i < segmentCount; i++)
            {
                float angleA = Mathf.PI * 2f * i / segmentCount;
                float angleB = Mathf.PI * 2f * (i + 1) / segmentCount;
                Vector2 dirA = new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA));
                Vector2 dirB = new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB));

                int baseIndex = vh.currentVertCount;
                AddVertex(vh, center + dirA * innerRadius, vertexColor);
                AddVertex(vh, center + dirB * innerRadius, vertexColor);
                AddVertex(vh, center + dirB * outerRadius, vertexColor);
                AddVertex(vh, center + dirA * outerRadius, vertexColor);

                vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
                vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
            }
        }

        private static void AddVertex(VertexHelper vh, Vector2 position, Color32 vertexColor)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            vh.AddVert(vertex);
        }
    }
}

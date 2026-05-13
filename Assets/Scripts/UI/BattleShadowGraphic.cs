using UnityEngine;
using UnityEngine.UI;

namespace CardAdventure
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleShadowGraphic : Graphic
    {
        [SerializeField, Range(12, 96)] private int segments = 48;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            float radiusX = rect.width * 0.5f;
            float radiusY = rect.height * 0.5f;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = center;
            vh.AddVert(vertex);

            int count = Mathf.Max(12, segments);
            for (int i = 0; i <= count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / count;
                vertex.position = new Vector3(
                    center.x + Mathf.Cos(angle) * radiusX,
                    center.y + Mathf.Sin(angle) * radiusY,
                    0f);
                vh.AddVert(vertex);
            }

            for (int i = 1; i <= count; i++)
            {
                vh.AddTriangle(0, i, i + 1);
            }
        }
    }
}

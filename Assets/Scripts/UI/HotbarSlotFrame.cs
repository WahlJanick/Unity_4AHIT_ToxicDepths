using UnityEngine;
using UnityEngine.UI;

namespace ToxicDepths.UI
{
    [AddComponentMenu("Toxic Depths/UI/Hotbar Slot Frame")]
    public sealed class HotbarSlotFrame : MaskableGraphic
    {
        [SerializeField, Min(0f)] private float cornerRadius = 7f;
        [SerializeField, Min(0f)] private float borderWidth = 2f;
        [SerializeField] private Color borderColor = new Color(0.25f, 0.32f, 0.27f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.15f, 0.85f, 0.25f, 1f);
        [SerializeField] private bool selected;

        public void SetSelected(bool value)
        {
            if (selected == value) return;
            selected = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;

            Color edge = selected ? selectedColor : borderColor;
            edge.a *= color.a;
            AddRoundedRect(vertices, rect, cornerRadius, edge);
            float inset = Mathf.Min(borderWidth, Mathf.Min(rect.width, rect.height) * 0.5f);
            Rect inner = new Rect(rect.x + inset, rect.y + inset,
                rect.width - inset * 2f, rect.height - inset * 2f);
            Color background = selected ? Color.Lerp(color, selectedColor, 0.1f) : color;
            background.a = color.a;
            AddRoundedRect(vertices, inner, Mathf.Max(0f, cornerRadius - inset), background);
        }

        private static void AddRoundedRect(VertexHelper vertices, Rect rect, float radius, Color tint)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            const int segments = 6;
            const int perimeterCount = 4 * (segments + 1);
            int start = vertices.currentVertCount;
            vertices.AddVert(rect.center, tint, new Vector2(0.5f, 0.5f));
            for (int corner = 0; corner < 4; corner++)
            {
                float cx = corner < 2 ? rect.xMax - radius : rect.xMin + radius;
                float cy = corner == 0 || corner == 3 ? rect.yMax - radius : rect.yMin + radius;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float angle = (90f - corner * 90f - segment * 90f / segments) * Mathf.Deg2Rad;
                    Vector2 position = new Vector2(cx + Mathf.Cos(angle) * radius,
                        cy + Mathf.Sin(angle) * radius);
                    vertices.AddVert(position, tint, Vector2.zero);
                }
            }
            for (int i = 0; i < perimeterCount; i++)
                vertices.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % perimeterCount);
        }
    }
}

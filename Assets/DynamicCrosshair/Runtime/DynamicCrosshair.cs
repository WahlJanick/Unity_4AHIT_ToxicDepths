using UnityEngine;
using UnityEngine.UI;

namespace ToxicDepths.UI
{
    [AddComponentMenu("Toxic Depths/UI/Dynamic Crosshair")]
    [DisallowMultipleComponent]
    public sealed class DynamicCrosshair : MaskableGraphic
    {
        [Header("Form (Pixel bei Referenzaufloesung)")]
        [SerializeField, Min(0f)] private float gap = 10f;
        [SerializeField, Min(0f)] private float lineLength = 8f;
        [SerializeField, Min(0.5f)] private float lineWidth = 2f;
        [SerializeField, Min(0f)] private float centerSize = 2f;
        [SerializeField, Min(0f)] private float outlineWidth = 1f;
        [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 0.85f);

        [Header("Schlag")]
        [Tooltip("Groesse am Trefferpunkt im Verhaeltnis zur normalen Groesse.")]
        [SerializeField, Range(0.05f, 1f)] private float strikeScale = 0.3f;
        [Tooltip("Trefferzeit innerhalb der Animation: 0 = Anfang, 1 = Ende.")]
        [SerializeField, Range(0.01f, 0.99f)] private float impactTime = 0.45f;
        [Tooltip("Dauer fuer die Vorschau bzw. PlaySwing ohne Zeitangabe.")]
        [SerializeField, Min(0.01f)] private float defaultSwingDuration = 0.7f;

        private float currentScale = 1f;
        private float elapsed;
        private float swingDuration;
        private bool playingTimedSwing;

        public float CurrentScale => currentScale;

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            ResetCrosshair();
        }

        protected override void OnDisable()
        {
            playingTimedSwing = false;
            currentScale = 1f;
            base.OnDisable();
        }

        private void Update()
        {
            if (!playingTimedSwing) return;

            elapsed += Time.deltaTime;
            ApplyProgress(elapsed / swingDuration);
            if (elapsed >= swingDuration) playingTimedSwing = false;
        }

        /// <summary>Starts a timed swing. For exact animation sync use SetSwingProgress.</summary>
        public void PlaySwing(float duration)
        {
            swingDuration = Mathf.Max(0.01f, duration);
            elapsed = 0f;
            playingTimedSwing = true;
            ApplyProgress(0f);
        }

        public void PlaySwing()
        {
            PlaySwing(defaultSwingDuration);
        }

        /// <summary>Supply the actual animation progress from 0 to 1, including recovery.</summary>
        public void SetSwingProgress(float normalizedTime)
        {
            playingTimedSwing = false;
            ApplyProgress(normalizedTime);
        }

        public void ResetCrosshair()
        {
            playingTimedSwing = false;
            currentScale = 1f;
            SetVerticesDirty();
        }

        private void ApplyProgress(float normalizedTime)
        {
            float progress = Mathf.Clamp01(normalizedTime);
            float impact = Mathf.Clamp(impactTime, 0.01f, 0.99f);
            float contraction = progress <= impact
                ? Mathf.SmoothStep(0f, 1f, progress / impact)
                : Mathf.SmoothStep(1f, 0f, (progress - impact) / (1f - impact));
            float nextScale = Mathf.Lerp(1f, strikeScale, contraction);
            if (Mathf.Approximately(currentScale, nextScale)) return;

            currentScale = nextScale;
            SetVerticesDirty();
        }

        [ContextMenu("Schlagvorschau (im Play-Modus)")]
        private void PreviewSwing()
        {
            if (Application.isPlaying && isActiveAndEnabled) PlaySwing();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            Vector2 center = rectTransform.rect.center;
            float inner = gap * currentScale;
            float length = lineLength * currentScale;
            float halfWidth = lineWidth * 0.5f;

            AddBar(vertices, center, new Rect(inner, -halfWidth, length, lineWidth));
            AddBar(vertices, center, new Rect(-inner - length, -halfWidth, length, lineWidth));
            AddBar(vertices, center, new Rect(-halfWidth, inner, lineWidth, length));
            AddBar(vertices, center, new Rect(-halfWidth, -inner - length, lineWidth, length));
            if (centerSize > 0f)
            {
                AddBar(vertices, center, new Rect(-centerSize * 0.5f, -centerSize * 0.5f,
                    centerSize, centerSize));
            }
        }

        private void AddBar(VertexHelper vertices, Vector2 center, Rect bar)
        {
            if (bar.width <= 0f || bar.height <= 0f) return;
            bar.position += center;
            if (outlineWidth > 0f)
            {
                Color outline = outlineColor;
                outline.a *= color.a;
                AddQuad(vertices, new Rect(bar.x - outlineWidth, bar.y - outlineWidth,
                    bar.width + outlineWidth * 2f, bar.height + outlineWidth * 2f), outline);
            }
            AddQuad(vertices, bar, color);
        }

        private static void AddQuad(VertexHelper vertices, Rect rect, Color tint)
        {
            int start = vertices.currentVertCount;
            vertices.AddVert(new Vector3(rect.xMin, rect.yMin), tint, Vector2.zero);
            vertices.AddVert(new Vector3(rect.xMin, rect.yMax), tint, Vector2.up);
            vertices.AddVert(new Vector3(rect.xMax, rect.yMax), tint, Vector2.one);
            vertices.AddVert(new Vector3(rect.xMax, rect.yMin), tint, Vector2.right);
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start + 2, start + 3, start);
        }
    }
}

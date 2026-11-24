using UnityEngine;

namespace ToxicDepths.UI
{
    internal sealed class OreScanHUD
    {
        private static readonly Color Panel = new Color(0.035f, 0.055f, 0.045f, 0.92f);
        private static readonly Color Green = new Color(0.44f, 1f, 0.20f);
        private readonly Texture2D oreTypeIcon;
        private readonly Texture2D poisonIcon;
        private GUIStyle heading;
        private int percent;
        private string progressLabel;
        private string oreLabel;
        private Color oreColor;
        private bool showPoison;

        internal OreScanHUD(Texture2D oreTypeIcon, Texture2D poisonIcon)
        {
            this.oreTypeIcon = oreTypeIcon;
            this.poisonIcon = poisonIcon;
        }

        internal void Begin()
        {
            percent = -1;
        }

        internal void Complete(string ore, bool poisonous)
        {
            string type = ore.ToUpperInvariant();
            oreLabel = type;
            showPoison = poisonous;
            oreColor = type switch
            {
                "EMERALD" => new Color(0.20f, 0.91f, 0.42f),
                "RUBY" => new Color(1f, 0.29f, 0.35f),
                _ => Color.white
            };
        }

        internal void DrawProgress(float progress)
        {
            int nextPercent = Mathf.Clamp(Mathf.FloorToInt(progress * 100f), 0, 100);
            if (nextPercent != percent)
            {
                percent = nextPercent;
                progressLabel = "ANALYZING... " + percent + "%";
            }
            Rect box = DrawPanel(39f);
            DrawHeading(new Rect(box.x + 12f, box.y + 4f, box.width - 24f, 24f), progressLabel, Green);
            DrawRect(new Rect(box.x + 12f, box.y + 31f, (box.width - 24f) * Mathf.Clamp01(progress), 2f), Green);
        }

        internal void DrawResult()
        {
            Rect box = DrawPanel(54f);
            DrawResultLine(box.x + 12f, box.y + 5f, showPoison ? "TOXIC" : "NEUTRAL",
                showPoison ? Green : Color.white, showPoison ? poisonIcon : null);
            DrawResultLine(box.x + 12f, box.y + 27f, oreLabel, oreColor, oreTypeIcon);
        }

        private void DrawResultLine(float x, float y, string label, Color color, Texture2D icon)
        {
            float width = heading.CalcSize(new GUIContent(label)).x;
            DrawHeading(new Rect(x, y, width, 22f), label, color);
            DrawIcon(new Rect(x + width + 4f, y + 3f, 16f, 16f), icon, color);
        }

        private void DrawHeading(Rect rect, string label, Color color)
        {
            heading.normal.textColor = color;
            GUI.Label(rect, label, heading);
        }

        private Rect DrawPanel(float height)
        {
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
            }
            const float width = 218f;
            var box = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.5f + 32f, width, height);
            DrawRect(box, Panel);
            DrawRect(new Rect(box.x, box.y, 2f, height), Green);
            return box;
        }

        private static void DrawRect(Rect rect, Color color) =>
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);

        private static void DrawIcon(Rect rect, Texture2D icon, Color color)
        {
            if (icon != null) GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true, 0f, color, 0f, 0f);
        }
    }
}

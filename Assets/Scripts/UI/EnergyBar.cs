using MagicPigGames;
using UnityEngine;
using UnityEngine.UI;

namespace ToxicDepths.UI
{
    [AddComponentMenu("Toxic Depths/UI/Energy Bar")]
    [DisallowMultipleComponent]
    public sealed class EnergyBar : MonoBehaviour
    {
        [Header("Energie")]
        [Tooltip("0 = leer, 1 = voll. Spaeter aus der Spielersteuerung aktualisieren.")]
        [SerializeField, Range(0f, 1f)] private float normalizedValue = 1f;

        [Header("Farbe")]
        [Tooltip("Faerbt die Fuellung; die Blasen erhalten eine hellere Variante derselben Farbe.")]
        [SerializeField] private Color fillColor = new Color(0.15f, 0.85f, 0.25f, 1f);

        [Header("Referenzen")]
        [SerializeField] private VerticalProgressBar progressBar;
        [SerializeField] private Graphic fillImage;
        [SerializeField] private Graphic detailImage;

        public float NormalizedValue => normalizedValue;

        private void OnEnable()
        {
            ApplyColor();
            ApplyValue();
        }

        public void SetNormalizedValue(float value)
        {
            normalizedValue = Mathf.Clamp01(value);
            if (isActiveAndEnabled) ApplyValue();
        }

        public void SetEnergy(float current, float maximum)
        {
            SetNormalizedValue(maximum > 0f ? current / maximum : 0f);
        }

        private void ApplyValue()
        {
            if (progressBar != null) progressBar.SetProgress(normalizedValue);
        }

        private void ApplyColor()
        {
            if (fillImage != null) fillImage.color = fillColor;
            if (detailImage == null) return;

            Color highlights = Color.Lerp(fillColor, Color.white, 0.3f);
            highlights.a = fillColor.a;
            detailImage.color = highlights;
        }

        private void OnValidate()
        {
            normalizedValue = Mathf.Clamp01(normalizedValue);
            ApplyColor();
            if (Application.isPlaying && isActiveAndEnabled) ApplyValue();
        }
    }
}

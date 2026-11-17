using System.Globalization;
using TMPro;
using UnityEngine;

namespace ToxicDepths.UI
{
    [AddComponentMenu("Toxic Depths/UI/Currency Count")]
    [DisallowMultipleComponent]
    public sealed class CurrencyCount : MonoBehaviour
    {
        [Header("Anzeige")]
        [SerializeField, Min(0)] private int amount;
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private TMP_Text changeLabel;

        [Header("Aenderungsanzeige")]
        [SerializeField, Min(0.1f)] private float changeDuration = 2f;
        [SerializeField] private Color incomeColor = new Color(0.4f, 0.94f, 0.66f);
        [SerializeField] private Color expenseColor = new Color(1f, 0.42f, 0.36f);

        private float changeTimeRemaining;
        private Color changeColor;

        public int Amount => amount;

        private void OnEnable()
        {
            RefreshAmount();
            HideChange();
        }

        // Presentation only: the future money system supplies the actual balance.
        public void SetAmount(int value, bool showChange = true)
        {
            int nextAmount = Mathf.Max(0, value);
            int difference = nextAmount - amount;
            amount = nextAmount;
            RefreshAmount();

            if (!showChange)
            {
                HideChange();
                return;
            }

            if (difference == 0 || !isActiveAndEnabled || changeLabel == null) return;
            changeColor = difference > 0 ? incomeColor : expenseColor;
            changeLabel.text = (difference > 0 ? "+ " : "- ") +
                Mathf.Abs(difference).ToString("N0", CultureInfo.InvariantCulture);
            changeLabel.color = changeColor;
            changeLabel.enabled = true;
            changeTimeRemaining = changeDuration;
        }

        private void Update()
        {
            if (changeTimeRemaining <= 0f) return;
            changeTimeRemaining = Mathf.Max(0f, changeTimeRemaining - Time.unscaledDeltaTime);
            if (changeLabel == null) return;

            float fadeDuration = Mathf.Min(0.5f, changeDuration);
            Color tint = changeColor;
            tint.a *= Mathf.Clamp01(changeTimeRemaining / fadeDuration);
            changeLabel.color = tint;
            if (changeTimeRemaining <= 0f) HideChange();
        }

        private void RefreshAmount()
        {
            if (amountLabel != null)
                amountLabel.text = amount.ToString("N0", CultureInfo.InvariantCulture);
        }

        private void HideChange()
        {
            changeTimeRemaining = 0f;
            if (changeLabel != null) changeLabel.enabled = false;
        }

        private void OnDisable() => HideChange();

        private void OnValidate()
        {
            amount = Mathf.Max(0, amount);
            changeDuration = Mathf.Max(0.1f, changeDuration);
            RefreshAmount();
        }
    }
}

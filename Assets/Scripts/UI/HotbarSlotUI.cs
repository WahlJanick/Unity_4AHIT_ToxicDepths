using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ToxicDepths.UI
{
    [DisallowMultipleComponent]
    public sealed class HotbarSlotUI : MonoBehaviour
    {
        [SerializeField] private HotbarSlotFrame frame;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text keyLabel;
        [SerializeField] private TMP_Text countLabel;

        private float targetScale = 1f;

        public void SetContent(int slotNumber, Sprite icon, int count)
        {
            if (keyLabel != null) keyLabel.text = slotNumber.ToString();
            bool hasItem = icon != null && count > 0;
            if (itemIcon != null)
            {
                itemIcon.sprite = icon;
                itemIcon.enabled = hasItem;
            }
            if (countLabel != null)
            {
                countLabel.text = hasItem && count > 1 ? count.ToString() : string.Empty;
                countLabel.enabled = hasItem && count > 1;
            }
        }

        public void SetSelected(bool selected, bool immediate = false)
        {
            if (frame != null) frame.SetSelected(selected);
            targetScale = selected ? 1.04f : 1f;
            if (immediate) transform.localScale = Vector3.one * targetScale;
        }

        private void Update()
        {
            float scale = Mathf.MoveTowards(transform.localScale.x, targetScale,
                Time.unscaledDeltaTime * 0.8f);
            if (!Mathf.Approximately(transform.localScale.x, scale))
                transform.localScale = Vector3.one * scale;
        }
    }
}

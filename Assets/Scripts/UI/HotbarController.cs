using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ToxicDepths.UI
{
    [AddComponentMenu("Toxic Depths/UI/Hotbar Controller")]
    [DisallowMultipleComponent]
    public sealed class HotbarController : MonoBehaviour
    {
        public const int SlotCount = 4;

        [Serializable]
        public sealed class Slot
        {
            public Sprite icon;
            [Min(0)] public int count = 1;
        }

        [Header("Vier Plaetze")]
        [SerializeField] private Slot[] slots = { new Slot(), new Slot(), new Slot(), new Slot() };
        [SerializeField, Range(0, SlotCount - 1)] private int selectedIndex;
        [Header("Anzeige")]
        [SerializeField] private HotbarSlotUI[] slotViews = new HotbarSlotUI[SlotCount];
        [Header("Werkzeugwechsel (Index 0 bis 3)")]
        [SerializeField] private UnityEvent<int> onSelectionChanged = new UnityEvent<int>();

        public int SelectedIndex => selectedIndex;
        public UnityEvent<int> OnSelectionChanged => onSelectionChanged;

        private void OnEnable()
        {
            EnsureFourSlots();
            Refresh(true);
        }

        private void Update()
        {
            // Same control state as the FPS player: no selection while paused or in a menu.
            if (!Application.isFocused || Time.timeScale <= 0f ||
                Cursor.lockState != CursorLockMode.Locked) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) { SelectSlot(0); return; }
                if (keyboard.digit2Key.wasPressedThisFrame) { SelectSlot(1); return; }
                if (keyboard.digit3Key.wasPressedThisFrame) { SelectSlot(2); return; }
                if (keyboard.digit4Key.wasPressedThisFrame) { SelectSlot(3); return; }
            }
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                SelectSlot((selectedIndex + (scroll > 0f ? -1 : 1) + SlotCount) % SlotCount);
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= SlotCount || index == selectedIndex) return;
            selectedIndex = index;
            Refresh(false);
            onSelectionChanged.Invoke(selectedIndex);
        }

        public void SetSlot(int index, Sprite icon, int count = 1)
        {
            if (index < 0 || index >= SlotCount) return;
            EnsureFourSlots();
            slots[index].icon = icon;
            slots[index].count = Mathf.Max(0, count);
            Refresh(false);
        }

        private void Refresh(bool immediate)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slotViews[i] == null) continue;
                slotViews[i].SetContent(i + 1, slots[i].icon, slots[i].count);
                slotViews[i].SetSelected(i == selectedIndex, immediate);
            }
        }

        private void EnsureFourSlots()
        {
            if (slots == null) slots = new Slot[SlotCount];
            if (slotViews == null) slotViews = new HotbarSlotUI[SlotCount];
            if (slots.Length != SlotCount) Array.Resize(ref slots, SlotCount);
            if (slotViews.Length != SlotCount) Array.Resize(ref slotViews, SlotCount);
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i] == null) slots[i] = new Slot();
                slots[i].count = Mathf.Max(0, slots[i].count);
            }
        }

        private void OnValidate()
        {
            EnsureFourSlots();
            selectedIndex = Mathf.Clamp(selectedIndex, 0, SlotCount - 1);
            Refresh(true);
        }
    }
}

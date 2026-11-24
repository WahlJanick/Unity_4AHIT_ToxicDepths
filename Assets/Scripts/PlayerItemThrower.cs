using ToxicDepths.Mining;
using ToxicDepths.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ToxicDepths
{
    [RequireComponent(typeof(PlayerMining), typeof(ShredderPlacement), typeof(ShredderShop))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(130)]
    public sealed class PlayerItemThrower : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float rotationTapTime = 0.22f;
        private PlayerMovement movement;
        private PlayerMining mining;
        private ShredderPlacement placement;
        private ShredderShop shop;
        private HotbarController hotbar;
        private float startedAt;
        private int startedSlot;
        private bool rotatingShredder;

        public bool IsCharging { get; private set; }
        public float Charge { get; private set; }

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            mining = GetComponent<PlayerMining>();
            placement = GetComponent<ShredderPlacement>();
            shop = GetComponent<ShredderShop>();
        }

        private void OnEnable()
        {
            hotbar = placement.Hotbar;
            if (hotbar != null) hotbar.OnSelectionChanged.AddListener(OnSelectionChanged);
            CancelCharge();
        }

        private bool CanThrow => mining.IsCarryingOre || placement.HasSelectedShredder ||
            shop.SelectedOutput != null || (hotbar != null && hotbar.SelectedIndex == 5 && mining.HasPickaxe);

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!movement.CanUseTools || !mining.isActiveAndEnabled || !placement.isActiveAndEnabled ||
                keyboard == null || hotbar == null)
            {
                CancelCharge();
                return;
            }
            ProcessInput(keyboard.qKey.wasPressedThisFrame, keyboard.qKey.isPressed,
                keyboard.qKey.wasReleasedThisFrame, Time.time);
        }

        internal void ProcessInput(bool pressed, bool held, bool released, float now)
        {
            if (pressed && CanThrow)
            {
                IsCharging = true;
                startedAt = now;
                startedSlot = hotbar.SelectedIndex;
                rotatingShredder = !mining.IsCarryingOre && placement.HasSelectedShredder;
            }
            if (!IsCharging) return;
            if (hotbar.SelectedIndex != startedSlot || !CanThrow) { CancelCharge(); return; }
            Charge = Mathf.Clamp01((now - startedAt) / mining.ThrowChargeTime);
            if (held)
            {
                var aim = mining.AimCrosshair;
                if (aim != null) aim.SetThrowCharge(Charge);
                return;
            }
            if (released)
            {
                if (rotatingShredder && now - startedAt < rotationTapTime) placement.RotateSelected(-45f);
                else if (mining.IsCarryingOre) mining.TryThrowHeldOre(Charge);
                else if (placement.HasSelectedShredder) placement.TryThrowSelected(mining.GetThrowSpeed(Charge), Charge);
                else if (shop.SelectedOutput != null) shop.TryThrowSelected(mining.GetThrowSpeed(Charge), Charge);
                else mining.TryDropPickaxe(Charge);
            }
            CancelCharge();
        }

        private void OnSelectionChanged(int _) => CancelCharge();

        internal void CancelCharge()
        {
            var aim = mining != null ? mining.AimCrosshair : null;
            if (IsCharging && aim != null) aim.ResetCrosshair();
            IsCharging = false;
            Charge = 0f;
        }

        private void OnDisable()
        {
            if (hotbar != null) hotbar.OnSelectionChanged.RemoveListener(OnSelectionChanged);
            CancelCharge();
        }
    }
}

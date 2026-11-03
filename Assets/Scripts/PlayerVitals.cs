using ToxicDepths.UI;
using UnityEngine;

namespace ToxicDepths
{
    [AddComponentMenu("Toxic Depths/Player Vitals")]
    [DisallowMultipleComponent]
    public sealed class PlayerVitals : MonoBehaviour
    {
        [Header("Stamina")]
        [SerializeField, Min(0.01f)] private float maxStamina = 100f;
        [SerializeField, Min(0f)] private float sprintDrainPerSecond = 20f;
        [SerializeField, Min(0f)] private float staminaRegenerationPerSecond = 30f;
        [SerializeField, Min(0f)] private float staminaRegenerationDelay = 0.8f;
        [Tooltip("Nach Erschoepfung erst ab diesem Anteil der aktuellen Maximalstamina wieder sprinten.")]
        [SerializeField, Range(0.01f, 1f)] private float sprintRecoveryFraction = 0.2f;

        [Header("Energie")]
        [SerializeField, Min(0.01f)] private float maxEnergy = 100f;
        [Tooltip("Energieverlust pro verbrauchtem Staminapunkt. Keine natuerliche Energieregeneration.")]
        [SerializeField, Min(0f)] private float energyPerStamina = 0.05f;
        [Tooltip("Verbleibende Maximalstamina bei leerer Energie; dazwischen linear skaliert.")]
        [SerializeField, Range(0.01f, 1f)] private float minimumStaminaFraction = 0.7f;

        [Header("Anzeige")]
        [SerializeField] private StaminaBar staminaBar;
        [SerializeField] private EnergyBar energyBar;

        private PlayerVitalsState state;
        private PlayerVitalsState State => state ??= new PlayerVitalsState(
            maxStamina, maxEnergy, minimumStaminaFraction, sprintDrainPerSecond,
            staminaRegenerationPerSecond, staminaRegenerationDelay, energyPerStamina,
            sprintRecoveryFraction);

        public float Stamina => State.Stamina;
        public float MaxStamina => State.MaxStamina;
        public float Energy => State.Energy;
        public float MaxEnergy => State.MaxEnergy;
        public bool IsSprinting { get; private set; }

        private void Start() => RefreshBars();

        private void OnDisable() => StopSprint();

        public float UpdateSprint(bool wantsToSprint, float deltaTime)
        {
            float fraction = State.TickSprint(wantsToSprint, deltaTime);
            IsSprinting = fraction > 0f;
            RefreshBars();
            return fraction;
        }

        public void StopSprint() => IsSprinting = false;

        // Future consumables can call this with an energy amount, e.g. RestoreEnergy(25f).
        public void RestoreEnergy(float amount)
        {
            State.RestoreEnergy(amount);
            RefreshBars();
        }

        private void RefreshBars()
        {
            // Use the original capacity so the energy penalty stays visible in the stamina bar.
            float staminaFraction = State.Stamina / State.BaseMaxStamina;
            if (staminaBar != null && !Mathf.Approximately(staminaBar.NormalizedValue, staminaFraction))
                staminaBar.SetStamina(State.Stamina, State.BaseMaxStamina);
            float energyFraction = State.Energy / State.MaxEnergy;
            if (energyBar != null && !Mathf.Approximately(energyBar.NormalizedValue, energyFraction))
                energyBar.SetEnergy(State.Energy, State.MaxEnergy);
        }
    }
}

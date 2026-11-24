using ToxicDepths.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        [Header("Vergiftung")]
        [SerializeField, Min(0f)] private float poisonEnergyPerSecond = 16f;

        [Header("Anzeige")]
        [SerializeField] private StaminaBar staminaBar;
        [SerializeField] private EnergyBar energyBar;

        private PlayerVitalsState state;
        private Volume poisonVolume;
        private VolumeProfile poisonProfile;
        private Vignette poisonVignette;
        private PlayerVitalsState State => state ??= new PlayerVitalsState(
            maxStamina, maxEnergy, minimumStaminaFraction, sprintDrainPerSecond,
            staminaRegenerationPerSecond, staminaRegenerationDelay, energyPerStamina,
            sprintRecoveryFraction);

        public float Stamina => State.Stamina;
        public float MaxStamina => State.MaxStamina;
        public float Energy => State.Energy;
        public float MaxEnergy => State.MaxEnergy;
        public bool IsSprinting { get; private set; }
        public bool IsPoisoned => State.IsPoisoned;
        public bool IsDefeated { get; private set; }

        private void Start() => RefreshBars();

        private void Update()
        {
            if (IsDefeated) return;

            if (IsPoisoned)
            {
                State.TickPoison(Time.deltaTime, poisonEnergyPerSecond);
                RefreshBars();
                CheckDefeat();
            }

            if (poisonVolume != null)
                poisonVolume.weight = Mathf.MoveTowards(poisonVolume.weight, IsPoisoned ? 1f : 0f, 6f * Time.deltaTime);
        }

        private void OnDisable()
        {
            StopSprint();
            if (poisonVolume != null) poisonVolume.weight = 0f;
        }

        private void OnDestroy()
        {
            if (poisonVolume != null) Destroy(poisonVolume.gameObject);
            if (poisonVignette != null) Destroy(poisonVignette);
            if (poisonProfile != null) Destroy(poisonProfile);
        }

        internal void RefreshPoison(float duration)
        {
            if (!isActiveAndEnabled || IsDefeated) return;

            State.RefreshPoison(duration);
            if (poisonVolume != null) return;

            poisonProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            poisonVignette = poisonProfile.Add<Vignette>();
            poisonVignette.color.Override(new Color(0.16f, 0.85f, 0.10f));
            poisonVignette.intensity.Override(0.52f);
            poisonVignette.smoothness.Override(0.45f);

            var effect = new GameObject("Poison Vignette");
            effect.transform.SetParent(transform, false);
            poisonVolume = effect.AddComponent<Volume>();
            poisonVolume.isGlobal = true;
            poisonVolume.priority = 100f;
            poisonVolume.weight = 0f;
            poisonVolume.sharedProfile = poisonProfile;
        }

        public float UpdateSprint(bool wantsToSprint, float deltaTime)
        {
            if (IsDefeated) return 0f;

            float fraction = State.TickSprint(wantsToSprint, deltaTime);
            IsSprinting = fraction > 0f;
            RefreshBars();
            CheckDefeat();
            return IsDefeated ? 0f : fraction;
        }

        public void StopSprint() => IsSprinting = false;

        public bool TrySpendStamina(float amount)
        {
            if (IsDefeated) return false;

            bool spent = State.TrySpendStamina(amount);
            RefreshBars();
            CheckDefeat();
            return spent;
        }

        // Future consumables can call this with an energy amount, e.g. RestoreEnergy(25f).
        public void RestoreEnergy(float amount)
        {
            if (IsDefeated) return;

            State.RestoreEnergy(amount);
            RefreshBars();
        }

        private void CheckDefeat()
        {
            if (Energy > 0f) return;

            IsDefeated = true;
            StopSprint();
            if (TryGetComponent(out PlayerMovement movement)) movement.Collapse();
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

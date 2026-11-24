using System;

namespace ToxicDepths
{
    // Resource rules are independent of input and UI so other actions/items can reuse them.
    public sealed class PlayerVitalsState
    {
        public float BaseMaxStamina { get; }
        public float MaxEnergy { get; }
        public float Stamina { get; private set; }
        public float Energy { get; private set; }
        public bool IsExhausted { get; private set; }
        public bool IsPoisoned => poisonTimeRemaining > 0f;
        public float MaxStamina => BaseMaxStamina *
            (minimumStaminaFraction + (1f - minimumStaminaFraction) * Energy / MaxEnergy);

        private readonly float minimumStaminaFraction;
        private readonly float sprintDrain;
        private readonly float regenerationRate;
        private readonly float regenerationDelay;
        private readonly float energyPerStamina;
        private readonly float sprintRecoveryFraction;
        private float regenerationWait;
        private float poisonTimeRemaining;

        public PlayerVitalsState(float maxStamina = 100f, float maxEnergy = 100f,
            float minimumStaminaFraction = 0.7f, float sprintDrain = 20f,
            float regenerationRate = 30f, float regenerationDelay = 0.8f,
            float energyPerStamina = 0.05f, float sprintRecoveryFraction = 0.2f)
        {
            BaseMaxStamina = Math.Max(0.01f, maxStamina);
            MaxEnergy = Math.Max(0.01f, maxEnergy);
            this.minimumStaminaFraction = Math.Clamp(minimumStaminaFraction, 0.01f, 1f);
            this.sprintDrain = Math.Max(0f, sprintDrain);
            this.regenerationRate = Math.Max(0f, regenerationRate);
            this.regenerationDelay = Math.Max(0f, regenerationDelay);
            this.energyPerStamina = Math.Max(0f, energyPerStamina);
            this.sprintRecoveryFraction = Math.Clamp(sprintRecoveryFraction, 0.01f, 1f);
            Energy = MaxEnergy;
            Stamina = MaxStamina;
        }

        // Returns the fraction of this frame that can be spent sprinting.
        public float TickSprint(bool wantsToSprint, float deltaTime)
        {
            if (deltaTime <= 0f) return 0f;

            if (wantsToSprint && !IsExhausted && Stamina > 0f)
            {
                if (sprintDrain <= 0f) return 1f;
                float requested = sprintDrain * deltaTime;
                return ConsumeStamina(requested) / requested;
            }

            // Only the part of the frame after the cooldown contributes to recovery.
            float recoveryTime = Math.Max(0f, deltaTime - regenerationWait);
            regenerationWait = Math.Max(0f, regenerationWait - deltaTime);
            Stamina = Math.Min(MaxStamina, Stamina + regenerationRate * recoveryTime);
            if (IsExhausted && Stamina >= MaxStamina * sprintRecoveryFraction)
                IsExhausted = false;
            return 0f;
        }

        // Discrete actions must pay their full cost; a rejected swing spends nothing.
        public bool TrySpendStamina(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount < 0f) return false;
            if (amount == 0f) return true;
            if (IsExhausted || Stamina < amount || Energy < amount * energyPerStamina) return false;
            ConsumeStamina(amount);
            return true;
        }

        public float ConsumeStamina(float amount)
        {
            if (amount <= 0f) return 0f;
            float consumed = Math.Min(Stamina, amount);
            if (consumed <= 0f) return 0f;

            Stamina -= consumed;
            DrainEnergy(consumed * energyPerStamina);
            regenerationWait = regenerationDelay;
            if (Stamina <= 0f) IsExhausted = true;
            return consumed;
        }

        public void RefreshPoison(float duration)
        {
            poisonTimeRemaining = Math.Max(poisonTimeRemaining, duration);
        }

        public void TickPoison(float deltaTime, float energyPerSecond)
        {
            if (!IsPoisoned || deltaTime <= 0f) return;

            float poisonedTime = Math.Min(poisonTimeRemaining, deltaTime);
            poisonTimeRemaining -= poisonedTime;
            DrainEnergy(poisonedTime * energyPerSecond);
        }

        private void DrainEnergy(float amount)
        {
            Energy = Math.Max(0f, Energy - Math.Max(0f, amount));
            Stamina = Math.Min(Stamina, MaxStamina);
        }

        public void RestoreEnergy(float amount)
        {
            if (amount > 0f) Energy = Math.Min(MaxEnergy, Energy + amount);
        }
    }
}

using System;
using UnityEngine;

namespace DrillRunner
{
    [Serializable]
    public sealed class DrillingSettings
    {
        [Min(0.01f)] public float DrillPower = 5;
        [Min(0.01f)] public float DamagePerPower = 10;
        [Min(0.01f)] public float EnergyCapacity = 100;
        [Min(0)] public float MovingEnergyPerSecond = 0.7f;
        [Min(0)] public float BlockedEnergyPerSecond = 0.5f;

        public void Validate()
        {
            DrillMovementSettings.Positive(DrillPower, nameof(DrillPower));
            DrillMovementSettings.Positive(DamagePerPower, nameof(DamagePerPower));
            DrillMovementSettings.Positive(EnergyCapacity, nameof(EnergyCapacity));
            DrillRules.NonNegative(MovingEnergyPerSecond, nameof(MovingEnergyPerSecond));
            DrillRules.NonNegative(BlockedEnergyPerSecond, nameof(BlockedEnergyPerSecond));
        }

        internal DrillingSettings Snapshot()
        {
            Validate();
            return (DrillingSettings)MemberwiseClone();
        }
    }

    public static class DrillRules
    {
        public static void NonNegative(float value, string name)
        {
            if (!DrillMovementSettings.Finite(value) || value < 0)
                throw new ArgumentOutOfRangeException(name, "Expected a finite nonnegative value.");
        }

        public static double DamageRate(float power, float damagePerPower, float hardness)
        {
            DrillMovementSettings.Positive(power, nameof(power));
            DrillMovementSettings.Positive(damagePerPower, nameof(damagePerPower));
            NonNegative(hardness, nameof(hardness));
            return power >= hardness ? (double)power * damagePerPower : 0;
        }
    }

    /// <summary>One obstacle's mutable run data; the shared material asset is never damaged.</summary>
    public sealed class ObstacleDurability
    {
        private double remaining;
        public string DisplayName { get; }
        public float Hardness { get; }
        public float Maximum { get; }
        public float EnergyRate { get; }
        public float Remaining => (float)remaining;
        public float Fraction => (float)(remaining / Maximum);
        public bool IsDestroyed { get; private set; }
        public event Action Destroyed;

        public ObstacleDurability(MaterialDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            definition.Validate();
            DisplayName = definition.DisplayName;
            Hardness = definition.Hardness;
            Maximum = definition.Durability;
            EnergyRate = definition.DrillingEnergyPerSecond;
            Reset();
        }

        public double TimeToBreak(float power, float damagePerPower)
        {
            var rate = DrillRules.DamageRate(power, damagePerPower, Hardness);
            return IsDestroyed ? 0 : rate > 0 ? remaining / rate : double.PositiveInfinity;
        }

        public void Drill(float power, float damagePerPower, double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            var rate = DrillRules.DamageRate(power, damagePerPower, Hardness);
            if (IsDestroyed || rate == 0 || seconds == 0)
                return;
            remaining = Math.Max(0, remaining - rate * seconds);
            if (remaining <= Maximum * 1e-10)
            {
                remaining = 0;
                IsDestroyed = true;
                Destroyed?.Invoke();
            }
        }

        public void Reset() { remaining = Maximum; IsDestroyed = false; }
    }

    public sealed class RunEnergy
    {
        private double remaining;
        public float Capacity { get; }
        public float Remaining => (float)remaining;
        public float Fraction => (float)(remaining / Capacity);
        public bool IsEmpty => remaining <= 0;
        public event Action Depleted;

        public RunEnergy(float capacity)
        {
            DrillMovementSettings.Positive(capacity, nameof(capacity));
            Capacity = capacity;
            Reset();
        }

        public double TimeUntilEmpty(float rate)
        {
            DrillRules.NonNegative(rate, nameof(rate));
            return rate == 0 ? double.PositiveInfinity : remaining / rate;
        }

        public void Consume(float rate, double seconds)
        {
            DrillRules.NonNegative(rate, nameof(rate));
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (IsEmpty || rate == 0 || seconds == 0)
                return;
            remaining = Math.Max(0, remaining - rate * seconds);
            if (remaining <= Capacity * 1e-12)
            {
                remaining = 0;
                Depleted?.Invoke();
            }
        }

        public void Reset() => remaining = Capacity;
    }
}

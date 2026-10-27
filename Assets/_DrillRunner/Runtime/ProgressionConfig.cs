using System;
using UnityEngine;

namespace DrillRunner
{
    public enum UpgradeCategory { DrillPower, EnergyCapacity, DrivePower }

    [Serializable]
    public sealed class UpgradeTable
    {
        public int[] Costs;
        public float[] Values;
        public int MaximumLevel => Costs.Length;
        public void Validate()
        {
            if (Costs == null || Values == null || Costs.Length < 1 || Costs.Length > 20 || Values.Length != Costs.Length + 1)
                throw new ArgumentException("Upgrade tables need a base value and one value/positive price per level (1-20 levels).");
            for (var i = 0; i < Values.Length; i++)
            {
                DrillMovementSettings.Positive(Values[i], "Upgrade value");
                if (i > 0 && (Values[i] <= Values[i - 1] || Costs[i - 1] <= 0))
                    throw new ArgumentException("Upgrade values must increase and prices must be positive.");
            }
        }
    }

    public readonly struct EffectiveAttributes
    {
        public readonly float Power, Energy, Cruise, MinimumBoost, MaximumBoost, Recovery;
        public EffectiveAttributes(float power, float energy, float cruise, float minimumBoost, float maximumBoost, float recovery)
        { Power = power; Energy = energy; Cruise = cruise; MinimumBoost = minimumBoost; MaximumBoost = maximumBoost; Recovery = recovery; }
    }

    [CreateAssetMenu(menuName = "DrillRunner/Pocket Drill Progression")]
    public sealed class ProgressionConfig : ScriptableObject
    {
        public const string PocketForm = "pocket_drill";
        public UpgradeTable Power = new UpgradeTable { Costs = new[] { 60, 100, 160, 240, 340 }, Values = new[] { 5f, 6, 7, 8, 9, 10 } };
        public UpgradeTable Energy = new UpgradeTable { Costs = new[] { 50, 90, 140, 210, 300 }, Values = new[] { 100f, 115, 130, 145, 160, 180 } };
        public UpgradeTable Drive = new UpgradeTable { Costs = new[] { 50, 90, 140, 210, 300 }, Values = new[] { 10f, 10.4f, 10.8f, 11.2f, 11.6f, 12 } };
        public float[] MinimumBoost = { 2, 2.2f, 2.4f, 2.6f, 2.8f, 3 };
        public float[] MaximumBoost = { 12, 12, 12, 12, 12, 12 };
        public float[] Recovery = { 12, 14, 16, 18, 20, 22 };

        public UpgradeTable Table(UpgradeCategory category)
        {
            switch (category)
            {
                case UpgradeCategory.DrillPower: return Power;
                case UpgradeCategory.EnergyCapacity: return Energy;
                case UpgradeCategory.DrivePower: return Drive;
                default: throw new ArgumentException("Unknown upgrade category.");
            }
        }

        public void Validate()
        {
            if (Power == null || Energy == null || Drive == null) throw new ArgumentException("Missing upgrade table.");
            Power.Validate(); Energy.Validate(); Drive.Validate();
            if (Power.Values[0] != 5 || Power.Values[Power.MaximumLevel] < 9 || Energy.Values[0] != 100 || Drive.Values[0] != 10 ||
                MinimumBoost == null || MaximumBoost == null || Recovery == null || MinimumBoost.Length != Drive.Values.Length ||
                MaximumBoost.Length != Drive.Values.Length || Recovery.Length != Drive.Values.Length)
                throw new ArgumentException("Pocket Drill needs power 5 through at least 9, energy 100, cruise 10, and complete drive tables.");
            for (var i = 0; i < Drive.Values.Length; i++)
            {
                DrillRules.NonNegative(MinimumBoost[i], "Minimum boost"); DrillRules.NonNegative(MaximumBoost[i], "Maximum boost");
                DrillMovementSettings.Positive(Recovery[i], "Recovery");
                if (MinimumBoost[i] > MaximumBoost[i] || Drive.Values[i] > 12 || Drive.Values[i] + MaximumBoost[i] > 24 ||
                    (i > 0 && (MinimumBoost[i] < MinimumBoost[i - 1] || MaximumBoost[i] < MaximumBoost[i - 1] || Recovery[i] < Recovery[i - 1])))
                    throw new ArgumentException("Drive values must be monotonic, controllable, and within the existing 24-unit speed ceiling.");
            }
        }

        public EffectiveAttributes Calculate(PlayerProgress progress)
        {
            Validate(); progress.Validate(this);
            return new EffectiveAttributes(Power.Values[progress.PowerLevel], Energy.Values[progress.EnergyLevel], Drive.Values[progress.DriveLevel],
                MinimumBoost[progress.DriveLevel], MaximumBoost[progress.DriveLevel], Recovery[progress.DriveLevel]);
        }
    }
}

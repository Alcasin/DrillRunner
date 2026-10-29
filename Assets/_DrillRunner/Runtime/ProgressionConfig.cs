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
        public void Validate(bool allowPlateau = false)
        {
            if (Costs == null || Values == null || Costs.Length < 1 || Costs.Length > 20 || Values.Length != Costs.Length + 1)
                throw new ArgumentException("Upgrade tables need a base value and one value/positive price per level (1-20 levels).");
            for (var i = 0; i < Values.Length; i++)
            {
                DrillMovementSettings.Positive(Values[i], "Upgrade value");
                if (i > 0 && ((allowPlateau ? Values[i] < Values[i - 1] : Values[i] <= Values[i - 1]) || Costs[i - 1] <= 0))
                    throw new ArgumentException("Upgrade values must increase and prices must be positive.");
            }
        }
    }

    public readonly struct EffectiveAttributes
    {
        public readonly float Power, Energy, Cruise, MinimumBoost, MaximumBoost, Recovery, SpeedCeiling;
        public EffectiveAttributes(float power, float energy, float cruise, float minimumBoost, float maximumBoost, float recovery, float speedCeiling = 24)
        { Power = power; Energy = energy; Cruise = cruise; MinimumBoost = minimumBoost; MaximumBoost = maximumBoost; Recovery = recovery; SpeedCeiling = speedCeiling; }
    }

    [CreateAssetMenu(menuName = "DrillRunner/Pocket Drill Progression")]
    public sealed class ProgressionConfig : ScriptableObject
    {
        public const string PocketForm = "pocket_drill";
        public const string WorkshopForm = "workshop_drill";
        public static readonly string[] FormIds = { PocketForm, WorkshopForm, "industrial_drill", "heavy_borer", "hydraulic_drill", "crystal_core_drill", "fusion_drill", "quantum_drill" };
        public int Stage => Array.IndexOf(FormIds, FormId) + 1;
        public int EligibleRegionCount => Stage >= 7 ? 4 : Stage >= 5 ? 3 : Stage >= 3 ? 2 : 1;
        public string FormId = PocketForm;
        public string DisplayName = "Pocket Drill";
        public GameObject VisualPrefab;
        public Texture2D PreviewImage;
        public ProgressionConfig NextForm;
        public int EvolutionCopper = 12;
        public ResourceCost[] EvolutionResources = Array.Empty<ResourceCost>();
        public ResourceCost[] RequiredResources => EvolutionResources != null && EvolutionResources.Length > 0 ? EvolutionResources :
            EvolutionCopper > 0 ? new[] { new ResourceCost(EvolutionResource.Copper, EvolutionCopper) } : Array.Empty<ResourceCost>();
        public float SpeedCeiling = 24;
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
            var seen = new System.Collections.Generic.HashSet<ProgressionConfig>();
            for (var form = this; form != null; form = form.NextForm)
            {
                if (!seen.Add(form) || seen.Count > 8 || form.Stage == 0) throw new ArgumentException("Unsupported or cyclic drill evolution chain.");
                form.ValidateTier();
                if (form.FormId == PocketForm && (form.Power.Values[0] != 5 || form.Power.Values[form.Power.MaximumLevel] < 9 || form.Drive.Values[0] != 10 || form.SpeedCeiling != 24))
                    throw new ArgumentException("Pocket Drill retains the accepted base stats and speed limit.");
                if (form.EvolutionCopper < 0 || form.EvolutionCopper > 100000 || (form.Stage == 1 && form.EvolutionCopper == 0))
                    throw new ArgumentException("Invalid legacy Copper requirement.");
                var resources = new System.Collections.Generic.HashSet<EvolutionResource>();
                foreach (var cost in form.RequiredResources)
                {
                    if (cost == null || !Enum.IsDefined(typeof(EvolutionResource), cost.Resource) || !resources.Add(cost.Resource) || cost.Amount <= 0 || cost.Amount > 100000 ||
                        ResourceCost.AvailableFromStage(cost.Resource) > form.Stage) throw new ArgumentException("Invalid, duplicate, or circular resource requirement.");
                }
                var next = form.NextForm;
                if (next != null)
                {
                    next.ValidateTier();
                    if (form.Stage == 8 || next.Stage != form.Stage + 1 || form.RequiredResources.Length == 0) throw new ArgumentException("Evolution requires an adjacent form and configured resources.");
                    if (next.Power.Values[0] <= form.Power.Values[form.Power.MaximumLevel] || next.Energy.Values[0] < form.Energy.Values[form.Energy.MaximumLevel] ||
                        next.Drive.Values[0] < form.Drive.Values[form.Drive.MaximumLevel] || next.MinimumBoost[0] < form.MinimumBoost[form.Drive.MaximumLevel] ||
                        next.MaximumBoost[0] < form.MaximumBoost[form.Drive.MaximumLevel] || next.Recovery[0] < form.Recovery[form.Drive.MaximumLevel])
                        throw new ArgumentException("Evolution must preserve every purchased attribute and improve Drill Power.");
                }
            }
        }

        private void ValidateTier()
        {
            if (Power == null || Energy == null || Drive == null) throw new ArgumentException("Missing upgrade table.");
            Power.Validate(); Energy.Validate(); Drive.Validate(Stage >= 3);
            if (string.IsNullOrWhiteSpace(DisplayName) || !DrillMovementSettings.Finite(SpeedCeiling) || SpeedCeiling < 24 || SpeedCeiling > 26 ||
                MinimumBoost == null || MaximumBoost == null || Recovery == null || MinimumBoost.Length != Drive.Values.Length ||
                MaximumBoost.Length != Drive.Values.Length || Recovery.Length != Drive.Values.Length)
                throw new ArgumentException("Each form needs a name, bounded speed ceiling, and complete drive tables.");
            for (var i = 0; i < Drive.Values.Length; i++)
            {
                DrillRules.NonNegative(MinimumBoost[i], "Minimum boost"); DrillRules.NonNegative(MaximumBoost[i], "Maximum boost");
                DrillMovementSettings.Positive(Recovery[i], "Recovery");
                if (MinimumBoost[i] > MaximumBoost[i] || Drive.Values[i] > (FormId == PocketForm ? 12 : 13) || Drive.Values[i] + MaximumBoost[i] > SpeedCeiling ||
                    (i > 0 && (MinimumBoost[i] < MinimumBoost[i - 1] || MaximumBoost[i] < MaximumBoost[i - 1] || Recovery[i] < Recovery[i - 1])))
                    throw new ArgumentException("Drive values must be monotonic and remain within the form's validated speed ceiling.");
            }
        }

        public EffectiveAttributes Calculate(PlayerProgress progress)
        {
            Validate(); progress.Validate(this);
            var form = ForForm(progress.FormId); var power = progress.Level(UpgradeCategory.DrillPower);
            var energy = progress.Level(UpgradeCategory.EnergyCapacity); var drive = progress.Level(UpgradeCategory.DrivePower);
            return new EffectiveAttributes(form.Power.Values[power], form.Energy.Values[energy], form.Drive.Values[drive],
                form.MinimumBoost[drive], form.MaximumBoost[drive], form.Recovery[drive], form.SpeedCeiling);
        }

        public ProgressionConfig ForForm(string id)
        {
            var form = this;
            for (var i = 0; i < 8 && form != null; i++, form = form.NextForm) if (form.FormId == id) return form;
            throw new ArgumentException("Unsupported drill form.");
        }
    }
}

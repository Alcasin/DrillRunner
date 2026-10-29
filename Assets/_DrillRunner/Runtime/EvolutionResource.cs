using System;

namespace DrillRunner
{
    // Serialized numeric identifiers are permanent; display names may change.
    public enum EvolutionResource { Copper = 0, Iron = 1, Quartz = 2, AlloyCore = 3, EnergyShard = 4 }
    [Serializable] public sealed class ResourceCost
    {
        public EvolutionResource Resource;
        public int Amount;
        public ResourceCost(EvolutionResource resource, int amount) { Resource = resource; Amount = amount; }
        public static string Name(EvolutionResource resource)
        {
            switch (resource)
            {
                case EvolutionResource.Copper: return "Copper";
                case EvolutionResource.Iron: return "Iron";
                case EvolutionResource.Quartz: return "Quartz";
                case EvolutionResource.AlloyCore: return "Alloy Core";
                case EvolutionResource.EnergyShard: return "Energy Shard";
                default: throw new ArgumentException("Unknown evolution resource.");
            }
        }
        public static int AvailableFromStage(EvolutionResource resource) => resource == EvolutionResource.Copper ? 1 :
            resource == EvolutionResource.Iron ? 3 : resource == EvolutionResource.EnergyShard ? 7 : 5;
    }
    [Serializable] public sealed class FormUpgradeRecord
    {
        public string FormId;
        public int Power, Energy, Drive;
        public FormUpgradeRecord Copy() => (FormUpgradeRecord)MemberwiseClone();
        public int Level(UpgradeCategory category) => category == UpgradeCategory.DrillPower ? Power : category == UpgradeCategory.EnergyCapacity ? Energy : Drive;
    }
}

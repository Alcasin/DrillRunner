using System;
using UnityEngine;

namespace DrillRunner
{
    [Serializable] public struct MaterialDrillingCost
    {
        public string MaterialId;
        public float Multiplier;
        public MaterialDrillingCost(string id,float multiplier){MaterialId=id;Multiplier=multiplier;}
        public static void Validate(MaterialDrillingCost[] costs)
        {
            if(costs==null)throw new ArgumentException("Material drilling costs must be an explicit array.");
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var cost in costs)
                if(string.IsNullOrWhiteSpace(cost.MaterialId)||!ids.Add(cost.MaterialId)||!DrillMovementSettings.Finite(cost.Multiplier)||cost.Multiplier<1||cost.Multiplier>30)
                    throw new ArgumentException("Regional drilling costs need unique material IDs and fixed finite multipliers from 1 to 30.");
        }
    }
    public static class RegionIds
    {
        public const string Yard = "workshop_yard", Quarry = "quarry", Advanced = "advanced_works", Energy = "high_energy";
        public static readonly string[] All = {Yard,Quarry,Advanced,Energy};
        public static int UnlockStage(string id) => id == Yard ? 1 : id == Quarry ? 3 : id == Advanced ? 5 : id == Energy ? 7 : throw new ArgumentException("Unknown region ID.");
        public static bool Implemented(string id) => Array.IndexOf(All, id) >= 0;
    }
    [Serializable] public sealed class RegionDistanceRecord
    {
        public string RegionId;
        public double Distance;
        public RegionDistanceRecord Copy() => (RegionDistanceRecord)MemberwiseClone();
    }
    [CreateAssetMenu(menuName="DrillRunner/Region")]
    public sealed class RegionDefinition : ScriptableObject
    {
        public string Id, DisplayName, Identity;
        public int UnlockStage = 1;
        public bool Implemented;
        public TrackCatalog Track;
        public EncounterLibrary Encounters;
        public TrackSection Introduction;
        public GameObject Environment;
        public MaterialDefinition[] Materials = Array.Empty<MaterialDefinition>();
        public EvolutionResource[] Resources = Array.Empty<EvolutionResource>();
        [Tooltip("Multiplies the existing form/material drilling rate only in this region. Empty preserves original costs. Independent of purchased Power/Capacity.")]
        public MaterialDrillingCost[] DrillingCosts = Array.Empty<MaterialDrillingCost>();
        public Color Sky = new Color(0.69f,0.75f,0.76f);
        public bool CanPlay(int stage) => Implemented && stage >= UnlockStage;
        public void Validate()
        {
            MaterialDrillingCost.Validate(DrillingCosts);
            foreach(var cost in DrillingCosts)
                if(!Array.Exists(Materials,m=>m!=null&&m.MaterialId==cost.MaterialId))throw new ArgumentException("Regional drilling cost refers to an undeclared material.");
            if (string.IsNullOrWhiteSpace(DisplayName) || UnlockStage != RegionIds.UnlockStage(Id) || Implemented != RegionIds.Implemented(Id)) throw new ArgumentException("Invalid region identity, unlock, or implementation metadata.");
            if (Implemented && (Track == null || Encounters == null || Encounters.Catalog != Track || Introduction == null)) throw new ArgumentException("Playable regions require compatible track, encounters and introduction.");
            if (!Implemented && (Track != null || Encounters != null || Introduction != null || Environment != null)) throw new ArgumentException("Deferred regions must not advertise playable placeholder content.");
        }
    }
}

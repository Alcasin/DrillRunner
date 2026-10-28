using UnityEngine;

namespace DrillRunner
{
    public enum EncounterCategory { EasyFlow, DenseDrilling, RiskReward, PrecisionEscape, PowerTest, Recovery }
    public enum EncounterArrangement { FlowArc, SplitCluster, OreChoice, GuardedChoice, StoneStagger, PowerFork, QuietDeck }

    [CreateAssetMenu(menuName = "DrillRunner/Encounter")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        public TrackSection Section;
        public EncounterCategory Category;
        public EncounterArrangement Arrangement;
        [TextArea] public string Intent;
        [Range(0, 2)] public int Pressure;
        [Min(0)] public int MinimumOrdinal;
        [Min(0)] public int EarlyWeight = 1, Weight = 2, LateWeight = 2;
        [Tooltip("A full-footprint collision-free witness, not a lane or movement restriction.")]
        public float BypassX = -2.05f;
    }
}

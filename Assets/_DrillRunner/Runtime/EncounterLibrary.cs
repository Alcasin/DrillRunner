using UnityEngine;

namespace DrillRunner
{
    [CreateAssetMenu(menuName = "DrillRunner/Encounter Library")]
    public sealed class EncounterLibrary : ScriptableObject
    {
        [Tooltip("Explicit legacy catalog overrides keep their own content unless paired with a matching library.")]
        public TrackCatalog Catalog;
        public EncounterDefinition[] Patterns;
        [Min(1)] public int IntroductionSections = 1;
        [Min(0)] public int OpeningBufferSections;
        public bool FirstSelectionCopper;
        [Min(0)] public int OpeningCopperBySelection;
        public bool CalmFirstEncounter = true;
        public bool ReliefAfterHighPressure = true;
        [Range(0, 4)] public int ArrangementMemory = 2;
        [Min(0)] public int EmptyRecoverySpacing = 4;
        [HideInInspector] public int Revision;
        [Min(1)] public int EarlySections = 3, RicherFromSection = 20;
        [Min(1)] public int MaximumCopperGap = 3, MaximumRecoveryGap = 6;
        [Min(1)] public int MaximumConsecutivePressure = 1;
        [Min(0)] public float ReactionSeconds = 0.35f;
        [Min(0.01f)] public float SteeringTolerance = 0.04f;
        [Tooltip("Maximum drilling Energy for a full starting-Power Ore line. Validation only; never changes damage or Energy consumption.")]
        [Min(1)] public float StartingOreEnergyBudget = 60;
    }
}

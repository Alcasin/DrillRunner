using System;
using UnityEngine;

namespace DrillRunner
{
    [CreateAssetMenu(menuName = "DrillRunner/Material Definition")]
    public sealed class MaterialDefinition : ScriptableObject
    {
        public string MaterialId = "wood";
        public string DisplayName = "Wood";
        [Min(0)] public float Hardness = 2;
        [Min(0.01f)] public float Durability = 70;
        [Min(0)] public float DrillingEnergyPerSecond = 8;
        public Color FeedbackColor = new Color(0.65f, 0.36f, 0.12f);

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(MaterialId) || string.IsNullOrWhiteSpace(DisplayName))
                throw new ArgumentException("Material identifier and display name are required.");
            DrillRules.NonNegative(Hardness, nameof(Hardness));
            DrillMovementSettings.Positive(Durability, nameof(Durability));
            DrillRules.NonNegative(DrillingEnergyPerSecond, nameof(DrillingEnergyPerSecond));
            if (!DrillMovementSettings.Finite(FeedbackColor.r) || !DrillMovementSettings.Finite(FeedbackColor.g) ||
                !DrillMovementSettings.Finite(FeedbackColor.b) || !DrillMovementSettings.Finite(FeedbackColor.a))
                throw new ArgumentException("Material feedback color must be finite.");
        }
    }
}

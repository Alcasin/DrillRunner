using System;
using UnityEngine;

namespace DrillRunner
{
    public readonly struct EncounterEnvelope
    {
        public readonly float Width, HalfWidth, Front, Rear, Speed, Response, DamagePerPower;
        public EncounterEnvelope(float width, float half, float front, float rear, float speed, float response, float damagePerPower = 10)
        { Width = width; HalfWidth = half; Front = front; Rear = rear; Speed = speed; Response = response; DamagePerPower = damagePerPower; }
        public static EncounterEnvelope From(DrillMotor motor)
        {
            var bounds = DrillContacts.WorldBounds(motor.GetComponent<BoxCollider>());
            return new EncounterEnvelope(motor.Settings.TrackWidth, bounds.extents.x, bounds.max.z - motor.transform.position.z,
                motor.transform.position.z - bounds.min.z, motor.Motion.SpeedCeiling, motor.Motion.SteeringResponse, motor.Drilling.DamagePerPower);
        }
        public static EncounterEnvelope Accepted => new EncounterEnvelope(6, 0.85f, 1.85f, 1.15f, 26, 18);
    }

    /// <summary>Conservative authored witnesses: an obstacle-free straight bypass plus reachable mining lines.
    /// Steering is the actual exponential motor response, preceded by a reaction allowance.
    /// Certificates establish available choices, not a guarantee for arbitrary player actions.</summary>
    public static class EncounterSafety
    {
        public const float Clearance = 0.08f;
        public static float ManeuverSeconds(EncounterEnvelope e, EncounterLibrary library)
            => library.ReactionSeconds + Mathf.Log(Mathf.Max(1, (e.Width - 2 * e.HalfWidth) / library.SteeringTolerance)) / e.Response;

        public static bool Validate(EncounterDefinition definition, EncounterLibrary library, EncounterEnvelope e, out string reason)
        {
            reason = "";
            if (library == null || library.Catalog == null || definition == null || definition.Section == null ||
                !Enum.IsDefined(typeof(EncounterCategory), definition.Category) || definition.Pressure < 0 || definition.Pressure > 2 ||
                !DrillMovementSettings.Finite(e.Speed) || !DrillMovementSettings.Finite(e.Response) || e.Speed <= 0 || e.Response <= 0 ||
                !DrillMovementSettings.Finite(e.Width) || !DrillMovementSettings.Finite(e.HalfWidth) || e.HalfWidth <= 0 ||
                e.Width <= 2 * (e.HalfWidth + Clearance) || !DrillMovementSettings.Finite(e.Front) || !DrillMovementSettings.Finite(e.Rear) || e.Front <= 0 || e.Rear <= 0 ||
                !DrillMovementSettings.Finite(library.ReactionSeconds) || library.ReactionSeconds < 0 ||
                !DrillMovementSettings.Finite(e.DamagePerPower) || e.DamagePerPower <= 0 ||
                !DrillMovementSettings.Finite(library.StartingOreEnergyBudget) || library.StartingOreEnergyBudget <= 0 ||
                !DrillMovementSettings.Finite(library.SteeringTolerance) || library.SteeringTolerance <= 0 || library.SteeringTolerance > Clearance)
            { reason = "Invalid encounter, movement envelope, or steering allowance."; return false; }
            try
            {
                var section = definition.Section; var catalog = library.Catalog;
                if (!SectionSequence.IsSafe(section, catalog, e.HalfWidth) || Mathf.Abs(e.Width - catalog.Width) > 0.001f)
                { reason = "Section geometry or legacy material/route validation failed."; return false; }
                var placements = section.ReadPlacements(); var limit = e.Width / 2 - e.HalfWidth - Clearance;
                if (!DrillMovementSettings.Finite(definition.BypassX) || Mathf.Abs(definition.BypassX) > limit)
                { reason = "Bypass witness exceeds the continuous steering bounds."; return false; }
                var first = section.Length;
                foreach (var p in placements)
                {
                    first = Mathf.Min(first, p.Bounds.min.z);
                    if (OverlapsX(definition.BypassX, e.HalfWidth, p.Bounds))
                    { reason = "Bypass consumes an obstacle or leaves less than full-footprint clearance."; return false; }
                }
                if (placements.Length > 0 && first - e.Front < e.Speed * ManeuverSeconds(e, library) + Clearance)
                { reason = "Insufficient reaction/steering distance from an arbitrary entry X."; return false; }
                // Every Ore has its own constant-X approach, including any guard, at starting Power.
                foreach (var ore in placements)
                {
                    if (ore.Reward != ObstacleRewardKind.Copper) continue;
                    var x = ore.Bounds.center.x;
                    var energy = 0f;
                    if (Mathf.Abs(x) > limit) { reason = "Ore center is outside the steering envelope."; return false; }
                    foreach (var obstacle in placements)
                    {
                        if (!OverlapsX(x, e.HalfWidth, obstacle.Bounds)) continue;
                        if (obstacle.Hardness > catalog.InitialPower || x < obstacle.Bounds.min.x + 0.1f || x > obstacle.Bounds.max.x - 0.1f)
                        { reason = "Ore line has an undrillable guard or an off-center collision."; return false; }
                    }
                    foreach (var obstacle in section.Obstacles)
                        if (OverlapsX(x, e.HalfWidth, obstacle.WorldBounds))
                            energy += obstacle.Definition.Durability / (catalog.InitialPower * e.DamagePerPower) * obstacle.Definition.DrillingEnergyPerSecond;
                    if (energy > library.StartingOreEnergyBudget)
                    { reason = "Ore line exceeds the starting drilling Energy budget."; return false; }
                }
                return true;
            }
            catch (ArgumentException error) { reason = error.Message; return false; }
        }

        public static bool Transition(TrackSection previous, TrackSection next, EncounterLibrary library, EncounterEnvelope e, out float freeDistance)
        {
            var last = 0f; foreach (var p in previous.ReadPlacements()) last = Mathf.Max(last, p.Bounds.max.z);
            var first = next.Length; foreach (var p in next.ReadPlacements()) first = Mathf.Min(first, p.Bounds.min.z);
            freeDistance = previous.Length - last + first - e.Front - e.Rear;
            return freeDistance >= e.Speed * ManeuverSeconds(e, library) + Clearance;
        }

        public static bool OverlapsX(float x, float half, Bounds b) => x + half > b.min.x - Clearance && x - half < b.max.x + Clearance;
    }
}

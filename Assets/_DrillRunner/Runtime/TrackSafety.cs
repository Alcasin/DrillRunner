using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrillRunner
{
    public readonly struct TrackPlacement
    {
        public readonly Bounds Bounds;
        public readonly float Hardness;
        public readonly ObstacleRewardKind Reward;
        public TrackPlacement(Bounds bounds, float hardness, ObstacleRewardKind reward)
        { Bounds = bounds; Hardness = hardness; Reward = reward; }
    }

    public static class TrackSafety
    {
        public const float Margin = 0.05f;

        public static bool Validate(float width, float length, float halfFootprint, float initialPower,
            TrackPlacement[] placements, out string reason)
        {
            reason = "";
            if (!DrillMovementSettings.Finite(width) || !DrillMovementSettings.Finite(length) ||
                !DrillMovementSettings.Finite(halfFootprint) || !DrillMovementSettings.Finite(initialPower) ||
                halfFootprint <= 0 || initialPower <= 0 || width <= 2 * (halfFootprint + Margin) || length < 24 || placements == null)
            { reason = "Invalid section dimensions or validation parameters."; return false; }
            var limit = width / 2 - halfFootprint - Margin;
            var candidates = new List<float> { -limit, limit };
            for (var i = 0; i < placements.Length; i++)
            {
                var p = placements[i]; var b = p.Bounds;
                if (!DrillMovementSettings.Finite(b.center) || !DrillMovementSettings.Finite(b.size) ||
                    !DrillMovementSettings.Finite(p.Hardness) || p.Hardness < 0 || b.size.x <= 0 || b.size.y <= 0 || b.size.z <= 0 ||
                    b.min.x < -width / 2 || b.max.x > width / 2 || b.min.y < -0.001f ||
                    b.min.z < 4 || b.max.z > length - 4 || Mathf.Abs(b.center.x) > limit ||
                    (p.Hardness > initialPower && (b.min.z < 8 || b.max.z > length - 8)))
                { reason = "Obstacle dimensions, placement, or boundary clearance are unsafe."; return false; }
                if (MaterialDefinition.IsResource(p.Reward) && p.Hardness > initialPower)
                { reason = "Copper must be drillable at initial power."; return false; }
                for (var j = 0; j < i; j++)
                    if (b.Intersects(placements[j].Bounds))
                    { reason = "Authored obstacles overlap."; return false; }
                var tipMargin = Mathf.Min(0.1f, b.extents.x * 0.5f);
                candidates.Add(Mathf.Clamp(b.min.x - halfFootprint - Margin, -limit, limit));
                candidates.Add(Mathf.Clamp(b.max.x + halfFootprint + Margin, -limit, limit));
                candidates.Add(Mathf.Clamp(b.min.x + tipMargin, -limit, limit));
                candidates.Add(Mathf.Clamp(b.max.x - tipMargin, -limit, limit));
            }
            candidates.Sort();
            var route = false;
            for (var i = 0; i < candidates.Count; i++)
            {
                route |= SafeAt(candidates[i], halfFootprint, initialPower, placements);
                if (i > 0) route |= SafeAt((candidates[i] + candidates[i - 1]) / 2, halfFootprint, initialPower, placements);
            }
            if (!route) { reason = "No complete-footprint constant-X route exists."; return false; }
            foreach (var p in placements)
                if (MaterialDefinition.IsResource(p.Reward) && !SafeAt(p.Bounds.center.x, halfFootprint, initialPower, placements))
                { reason = "Copper has no certified approach/exit route."; return false; }
            return true;
        }

        private static bool SafeAt(float x, float half, float power, TrackPlacement[] placements)
        {
            foreach (var p in placements)
            {
                var b = p.Bounds;
                if (x + half <= b.min.x - Margin || x - half >= b.max.x + Margin) continue;
                var tipMargin = Mathf.Min(0.1f, b.extents.x * 0.5f);
                if (p.Hardness > power || x < b.min.x + tipMargin || x > b.max.x - tipMargin) return false;
            }
            return true;
        }
    }
}

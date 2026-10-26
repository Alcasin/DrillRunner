using System;
using UnityEngine;

namespace DrillRunner
{
    /// <summary>Allocation-free, axis-separated swept boxes for M2's static axis-aligned layout.</summary>
    public static class DrillContacts
    {
        public const float Skin = 0.005f;

        public static Bounds WorldBounds(BoxCollider collider)
        {
            if (collider == null || Quaternion.Angle(collider.transform.rotation, Quaternion.identity) > 0.01f)
                throw new ArgumentException("Drill contact boxes must be present and axis-aligned.");
            var scale = collider.transform.lossyScale;
            var bounds = new Bounds(collider.transform.TransformPoint(collider.center),
                Vector3.Scale(collider.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))));
            if (!DrillMovementSettings.Finite(bounds.center) || !DrillMovementSettings.Finite(bounds.size) ||
                bounds.size.x <= 0 || bounds.size.y <= 0 || bounds.size.z <= 0)
                throw new ArgumentException("Drill contact bounds must be finite and nonzero.");
            return bounds;
        }

        public static float SweepX(Bounds player, Bounds obstacle, float desired)
        {
            if (!DrillMovementSettings.Finite(desired))
                throw new ArgumentOutOfRangeException(nameof(desired));
            if (!Overlap(player.min.y, player.max.y, obstacle.min.y, obstacle.max.y) ||
                !Overlap(player.min.z, player.max.z, obstacle.min.z, obstacle.max.z))
                return desired;
            if (desired > 0 && player.max.x <= obstacle.min.x + Skin)
                return Mathf.Min(desired, Mathf.Max(0, obstacle.min.x - player.max.x - Skin));
            if (desired < 0 && player.min.x >= obstacle.max.x - Skin)
                return Mathf.Max(desired, Mathf.Min(0, obstacle.max.x - player.min.x + Skin));
            return desired;
        }

        public static float SweepForward(Bounds player, Bounds obstacle, float desired)
        {
            if (desired < 0 || !DrillMovementSettings.Finite(desired))
                throw new ArgumentOutOfRangeException(nameof(desired));
            if (!Overlap(player.min.y, player.max.y, obstacle.min.y, obstacle.max.y) ||
                !Overlap(player.min.x, player.max.x, obstacle.min.x, obstacle.max.x) || player.min.z >= obstacle.max.z - Skin)
                return desired;
            return Mathf.Min(desired, Mathf.Max(0, obstacle.min.z - player.max.z - Skin));
        }

        public static bool CentralTip(Vector3 tip, Bounds obstacle)
        {
            var tipHalfWidth = Mathf.Min(0.1f, obstacle.extents.x * 0.5f);
            return tip.x >= obstacle.min.x + tipHalfWidth && tip.x <= obstacle.max.x - tipHalfWidth &&
                   tip.y >= obstacle.min.y && tip.y <= obstacle.max.y &&
                   Mathf.Abs(tip.z - obstacle.min.z) <= Skin * 3;
        }

        private static bool Overlap(float minA, float maxA, float minB, float maxB) => minA < maxB && maxA > minB;
    }
}

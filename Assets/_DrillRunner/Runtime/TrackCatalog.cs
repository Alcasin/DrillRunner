using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrillRunner
{
    [CreateAssetMenu(menuName = "DrillRunner/Track Catalog")]
    public sealed class TrackCatalog : ScriptableObject
    {
        public float Width = 6;
        public float SectionLength = 40;
        public float InitialPower = 5;
        public TrackSection Fallback;
        public TrackSection[] Variants = Array.Empty<TrackSection>();
        [Min(0)] public int ResourceInterval = 3;
        public ObstacleRewardKind ResourceKind = ObstacleRewardKind.Copper;
    }

    /// <summary>Private xorshift state; layout never consumes Unity's global random state.</summary>
    public sealed class SectionSequence
    {
        private uint state;
        private readonly bool[] valid;
        private readonly int[] copper;
        private readonly int resourceInterval;
        private int ordinal;
        public SectionSequence(int seed, TrackCatalog catalog, float halfFootprint)
        {
            if (catalog == null || catalog.Fallback == null || catalog.Variants == null || catalog.Variants.Length == 0 || catalog.ResourceInterval < 0)
                throw new ArgumentException("A catalog needs variants and a safe fallback.");
            if (!IsSafe(catalog.Fallback, catalog, halfFootprint) || catalog.Fallback.Obstacles.Length != 0)
                throw new ArgumentException("Fallback must be an open, safe section of matching dimensions.");
            state = unchecked((uint)seed); if (state == 0) state = 0x9e3779b9;
            valid = new bool[catalog.Variants.Length];
            var resources = new List<int>();
            for (var i = 0; i < valid.Length; i++)
            {
                valid[i] = IsSafe(catalog.Variants[i], catalog, halfFootprint);
                if (valid[i])
                    foreach (var p in catalog.Variants[i].ReadPlacements())
                        if (p.Reward == catalog.ResourceKind) { resources.Add(i); break; }
            }
            copper = resources.ToArray(); resourceInterval = catalog.ResourceInterval;
            if (resourceInterval > 0 && copper.Length == 0) throw new ArgumentException("Resource cadence needs a safe, reachable Copper section.");
        }

        public int NextIndex()
        {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            var resource = resourceInterval > 0 && ordinal++ % resourceInterval == 0;
            var index = resource ? copper[state % (uint)copper.Length] : (int)(state % (uint)valid.Length);
            return valid[index] ? index : -1;
        }

        public static bool IsSafe(TrackSection section, TrackCatalog catalog, float half)
        {
            try
            {
                return section != null && Mathf.Abs(section.Width - catalog.Width) < 0.001f &&
                    Mathf.Abs(section.Length - catalog.SectionLength) < 0.001f &&
                    TrackSafety.Validate(section.Width, section.Length, half, catalog.InitialPower, section.ReadPlacements(), out _);
            }
            catch (ArgumentException) { return false; }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrillRunner
{
    [DisallowMultipleComponent]
    public sealed class TrackStreamer : MonoBehaviour
    {
        [SerializeField] private DrillMotor motor;
        [SerializeField] private TrackCatalog catalog;
        [SerializeField] private TrackSection[] starters = Array.Empty<TrackSection>();
        [SerializeField] private TrackSection[] starterTemplates = Array.Empty<TrackSection>();
        [SerializeField] private float aheadDistance = 200;
        [SerializeField] private float behindDistance = 80;
        private readonly List<TrackSection> active = new List<TrackSection>(12);
        private readonly HashSet<TrackSection> authored = new HashSet<TrackSection>();
        private SectionSequence sequence;
        private Transform generatedRoot;
        private float cursor;
        private float runLength, runAhead, runBehind;
        private TrackSection runFallback;
        private TrackSection[] variants;
        public TrackCatalog Catalog { get => catalog; set => catalog = value; }
        public IReadOnlyList<TrackSection> ActiveSections => active;
        public TrackSection[] StarterSections => starters;
        public float AheadDistance => aheadDistance;
        public float BehindDistance => behindDistance;
        public int GeneratedCount { get; private set; }

        public void Begin(int seed)
        {
            if (motor == null || catalog == null || !DrillMovementSettings.Finite(aheadDistance) ||
                !DrillMovementSettings.Finite(behindDistance) || aheadDistance < 160 || behindDistance < 40 ||
                !DrillMovementSettings.Finite(catalog.SectionLength) || catalog.SectionLength <= 0 ||
                (aheadDistance + behindDistance) / catalog.SectionLength > 32 ||
                Mathf.Abs(catalog.Width - motor.Settings.TrackWidth) > 0.001f || motor.Settings.TrackCenterX != 0 ||
                starters == null || starters.Length == 0 || starterTemplates == null || starterTemplates.Length != starters.Length)
                throw new ArgumentException("Track streaming needs compatible motor/catalog, starters, and camera-safe coverage.");
            var plan = new SectionSequence(seed, catalog, motor.Settings.FootprintHalfWidth);
            foreach (var template in starterTemplates)
                if (!SectionSequence.IsSafe(template, catalog, motor.Settings.FootprintHalfWidth))
                    throw new ArgumentException("An authored starter template failed route validation.");
            Cleanup();
            sequence = plan; GeneratedCount = 0; cursor = 0;
            variants = (TrackSection[])catalog.Variants.Clone(); runFallback = catalog.Fallback;
            runLength = catalog.SectionLength; runAhead = aheadDistance; runBehind = behindDistance;
            if (generatedRoot == null)
            {
                generatedRoot = new GameObject("Generated Sections").transform;
                generatedRoot.SetParent(transform, false);
            }
            for (var i = 0; i < starters.Length; i++)
            {
                var section = starters[i];
                if (section == null || Mathf.Abs(section.StartZ - cursor) > 0.001f ||
                    Mathf.Abs(section.Length - catalog.SectionLength) > 0.001f || Mathf.Abs(section.Width - catalog.Width) > 0.001f)
                    throw new ArgumentException("Starter modules must form a connected course from Z=0.");
                authored.Add(section); section.gameObject.SetActive(true); section.RestoreContent();
                active.Add(section); Register(section); cursor = section.EndZ;
            }
            Spawn(runFallback, -runLength);
            // One open buffer preserves the accepted opening and provides clear transitions.
            while (cursor < runAhead - 0.001f)
            {
                Spawn(runFallback, cursor); cursor += runLength;
            }
        }

        public void UpdateCoverage(float playerZ, DrillObstacle protectedTarget)
        {
            if (!DrillMovementSettings.Finite(playerZ) || playerZ < 0) throw new ArgumentOutOfRangeException(nameof(playerZ));
            if (sequence == null || motor.Motion.State == RunState.RunEnded) return;
            if (playerZ - cursor > runLength * 10000) throw new ArgumentOutOfRangeException(nameof(playerZ), "Diagnostic jump exceeds the bounded streaming workload.");
            // Fill from the current tail before pruning. Normal simulation requests stay
            // close to the tail; a large diagnostics jump relocates the window directly.
            if (playerZ > cursor + runAhead)
            {
                var windowStart = Mathf.Floor((playerZ - runBehind) / runLength) * runLength;
                while (cursor < windowStart) { sequence.NextIndex(); cursor += runLength; }
            }
            while (cursor < playerZ + runAhead - 0.001f)
            {
                var index = sequence.NextIndex();
                var prefab = index < 0 ? runFallback : variants[index];
                Spawn(prefab, cursor); cursor += runLength;
            }
            for (var i = active.Count - 1; i >= 0; i--)
            {
                var section = active[i];
                if (section.EndZ >= playerZ - runBehind || (protectedTarget != null && section.Contains(protectedTarget)) ||
                    (playerZ >= section.StartZ && playerZ < section.EndZ)) continue;
                Unregister(section); active.RemoveAt(i); section.gameObject.SetActive(false);
                if (!authored.Contains(section)) Destroy(section.gameObject);
            }
        }

        private void Spawn(TrackSection prefab, float z)
        {
            var section = Instantiate(prefab, new Vector3(0, 0, z), Quaternion.identity, generatedRoot);
            section.name = prefab.SectionId + " @ " + z.ToString("0");
            section.RestoreContent(); active.Add(section); Register(section); GeneratedCount++;
        }

        private void Register(TrackSection section)
        { foreach (var obstacle in section.Obstacles) motor.RegisterObstacle(obstacle); }
        private void Unregister(TrackSection section)
        { foreach (var obstacle in section.Obstacles) motor.UnregisterObstacle(obstacle); }

        private void Cleanup()
        {
            foreach (var section in active)
            {
                if (section == null) continue;
                Unregister(section); section.gameObject.SetActive(false);
                if (!authored.Contains(section)) Destroy(section.gameObject);
            }
            active.Clear();
        }

        private void OnDestroy() { if (motor != null) Cleanup(); }
    }
}

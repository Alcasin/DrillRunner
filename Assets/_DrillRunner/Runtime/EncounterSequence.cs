using System;
using System.Collections.Generic;

namespace DrillRunner
{
    /// <summary>Selection/pacing only. The original TrackStreamer owns all spawning, registration and retirement.</summary>
    public sealed class EncounterSequence
    {
        private sealed class Entry
        {
            public EncounterDefinition Definition;
            public TrackSection Section;
            public EncounterCategory Category;
            public EncounterArrangement Arrangement;
            public int Pressure, Minimum, Early, Weight, Late;
            public bool Copper;
            public bool Empty;
        }
        private readonly Entry[] entries;
        private readonly bool[,] joins;
        private readonly int[] eligible;
        private readonly TrackSection fallback;
        private readonly int early, richer, copperGap, recoveryGap, pressureLimit;
        private readonly bool firstCopper, calmFirst, relief;
        private readonly EncounterArrangement[] recent;
        private readonly int emptySpacing;
        private readonly int openingCopperDeadline;
        private bool copperSeen;
        private int recentCount, sinceEmpty = 1000000, previousPressure;
        private uint state;
        private int ordinal, sinceCopper, sinceRecovery, consecutivePressure, previous = -1;
        private EncounterCategory? previousCategory;
        public EncounterDefinition Selected { get; private set; }
        public int[] Counts { get; } = new int[6];
        public int Ordinal => ordinal;
        public int ValidPatterns => entries.Length;
        public int RejectedPatterns { get; private set; }
        public int FallbackCount { get; private set; }
        public int ArrangementRelaxations { get; private set; }
        public string Rejection { get; private set; } = "";
        public EncounterSequence(int seed, EncounterLibrary library, EncounterEnvelope envelope)
        {
            if (library == null || library.Catalog == null || library.Patterns == null || library.EarlySections < 1 ||
                library.RicherFromSection < library.EarlySections || library.MaximumCopperGap < 2 || library.MaximumRecoveryGap < 2 || library.MaximumConsecutivePressure < 1 ||
                library.ArrangementMemory < 0 || library.ArrangementMemory > 4 || library.EmptyRecoverySpacing < 0 || library.OpeningCopperBySelection < 0 || library.OpeningCopperBySelection > 3)
                throw new ArgumentException("Invalid encounter pacing configuration.");
            fallback = library.Catalog.Fallback;
            if (!SectionSequence.IsSafe(fallback, library.Catalog, envelope.HalfWidth) || fallback.Obstacles.Length != 0)
                throw new ArgumentException("Encounter fallback must remain open and safe.");
            early = library.EarlySections; richer = library.RicherFromSection; copperGap = library.MaximumCopperGap;
            recoveryGap = library.MaximumRecoveryGap; pressureLimit = library.MaximumConsecutivePressure;
            firstCopper = library.FirstSelectionCopper; calmFirst = library.CalmFirstEncounter; relief = library.ReliefAfterHighPressure;
            recent = new EncounterArrangement[library.ArrangementMemory]; emptySpacing = library.EmptyRecoverySpacing;
            openingCopperDeadline = library.OpeningCopperBySelection;
            state = unchecked((uint)seed); if (state == 0) state = 0x9e3779b9;
            var accepted = new List<Entry>(); var seen = new HashSet<TrackSection>();
            foreach (var definition in library.Patterns)
            {
                if (!EncounterSafety.Validate(definition, library, envelope, out var reason) || !seen.Add(definition.Section) ||
                    definition.MinimumOrdinal < 0 || definition.EarlyWeight < 0 || definition.Weight < 0 || definition.LateWeight < 0 ||
                    definition.EarlyWeight > 10000 || definition.Weight > 10000 || definition.LateWeight > 10000)
                { RejectedPatterns++; Rejection = reason.Length > 0 ? reason : "Duplicate section or invalid weight."; continue; }
                var copper = false; foreach (var p in definition.Section.ReadPlacements()) copper |= p.Reward == library.ResourceKind;
                accepted.Add(new Entry { Definition = definition, Section = definition.Section, Category = definition.Category,
                    Pressure = definition.Pressure, Minimum = definition.MinimumOrdinal, Early = definition.EarlyWeight,
                    Weight = definition.Weight, Late = definition.LateWeight, Copper = copper, Arrangement = definition.Arrangement, Empty = definition.Section.Obstacles.Length == 0 });
            }
            entries = accepted.ToArray(); eligible = new int[entries.Length]; joins = new bool[entries.Length, entries.Length];
            for (var a = 0; a < entries.Length; a++) for (var b = 0; b < entries.Length; b++)
                joins[a, b] = EncounterSafety.Transition(entries[a].Section, entries[b].Section, library, envelope, out _);
        }

        public TrackSection Next()
        {
            var wantCopper = (firstCopper && ordinal == 0) || sinceCopper >= copperGap - 1 || (!copperSeen && openingCopperDeadline > 0 && ordinal >= openingCopperDeadline - 1);
            var wantRecovery = sinceRecovery >= recoveryGap - 1;
            var count = 0; var total = 0;
            // Layout-family cooldown is soft; reachability, relief and resource cadence remain hard rules.
            for (var pass = 0; pass < 2; pass++)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var p = entries[i]; var weight = ordinal < early ? p.Early : ordinal >= richer ? p.Late : p.Weight;
                    if (weight <= 0 || ordinal < p.Minimum || (previousCategory.HasValue && p.Category == previousCategory.Value) ||
                        (p.Pressure == 2 && consecutivePressure >= pressureLimit) || (wantCopper && !p.Copper) ||
                        (wantRecovery && p.Category != EncounterCategory.Recovery) || (previous >= 0 && !joins[previous, i]) ||
                        (calmFirst && ordinal == 0 && p.Pressure > 0) || (relief && previousPressure == 2 && p.Pressure > 0) ||
                        (p.Empty && sinceEmpty < emptySpacing)) continue;
                    var repeated = false; for (var h = 0; h < recentCount; h++) repeated |= recent[h] == p.Arrangement;
                    if (pass == 0 && repeated) continue;
                    eligible[count++] = i; total = checked(total + weight);
                }
                if (total > 0) { if (pass > 0) ArrangementRelaxations++; break; }
            }
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            if (total == 0)
            {
                Selected = null; previous = -1; previousCategory = EncounterCategory.Recovery; consecutivePressure = sinceRecovery = 0;
                previousPressure = 0; sinceEmpty = 0;
                sinceCopper++; ordinal++; FallbackCount++; Counts[(int)EncounterCategory.Recovery]++;
                Rejection = entries.Length == 0 ? Rejection : "No pattern satisfies the current pacing/transition constraints."; return fallback;
            }
            var roll = (int)(state % (uint)total); var chosen = -1;
            for (var n = 0; n < count; n++)
            {
                var i = eligible[n]; var p = entries[i]; var weight = ordinal < early ? p.Early : ordinal >= richer ? p.Late : p.Weight;
                if (roll < weight) { chosen = i; break; } roll -= weight;
            }
            var entry = entries[chosen]; Selected = entry.Definition; previous = chosen; previousCategory = entry.Category;
            copperSeen |= entry.Copper;
            sinceCopper = entry.Copper ? 0 : sinceCopper + 1; sinceRecovery = entry.Category == EncounterCategory.Recovery ? 0 : sinceRecovery + 1;
            consecutivePressure = entry.Pressure == 2 ? consecutivePressure + 1 : 0; previousPressure = entry.Pressure;
            sinceEmpty = entry.Empty ? 0 : sinceEmpty + 1;
            if (recent.Length > 0)
            {
                for (var h = Math.Min(recentCount, recent.Length - 1); h > 0; h--) recent[h] = recent[h - 1];
                recent[0] = entry.Arrangement; recentCount = Math.Min(recent.Length, recentCount + 1);
            }
            Counts[(int)entry.Category]++; ordinal++; return entry.Section;
        }
    }
}

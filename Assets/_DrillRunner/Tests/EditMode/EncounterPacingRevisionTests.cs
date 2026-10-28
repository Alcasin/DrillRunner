using System.Collections.Generic;
using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;

namespace DrillRunner.Tests.EditMode
{
    public sealed class EncounterPacingRevisionTests
    {
        private EncounterLibrary Library => AssetDatabase.LoadAssetAtPath<EncounterLibrary>(GameplayVarietySetup.LibraryPath);
        [Test] public void EverySeedGetsCalmMultiTargetChoicesAndAccessibleCopperBeforeOneHundredFifty()
        {
            var library = Library; Assert.That(library.IntroductionSections, Is.EqualTo(1)); Assert.That(library.OpeningBufferSections, Is.Zero);
            var openings = new HashSet<string>();
            for (var seed = 0; seed < 256; seed++)
            {
                var sequence = new EncounterSequence(seed, library, EncounterEnvelope.Accepted); var copperZ = float.MaxValue; var multiZ = float.MaxValue; var ids = "";
                for (var ordinal = 0; ordinal < 3; ordinal++)
                {
                    var section = sequence.Next(); var start = library.Catalog.SectionLength * (library.IntroductionSections + ordinal); ids += section.SectionId + "/";
                    if (ordinal == 0) Assert.That(sequence.Selected.Pressure, Is.Zero);
                    Assert.That(section.Obstacles.All(o => o.Definition.Hardness <= 5), Is.True);
                    if (section.Obstacles.Length >= 2) multiZ = System.Math.Min(multiZ, start + section.Obstacles.Min(o => o.transform.localPosition.z));
                    foreach (var ore in section.Obstacles.Where(o => o.Definition.RewardKind == ObstacleRewardKind.Copper)) copperZ = System.Math.Min(copperZ, start + ore.transform.localPosition.z);
                }
                Assert.That(multiZ, Is.LessThan(100)); Assert.That(copperZ, Is.LessThan(150)); openings.Add(ids);
            }
            Assert.That(openings.Count, Is.GreaterThan(20), "An early guarantee must not mean one scripted opening.");
        }
        [Test] public void ArrangementMemoryPressureReliefAndEmptySpacingRemainCompatibleWithResourceCadence()
        {
            var library = Library; var seen = new HashSet<string>(); long relaxations = 0;
            for (var seed = 0; seed < 64; seed++)
            {
                var sequence = new EncounterSequence(seed, library, EncounterEnvelope.Accepted); var previousPressure = 0; var lastEmpty = -100;
                for (var ordinal = 0; ordinal < 1000; ordinal++)
                {
                    var section = sequence.Next(); var current = sequence.Selected; Assert.That(current, Is.Not.Null, sequence.Rejection); seen.Add(section.SectionId);
                    if (previousPressure == 2) Assert.That(current.Pressure, Is.Zero);
                    if (section.Obstacles.Length == 0) { Assert.That(ordinal - lastEmpty, Is.GreaterThanOrEqualTo(library.EmptyRecoverySpacing)); lastEmpty = ordinal; }
                    previousPressure = current.Pressure;
                }
                relaxations += sequence.ArrangementRelaxations; Assert.That(sequence.FallbackCount, Is.Zero);
            }
            Assert.That(seen.Count, Is.EqualTo(14)); Assert.That(relaxations, Is.LessThan(64000 * 0.2));
        }
        [Test] public void IntroductionJoinsEveryPatternAtMaximumAcceptedSpeed()
        {
            var introduction = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(ModularTrackSetup.SectionFolder + "/Starter0.prefab").GetComponent<TrackSection>();
            foreach (var pattern in Library.Patterns)
                Assert.That(EncounterSafety.Transition(introduction, pattern.Section, Library, EncounterEnvelope.Accepted, out _), Is.True, pattern.name);
        }
        [Test] public void GuardedChoiceContainsAnIndependentCheaperWoodOptionAndAValidOreLine()
        {
            foreach (var pattern in Library.Patterns.Where(p => p.name.StartsWith("OreGuard")))
            {
                var wood = pattern.Section.Obstacles.Single(o => o.Definition.MaterialId == "wood"); var ore = pattern.Section.Obstacles.Single(o => o.Definition.RewardKind == ObstacleRewardKind.Copper);
                Assert.That(EncounterSafety.OverlapsX(wood.transform.localPosition.x, 0.85f, ore.WorldBounds), Is.False);
                Assert.That(EncounterSafety.OverlapsX(ore.transform.localPosition.x, 0.85f, wood.WorldBounds), Is.False);
                Assert.That(EncounterSafety.Validate(pattern, Library, EncounterEnvelope.Accepted, out _), Is.True);
                var cheap = wood.Definition.Durability / 50 * wood.Definition.DrillingEnergyPerSecond;
                var guarded = pattern.Section.Obstacles.Where(o => EncounterSafety.OverlapsX(ore.transform.localPosition.x, 0.85f, o.WorldBounds))
                    .Sum(o => o.Definition.Durability / 50 * o.Definition.DrillingEnergyPerSecond);
                Assert.That(cheap, Is.EqualTo(11.2f).Within(0.01)); Assert.That(guarded, Is.EqualTo(51.6f).Within(0.01));
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.EditMode
{
    public sealed class EncounterTests
    {
        private EncounterLibrary Library => AssetDatabase.LoadAssetAtPath<EncounterLibrary>(GameplayVarietySetup.LibraryPath);
        [Test] public void SixCategoriesHaveFourteenAuthoredPatternsWithSharedPresentationAndNoExtraBlockingGeometry()
        {
            var library = Library; Assert.That(library.Patterns.Length, Is.EqualTo(14));
            Assert.That(library.Patterns.Select(p => p.Category).Distinct().Count(), Is.EqualTo(6));
            foreach (var pattern in library.Patterns)
            {
                Assert.That(EncounterSafety.Validate(pattern, library, EncounterEnvelope.Accepted, out var reason), Is.True, pattern.name + ": " + reason);
                Assert.That(pattern.Section.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(pattern.Section.Obstacles.Length + 1));
                foreach (var obstacle in pattern.Section.Obstacles) Assert.That(obstacle.GetComponent<ObstacleAppearance>(), Is.Not.Null);
                Assert.That(pattern.Section.GetComponentsInChildren<ParticleSystem>(true), Is.Empty);
            }
        }
        [Test] public void SeedsReplayIncludingSkippedPicksAndNeverConsumeDecorativeRandomState()
        {
            var state = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(418); var expected = UnityEngine.Random.value; UnityEngine.Random.InitState(418);
                var a = new EncounterSequence(1337, Library, EncounterEnvelope.Accepted); var b = new EncounterSequence(1337, Library, EncounterEnvelope.Accepted);
                var c = new EncounterSequence(901, Library, EncounterEnvelope.Accepted);
                var first = Enumerable.Range(0, 1000).Select(_ => a.Next().SectionId).ToArray();
                Assert.That(Enumerable.Range(0, 1000).Select(_ => b.Next().SectionId), Is.EqualTo(first));
                Assert.That(Enumerable.Range(0, 1000).Select(_ => c.Next().SectionId), Is.Not.EqualTo(first));
                Assert.That(UnityEngine.Random.value, Is.EqualTo(expected));
            }
            finally { UnityEngine.Random.state = state; }
        }
        [Test] public void ManySeedsMaintainDiversityResourceRecoveryAndPressureBoundsWithoutFallbacks()
        {
            var seen = new System.Collections.Generic.HashSet<string>(); var counts = new long[6];
            for (var seed = 0; seed < 128; seed++)
            {
                var sequence = new EncounterSequence(seed, Library, EncounterEnvelope.Accepted);
                var copperGap = 0; var recoveryGap = 0; var high = 0; EncounterCategory? previous = null;
                for (var i = 0; i < 1000; i++)
                {
                    var section = sequence.Next(); var p = sequence.Selected; Assert.That(p, Is.Not.Null, sequence.Rejection);
                    seen.Add(section.SectionId); counts[(int)p.Category]++;
                    copperGap = section.ReadPlacements().Any(o => o.Reward == ObstacleRewardKind.Copper) ? 0 : copperGap + 1;
                    recoveryGap = p.Category == EncounterCategory.Recovery ? 0 : recoveryGap + 1; high = p.Pressure == 2 ? high + 1 : 0;
                    Assert.That(copperGap, Is.LessThan(Library.MaximumCopperGap)); Assert.That(recoveryGap, Is.LessThan(Library.MaximumRecoveryGap));
                    Assert.That(high, Is.LessThanOrEqualTo(Library.MaximumConsecutivePressure)); Assert.That(p.Category, Is.Not.EqualTo(previous)); previous = p.Category;
                }
                Assert.That(sequence.FallbackCount, Is.Zero);
            }
            Assert.That(seen.Count, Is.EqualTo(14)); Assert.That(counts.All(c => c > 1000), Is.True);
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/M7-Distribution.txt", "128 seeds x 1000 sections (5,120 km of selection). Counts: " + string.Join(", ", counts));
        }
        [Test] public void EntryAndEveryPairPassTheActualMotionAndSweptFootprintAtMaximumSpeeds()
        {
            foreach (var speed in new[] { 24f, 26f }) foreach (var a in Library.Patterns) foreach (var b in Library.Patterns)
            foreach (var startX in new[] { -2.15f, 2.15f })
            {
                var envelope = new EncounterEnvelope(6, 0.85f, 1.85f, 1.15f, speed, 18);
                Assert.That(EncounterSafety.Transition(a.Section, b.Section, Library, envelope, out _), Is.True);
                var placements = a.Section.ReadPlacements().Concat(b.Section.ReadPlacements().Select(p =>
                { var bounds = p.Bounds; bounds.center += Vector3.forward * 40; return new TrackPlacement(bounds, p.Hardness, p.Reward); })).ToArray();
                var movement = new DrillMotion(new DrillMovementSettings { CruiseSpeed = speed, MaximumSpeed = speed, MinimumLaunchBoost = 0, MaximumLaunchBoost = 0 }, new Vector3(startX, 0, 0));
                movement.Press(); movement.Release(); var phase = 0; var clock = 0f; var commanded = false; const float dt = 1f / 240;
                while (movement.Position.z < 80)
                {
                    var nextPhase = movement.Position.z < 40 ? 0 : 1;
                    if (nextPhase != phase) { phase = nextPhase; clock = 0; commanded = false; movement.CancelPointer(); }
                    if (!commanded && clock >= Library.ReactionSeconds)
                    { movement.Press(); var x = phase == 0 ? a.BypassX : b.BypassX; movement.Drag(new Vector2((x - movement.Position.x) * 540 / 8, 0), 540); commanded = true; }
                    var delta = movement.PredictPosition(dt) - movement.Position;
                    var player = new Bounds(movement.Position + new Vector3(0, 0.65f, 0.35f), new Vector3(1.7f, 1.3f, 3));
                    foreach (var p in placements)
                        if (Mathf.Abs(DrillContacts.SweepX(player, p.Bounds, delta.x) - delta.x) > 0.00001f)
                            Assert.Fail("Lateral collision: " + a.name + " -> " + b.name);
                    player.center += Vector3.right * delta.x;
                    foreach (var p in placements)
                        if (DrillContacts.SweepForward(player, p.Bounds, delta.z) < delta.z - 0.00001f)
                            Assert.Fail("Forward collision: " + a.name + " -> " + b.name + " at " + movement.Position);
                    movement.Step(dt); clock += dt;
                }
            }
        }
        [Test] public void EveryOreHasAnAlignedStartingPowerMiningLineAndGuardEnergyIsAMeaningfulChoice()
        {
            var costs = new System.Collections.Generic.List<float>();
            foreach (var pattern in Library.Patterns) foreach (var ore in pattern.Section.Obstacles.Where(o => o.Definition.RewardKind == ObstacleRewardKind.Copper))
            {
                var x = ore.transform.localPosition.x; var cost = 0f;
                foreach (var obstacle in pattern.Section.Obstacles)
                {
                    if (!EncounterSafety.OverlapsX(x, 0.85f, obstacle.WorldBounds)) continue;
                    Assert.That(obstacle.Definition.Hardness, Is.LessThanOrEqualTo(5));
                    cost += obstacle.Definition.Durability / 50 * obstacle.Definition.DrillingEnergyPerSecond;
                }
                Assert.That(cost, Is.InRange(18f, 60f)); costs.Add(cost);
            }
            Assert.That(costs.Max(), Is.GreaterThan(costs.Min() * 2));
        }
        [Test] public void InvalidOverlappingOrEarlyPatternsAreRejectedAndProduceDeterministicOpenFallback()
        {
            var library = Object.Instantiate(Library); var definition = Object.Instantiate(Library.Patterns.First(p => p.Section.Obstacles.Length > 1));
            var section = Object.Instantiate(definition.Section.gameObject);
            try
            {
                definition.Section = section.GetComponent<TrackSection>(); library.Patterns = new[] { definition };
                definition.Section.Obstacles[0].transform.localPosition = new Vector3(0, 1, 4.5f);
                Assert.That(EncounterSafety.Validate(definition, library, EncounterEnvelope.Accepted, out _), Is.False);
                definition.Section.Obstacles[0].transform.localPosition = definition.Section.Obstacles[1].transform.localPosition;
                Assert.That(EncounterSafety.Validate(definition, library, EncounterEnvelope.Accepted, out _), Is.False);
                var plan = new EncounterSequence(1, library, EncounterEnvelope.Accepted); Assert.That(plan.RejectedPatterns, Is.EqualTo(1));
                for (var i = 0; i < 100; i++) Assert.That(plan.Next(), Is.SameAs(library.Catalog.Fallback));
                Assert.That(plan.FallbackCount, Is.EqualTo(100)); Assert.That(plan.Rejection, Is.Not.Empty);
            }
            finally { Object.DestroyImmediate(section); Object.DestroyImmediate(definition); Object.DestroyImmediate(library); }
        }
        [Test] public void FasterOrSlowerCustomMovementCannotSilentlyReuseAnInvalidCertificate()
        {
            var pattern = Library.Patterns.First(p => p.Category == EncounterCategory.DenseDrilling);
            Assert.That(EncounterSafety.Validate(pattern, Library, new EncounterEnvelope(6, 0.85f, 1.85f, 1.15f, 60, 18), out _), Is.False);
            Assert.That(EncounterSafety.Validate(pattern, Library, new EncounterEnvelope(6, 0.85f, 1.85f, 1.15f, 26, 1), out _), Is.False);
        }
        [Test] public void GeometricallyReachableButUnaffordableOreGuardsFailValidation()
        {
            var definition = Object.Instantiate(Library.Patterns.First(p => p.name == "OreGuardRight"));
            var section = Object.Instantiate(definition.Section.gameObject);
            var guard = section.GetComponent<TrackSection>().Obstacles.First(o => o.Definition.MaterialId == "brick");
            var material = Object.Instantiate(guard.Definition);
            try
            {
                material.Durability = 500; var data = new SerializedObject(guard); data.FindProperty("definition").objectReferenceValue = material; data.ApplyModifiedProperties();
                definition.Section = section.GetComponent<TrackSection>();
                Assert.That(SectionSequence.IsSafe(definition.Section, Library.Catalog, 0.85f), Is.True, "A static geometric test alone would accept this route.");
                Assert.That(EncounterSafety.Validate(definition, Library, EncounterEnvelope.Accepted, out var reason), Is.False);
                Assert.That(reason, Does.Contain("Energy budget"));
            }
            finally { Object.DestroyImmediate(section); Object.DestroyImmediate(definition); Object.DestroyImmediate(material); }
        }
        [Test] public void AcceptedProgressionKeepsTheSameChoicesAndThresholds()
        {
            var stone = Library.Patterns.SelectMany(p => p.Section.Obstacles).First(o => o.Definition.MaterialId == "stone").Definition;
            foreach (var power in new[] { 5, 7, 10, 12, 18 })
            {
                var durability = new ObstacleDurability(stone); durability.Drill(power, 10, 1);
                Assert.That(durability.Remaining, Is.EqualTo(power < 9 ? 240 : 240 - power * 10).Within(0.001));
            }
            Assert.That(stone.Hardness, Is.EqualTo(9)); Assert.That(stone.Durability, Is.EqualTo(240));
        }
        [Test] public void M7SetupPreservesSceneGuidsCustomWorkAndAllTuningOnReruns()
        {
            const string copy = "Assets/_DrillRunner/Scenes/M7SetupTemporary.unity";
            var paths = new[] { FoundationSetup.ScenePath, GameplayVarietySetup.LibraryPath, ModularTrackSetup.CatalogPath };
            var bytes = paths.Select(File.ReadAllBytes).ToArray(); Assert.That(AssetDatabase.CopyAsset(FoundationSetup.ScenePath, copy), Is.True);
            try
            {
                var scene = EditorSceneManager.OpenScene(copy, OpenSceneMode.Additive);
                try
                {
                    var custom = new GameObject("M7 Custom Work"); SceneManager.MoveGameObjectToScene(custom, scene); EditorSceneManager.SaveScene(scene);
                    var before = File.ReadAllBytes(copy); var guid = AssetDatabase.AssetPathToGUID(copy);
                    GameplayVarietySetup.SetupScene(copy); GameplayVarietySetup.SetupScene(copy);
                    Assert.That(File.ReadAllBytes(copy), Is.EqualTo(before)); Assert.That(AssetDatabase.AssetPathToGUID(copy), Is.EqualTo(guid));
                    Assert.That(custom, Is.Not.Null); EditorSceneManager.MarkSceneDirty(scene); Assert.Throws<InvalidOperationException>(() => GameplayVarietySetup.SetupScene(copy));
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            finally { AssetDatabase.DeleteAsset(copy); }
            for (var i = 0; i < paths.Length; i++) Assert.That(File.ReadAllBytes(paths[i]), Is.EqualTo(bytes[i]));
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public sealed class EncounterPlayModeTests
    {
        private WorkshopController flow;
        private DrillMotor motor;
        private TrackStreamer track;
        private PrototypeHud hud;
        private DrillPresentation presentation;
        private EncounterLibrary library;
        private TrackCatalog normalCatalog;
        private readonly List<Object> owned = new List<Object>();
        private readonly Dictionary<string, int> ordinals = new Dictionary<string, int>();
        private readonly List<string> balance = new List<string>();
        [UnitySetUp] public IEnumerator Open() { yield return Load(); }
        private IEnumerator Load()
        {
            WorkshopController.StoreFactory = () => new MemoryProgressStore(); yield return HistoricalRunFixture.LoadScene(); yield return null;
            flow = Object.FindFirstObjectByType<WorkshopController>(); motor = flow.Run.Motor; track = flow.Run.Track;
            hud = Object.FindFirstObjectByType<PrototypeHud>(); presentation = Object.FindFirstObjectByType<DrillPresentation>();
            motor.enabled = false; motor.GetComponent<DrillPointerInput>().enabled = false; presentation.SendMessage("OnApplicationFocus", true);
            library = track.Encounters; normalCatalog = track.Catalog; ordinals.Clear();
            var sequence = new EncounterSequence(1337, library, EncounterEnvelope.Accepted);
            for (var i = 0; i < 500 && ordinals.Count < library.Patterns.Length; i++)
            { var section = sequence.Next(); if (!ordinals.ContainsKey(section.SectionId)) ordinals.Add(section.SectionId, i); }
            Assert.That(ordinals.Count, Is.EqualTo(14));
        }
        [UnityTearDown] public IEnumerator Close()
        {
            yield return HistoricalRunFixture.UnloadScene(); WorkshopController.StoreFactory = () => new MemoryProgressStore();
            foreach (var item in owned) if (item != null) Object.Destroy(item); owned.Clear();
        }
        private void Launch() { motor.Motion.Press(); motor.Simulate(1.5f); motor.Motion.Release(); motor.Simulate(0.001f); }
        private void Steer(float x)
        {
            if (motor.Motion.State != RunState.Moving && motor.Motion.State != RunState.Blocked) return;
            motor.Motion.CancelPointer(); motor.Motion.Press(); motor.Motion.Drag(new Vector2((x - motor.Motion.Position.x) * 540 / 8, 0), 540);
        }
        private void Tick(float seconds = 1f / 60)
        { motor.Simulate(seconds); track.UpdateCoverage(motor.Motion.Position.z, motor.ContactTarget); }
        private float Stage(EncounterDefinition definition, float x = 0)
        {
            track.Catalog = normalCatalog; flow.Run.UseFixedSeed = true; flow.Run.ConfiguredSeed = 1337; hud.Restart();
            var start = track.EncounterStartZ + ordinals[definition.Section.SectionId] * 40; track.UpdateCoverage(start, null);
            motor.Motion.ResolvePosition(new Vector3(x, 0, start), true); motor.transform.position = motor.Motion.Position;
            Assert.That(track.ActiveSections.Single(s => s.StartZ == start).SectionId, Is.EqualTo(definition.Section.SectionId));
            Assert.That(track.ActiveEncounter, Is.SameAs(definition)); return start;
        }
        private static PlayerProgress Profile(int index)
        {
            if (index < 3) { var level = index == 0 ? 0 : index == 1 ? 2 : 5; return new PlayerProgress { PowerLevel = level, EnergyLevel = level, DriveLevel = level }; }
            var advanced = index == 4 ? 5 : 0;
            return new PlayerProgress { FormId = ProgressionConfig.WorkshopForm, FormSchema = 1, PowerLevel = 5, EnergyLevel = 5, DriveLevel = 5,
                WorkshopPowerLevel = advanced, WorkshopEnergyLevel = advanced, WorkshopDriveLevel = advanced };
        }
        private void ApplyProfile(int index)
        {
            hud.Restart(); var profile = Profile(index); motor.ApplyAttributes(flow.Configuration.Calculate(profile));
            flow.Visuals.Show(flow.Configuration.ForForm(profile.FormId));
        }
        [UnityTest] public IEnumerator AllPatternsHaveRealMotorBypassesFromBothEdgesAtFiveAcceptedProgressionLevels()
        {
            for (var profile = 0; profile < 5; profile++) foreach (var pattern in library.Patterns) foreach (var x in new[] { -2.15f, 2.15f })
            {
                ApplyProfile(profile); var start = Stage(pattern, x); Launch(); Tick(library.ReactionSeconds); Steer(pattern.BypassX);
                var count = 0;
                while (motor.Motion.Position.z < start + 41.2f && count++ < 1000)
                {
                    Tick();
                    Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving), pattern.name + " Power " + motor.DrillPower);
                }
                Assert.That(motor.Motion.Position.z, Is.GreaterThanOrEqualTo(start + 41.2f));
                Assert.That(flow.Run.Rewards.ObstacleCoins, Is.Zero); Assert.That(flow.Run.Rewards.Copper, Is.Zero);
                Assert.That(track.ActiveSections.Count, Is.LessThanOrEqualTo(10)); yield return null;
            }
        }
        [UnityTest] public IEnumerator AllOreRoutesAreMineableAtStartingPowerAndBankOnceWhileBypassingAwardsNothing()
        {
            foreach (var definition in library.Patterns.Where(p => p.Section.Obstacles.Any(o => o.Definition.RewardKind == ObstacleRewardKind.Copper)))
            {
                ApplyProfile(0); var oreX = definition.Section.Obstacles.First(o => o.Definition.RewardKind == ObstacleRewardKind.Copper).transform.localPosition.x;
                var start = Stage(definition); var responses = presentation.Effects.CopperResponses; var copperBefore = flow.Service.Snapshot.Copper;
                Launch(); Steer(oreX); var ticks = 0;
                while (flow.Run.Rewards.Copper == 0 && motor.Motion.State != RunState.RunEnded && ticks++ < 2000) Tick();
                Assert.That(flow.Run.Rewards.Copper, Is.EqualTo(3), definition.name); Assert.That(presentation.Effects.CopperResponses, Is.EqualTo(responses + 1));
                yield return Capture("OreMined-" + definition.name);
                var ore = track.ActiveSections.Single(s => s.StartZ == start).Obstacles.First(o => o.Definition.RewardKind == ObstacleRewardKind.Copper);
                Assert.That(ore.IsSolid, Is.False); ore.Restore(); ore.Durability.Drill(5, 10, 10);
                Assert.That(flow.Run.Rewards.Copper, Is.EqualTo(3)); Assert.That(presentation.Effects.CopperResponses, Is.EqualTo(responses + 1));
                motor.Energy.Consume(10000, 1); Tick(); Assert.That(flow.IsBanked, Is.True);
                for (var i = 0; i < 4; i++) flow.Run.SendMessage("Complete");
                Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(copperBefore + 3)); Assert.That(presentation.Effects.ParticleCount, Is.Zero);
                Assert.That(flow.OpenWorkshop(), Is.True); Assert.That(flow.StartRun(), Is.True); yield return null;
            }
        }
        [UnityTest] public IEnumerator PowerTestsKeepStoneBlockedBelowNineAndMakeItFasterAfterUpgrades()
        {
            var escape = library.Patterns.First(p => p.name == "EscapeRight");
            Stage(escape, 0.15f); Launch(); var escapeTicks = 0;
            while (motor.Motion.State == RunState.Moving && escapeTicks++ < 1000) Tick();
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Blocked)); Steer(escape.BypassX);
            escapeTicks = 0; while (motor.Motion.State == RunState.Blocked && escapeTicks++ < 120) Tick();
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            var definition = library.Patterns.First(p => p.name == "PowerRight");
            for (var profile = 0; profile < 5; profile++)
            {
                ApplyProfile(profile); var start = Stage(definition, 1); Launch();
                var stone = track.ActiveSections.Single(s => s.StartZ == start).Obstacles.First(o => o.Definition.MaterialId == "stone");
                var ticks = 0; while (motor.Motion.State == RunState.Moving && ticks++ < 1000) Tick();
                if (motor.DrillPower < 9)
                {
                    Assert.That(motor.Motion.State, Is.EqualTo(RunState.Blocked)); Assert.That(stone.Durability.Remaining, Is.EqualTo(240));
                    Assert.That(presentation.Effects.IsDrilling, Is.False); Steer(definition.BypassX);
                    ticks = 0; while (motor.Motion.State != RunState.Moving && ticks++ < 300)
                    {
                        if (motor.Motion.State == RunState.Drilling) Assert.That(motor.ContactTarget.Definition.MaterialId, Is.EqualTo("wood"), "The lower-power escape can enter the adjacent drillable Wood option.");
                        Tick();
                    }
                    Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving)); Assert.That(stone.Durability.Remaining, Is.EqualTo(240));
                }
                else
                {
                    Assert.That(motor.Motion.State, Is.EqualTo(RunState.Drilling));
                    ticks = 0; while (!stone.Durability.IsDestroyed && ticks++ < 300) Tick(); Assert.That(stone.Durability.IsDestroyed, Is.True);
                    Assert.That(flow.Run.Rewards.ObstacleCoins, Is.EqualTo(3));
                }
                yield return Capture("Power-" + motor.DrillPower.ToString("0"));
            }
        }
        [UnityTest] public IEnumerator SixCategoriesAndConsecutiveJoinsRenderInPortraitWithExistingPresentation()
        {
            foreach (var name in new[] { "EasyRight", "DenseRight", "OreGuardRight", "EscapeRight", "PowerRight", "RecoveryOpen" })
            {
                ApplyProfile(0); var definition = library.Patterns.First(p => p.name == name); var start = Stage(definition);
                Launch(); yield return Capture(name + "-Approach"); Tick(0.4f);
                Steer(definition.Category == EncounterCategory.PrecisionEscape || definition.Category == EncounterCategory.Recovery ? definition.BypassX : name == "PowerRight" ? 1 : 0.65f);
                Tick(0.3f); yield return Capture(name + "-Choice"); Tick(0.6f); yield return Capture(name + "-Response");
                if (name == "RecoveryOpen") Assert.That(motor.Energy.Remaining, Is.LessThan(motor.Energy.Capacity));
            }
            ApplyProfile(0); var first = library.Patterns.First(p => p.name == "EasyRight"); var origin = Stage(first, first.BypassX); Launch();
            var frames = 0;
            while (motor.Motion.Position.z < origin + 95 && motor.Motion.State != RunState.RunEnded)
            {
                var active = track.ActiveEncounter; if (active != null) Steer(active.BypassX); Tick(0.1f);
                if (frames < 3 && motor.Motion.Position.z > origin + 34 + frames * 8)
                { yield return Capture("Transition-" + frames); frames++; }
            }
            Assert.That(frames, Is.EqualTo(3)); Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length, Is.EqualTo(5));
            Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(5));
        }
        [UnityTest] public IEnumerator LongDistanceWindowsStayBoundedAndDiagnosticSkippingPreservesTheReplay()
        {
            for (var seed = 0; seed < 8; seed++)
            {
                flow.Run.UseFixedSeed = true; flow.Run.ConfiguredSeed = seed; hud.Restart();
                for (var z = 2000; z <= 50000; z += 2000)
                {
                    track.UpdateCoverage(z, null); yield return null;
                    Assert.That(track.ActiveSections.Count, Is.LessThanOrEqualTo(10));
                    Assert.That(Object.FindObjectsByType<TrackSection>(FindObjectsSortMode.None).Length, Is.LessThanOrEqualTo(10));
                    Assert.That(motor.RegisteredObstacles.Count, Is.LessThanOrEqualTo(40));
                    Assert.That(motor.RegisteredObstacles.Distinct().Count(), Is.EqualTo(motor.RegisteredObstacles.Count));
                    var sections = track.ActiveSections.OrderBy(s => s.StartZ).ToArray();
                    for (var i = 1; i < sections.Length; i++) Assert.That(sections[i].StartZ, Is.EqualTo(sections[i - 1].EndZ));
                }
                Assert.That(track.EncounterPlan.FallbackCount, Is.Zero);
            }
            flow.Run.ConfiguredSeed = 1337; hud.Restart(); track.UpdateCoverage(4000, null);
            var jumped = track.ActiveSections.Where(s => s.StartZ >= 3920).OrderBy(s => s.StartZ).Select(s => s.SectionId).ToArray();
            hud.Restart(); for (var z = 40; z <= 4000; z += 40) track.UpdateCoverage(z, null);
            Assert.That(track.ActiveSections.Where(s => s.StartZ >= 3920).OrderBy(s => s.StartZ).Select(s => s.SectionId), Is.EqualTo(jumped));
            yield return null;
        }

        [UnityTest] public IEnumerator DenseChainRunsAtNormalRuntimeSpeedAndRecordsBoundedRenderedPresentation()
        {
            var definition = library.Patterns.First(p => p.name == "DenseRight"); var start = Stage(definition, 0.65f);
            var section = track.ActiveSections.Single(s => s.StartZ == start); var wood = section.Obstacles.Where(o => o.Definition.MaterialId == "wood").OrderBy(o => o.WorldBounds.center.z).ToArray();
            Launch(); motor.enabled = true;
            try
            {
                yield return new WaitUntil(() => wood[0].Durability.Fraction < 0.65f || motor.Motion.State == RunState.RunEnded);
                Assert.That(motor.Motion.State, Is.EqualTo(RunState.Drilling)); yield return Capture("Dense-LiveContact");
                var camera = Object.FindFirstObjectByType<RunnerCamera>().GetComponent<Camera>(); var canvas = hud.GetComponent<Canvas>();
                var texture = new RenderTexture(540, 960, 24); var oldTarget = camera.targetTexture; var oldMode = canvas.renderMode;
                var oldCamera = canvas.worldCamera; var oldPlane = canvas.planeDistance;
                using (var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread"))
                using (var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
                using (var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
                {
                    try
                    {
                        camera.targetTexture = texture; camera.aspect = 9f / 16; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 0.5f;
                        yield return null; yield return null; long totalMain = 0, peakMain = 0, peakDraws = 0, peakGc = 0; var peakParticles = 0;
                        for (var frame = 0; frame < 30; frame++)
                        {
                            yield return null; totalMain += main.LastValue; peakMain = Math.Max(peakMain, main.LastValue); peakDraws = Math.Max(peakDraws, draws.LastValue);
                            peakGc = Math.Max(peakGc, gc.LastValue); peakParticles = Math.Max(peakParticles, presentation.Effects.ParticleCount);
                        }
                        Assert.That(peakDraws, Is.GreaterThan(0)); Assert.That(peakParticles, Is.InRange(1, 112));
                        File.WriteAllText("Logs/M7-DenseProfiler.txt", "30 rendered Editor frames during live Dense Drilling; not mobile or GPU performance.\nMean main thread ns: " + totalMain / 30 +
                            "\nPeak main thread ns: " + peakMain + "\nPeak draw calls: " + peakDraws + "\nPeak GC bytes/frame (Editor/test included): " + peakGc +
                            "\nPeak particles: " + peakParticles + "\nActive sections: " + track.ActiveSections.Count + "\nRegistered obstacles: " + motor.RegisteredObstacles.Count + "\n");
                    }
                    finally { camera.targetTexture = oldTarget; camera.ResetAspect(); canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane; texture.Release(); Object.Destroy(texture); }
                }
                yield return new WaitUntil(() => wood[0].Durability.IsDestroyed || motor.Motion.State == RunState.RunEnded); yield return Capture("Dense-LiveFirstBreak");
                yield return new WaitUntil(() => wood[1].Durability.Fraction < 0.7f || motor.Motion.State == RunState.RunEnded); yield return Capture("Dense-LiveSecondContact");
                yield return new WaitUntil(() => wood[2].Durability.IsDestroyed || motor.Motion.State == RunState.RunEnded);
                Assert.That(wood.All(o => o.Durability.IsDestroyed), Is.True); Assert.That(flow.Run.Rewards.ObstacleCoins, Is.EqualTo(3));
                Assert.That(section.Obstacles.Single(o => o.Definition.MaterialId == "brick").Durability.Fraction, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length, Is.EqualTo(5));
                Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(5));
            }
            finally { motor.enabled = false; }
        }

        private sealed class Sample
        {
            public double Distance, DrillingEnergy; public long Coins, Copper; public int Broken, Blocked, Opportunities;
            public string Csv(string mode, int seed, int profile) => string.Join(",", mode, seed, profile, Distance.ToString("0.###", CultureInfo.InvariantCulture),
                Coins, Copper, Broken, DrillingEnergy.ToString("0.###", CultureInfo.InvariantCulture), Blocked, Opportunities);
        }
        private Sample lastSample;
        private float Policy(bool seekCopper)
        {
            var current = track.ActiveSections.FirstOrDefault(s => motor.Motion.Position.z >= s.StartZ && motor.Motion.Position.z < s.EndZ);
            if (current == null) return -2.05f;
            var obstacles = current.Obstacles.Where(o => o != null && o.IsSolid && o.WorldBounds.max.z >= motor.Motion.Position.z - 1.15f).ToArray();
            if (seekCopper && flow.Run.Rewards.Copper == 0)
            foreach (var ore in obstacles.Where(o => o.Definition.RewardKind == ObstacleRewardKind.Copper && o.WorldBounds.min.z < motor.Motion.Position.z + 32))
            {
                var x = ore.WorldBounds.center.x; var cost = 0f; var valid = true;
                foreach (var obstacle in obstacles.Where(o => o.WorldBounds.min.z <= ore.WorldBounds.max.z && EncounterSafety.OverlapsX(x, 0.85f, o.WorldBounds)))
                {
                    valid &= obstacle.Definition.Hardness <= motor.DrillPower && x >= obstacle.WorldBounds.min.x + 0.1f && x <= obstacle.WorldBounds.max.x - 0.1f;
                    cost += obstacle.Durability.Remaining / (motor.DrillPower * motor.Drilling.DamagePerPower) * obstacle.Definition.DrillingEnergyPerSecond;
                }
                if (valid && cost < motor.Energy.Capacity * 0.28f && cost + 8 < motor.Energy.Remaining) return x;
            }
            var candidates = new[] { -2.05f, 2.05f, 0f };
            foreach (var x in candidates.OrderBy(x => Mathf.Abs(x - motor.Motion.Position.x)))
                if (obstacles.All(o => !EncounterSafety.OverlapsX(x, 0.85f, o.WorldBounds))) return x;
            // Older M6 Mixed sections have successive free paths rather than a free line through the whole section.
            // When that stronger option is absent, steer for the nearest collision group, like the original guided-run QA.
            if (obstacles.Length > 0)
            {
                var first = obstacles.Min(o => o.WorldBounds.min.z); var group = obstacles.Where(o => o.WorldBounds.min.z < first + 3).ToArray();
                foreach (var x in candidates.OrderBy(x => Mathf.Abs(x - motor.Motion.Position.x)))
                    if (group.All(o => !EncounterSafety.OverlapsX(x, 0.85f, o.WorldBounds))) return x;
            }
            return track.ActiveEncounter != null ? track.ActiveEncounter.BypassX : -2.1f;
        }
        private IEnumerator DriveToEnd(bool seekCopper)
        {
            var sample = new Sample(); var seen = new HashSet<DrillObstacle>(); var previous = motor.Motion.State; var ticks = 0;
            Action<DrillObstacle> broken = _ => sample.Broken++; motor.ObstacleBroken += broken;
            try
            {
                while (motor.Motion.State != RunState.RunEnded && ticks++ < 10000)
                {
                    Steer(Policy(seekCopper)); var before = motor.ContactTarget; var hp = before != null && before.Durability != null ? before.Durability.Remaining : 0;
                    Tick(0.1f);
                    if (before != null) sample.DrillingEnergy += (hp - before.Durability.Remaining) / (motor.DrillPower * motor.Drilling.DamagePerPower) * before.Definition.DrillingEnergyPerSecond;
                    var after = motor.ContactTarget;
                    if (after != null && after != before) sample.DrillingEnergy += (after.Durability.Maximum - after.Durability.Remaining) / (motor.DrillPower * motor.Drilling.DamagePerPower) * after.Definition.DrillingEnergyPerSecond;
                    if (motor.Motion.State == RunState.Blocked && previous != RunState.Blocked) sample.Blocked++; previous = motor.Motion.State;
                    foreach (var obstacle in motor.RegisteredObstacles)
                        if (obstacle.Definition.RewardKind == ObstacleRewardKind.Copper && obstacle.WorldBounds.min.z < motor.Motion.Position.z + 32 && obstacle.WorldBounds.max.z >= motor.Motion.Position.z) seen.Add(obstacle);
                    if (ticks % 250 == 0) yield return null;
                }
            }
            finally { motor.ObstacleBroken -= broken; }
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.RunEnded)); Assert.That(flow.IsBanked, Is.True);
            sample.Distance = flow.Run.Rewards.Distance; sample.Coins = flow.Run.Rewards.Coins; sample.Copper = flow.Run.Rewards.Copper; sample.Opportunities = seen.Count;
            lastSample = sample;
        }
        [UnityTest] public IEnumerator ReproducibleEnergyLimitedJourneysCompareM6AndM7AtFiveProgressionLevels()
        {
            balance.Clear(); balance.Add("mode,seed,profile,distance,coins,copper,destroyed,drillingEnergy,blocked,visibleOre");
            var legacy = Object.Instantiate(normalCatalog); owned.Add(legacy);
            for (var profile = 0; profile < 5; profile++)
            {
                var before = new List<Sample>(); var after = new List<Sample>();
                foreach (var mode in new[] { "M6", "M7" }) foreach (var seed in new[] { 1337, 904, 2026 })
                {
                    hud.Restart(); motor.ApplyAttributes(flow.Configuration.Calculate(Profile(profile))); track.Catalog = mode == "M6" ? legacy : normalCatalog;
                    flow.Run.ConfiguredSeed = seed; flow.Run.UseFixedSeed = true; hud.Restart(); Launch(); yield return DriveToEnd(true);
                    balance.Add(lastSample.Csv(mode, seed, profile)); File.WriteAllLines("Logs/M7-Balance.csv", balance); (mode == "M6" ? before : after).Add(lastSample);
                }
                var ratio = after.Average(s => s.Coins) / before.Average(s => s.Coins);
                Assert.That(ratio, Is.InRange(0.8, 1.2), "Density must not cause large Coin pacing drift at profile " + profile);
                Assert.That(after.All(s => s.Copper == 3), Is.True, "Starting and stronger forms can complete an ordinary Ore opportunity.");
            }
            File.WriteAllLines("Logs/M7-Balance.csv", balance);
        }
        [UnityTest] public IEnumerator TwoUnfundedEvolutionJourneysPerGeneratorKeepTheAcceptedRunBudget()
        {
            var results = new List<string> { "mode,seedFamily,runs,coins,copper" };
            foreach (var mode in new[] { "M6", "M7" }) foreach (var family in new[] { 900, 1900 })
            {
                yield return Load(); var legacy = Object.Instantiate(normalCatalog); owned.Add(legacy); track.Catalog = mode == "M6" ? legacy : normalCatalog;
                flow.OpenWorkshop(); var runs = 0; long coins = 0, copper = 0;
                while (!flow.Evolution.Ready && runs < 8)
                {
                    Assert.That(flow.StartRun(), Is.True); flow.Run.ConfiguredSeed = family + ++runs; flow.Run.UseFixedSeed = true; hud.Restart();
                    Launch(); yield return DriveToEnd(flow.Service.Snapshot.Copper < 12); coins += lastSample.Coins; copper += lastSample.Copper;
                    Assert.That(flow.OpenWorkshop(), Is.True);
                    foreach (var category in new[] { UpgradeCategory.EnergyCapacity, UpgradeCategory.DrivePower, UpgradeCategory.DrillPower }) while (flow.Purchase(category)) { }
                }
                results.Add(string.Join(",", mode, family, runs, coins, copper)); File.WriteAllLines("Logs/M7-EvolutionJourneys.csv", results);
                Assert.That(flow.Evolution.Ready, Is.True, mode + " family " + family); Assert.That(runs, Is.InRange(5, 8));
                Assert.That(flow.Evolve(), Is.True); yield return new WaitForSecondsRealtime(1); Assert.That(flow.Service.Attributes.Power, Is.EqualTo(12));
            }
        }
        private IEnumerator Capture(string name)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            var follower = Object.FindFirstObjectByType<RunnerCamera>(); follower.Snap(); var camera = follower.GetComponent<Camera>(); var canvas = hud.GetComponent<Canvas>();
            var texture = new RenderTexture(540, 960, 24); var image = new Texture2D(540, 960, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldPlane = canvas.planeDistance; var oldActive = RenderTexture.active;
            try
            {
                camera.targetTexture = texture; camera.aspect = 9f / 16; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 0.5f;
                yield return new WaitForSecondsRealtime(0.12f); yield return null; yield return null;
                RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0); image.Apply(); File.WriteAllBytes("Logs/M7-" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane; camera.targetTexture = oldTarget;
                camera.ResetAspect(); RenderTexture.active = oldActive; texture.Release(); Object.Destroy(texture); Object.Destroy(image);
            }
        }
    }
}

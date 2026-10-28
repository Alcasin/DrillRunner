using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public sealed class EncounterPacingRevisionPlayModeTests
    {
        private WorkshopController flow;
        private DrillMotor motor;
        private TrackStreamer track;
        private PrototypeHud hud;
        private EncounterLibrary current;
        private readonly List<Object> owned = new List<Object>();
        [UnitySetUp] public IEnumerator Open() { yield return Load(); }
        private IEnumerator Load()
        {
            WorkshopController.StoreFactory = () => new MemoryProgressStore(); yield return SceneManager.LoadSceneAsync("Gameplay");
            foreach (var item in owned) if (item != null) Object.Destroy(item); owned.Clear(); yield return null;
            flow = Object.FindFirstObjectByType<WorkshopController>(); motor = flow.Run.Motor; track = flow.Run.Track; current = track.Encounters;
            hud = Object.FindFirstObjectByType<PrototypeHud>(); motor.enabled = false; motor.GetComponent<DrillPointerInput>().enabled = false;
            Object.FindFirstObjectByType<DrillPresentation>().SendMessage("OnApplicationFocus", true);
        }
        [UnityTearDown] public IEnumerator Close()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene"); foreach (var item in owned) if (item != null) Object.Destroy(item); owned.Clear();
            WorkshopController.StoreFactory = () => new MemoryProgressStore();
        }
        private void Launch() { motor.Motion.Press(); motor.Simulate(1.5f); motor.Motion.Release(); motor.Simulate(0.001f); }
        private void Tick(float dt = 0.1f) { motor.Simulate(dt); track.UpdateCoverage(motor.Motion.Position.z, motor.ContactTarget); }
        private void Steer(float x)
        {
            if (motor.Motion.State != RunState.Moving && motor.Motion.State != RunState.Blocked) return;
            motor.Motion.CancelPointer(); motor.Motion.Press(); motor.Motion.Drag(new Vector2((x - motor.Motion.Position.x) * 540 / 8, 0), 540);
        }
        private void Restart(int seed)
        { flow.Run.UseFixedSeed = true; flow.Run.ConfiguredSeed = seed; hud.Restart(); }
        private static PlayerProgress Profile(int index)
        {
            if (index < 3) { var level = index == 0 ? 0 : index == 1 ? 2 : 5; return new PlayerProgress { PowerLevel = level, EnergyLevel = level, DriveLevel = level }; }
            var level2 = index == 4 ? 5 : 0;
            return new PlayerProgress { FormId = ProgressionConfig.WorkshopForm, FormSchema = 1, PowerLevel = 5, EnergyLevel = 5, DriveLevel = 5,
                WorkshopPowerLevel = level2, WorkshopEnergyLevel = level2, WorkshopDriveLevel = level2 };
        }
        private void Apply(int profile)
        { hud.Restart(); var progress = Profile(profile); motor.ApplyAttributes(flow.Configuration.Calculate(progress)); flow.Visuals.Show(flow.Configuration.ForForm(progress.FormId)); }
        [UnityTest] public IEnumerator FrozenM7ReproducesAllGoldenSequencesAndTheOriginalTwoHundredMeterStart()
        {
            var reference = M7ReferenceFixture.Create(current, owned);
            foreach (var golden in M7ReferenceFixture.Read().Sequences)
            {
                var sequence = new EncounterSequence(golden.Seed, reference, EncounterEnvelope.Accepted);
                Assert.That(sequence.ValidPatterns, Is.EqualTo(14));
                Assert.That(Enumerable.Range(0, golden.Ids.Length).Select(_ => sequence.Next().SectionId), Is.EqualTo(golden.Ids));
            }
            track.Encounters = reference; Restart(1337); Assert.That(track.EncounterStartZ, Is.EqualTo(200));
            Assert.That(motor.RegisteredObstacles.Count, Is.EqualTo(4)); Assert.That(track.ActiveSections.Count, Is.EqualTo(6)); yield return null;
        }
        [UnityTest] public IEnumerator NaturalEarlyTravelExposesChoicesAndPaysCopperBeforeOneHundredFiftyAtFiveProfiles()
        {
            for (var profile = 0; profile < 5; profile++) foreach (var seed in new[] { 1337, 904, 2026 })
            {
                Apply(profile); track.Encounters = current; Restart(seed); Assert.That(track.EncounterStartZ, Is.EqualTo(40));
                Assert.That(track.ActiveSections.Any(s => s.StartZ == 40 && s.Obstacles.Length >= 2), Is.True);
                foreach (var section in track.StarterSections.Skip(1)) Assert.That(section.gameObject.activeSelf, Is.False);
                var seenChoice = false; Launch(); var ticks = 0;
                while (motor.Motion.Position.z < 150 && motor.Motion.State != RunState.RunEnded && ticks++ < 3000)
                {
                    var active = track.ActiveEncounter; if (active != null && active.Section.Obstacles.Length >= 2) seenChoice = true;
                    Steer(Policy(true)); Tick();
                    Assert.That(motor.Motion.State, Is.Not.EqualTo(RunState.Blocked));
                }
                Assert.That(seenChoice, Is.True); Assert.That(flow.Run.Rewards.Copper, Is.EqualTo(3)); Assert.That(motor.Energy.Remaining, Is.GreaterThan(0));
                Assert.That(track.ActiveSections.Count, Is.LessThanOrEqualTo(10)); yield return null;
            }
        }
        [UnityTest] public IEnumerator GuardedAndCheapRoutesHaveDistinctActualCostsAndOnlyCompletedOrePaysCopper()
        {
            var pattern = current.Patterns.First(p => p.name == "OreGuardRight");
            for (var profile = 0; profile < 5; profile++) foreach (var rewardRoute in new[] { false, true })
            {
                Apply(profile); Restart(1337); var plan = new EncounterSequence(1337, current, EncounterEnvelope.Accepted); var ordinal = 0;
                while (plan.Next().SectionId != pattern.Section.SectionId) ordinal++;
                var start = track.EncounterStartZ + ordinal * 40; track.UpdateCoverage(start, null);
                var x = rewardRoute ? 0.8f : -0.65f; motor.Motion.ResolvePosition(new Vector3(x, 0, start), true); motor.transform.position = motor.Motion.Position;
                var section = track.ActiveSections.Single(s => s.StartZ == start); Launch(); var ticks = 0;
                while (motor.Motion.Position.z < start + 41.2f && motor.Motion.State != RunState.RunEnded && ticks++ < 2000)
                {
                    Steer(x); Tick(1f / 60);
                    var ore = section.Obstacles.Single(o => o.Definition.RewardKind == ObstacleRewardKind.Copper);
                    if (!ore.Durability.IsDestroyed) Assert.That(flow.Run.Rewards.Copper, Is.Zero);
                }
                var drillingEnergy = section.Obstacles.Sum(o => (o.Definition.Durability - o.Durability.Remaining) / (motor.DrillPower * motor.Drilling.DamagePerPower) * o.Definition.DrillingEnergyPerSecond);
                Assert.That(drillingEnergy, Is.EqualTo((rewardRoute ? 51.6f : 11.2f) * 5 / motor.DrillPower).Within(0.02));
                Assert.That(flow.Run.Rewards.ObstacleCoins, Is.EqualTo(rewardRoute ? 2 : 1)); Assert.That(flow.Run.Rewards.Copper, Is.EqualTo(rewardRoute ? 3 : 0));
                if (profile == 0) yield return Capture(rewardRoute ? "Guarded-Outcome" : "Cheap-Outcome");
                yield return null;
            }
        }

        // Same preservation-oriented policy and starting states on both sides of the M7/M7-B comparison.
        private float Policy(bool seekCopper)
        {
            var section = track.ActiveSections.FirstOrDefault(s => motor.Motion.Position.z >= s.StartZ && motor.Motion.Position.z < s.EndZ);
            if (section == null) return -2.05f;
            var obstacles = section.Obstacles.Where(o => o.IsSolid && o.WorldBounds.max.z >= motor.Motion.Position.z - 1.15f).ToArray();
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
            var candidates = new[] { -2.05f, 2.05f, 0f }.OrderBy(x => Mathf.Abs(x - motor.Motion.Position.x)).ToArray();
            foreach (var x in candidates) if (obstacles.All(o => !EncounterSafety.OverlapsX(x, 0.85f, o.WorldBounds))) return x;
            if (obstacles.Length > 0)
            {
                var first = obstacles.Min(o => o.WorldBounds.min.z); var group = obstacles.Where(o => o.WorldBounds.min.z < first + 3).ToArray();
                foreach (var x in candidates) if (group.All(o => !EncounterSafety.OverlapsX(x, 0.85f, o.WorldBounds))) return x;
            }
            return track.ActiveEncounter != null ? track.ActiveEncounter.BypassX : -2.1f;
        }
        private sealed class Sample
        {
            public double Distance, DrillingEnergy; public long Coins, Copper; public int Destroyed, Blocked;
            public string Csv(string mode, int seed, int profile) => string.Join(",", mode, seed, profile, Distance.ToString("0.###", CultureInfo.InvariantCulture), Coins, Copper,
                Destroyed, DrillingEnergy.ToString("0.###", CultureInfo.InvariantCulture), Blocked);
        }
        private Sample last;
        private IEnumerator Drive(bool copper)
        {
            var sample = new Sample(); var previous = motor.Motion.State; var ticks = 0; Action<DrillObstacle> broken = _ => sample.Destroyed++; motor.ObstacleBroken += broken;
            try
            {
                while (motor.Motion.State != RunState.RunEnded && ticks++ < 10000)
                {
                    Steer(Policy(copper)); var before = motor.ContactTarget; var hp = before != null ? before.Durability.Remaining : 0; Tick();
                    if (before != null) sample.DrillingEnergy += (hp - before.Durability.Remaining) / (motor.DrillPower * motor.Drilling.DamagePerPower) * before.Definition.DrillingEnergyPerSecond;
                    var after = motor.ContactTarget;
                    if (after != null && after != before) sample.DrillingEnergy += (after.Durability.Maximum - after.Durability.Remaining) / (motor.DrillPower * motor.Drilling.DamagePerPower) * after.Definition.DrillingEnergyPerSecond;
                    if (motor.Motion.State == RunState.Blocked && previous != RunState.Blocked) sample.Blocked++; previous = motor.Motion.State;
                    if (ticks % 250 == 0) yield return null;
                }
            }
            finally { motor.ObstacleBroken -= broken; }
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.RunEnded)); Assert.That(flow.IsBanked, Is.True);
            sample.Distance = flow.Run.Rewards.Distance; sample.Coins = flow.Run.Rewards.Coins; sample.Copper = flow.Run.Rewards.Copper; last = sample;
        }
        [UnityTest] public IEnumerator EquivalentPolicySamplesCompareFrozenM7AndTheRevisionAtFiveAcceptedProfiles()
        {
            var baseline = M7ReferenceFixture.Create(current, owned); var lines = new List<string> { "mode,seed,profile,distance,coins,copper,destroyed,drillingEnergy,blocked" };
            for (var profile = 0; profile < 5; profile++)
            {
                var oldCoins = new List<long>(); var newCoins = new List<long>();
                foreach (var mode in new[] { "M7", "M7B" }) foreach (var seed in new[] { 1337, 904, 2026 })
                {
                    Apply(profile); track.Encounters = mode == "M7" ? baseline : current; Restart(seed); Launch(); yield return Drive(true);
                    lines.Add(last.Csv(mode, seed, profile)); File.WriteAllLines("Logs/M7B-Balance.csv", lines);
                    (mode == "M7" ? oldCoins : newCoins).Add(last.Coins); Assert.That(last.Copper, Is.EqualTo(3));
                }
                Assert.That(newCoins.Average() / oldCoins.Average(), Is.InRange(0.75, 1.25));
            }
        }
        [UnityTest] public IEnumerator FrozenAndRevisedUnfundedEvolutionJourneysKeepTheFiveToEightRunTarget()
        {
            var lines = new List<string> { "mode,family,runs,coins,copper" };
            foreach (var mode in new[] { "M7", "M7B" }) foreach (var family in new[] { 900, 1900 })
            {
                yield return Load(); track.Encounters = mode == "M7" ? M7ReferenceFixture.Create(current, owned) : current;
                flow.OpenWorkshop(); var runs = 0; long coins = 0, copper = 0;
                while (!flow.Evolution.Ready && runs < 8)
                {
                    Assert.That(flow.StartRun(), Is.True); motor.GetComponent<DrillPointerInput>().enabled = false; Restart(family + ++runs); Launch();
                    yield return Drive(flow.Service.Snapshot.Copper < 12); coins += last.Coins; copper += last.Copper;
                    Assert.That(flow.OpenWorkshop(), Is.True);
                    foreach (var category in new[] { UpgradeCategory.EnergyCapacity, UpgradeCategory.DrivePower, UpgradeCategory.DrillPower }) while (flow.Purchase(category)) { }
                }
                lines.Add(string.Join(",", mode, family, runs, coins, copper)); File.WriteAllLines("Logs/M7B-Evolution.csv", lines);
                Assert.That(flow.Evolution.Ready, Is.True); Assert.That(runs, Is.InRange(5, 8)); Assert.That(flow.Evolve(), Is.True);
                yield return new WaitForSecondsRealtime(1); Assert.That(flow.Service.Attributes.Power, Is.EqualTo(12));
            }
        }
        [UnityTest] public IEnumerator NaturalRuntimeEpisodeRendersEarlyChoicesCopperAndCategoryTransitions()
        {
            Restart(1337); Launch(); motor.enabled = true; var targets = new[] { 42f, 62f, 95f, 130f, 170f, 210f, 280f, 330f };
            var next = 0; var trace = new List<string> { "Natural motor Update, no relocation; distance, encounter" };
            try
            {
                while (next < targets.Length && motor.Motion.State != RunState.RunEnded)
                {
                    Steer(Policy(true));
                    if (motor.Motion.Position.z >= targets[next])
                    {
                        var active = track.ActiveEncounter; trace.Add(motor.Motion.Position.z.ToString("0.0", CultureInfo.InvariantCulture) + ", " + (active != null ? active.name : "Introduction"));
                        yield return Capture("Natural-" + targets[next].ToString("0")); next++;
                    }
                    yield return null;
                }
                Assert.That(next, Is.EqualTo(targets.Length)); Assert.That(flow.Run.Rewards.Copper, Is.EqualTo(3)); File.WriteAllLines("Logs/M7B-NaturalTrace.txt", trace);
            }
            finally { motor.enabled = false; }
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
                RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0); image.Apply(); File.WriteAllBytes("Logs/M7B-" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane; camera.targetTexture = oldTarget;
                camera.ResetAspect(); RenderTexture.active = oldActive; texture.Release(); Object.Destroy(texture); Object.Destroy(image);
            }
        }
    }
}

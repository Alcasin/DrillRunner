using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public sealed partial class RapidRunPlayModeTests
    {
        private WorkshopController flow;
        private DrillMotor motor;
        private TrackStreamer track;
        private PrototypeHud hud;
        private MemoryProgressStore memory;
        private EncounterLibrary historicalLibrary;
        private readonly List<EncounterDefinition> historicalPatterns = new List<EncounterDefinition>();
        private DrillObstacle observedFront;
        private double decisionAt, blockedAt = -1;
        private float aim;
        private enum Policy { Safe, Balanced, Resource, Power }
        [UnitySetUp] public IEnumerator Open()
        { memory = new MemoryProgressStore(); WorkshopController.BalanceFactory = null; WorkshopController.StoreFactory = () => memory; yield return Load(); }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null;
            flow = Object.FindFirstObjectByType<WorkshopController>(); motor = flow.Run.Motor; track = flow.Run.Track; hud = Object.FindFirstObjectByType<PrototypeHud>();
            motor.enabled = false; motor.GetComponent<DrillPointerInput>().enabled = false; flow.OpenWorkshop();
            Object.FindFirstObjectByType<DrillPresentation>().SendMessage("OnApplicationFocus", true);
        }
        [UnityTearDown] public IEnumerator Close()
        { yield return SceneManager.LoadSceneAsync("SampleScene"); if (historicalLibrary != null) Object.Destroy(historicalLibrary); if (stateCatalog != null) Object.Destroy(stateCatalog); foreach (var pattern in historicalPatterns) Object.Destroy(pattern); historicalPatterns.Clear(); WorkshopController.StoreFactory = () => new MemoryProgressStore(); WorkshopController.BalanceFactory = null; }
        private void Configure(int stage, int level, int seed)
        {
            flow.UseTemporaryStage(stage, 1000000, 0, 0, 0, 0, 0, level == 5);
            if (level > 0 && level < 5) foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
                for (var i = 0; i < level; i++) Assert.That(flow.Purchase(category), Is.True);
            Assert.That(flow.StartRun(), Is.True); motor.GetComponent<DrillPointerInput>().enabled = false;
            flow.Run.UseFixedSeed = true; flow.Run.ConfiguredSeed = seed; hud.Restart();
            observedFront = null; decisionAt = 0; blockedAt = -1; aim = 0;
        }
        private void Launch(float held = 1.5f)
        { motor.Motion.Press(); motor.Simulate(held); motor.Motion.Release(); motor.Simulate(0.001f); }
        private void Steer(float x)
        {
            if (motor.Motion.State != RunState.Moving && motor.Motion.State != RunState.Blocked) return;
            motor.Motion.CancelPointer(); motor.Motion.Press(); motor.Motion.Drag(new Vector2((x - motor.Motion.Position.x) * 540 / 8, 0), 540);
        }
        private float Rate(DrillObstacle obstacle) => motor.DrillingEnergyRate(obstacle.Durability);
        private float Cost(IEnumerable<DrillObstacle> obstacles) => obstacles.Sum(o => o.Durability.Remaining / (motor.DrillPower * motor.Drilling.DamagePerPower) * Rate(o));
        private static bool Aligned(float x, DrillObstacle o) => x >= o.WorldBounds.min.x + 0.08f && x <= o.WorldBounds.max.x - 0.08f;
        private float Choose(Policy policy, bool collectCopper = true)
        {
            if(track.RegionId==RegionIds.Advanced||track.RegionId==RegionIds.Energy)return ChooseAdvanced(policy,collectCopper);
            var section = track.ActiveSections.FirstOrDefault(s => motor.Motion.Position.z >= s.StartZ && motor.Motion.Position.z < s.EndZ);
            if (section == null) return aim;
            var targets = section.Obstacles.Where(o => o.IsSolid && o.WorldBounds.max.z >= motor.Motion.Position.z - 1.15f).ToArray();
            var front = targets.OrderBy(o => o.WorldBounds.min.z).FirstOrDefault();
            if (front != observedFront) { observedFront = front; decisionAt = motor.Diagnostics.Seconds + 0.35; }
            if (motor.Motion.State == RunState.Blocked)
            {
                if (blockedAt < 0) blockedAt = motor.Diagnostics.Seconds;
                if (motor.Diagnostics.Seconds - blockedAt < 0.35) return aim;
                return Free(targets);
            }
            blockedAt = -1;
            if (motor.Diagnostics.Seconds < decisionAt) return aim;
            float reserve = flow.Balance != null && flow.Balance.Revision >= 5 ? Mathf.Max(1, motor.Drilling.MovingEnergyPerSecond * 0.61f) : 4;
            if (policy != Policy.Safe && collectCopper && (policy == Policy.Resource || (track.RegionId==RegionIds.Quarry?flow.Run.Rewards.Iron<12:flow.Run.Rewards.Copper<3)))
            {
                foreach (var ore in targets.Where(o => MaterialDefinition.IsResource(o.Definition.RewardKind) && o.WorldBounds.min.z < motor.Motion.Position.z + 32))
                {
                    var x = ore.WorldBounds.center.x;
                    var line = targets.Where(o => o.WorldBounds.min.z <= ore.WorldBounds.max.z && EncounterSafety.OverlapsX(x, 0.85f, o.WorldBounds)).ToArray();
                    var cost = Cost(line);
                    if (line.All(o => o.Definition.Hardness <= motor.DrillPower && Aligned(x, o)) && cost + reserve < motor.Energy.Remaining &&
                        cost < motor.Energy.Capacity * (policy == Policy.Resource ? 0.6f : 0.4f)) return x;
                }
            }
            if (policy != Policy.Safe && policy != Policy.Resource && front != null &&
                (!collectCopper || (track.RegionId==RegionIds.Quarry?flow.Run.Rewards.Iron>0:flow.Run.Rewards.Copper>0) || motor.Diagnostics.Destroyed == 0 || policy == Policy.Power))
            {
                var group = targets.Where(o => o.WorldBounds.min.z < front.WorldBounds.min.z + 3).ToArray();
                var ordered = policy == Policy.Power ? group.OrderByDescending(o => o.Definition.Hardness) : group.OrderBy(o => o.Definition.Hardness);
                foreach (var target in ordered.Where(o => !MaterialDefinition.IsResource(o.Definition.RewardKind)))
                {
                    var x = target.WorldBounds.center.x; var line = group.Where(o => EncounterSafety.OverlapsX(x, 0.85f, o.WorldBounds)).ToArray();
                    if (line.All(o => o.Definition.Hardness <= motor.DrillPower && Aligned(x, o)) && Cost(line) < motor.Energy.Capacity * (policy == Policy.Power ? 0.4f : 0.18f) && Cost(line) + reserve < motor.Energy.Remaining) return x;
                }
            }
            return Free(targets);
        }
        private float Free(DrillObstacle[] targets)
        {
            foreach (var x in new[] { -2.05f, 2.05f, 0f }.OrderBy(x => Mathf.Abs(x - motor.Motion.Position.x)))
                if (targets.All(o => !EncounterSafety.OverlapsX(x, 0.85f, o.WorldBounds))) return x;
            return track.ActiveEncounter != null ? track.ActiveEncounter.BypassX : -2.05f;
        }
        private sealed class Sample
        {
            public int Stage, Level, Seed, Destroyed, Contacts, CopperOpportunities, Approached;
            public string Policy;
            public double Seconds, Moving, Drilling, Blocked, MoveEnergy, DrillEnergy, BlockEnergy, OpenSeconds, Distance, FirstContact;
            public long Coins, Copper, Iron, DistanceCoins, ObstacleCoins;
            public const string Header = "mode,stage,level,seed,policy,seconds,movingSeconds,drillingSeconds,blockedSeconds,movingEnergy,drillingEnergy,blockedEnergy,openSeconds,distance,coins,copper,destroyed,contacts,copperOpportunities,firstContact,ending,approached,distanceCoins,obstacleCoins,iron";
            public string Csv(string mode) => string.Join(",", new object[] {mode,Stage,Level,Seed,Policy,Seconds,Moving,Drilling,Blocked,MoveEnergy,DrillEnergy,BlockEnergy,OpenSeconds,Distance,Coins,Copper,Destroyed,Contacts,CopperOpportunities,FirstContact,"EnergyEmpty",Approached,DistanceCoins,ObstacleCoins,Iron}
                .Select(v => v is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : v.ToString()));
        }
        private Sample last;
        private IEnumerator Drive(Policy policy, bool collectCopper = true, float dt = 0.05f, bool alreadyLaunched = false)
        {
            var opportunities = new HashSet<DrillObstacle>(); var approached = new HashSet<DrillObstacle>(); var open = 0.0; var ticks = 0;
            if(!alreadyLaunched)Launch();
            while (motor.Motion.State != RunState.RunEnded && ticks++ < 150000)
            {
                var section = track.ActiveSections.FirstOrDefault(s => motor.Motion.Position.z >= s.StartZ && motor.Motion.Position.z < s.EndZ);
                foreach (var ore in track.ActiveSections.SelectMany(s => s.Obstacles).Where(o => MaterialDefinition.IsResource(o.Definition.RewardKind) &&
                    o.WorldBounds.min.z >= motor.Motion.Position.z - 1.85f && o.WorldBounds.min.z <= motor.Motion.Position.z + 30)) opportunities.Add(ore);
                foreach (var target in motor.RegisteredObstacles)
                    if (target != null && target.gameObject.activeInHierarchy && target.WorldBounds.max.z >= motor.Motion.Position.z - 1.85f &&
                        target.WorldBounds.min.z <= motor.Motion.Position.z + 12) approached.Add(target);
                aim = Choose(policy, collectCopper); Steer(aim); var before = motor.Diagnostics.MovingSeconds;
                motor.Simulate(dt); track.UpdateCoverage(motor.Motion.Position.z, motor.ContactTarget);
                if (section != null && section.Obstacles.Length == 0) open += motor.Diagnostics.MovingSeconds - before;
                if (ticks % 250 == 0) yield return null;
            }
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.RunEnded), "Diagnostic workload bound is not a run-ending mechanic.");
            Assert.That(motor.Diagnostics.EndedByEnergy, Is.True); Assert.That(flow.IsBanked, Is.True);
            var m = motor.Diagnostics;
            Assert.That(m.Consumed, Is.EqualTo(motor.Energy.Capacity).Within(0.001));
            last = new Sample { Stage = flow.Service.ActiveForm.Stage, Level = flow.Service.Snapshot.Level(UpgradeCategory.EnergyCapacity), Seed = flow.Run.Rewards.Seed, Policy = policy.ToString(),
                Seconds = m.Seconds, Moving = m.MovingSeconds, Drilling = m.DrillingSeconds, Blocked = m.BlockedSeconds, MoveEnergy = m.MovingEnergy,
                DrillEnergy = m.DrillingEnergy, BlockEnergy = m.BlockedEnergy, OpenSeconds = open, Distance = flow.Run.Rewards.Distance, Coins = flow.Run.Rewards.Coins,
                Copper = flow.Run.Rewards.Copper, Iron = flow.Run.Rewards.Iron, DistanceCoins = flow.Run.Rewards.DistanceCoins, ObstacleCoins = flow.Run.Rewards.ObstacleCoins, Destroyed = m.Destroyed, Contacts = m.Contacts, CopperOpportunities = opportunities.Count, Approached = approached.Count, FirstContact = m.FirstContactSeconds };
        }
        [UnityTest] public IEnumerator InstrumentTheOriginalM8RunDurationAtBaseAndMaximumPocketAndWorkshop()
        {
            WorkshopController.BalanceFactory = _ => null; yield return Load();
            RestoreM8Capacities();
            historicalLibrary = Object.Instantiate(track.Encounters); historicalLibrary.OpeningCopperBySelection = 0; track.Encounters = historicalLibrary;
            historicalLibrary.Patterns = historicalLibrary.Patterns.Select(p =>
            {
                var copy = Object.Instantiate(p); historicalPatterns.Add(copy);
                if (copy.name.StartsWith("OreGuard", StringComparison.Ordinal)) { copy.MinimumOrdinal = 3; copy.EarlyWeight = 0; }
                return copy;
            }).ToArray();
            var rows = new List<string> { Sample.Header };
            foreach (var stage in new[] {1,2}) foreach (var level in new[] {0,5}) foreach (var seed in new[] {1337,904,2026}) foreach (Policy policy in Enum.GetValues(typeof(Policy)))
            {
                Configure(stage, level, seed); yield return Drive(policy, true, 0.1f); rows.Add(last.Csv("M8"));
                File.WriteAllLines("Logs/M9A-M8R4-OriginalBaseline.csv", rows); yield return null;
            }
        }
        [UnityTest] public IEnumerator ProductionShortRunDistributionsCoverEightFormsAndFourPolicies()
        {
            Assert.That(flow.Balance, Is.Not.Null); var rows = new List<string> { Sample.Header };
            var profiles = new[] {new[]{1,0},new[]{1,2},new[]{1,5},new[]{2,0},new[]{2,2},new[]{2,5},new[]{3,0},new[]{3,5},new[]{4,2},new[]{5,2},new[]{6,0},new[]{6,5},new[]{7,2},new[]{8,0},new[]{8,5}};
            foreach (var profile in profiles) foreach (var seed in new[] {1337,904,2026,8101,9201}) foreach (Policy policy in Enum.GetValues(typeof(Policy)))
            {
                Configure(profile[0], profile[1], seed); yield return Drive(policy); rows.Add(last.Csv("M8R4"));
                Assert.That(last.Seconds, Is.LessThan(36)); Assert.That(last.Seconds, Is.GreaterThan(profile[0] == 1 && profile[1] == 0 ? 6.5 : 8));
                File.WriteAllLines("Logs/M9A-M8R4-Policies.csv", rows); yield return null;
            }
        }
    }
}

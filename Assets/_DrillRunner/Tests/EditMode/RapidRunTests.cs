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
    public sealed class RapidRunTests
    {
        private RapidRunBalance Balance => AssetDatabase.LoadAssetAtPath<RapidRunBalance>(RapidRunSetup.Path);
        private ProgressionConfig Root => AssetDatabase.LoadAssetAtPath<ProgressionConfig>(WorkshopSetup.ConfigPath);
        [Test] public void AllEightFixedOperatingProfilesBoundTravelAndLeaveTimeForBlockedEscape()
        {
            Balance.Validate(); Root.Validate(); Assert.That(Balance.Forms.Select(p => p.FormId).Distinct().Count(), Is.EqualTo(8));
            foreach (var profile in Balance.Forms)
            {
                var form = Root.ForForm(profile.FormId);
                foreach (var capacity in form.Energy.Values)
                {
                    Assert.That(capacity / profile.Moving, Is.InRange(7,35));
                    Assert.That(profile.Blocked * 0.61f, Is.LessThan(capacity * 0.1f));
                    foreach (var materialRate in new[] {8f,10,12,18}) Assert.That(profile.DrillingRate(materialRate), Is.GreaterThan(0));
                }
                Assert.That(profile.DrillingRate(10) * 90 / (form.Power.Values[5] * 10), Is.LessThan(profile.DrillingRate(10) * 90 / (form.Power.Values[0] * 10)));
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void InvalidConsumptionAndCoastConfigurationsCannotStartAProductionRun(int kind)
        {
            var copy = Object.Instantiate(Balance);
            try
            {
                if (kind == 0) copy.Forms[0].Moving = 0;
                if (kind == 1) copy.Forms[0].Blocked = float.NaN;
                if (kind == 2) copy.Forms[0].DrillingBase = -1;
                if (kind == 3) copy.Forms[1].FormId = copy.Forms[0].FormId;
                if (kind == 4) copy.Forms[0].BoostDecayByDriveLevel[1] = copy.Forms[0].BoostDecayByDriveLevel[0];
                if (kind == 5) copy.Forms[0].BoostDecayByDriveLevel[5] = 0;
                Assert.Catch<ArgumentException>(() => copy.Validate());
            }
            finally { Object.DestroyImmediate(copy); }
        }
        [Test] public void DistanceAndDestructionRewardsSnapshotScaleOnceAndNeverScaleCopperOrPartialDamage()
        {
            var settings = new RewardSettings { DistancePerPayout = 5, CoinsPerPayout = 9, ObstacleCoinMultiplier = 25 };
            var rewards = new RunRewards(1337,0,settings); settings.CoinsPerPayout = 100; settings.ObstacleCoinMultiplier = 100;
            rewards.Start(); rewards.RecordForward(26); Assert.That(rewards.DistanceCoins, Is.EqualTo(45));
            Assert.That(rewards.Claim(1,ObstacleRewardKind.Coins,2,false), Is.False); Assert.That(rewards.ObstacleCoins, Is.Zero);
            Assert.That(rewards.Claim(1,ObstacleRewardKind.Coins,2,true), Is.True); Assert.That(rewards.ObstacleCoins, Is.EqualTo(50));
            Assert.That(rewards.Claim(1,ObstacleRewardKind.Coins,2,true), Is.False);
            Assert.That(rewards.Claim(2,ObstacleRewardKind.Copper,3,false), Is.False); Assert.That(rewards.Copper, Is.Zero);
            Assert.That(rewards.Claim(2,ObstacleRewardKind.Copper,3,true), Is.True); Assert.That(rewards.Copper, Is.EqualTo(3));
            rewards.Complete(); rewards.RecordForward(1000); Assert.That(rewards.Claim(3,ObstacleRewardKind.Coins,3,true), Is.False); Assert.That(rewards.Coins, Is.EqualTo(95));
        }
        [Test] public void RewardScalingPreservesOverflowRejectionAndDoesNotConsumeTheEncounterIdOnFailure()
        {
            var rewards = new RunRewards(1,0,new RewardSettings { CoinsPerPayout = int.MaxValue, ObstacleCoinMultiplier = 1000 });
            rewards.Start(); rewards.RecordForward(21474836450d);
            Assert.Throws<OverflowException>(() => rewards.Claim(1,ObstacleRewardKind.Coins,int.MaxValue,true));
            Assert.That(rewards.ObstacleCoins, Is.Zero); Assert.That(rewards.Claim(1,ObstacleRewardKind.Copper,3,true), Is.True); Assert.That(rewards.Copper, Is.EqualTo(3));
        }
        [Test] public void EarlyCopperAndPressureChoicesRemainVariedSafeAndDeterministic()
        {
            var library = AssetDatabase.LoadAssetAtPath<EncounterLibrary>(GameplayVarietySetup.LibraryPath); var prefixes = new System.Collections.Generic.HashSet<string>();
            for (var seed = 0; seed < 256; seed++)
            {
                var a = new EncounterSequence(seed,library,EncounterEnvelope.Accepted); var b = new EncounterSequence(seed,library,EncounterEnvelope.Accepted);
                var copper = false; var prefix = "";
                for (var i = 0; i < 3; i++)
                {
                    var section = a.Next(); Assert.That(b.Next(), Is.SameAs(section)); prefix += section.SectionId + "/";
                    if (i < 2) copper |= section.Obstacles.Any(o => o.Definition.RewardKind == ObstacleRewardKind.Copper);
                    if (i == 0) Assert.That(a.Selected.Pressure, Is.Zero);
                }
                Assert.That(copper, Is.True); Assert.That(a.FallbackCount, Is.Zero); prefixes.Add(prefix);
            }
            Assert.That(prefixes.Count, Is.GreaterThan(20));
            foreach (var guard in library.Patterns.Where(p => p.name.StartsWith("OreGuard")))
            { Assert.That(guard.MinimumOrdinal, Is.EqualTo(3)); Assert.That(guard.EarlyWeight, Is.GreaterThan(0)); }
        }
        [Test] public void EveryDriveLevelExtendsActualUnobstructedLaunchDistanceWithinTheExistingSpeedEnvelope()
        {
            for (var stage = 1; stage <= 8; stage++)
            {
                var form = Root.ForForm(ProgressionConfig.FormIds[stage - 1]); var previous = 0f;
                var coast = Balance.ForForm(form.FormId).BoostDecayByDriveLevel;
                if (stage > 1) Assert.That(coast[0],Is.LessThanOrEqualTo(Balance.ForForm(ProgressionConfig.FormIds[stage-2]).BoostDecayByDriveLevel[5]));
                for (var level = 0; level < 6; level++)
                {
                    var settings = new DrillMovementSettings { CruiseSpeed = form.Drive.Values[level], MinimumLaunchBoost = form.MinimumBoost[level],
                        MaximumLaunchBoost = form.MaximumBoost[level], ResumeAcceleration = form.Recovery[level], MaximumSpeed = form.SpeedCeiling,
                        ChargeDuration = Balance.ChargeSeconds, BoostDecayRate = coast[level] };
                    var motion = new DrillMotion(settings,Vector3.zero); motion.Press(); motion.Step(Balance.ChargeSeconds); motion.Release();
                    for (var step = 0; step < 300; step++) { motion.Step(0.1f); Assert.That(motion.ForwardSpeed, Is.LessThanOrEqualTo(form.SpeedCeiling)); }
                    if (level > 0) Assert.That(motion.Position.z, Is.GreaterThan(previous + 2)); previous = motion.Position.z;
                }
            }
        }
        [Test] public void SameSaveSchemaRetainsAllBalancesHistoryAndReceiptsAcrossRapidRewardBanking()
        {
            var initial = new PlayerProgress { Coins = 123, Copper = 7, Iron = 3, Quartz = 4, AlloyCore = 5, EnergyShard = 6, EvolutionSchema = 1 };
            var store = new MemoryProgressStore(initial); var service = new ProgressionService(store,Root);
            Assert.That(service.TryBeginAttempt(out var token,out _), Is.True); Assert.That(service.TryBank(token,333,3,out _), Is.True);
            Assert.That(service.TryBank(token,333,3,out _), Is.True); var saved = new ProgressionService(store,Root).Snapshot;
            Assert.That(saved.Coins, Is.EqualTo(456)); Assert.That(saved.Copper, Is.EqualTo(10)); Assert.That(saved.Iron, Is.EqualTo(3)); Assert.That(saved.Quartz, Is.EqualTo(4));
            Assert.That(saved.AlloyCore, Is.EqualTo(5)); Assert.That(saved.EnergyShard, Is.EqualTo(6)); Assert.That(saved.BankedAttempt, Is.EqualTo(token));
            var encoded = ProgressCodec.Encode(saved); Assert.That(encoded, Does.Not.Contain("MovingEnergy")); Assert.That(encoded, Does.Not.Contain("BoostDecay")); Assert.That(saved.Version, Is.EqualTo(1));
        }
        [Test] public void RapidSetupIsIdempotentAndPreservesCustomSceneWorkAndTuningBytes()
        {
            const string path = "Assets/_DrillRunner/Scenes/M8RSetupTemporary.unity";
            var paths = new[] {FoundationSetup.ScenePath,RapidRunSetup.Path,GameplayVarietySetup.LibraryPath}; var bytes = paths.Select(File.ReadAllBytes).ToArray();
            Assert.That(AssetDatabase.CopyAsset(FoundationSetup.ScenePath,path), Is.True);
            try
            {
                var scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    var custom = new GameObject("M8-R Custom Work"); SceneManager.MoveGameObjectToScene(custom,scene); EditorSceneManager.SaveScene(scene);
                    var before = File.ReadAllBytes(path); RapidRunSetup.SetupScene(path); RapidRunSetup.SetupScene(path);
                    Assert.That(File.ReadAllBytes(path), Is.EqualTo(before)); Assert.That(custom, Is.Not.Null);
                    var flow = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WorkshopController>(true)).Single();
                    Assert.That(new SerializedObject(flow).FindProperty("rapidRunBalance").objectReferenceValue, Is.SameAs(Balance));
                    EditorSceneManager.MarkSceneDirty(scene); Assert.Throws<InvalidOperationException>(() => RapidRunSetup.SetupScene(path));
                }
                finally { EditorSceneManager.CloseScene(scene,true); }
            }
            finally { AssetDatabase.DeleteAsset(path); }
            for (var i = 0; i < paths.Length; i++) Assert.That(File.ReadAllBytes(paths[i]), Is.EqualTo(bytes[i]));
        }
    }
}

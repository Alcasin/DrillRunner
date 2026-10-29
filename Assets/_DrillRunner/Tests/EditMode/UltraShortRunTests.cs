using System;
using System.IO;
using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
namespace DrillRunner.Tests.EditMode
{
    public sealed class UltraShortRunTests
    {
        private ProgressionConfig Root=>AssetDatabase.LoadAssetAtPath<ProgressionConfig>(WorkshopSetup.ConfigPath);
        private RapidRunBalance Balance=>AssetDatabase.LoadAssetAtPath<RapidRunBalance>(RapidRunSetup.Path);
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)][TestCase(8)]
        public void AuthoredCapacitiesAndFixedCostsAreCompactMonotonicAndIndependentOfUpgradeLevel(int stage)
        {
            Root.Validate();Balance.Validate();var f=Root.ForForm(ProgressionConfig.FormIds[stage-1]);var rate=Balance.ForForm(f.FormId);
            Assert.That(f.Energy.Values,Is.EqualTo(UltraShortRunSetup.Capacity[stage-1]));Assert.That(rate.Moving,Is.EqualTo(UltraShortRunSetup.Moving[stage-1]));
            Assert.That(rate.Blocked,Is.EqualTo(UltraShortRunSetup.Blocked[stage-1]));Assert.That(rate.DrillingBase,Is.EqualTo(UltraShortRunSetup.Drilling[stage-1]));
            Assert.That(rate.MaterialScale,Is.EqualTo(0.05f));Assert.That(f.SpeedCeiling,Is.EqualTo(stage==1?24:26));
            if(stage<8)Assert.That(f.NextForm.Energy.Values[0],Is.GreaterThan(f.Energy.Values[5]));
            Assert.That(Balance.Rewards.CoinsPerPayout,Is.EqualTo(UltraShortRunSetup.DistanceCoinsPerPayout));Assert.That(Balance.Rewards.ObstacleCoinMultiplier,Is.EqualTo(25));
        }
        [Test] public void EvolvingImprovesFamiliarMiningTimeAndEnergyEvenWithCompactOperatingDurations()
        {
            for(var p=Root;p.NextForm!=null;p=p.NextForm)
            {
                var n=p.NextForm;foreach(float material in new[]{8f,10,12,18})
                {
                    Assert.That(1/n.Power.Values[0],Is.LessThan(1/p.Power.Values[5]));
                    Assert.That(Balance.ForForm(n.FormId).DrillingRate(material)/n.Power.Values[0],Is.LessThan(Balance.ForForm(p.FormId).DrillingRate(material)/p.Power.Values[5]));
                }
            }
        }
        [Test] public void EarlyOreRemainsSafelyReachableAndGuardCostsFitAnEarlyCapacityPurchase()
        {
            var lib=AssetDatabase.LoadAssetAtPath<EncounterLibrary>(GameplayVarietySetup.LibraryPath);GameplayVarietySetup.Validate(lib);
            foreach(var p in lib.Patterns.Where(p=>p.name.StartsWith("OreOpen")))Assert.That(p.Section.Obstacles.Single(o=>o.Definition.RewardKind==ObstacleRewardKind.Copper).transform.localPosition.z,Is.EqualTo(19));
            var rate=Balance.ForForm(ProgressionConfig.PocketForm);
            foreach(var p in lib.Patterns.Where(p=>p.name.StartsWith("OreGuard")))
            {
                var ore=p.Section.Obstacles.Single(o=>o.Definition.RewardKind==ObstacleRewardKind.Copper);
                float cost=p.Section.Obstacles.Where(o=>EncounterSafety.OverlapsX(ore.WorldBounds.center.x,0.85f,o.WorldBounds)).Sum(o=>o.Definition.Durability/50*rate.DrillingRate(o.Definition.DrillingEnergyPerSecond));
                Assert.That(cost,Is.LessThanOrEqualTo(25*0.65f));Assert.That(cost,Is.LessThanOrEqualTo(Root.Energy.Values[1]*0.5f));Assert.That(p.MinimumOrdinal,Is.EqualTo(3));
            }
        }
        [Test] public void RevisionRerunLeavesEveryAuthoredAssetAndCurrentTuningUntouched()
        {
            var paths=Directory.GetFiles("Assets","*",SearchOption.AllDirectories);var before=paths.Select(File.ReadAllBytes).ToArray();UltraShortRunSetup.Setup();UltraShortRunSetup.Setup();
            for(int i=0;i<paths.Length;i++)Assert.That(File.ReadAllBytes(paths[i]),Is.EqualTo(before[i]),paths[i]);Assert.That(Directory.GetFiles("Assets","*",SearchOption.AllDirectories),Is.EquivalentTo(paths));
        }
        [Test] public void SavedProfilesAtEveryFormRetainBalancesPurchasedLevelsOwnershipAndReceipts()
        {
            for(int stage=1;stage<=8;stage++)
            {
                var p=new PlayerProgress{FormId=ProgressionConfig.FormIds[stage-1],FormSchema=1,EvolutionSchema=1,Coins=12345,Copper=17,Iron=11,Quartz=13,AlloyCore=7,EnergyShard=9,
                    IssuedAttempt=23,BankedAttempt=23,Revision=31,PowerLevel=stage==1?2:5,EnergyLevel=stage==1?3:5,DriveLevel=stage==1?4:5,
                    WorkshopPowerLevel=stage>2?5:stage==2?2:0,WorkshopEnergyLevel=stage>2?5:stage==2?3:0,WorkshopDriveLevel=stage>2?5:stage==2?4:0};
                p.AdvancedForms=Enumerable.Range(3,Math.Max(0,stage-2)).Select(s=>new FormUpgradeRecord{FormId=ProgressionConfig.FormIds[s-1],Power=s==stage?2:5,Energy=s==stage?3:5,Drive=s==stage?4:5}).ToArray();
                p.Validate(Root);var encoded=ProgressCodec.Encode(p);var store=new MemoryProgressStore(p);var service=new ProgressionService(store,Root);
                Assert.That(ProgressCodec.Encode(service.Snapshot),Is.EqualTo(encoded));Assert.That(ProgressCodec.Encode(store.Load().Progress),Is.EqualTo(encoded));
                Assert.That(service.Attributes.Energy,Is.EqualTo(service.ActiveForm.Energy.Values[service.Snapshot.Level(UpgradeCategory.EnergyCapacity)]));
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DrillRunner.Tests.EditMode
{
    public sealed class AdvancedRegionTests
    {
        private RegionCatalog Regions=>AssetDatabase.LoadAssetAtPath<RegionCatalog>(RegionSetup.CatalogPath);
        private ProgressionConfig Root=>AssetDatabase.LoadAssetAtPath<ProgressionConfig>(WorkshopSetup.ConfigPath);
        private static PlayerProgress Profile(int stage,bool max=false)=>new PlayerProgress{FormId=ProgressionConfig.FormIds[stage-1],FormSchema=1,EvolutionSchema=1,
            PowerLevel=stage>1?5:0,EnergyLevel=stage>1?5:0,DriveLevel=stage>1?5:0,WorkshopPowerLevel=stage>2?5:0,WorkshopEnergyLevel=stage>2?5:0,WorkshopDriveLevel=stage>2?5:0,
            AdvancedForms=Enumerable.Range(3,stage-2).Select(s=>new FormUpgradeRecord{FormId=ProgressionConfig.FormIds[s-1],Power=s<stage||max?5:0,Energy=s<stage||max?5:0,Drive=s<stage||max?5:0}).ToArray()};
        [TestCase(4,2)][TestCase(5,3)][TestCase(6,3)][TestCase(7,4)][TestCase(8,4)]
        public void AllRegionsUnlockAtExactStagesAndEarlierChoicesRemain(int stage,int count)
        {
            Regions.Validate();Assert.That(Regions.Regions.Count(r=>r.CanPlay(stage)),Is.EqualTo(count));
            var s=new ProgressionService(new MemoryProgressStore(Profile(stage)),Root);
            foreach(var r in Regions.Regions)Assert.That(s.TrySelectRegion(r,true,out _),Is.EqualTo(r.UnlockStage<=stage));
            Assert.Throws<ArgumentException>(()=>Regions.Find("unknown"));Assert.That(RegionIds.Implemented("unknown"),Is.False);
        }
        [TestCase("CrystalComposite",50,1500,16,0)][TestCase("EnergyAlloy",100,2200,32,0)]
        [TestCase("QuartzOre",50,650,4,3)][TestCase("AlloyCore",52,1800,3,4)][TestCase("EnergyShard",120,1200,2,5)]
        public void NewDefinitionsSeparateNormalCoinsFromCompleteOnlyResources(string name,int hardness,int hp,int payout,int kind)
        {
            var m=AssetDatabase.LoadAssetAtPath<MaterialDefinition>(AdvancedRegionSetup.Data+"/"+name+".asset");m.Validate();
            Assert.That(m.Hardness,Is.EqualTo(hardness));Assert.That(m.Durability,Is.EqualTo(hp));Assert.That(m.RewardAmount,Is.EqualTo(payout));Assert.That((int)m.RewardKind,Is.EqualTo(kind));
            var run=new RunRewards(1,0,new RewardSettings{ObstacleCoinMultiplier=25},kind==5?RegionIds.Energy:RegionIds.Advanced);run.Start();
            Assert.That(run.Claim(1,m.RewardKind,payout,false),Is.False);Assert.That(run.Resource(m.RewardKind),Is.Zero);
            Assert.That(run.Claim(1,m.RewardKind,payout,true),Is.True);Assert.That(run.Claim(1,m.RewardKind,payout,true),Is.False);
            Assert.That(run.Resource(m.RewardKind),Is.EqualTo(kind==0?0:payout));Assert.That(run.Coins,Is.EqualTo(kind==0?payout*25:0));
            run.Complete();Assert.That(run.Claim(2,m.RewardKind,payout,true),Is.False);
        }
        [TestCase(RegionIds.Advanced,52)][TestCase(RegionIds.Energy,125)]
        public void AdvancedLibrariesHaveSafeJoinsEarlyResourcesAndBoundedDistinctContent(string id,int power)
        {
            AdvancedRegionSetup.Validate(Regions);var r=Regions.Find(id);Assert.That(r.Track.InitialPower,Is.EqualTo(power));Assert.That(r.Encounters.Patterns.Length,Is.EqualTo(14));
            Assert.That(r.Encounters.Patterns.Select(p=>p.Category).Distinct().Count(),Is.EqualTo(6));
            Assert.That(r.Environment.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(r.Environment.GetComponentsInChildren<AudioSource>(true),Is.Empty);Assert.That(r.Environment.GetComponentsInChildren<ParticleSystem>(true),Is.Empty);
            foreach(int seed in new[]{0,1,904,1337,2026,8101,9201})
            {
                var a=new EncounterSequence(seed,r.Encounters,EncounterEnvelope.Accepted);var b=new EncounterSequence(seed,r.Encounters,EncounterEnvelope.Accepted);
                var first=a.Next();Assert.That(first.SectionId,Is.EqualTo(b.Next().SectionId));Assert.That(first.Obstacles.Any(o=>o.Definition.RewardKind==r.Encounters.ResourceKind),Is.True);
                if(id==RegionIds.Advanced)Assert.That(first.Obstacles.Any(o=>o.Definition.RewardKind==ObstacleRewardKind.AlloyCore),Is.True);
                for(int i=1;i<256;i++)Assert.That(a.Next().SectionId,Is.EqualTo(b.Next().SectionId));Assert.That(a.RejectedPatterns+a.FallbackCount,Is.Zero);
            }
        }
        [TestCase(5)][TestCase(6)][TestCase(7)]
        public void EveryEvolutionHasAlreadyUnlockedSourcesAndRequiresAllPaidMaxUpgrades(int stage)
        {
            var form=Root.ForForm(ProgressionConfig.FormIds[stage-1]);
            foreach(var cost in form.RequiredResources)
            {
                var source=Regions.Regions.First(r=>r.Resources.Contains(cost.Resource));Assert.That(source.CanPlay(stage),Is.True);
                Assert.That(source.Materials.Any(m=>(int)m.RewardKind==(int)cost.Resource+1&&m.Hardness<=form.Power.Values.Last()),Is.True);
            }
            var p=Profile(stage,true);p.Coins=1234;p.Quartz=60;p.AlloyCore=40;p.EnergyShard=35;
            var before=p.Copy();var s=new ProgressionService(new MemoryProgressStore(p),Root);Assert.That(s.TryEvolve(true,out _),Is.True);Assert.That(s.ActiveForm.Stage,Is.EqualTo(stage+1));
            foreach(var c in form.RequiredResources)Assert.That(s.Snapshot.Resource(c.Resource),Is.EqualTo(before.Resource(c.Resource)-c.Amount));
            p.AdvancedForms.Last().Drive=4;s=new ProgressionService(new MemoryProgressStore(p),Root);Assert.That(s.TryEvolve(true,out _),Is.False);
            if(stage==7)Assert.That(new ProgressionService(new MemoryProgressStore(Profile(8,true)),Root).TryEvolve(true,out _),Is.False);
        }
        [Test] public void M9AProfileAndAllFourRecordsSurviveAtomicMixedResourceBanksReloadAndFailure()
        {
            var p=Profile(7);p.Coins=90;p.Copper=11;p.Iron=12;p.Quartz=13;p.AlloyCore=14;p.EnergyShard=15;p.RegionSchema=1;p.SelectedRegionId=RegionIds.Quarry;
            p.RegionRecords=new[]{new RegionDistanceRecord{RegionId=RegionIds.Yard,Distance=123},new RegionDistanceRecord{RegionId=RegionIds.Quarry,Distance=234}};
            var store=new FailingStore(p);var original=ProgressCodec.Encode(p);var s=new ProgressionService(store,Root);Assert.That(ProgressCodec.Encode(s.Snapshot),Is.EqualTo(original));
            foreach(var id in new[]{RegionIds.Advanced,RegionIds.Energy})
            {
                Assert.That(s.TrySelectRegion(Regions.Find(id),true,out _),Is.True);Assert.That(s.TryBeginAttempt(id,out var token,out _),Is.True);
                var run=new RunRewards(1,0,new RewardSettings(),id);run.Start();run.RecordForward(345);
                if(id==RegionIds.Advanced){run.Claim(1,ObstacleRewardKind.Quartz,5,true);run.Claim(2,ObstacleRewardKind.AlloyCore,4,true);}else run.Claim(1,ObstacleRewardKind.EnergyShard,5,true);run.Complete();
                var before=ProgressCodec.Encode(s.Snapshot);store.Fail=true;Assert.That(s.TryBank(token,run,out _),Is.False);Assert.That(ProgressCodec.Encode(s.Snapshot),Is.EqualTo(before));Assert.That(ProgressCodec.Encode(store.Load().Progress),Is.EqualTo(before));
                store.Fail=false;Assert.That(s.TryBank(token,run,out _),Is.True);var bank=ProgressCodec.Encode(s.Snapshot);Assert.That(s.TryBank(token,run,out _),Is.True);Assert.That(ProgressCodec.Encode(s.Snapshot),Is.EqualTo(bank));s=new ProgressionService(store,Root);
            }
            Assert.That(s.Snapshot.Quartz,Is.EqualTo(18));Assert.That(s.Snapshot.AlloyCore,Is.EqualTo(18));Assert.That(s.Snapshot.EnergyShard,Is.EqualTo(20));Assert.That(s.Snapshot.Copper,Is.EqualTo(11));Assert.That(s.Snapshot.Iron,Is.EqualTo(12));Assert.That(s.Snapshot.RegionRecords.Length,Is.EqualTo(4));
            Assert.That(s.Snapshot.BestDistance(RegionIds.Yard),Is.EqualTo(123));Assert.That(s.Snapshot.BestDistance(RegionIds.Quarry),Is.EqualTo(234));
        }
        [Test] public void AdvancedSetupIsByteIdempotentAndDoesNotRewriteScenesOrExistingContent()
        {
            var files=Directory.GetFiles("Assets","*",SearchOption.AllDirectories);var bytes=files.Select(File.ReadAllBytes).ToArray();AdvancedRegionSetup.Setup();AdvancedRegionSetup.Setup();
            Assert.That(Directory.GetFiles("Assets","*",SearchOption.AllDirectories),Is.EquivalentTo(files));for(int i=0;i<files.Length;i++)Assert.That(File.ReadAllBytes(files[i]),Is.EqualTo(bytes[i]),files[i]);
        }
        private sealed class FailingStore:IProgressStore
        {
            private readonly MemoryProgressStore inner;public bool Fail;public FailingStore(PlayerProgress p){inner=new MemoryProgressStore(p);}public ProgressLoad Load()=>inner.Load();
            public bool TrySave(PlayerProgress p,out string error){if(Fail){error="Expected bank failure";return false;}return inner.TrySave(p,out error);}
        }
    }
}

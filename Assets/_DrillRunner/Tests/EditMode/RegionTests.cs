using System;
using System.IO;
using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DrillRunner.Tests.EditMode
{
    public sealed class RegionTests
    {
        private RegionCatalog Regions=>AssetDatabase.LoadAssetAtPath<RegionCatalog>(RegionSetup.CatalogPath);
        private ProgressionConfig Root=>AssetDatabase.LoadAssetAtPath<ProgressionConfig>(WorkshopSetup.ConfigPath);
        private static PlayerProgress Profile(int stage,bool max=false)
        {
            return new PlayerProgress{FormId=ProgressionConfig.FormIds[stage-1],FormSchema=1,EvolutionSchema=1,
                PowerLevel=stage>1||max?5:0,EnergyLevel=stage>1||max?5:0,DriveLevel=stage>1||max?5:0,
                WorkshopPowerLevel=stage>2||stage==2&&max?5:0,WorkshopEnergyLevel=stage>2||stage==2&&max?5:0,WorkshopDriveLevel=stage>2||stage==2&&max?5:0,
                AdvancedForms=Enumerable.Range(3,Math.Max(0,stage-2)).Select(s=>new FormUpgradeRecord{FormId=ProgressionConfig.FormIds[s-1],Power=s<stage||max?5:0,Energy=s<stage||max?5:0,Drive=s<stage||max?5:0}).ToArray()};
        }
        [TestCase(1,1)][TestCase(2,1)][TestCase(3,2)][TestCase(4,2)][TestCase(5,3)][TestCase(8,4)]
        public void ImplementedRegionAccessPreservesEarlierRoutesAndDeferredContent(int stage,int expected)
        {
            Regions.Validate();Assert.That(Regions.Regions.Count(r=>r.CanPlay(stage)),Is.EqualTo(expected));Assert.That(Regions.Find(RegionIds.Yard).CanPlay(stage),Is.True);
            var service=new ProgressionService(new MemoryProgressStore(Profile(stage)),Root);
            foreach(var r in Regions.Regions)Assert.That(service.TrySelectRegion(r,true,out _),Is.EqualTo(r.CanPlay(stage)));
            Assert.That(service.TrySelectRegion(Regions.Find(RegionIds.Yard),false,out _),Is.False);
        }
        [Test] public void QuarryGeometryHasMovementAwareWitnessesAndNoDecorativeColliders()
        {
            RegionSetup.Validate(Regions);var q=Regions.Find(RegionIds.Quarry);Assert.That(q.Encounters.Patterns.Select(p=>p.Category).Distinct().Count(),Is.EqualTo(6));
            Assert.That(q.Encounters.ResourceKind,Is.EqualTo(ObstacleRewardKind.Iron));Assert.That(q.Track.InitialPower,Is.EqualTo(20));
            Assert.That(q.Environment.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(q.Environment.GetComponentsInChildren<AudioSource>(true),Is.Empty);
            foreach(var p in q.Encounters.Patterns)foreach(var o in p.Section.Obstacles)Assert.That(new[]{"granite","plating","reinforced","iron_ore"},Does.Contain(o.Definition.MaterialId));
            foreach(var seed in new[]{0,1,904,1337,2026})
            {
                var a=new EncounterSequence(seed,q.Encounters,EncounterEnvelope.Accepted);var b=new EncounterSequence(seed,q.Encounters,EncounterEnvelope.Accepted);
                for(int i=0;i<256;i++)Assert.That(a.Next().SectionId,Is.EqualTo(b.Next().SectionId));Assert.That(a.RejectedPatterns,Is.Zero);Assert.That(a.FallbackCount,Is.Zero);
            }
        }
        [TestCase("Granite",18,260,12,0)][TestCase("MetalPlating",26,420,24,0)][TestCase("ReinforcedBlock",40,620,40,0)][TestCase("IronOre",18,240,6,2)]
        public void QuarryMaterialsUseFixedThresholdsAndCompleteOnlyRewards(string name,float power,float hp,int reward,int kind)
        {
            var m=AssetDatabase.LoadAssetAtPath<MaterialDefinition>(RegionSetup.Data+"/"+name+".asset");m.Validate();Assert.That(m.Hardness,Is.EqualTo(power));Assert.That(m.Durability,Is.EqualTo(hp));Assert.That(m.RewardAmount,Is.EqualTo(reward));Assert.That((int)m.RewardKind,Is.EqualTo(kind));
            var ledger=new RunRewards(1,0,new RewardSettings{CoinsPerPayout=6,ObstacleCoinMultiplier=25},RegionIds.Quarry);ledger.Start();
            Assert.That(ledger.Claim(1,m.RewardKind,m.RewardAmount,false),Is.False);Assert.That(ledger.Iron+ledger.Coins,Is.Zero);
            Assert.That(ledger.Claim(1,m.RewardKind,m.RewardAmount,true),Is.True);Assert.That(ledger.Claim(1,m.RewardKind,m.RewardAmount,true),Is.False);
            Assert.That(ledger.Iron,Is.EqualTo(kind==2?6:0));Assert.That(ledger.ObstacleCoins,Is.EqualTo(kind==0?reward*25:0));Assert.That(ledger.Copper,Is.Zero);
        }
        [Test] public void IronRecordsAndCoinsCommitAtomicallyAndDuplicateReceiptsCannotImproveRecords()
        {
            var initial=Profile(3);initial.Coins=123;initial.Copper=7;var store=new MemoryProgressStore(initial);var service=new ProgressionService(store,Root);
            Assert.That(service.TrySelectRegion(Regions.Find(RegionIds.Quarry),true,out _),Is.True);Assert.That(service.TryBeginAttempt(RegionIds.Quarry,out var token,out _),Is.True);
            var run=new RunRewards(1,0,new RewardSettings{CoinsPerPayout=6,ObstacleCoinMultiplier=25},RegionIds.Quarry);run.Start();run.RecordForward(93);run.Claim(1,ObstacleRewardKind.Iron,6,true);run.Complete();
            Assert.That(service.TryBank(token,run,out _),Is.True);Assert.That(service.Snapshot.Coins,Is.EqualTo(231));Assert.That(service.Snapshot.Iron,Is.EqualTo(6));Assert.That(service.Snapshot.Copper,Is.EqualTo(7));Assert.That(service.Snapshot.BestDistance(RegionIds.Quarry),Is.EqualTo(93));
            var snapshot=service.Snapshot;snapshot.RegionRecords[0].Distance=999;Assert.That(service.Snapshot.BestDistance(RegionIds.Quarry),Is.EqualTo(93));
            var reload=new ProgressionService(store,Root);Assert.That(reload.Snapshot.SelectedRegion,Is.EqualTo(RegionIds.Quarry));Assert.That(reload.TryBank(token,run,out _),Is.True);Assert.That(reload.Snapshot.Iron,Is.EqualTo(6));
            Assert.That(reload.Snapshot.BestDistance(RegionIds.Yard),Is.Zero);Assert.That(reload.Snapshot.BestDistance(RegionIds.Quarry),Is.EqualTo(93));
        }
        [Test] public void IronAndCopperRequirementsRemainExactAtBothMiddleEvolutions()
        {
            var p=Profile(3,true);p.Iron=29;var store=new MemoryProgressStore(p);var s=new ProgressionService(store,Root);Assert.That(s.TryEvolve(true,out _),Is.False);
            p.Iron=30;s=new ProgressionService(new MemoryProgressStore(p),Root);Assert.That(s.TryEvolve(true,out _),Is.True);Assert.That(s.Snapshot.Iron,Is.Zero);
            p=Profile(4,true);p.Iron=50;p.Copper=11;s=new ProgressionService(new MemoryProgressStore(p),Root);Assert.That(s.TryEvolve(true,out _),Is.False);
            p.Copper=12;s=new ProgressionService(new MemoryProgressStore(p),Root);Assert.That(s.TryEvolve(true,out _),Is.True);Assert.That(s.ActiveForm.Stage,Is.EqualTo(5));Assert.That(s.Snapshot.Iron+s.Snapshot.Copper,Is.Zero);
        }
        [TestCase(1)][TestCase(3)][TestCase(5)] public void OldSavesLoadWithoutRewritingAndRegionalDiskReceiptsRecoverFromBackup(int stage)
        {
            string dir=Path.Combine(Path.GetTempPath(),"DrillRunner-M9A-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try
            {
                var p=Profile(stage);p.Coins=999;p.Iron=3;p.Copper=4;var disk=new FileProgressStore(dir);disk.Load();Assert.That(disk.TrySave(p,out _),Is.True);var bytes=File.ReadAllBytes(disk.Path);
                var service=new ProgressionService(new FileProgressStore(dir),Root);Assert.That(service.Writable,Is.True);Assert.That(service.Snapshot.SelectedRegion,Is.EqualTo(RegionIds.Yard));Assert.That(File.ReadAllBytes(disk.Path),Is.EqualTo(bytes));
                Assert.That(service.TryBeginAttempt(RegionIds.Yard,out var token,out _),Is.True);var run=new RunRewards(1,0,new RewardSettings(),RegionIds.Yard);run.Start();run.RecordForward(30);run.Complete();Assert.That(service.TryBank(token,run,out _),Is.True);
                var reloaded=new ProgressionService(new FileProgressStore(dir),Root);Assert.That(reloaded.Snapshot.Coins,Is.EqualTo(1005));Assert.That(reloaded.Snapshot.Iron,Is.EqualTo(3));Assert.That(reloaded.Snapshot.BestDistance(RegionIds.Yard),Is.EqualTo(30));
                File.WriteAllText(disk.Path,"corrupt");var recovery=new FileProgressStore(dir);Assert.That(recovery.Load().Writable,Is.False);Assert.That(recovery.RecoverBackup(Root,out _),Is.True);Assert.That(new ProgressionService(new FileProgressStore(dir),Root).Writable,Is.True);
            }
            finally{Directory.Delete(dir,true);}
        }
        [TestCase("unknown",3)][TestCase(RegionIds.Quarry,2)][TestCase(RegionIds.Advanced,3)]
        public void InvalidOrLockedRegionReferencesPreserveOriginalSave(string region,int stage)
        {
            var p=Profile(stage);p.RegionSchema=1;p.SelectedRegionId=region;var store=new MemoryProgressStore(p);var before=ProgressCodec.Encode(store.Load().Progress);var service=new ProgressionService(store,Root);Assert.That(service.Writable,Is.False);Assert.That(ProgressCodec.Encode(store.Load().Progress),Is.EqualTo(before));
        }
        [Test] public void RegionSetupRerunsPreserveEveryAssetSceneAndGuid()
        {
            var files=Directory.GetFiles("Assets","*",SearchOption.AllDirectories);var before=files.Select(File.ReadAllBytes).ToArray();RegionSetup.Setup();RegionSetup.Setup();
            for(int i=0;i<files.Length;i++)Assert.That(File.ReadAllBytes(files[i]),Is.EqualTo(before[i]),files[i]);Assert.That(Directory.GetFiles("Assets","*",SearchOption.AllDirectories),Is.EquivalentTo(files));
        }
        private sealed class FailingRegionalStore:IProgressStore
        {
            public readonly MemoryProgressStore Inner;public bool Fail;
            public FailingRegionalStore(PlayerProgress p){Inner=new MemoryProgressStore(p);}
            public ProgressLoad Load()=>Inner.Load();
            public bool TrySave(PlayerProgress p,out string error){if(Fail){error="Expected regional write failure";return false;}return Inner.TrySave(p,out error);}
        }
        [Test] public void FailedIronBankRollsBackExistingRecordAndResourcesBeforeRetry()
        {
            var p=Profile(3);p.RegionSchema=1;p.SelectedRegionId=RegionIds.Quarry;p.RegionRecords=new[]{new RegionDistanceRecord{RegionId=RegionIds.Quarry,Distance=30}};
            var store=new FailingRegionalStore(p);var s=new ProgressionService(store,Root);Assert.That(s.TryBeginAttempt(RegionIds.Quarry,out var token,out _),Is.True);
            var before=ProgressCodec.Encode(s.Snapshot);var run=new RunRewards(1,0,new RewardSettings(),RegionIds.Quarry);run.Start();run.RecordForward(90);run.Claim(1,ObstacleRewardKind.Iron,6,true);run.Complete();
            store.Fail=true;Assert.That(s.TryBank(token,run,out _),Is.False);Assert.That(ProgressCodec.Encode(s.Snapshot),Is.EqualTo(before));Assert.That(ProgressCodec.Encode(store.Load().Progress),Is.EqualTo(before));
            store.Fail=false;Assert.That(s.TryBank(token,run,out _),Is.True);Assert.That(s.Snapshot.BestDistance(RegionIds.Quarry),Is.EqualTo(90));Assert.That(s.Snapshot.Iron,Is.EqualTo(6));
        }
    }
}

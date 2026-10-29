using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.EditMode
{
    public sealed class FullEvolutionTests
    {
        private ProgressionConfig Root => AssetDatabase.LoadAssetAtPath<ProgressionConfig>(WorkshopSetup.ConfigPath);
        private PlayerProgress At(int stage, bool maximum = false)
        {
            var service = new ProgressionService(new MemoryProgressStore(new PlayerProgress { Coins = 1000000000, Copper = 1000, Iron = 1000, Quartz = 1000, AlloyCore = 1000, EnergyShard = 1000, EvolutionSchema = 1 }), Root);
            while (service.ActiveForm.Stage < stage)
            {
                Max(service); Assert.That(service.TryEvolve(true, out _), Is.True);
            }
            if (maximum) Max(service); return service.Snapshot;
        }
        private static void Max(ProgressionService service)
        { foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory))) while (service.TryPurchase(category, out _)) { } }
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)]
        public void EveryFormHasFiveRealPurchasesSafeStatsAndPreservedHistory(int stage)
        {
            var start = At(stage); var service = new ProgressionService(new MemoryProgressStore(start), Root); var form = service.ActiveForm;
            Assert.That(form.Stage, Is.EqualTo(stage)); Assert.That(form.EligibleRegionCount, Is.EqualTo(stage >= 7 ? 4 : stage >= 5 ? 3 : stage >= 3 ? 2 : 1));
            Assert.That(service.Attributes.Power, Is.EqualTo(form.Power.Values[0])); Assert.That(service.Attributes.Energy, Is.EqualTo(form.Energy.Values[0]));
            var before = service.Attributes; long spent = 0;
            foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
            {
                var table = form.Table(category); Assert.That(table.MaximumLevel, Is.EqualTo(5));
                for (var level = 0; level < 5; level++)
                {
                    Assert.That(service.TryPurchase(category, out _), Is.True); spent += table.Costs[level];
                    Assert.That(service.Snapshot.Level(category), Is.EqualTo(level + 1));
                    var actual = service.Attributes; Assert.That(actual.Power, Is.GreaterThanOrEqualTo(before.Power)); Assert.That(actual.Energy, Is.GreaterThanOrEqualTo(before.Energy));
                    Assert.That(actual.Cruise + actual.MaximumBoost, Is.LessThanOrEqualTo(26)); Assert.That(actual.Recovery, Is.GreaterThanOrEqualTo(before.Recovery));
                    if (category == UpgradeCategory.DrivePower && stage >= 3) { Assert.That(actual.MinimumBoost, Is.GreaterThan(before.MinimumBoost)); Assert.That(actual.Recovery, Is.GreaterThan(before.Recovery)); }
                    before = actual;
                }
                Assert.That(service.TryPurchase(category, out _), Is.False);
            }
            Assert.That(service.Snapshot.Coins, Is.EqualTo(start.Coins - spent)); Assert.That(service.Snapshot.AdvancedForms.Length, Is.EqualTo(Math.Max(0, stage - 2)));
            if (stage == 1) Assert.That(spent, Is.EqualTo(2480)); if (stage == 2) Assert.That(spent, Is.EqualTo(5230));
            Assert.That(service.TryEvolve(false, out _), Is.False);
            if (stage < 8)
            {
                var max = service.Attributes; var resources = service.Snapshot; Assert.That(service.TryEvolve(true, out _), Is.True);
                Assert.That(service.ActiveForm.Stage, Is.EqualTo(stage + 1)); Assert.That(service.Attributes.Power, Is.GreaterThan(max.Power));
                Assert.That(service.Attributes.Energy, Is.GreaterThanOrEqualTo(max.Energy)); Assert.That(service.Attributes.Cruise, Is.GreaterThanOrEqualTo(max.Cruise));
                Assert.That(service.Attributes.MinimumBoost, Is.GreaterThanOrEqualTo(max.MinimumBoost)); Assert.That(service.Attributes.Recovery, Is.GreaterThanOrEqualTo(max.Recovery));
                foreach (EvolutionResource resource in Enum.GetValues(typeof(EvolutionResource)))
                    Assert.That(service.Snapshot.Resource(resource), Is.EqualTo(resources.Resource(resource) - form.RequiredResources.Where(c => c.Resource == resource).Sum(c => c.Amount)));
                Assert.That(service.TryEvolve(true, out _), Is.False); Assert.That(service.Snapshot.Level(UpgradeCategory.DrillPower), Is.Zero);
            }
            else { Assert.That(service.Evolution(true).HasNext, Is.False); Assert.That(service.TryEvolve(true, out _), Is.False); Assert.That(service.Evolution(true).Description, Does.Contain("ALL UPGRADES MAX")); }
        }
        [Test] public void AllSevenTransitionsRejectEachMissingUpgradeAndEveryMissingResourceWithoutSpending()
        {
            for (var stage = 1; stage < 8; stage++)
            {
                var profile = At(stage); var service = new ProgressionService(new MemoryProgressStore(profile), Root); Assert.That(service.TryEvolve(true, out _), Is.False);
                foreach (UpgradeCategory missing in Enum.GetValues(typeof(UpgradeCategory)))
                {
                    service = new ProgressionService(new MemoryProgressStore(profile), Root);
                    foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory))) for (var i = 0; i < (category == missing ? 4 : 5); i++) Assert.That(service.TryPurchase(category, out _), Is.True);
                    var before = ProgressCodec.Encode(service.Snapshot); Assert.That(service.TryEvolve(true, out _), Is.False); Assert.That(ProgressCodec.Encode(service.Snapshot), Is.EqualTo(before));
                }
                foreach (var cost in Root.ForForm(profile.FormId).RequiredResources)
                {
                    var empty = At(stage, true);
                    if (cost.Resource == EvolutionResource.Copper) empty.Copper = cost.Amount - 1; else if (cost.Resource == EvolutionResource.Iron) empty.Iron = cost.Amount - 1;
                    else if (cost.Resource == EvolutionResource.Quartz) empty.Quartz = cost.Amount - 1; else if (cost.Resource == EvolutionResource.AlloyCore) empty.AlloyCore = cost.Amount - 1; else empty.EnergyShard = cost.Amount - 1;
                    service = new ProgressionService(new MemoryProgressStore(empty), Root); var before = ProgressCodec.Encode(service.Snapshot);
                    Assert.That(service.Evolution(true).ResourcesAvailable, Is.False); Assert.That(service.TryEvolve(true, out _), Is.False); Assert.That(ProgressCodec.Encode(service.Snapshot), Is.EqualTo(before));
                }
            }
        }
        private sealed class FailingStore : IProgressStore
        {
            public MemoryProgressStore Store; public bool Fail = true; public Action DuringSave;
            public ProgressLoad Load() => Store.Load();
            public bool TrySave(PlayerProgress progress, out string error)
            { DuringSave?.Invoke(); if (Fail) { error = "Injected atomic write failure"; return false; } return Store.TrySave(progress, out error); }
        }
        [Test] public void MultiResourceEvolutionAndUpgradeFailuresRollBackDeepHistoryAndRejectReentrantCallbacks()
        {
            var store = new FailingStore { Store = new MemoryProgressStore(At(6, true)) }; var service = new ProgressionService(store, Root); var before = ProgressCodec.Encode(service.Snapshot);
            Assert.That(service.TryEvolve(true, out _), Is.False); Assert.That(ProgressCodec.Encode(service.Snapshot), Is.EqualTo(before));
            store.Fail = false; store.DuringSave = () => Assert.That(service.TryEvolve(true, out _), Is.False);
            Assert.That(service.TryEvolve(true, out _), Is.True); Assert.That(service.ActiveForm.Stage, Is.EqualTo(7));
            store.DuringSave = null; store.Fail = true; before = ProgressCodec.Encode(service.Snapshot); Assert.That(service.TryPurchase(UpgradeCategory.DrillPower, out _), Is.False);
            Assert.That(ProgressCodec.Encode(service.Snapshot), Is.EqualTo(before)); var detached = service.Snapshot; detached.AdvancedForms[0].Power = 0;
            Assert.That(service.Snapshot.AdvancedForms[0].Power, Is.EqualTo(5));
        }
        [Test] public void InvalidFutureSchemasBalancesSkippedStagesAndDuplicateHistoryStayReadOnly()
        {
            var good = At(8); Action<PlayerProgress>[] corruptions = { p=>p.EvolutionSchema=2,p=>p.FormId="evo_9",p=>p.Iron=-1,p=>p.Quartz=-1,p=>p.AlloyCore=-1,p=>p.EnergyShard=-1,
                p=>p.AdvancedForms[0].Power=4,p=>p.AdvancedForms[2].FormId=p.AdvancedForms[1].FormId,p=>p.AdvancedForms=p.AdvancedForms.Take(5).ToArray(),p=>p.WorkshopEnergyLevel=4,p=>p.AdvancedForms[5].Drive=6 };
            foreach (var corrupt in corruptions)
            { var bad = good.Copy(); corrupt(bad); var service = new ProgressionService(new MemoryProgressStore(bad), Root); Assert.That(service.Writable, Is.False); Assert.That(service.TryBeginAttempt(out _,out _), Is.False); }
            var full = At(3); full.Coins=long.MaxValue; full.Copper=long.MaxValue; var safe=new ProgressionService(new MemoryProgressStore(full),Root);
            Assert.That(safe.TryBeginAttempt(out var token,out _),Is.True); Assert.That(safe.TryBank(token,1,1,out _),Is.False); Assert.That(safe.Snapshot.Coins,Is.EqualTo(long.MaxValue));
        }
        [Serializable] private sealed class M5Profile
        {
            public int Version=1,FormSchema=1,PowerLevel=5,EnergyLevel=5,DriveLevel=5,WorkshopPowerLevel=2,WorkshopEnergyLevel=3,WorkshopDriveLevel=4;
            public string ProfileId="123456789012345678901234567890ab",FormId="workshop_drill";
            public long Revision=42,Coins=1234,Copper=17,IssuedAttempt=8,BankedAttempt=7;
        }
        [Serializable] private sealed class Envelope {public int Version=1;public string Payload,Checksum;}
        [Test] public void AuthenticM5WorkshopPayloadMigratesWithoutDiskRewriteAndPreservesReceipts()
        {
            var directory=Path.Combine(Path.GetTempPath(),"DrillRunnerM8-"+Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(directory);var disk=new FileProgressStore(directory);var payload=JsonUtility.ToJson(new M5Profile());string checksum;
                using(var sha=SHA256.Create())checksum=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
                var json=JsonUtility.ToJson(new Envelope{Payload=payload,Checksum=checksum});File.WriteAllText(disk.Path,json);
                var service=new ProgressionService(disk,Root);Assert.That(service.Writable,Is.True);Assert.That(File.ReadAllText(disk.Path),Is.EqualTo(json));
                var p=service.Snapshot;Assert.That(p.Coins,Is.EqualTo(1234));Assert.That(p.Copper,Is.EqualTo(17));Assert.That(p.Iron+p.Quartz+p.AlloyCore+p.EnergyShard,Is.Zero);
                Assert.That(p.FormId,Is.EqualTo(ProgressionConfig.WorkshopForm));Assert.That(p.WorkshopPowerLevel,Is.EqualTo(2));Assert.That(p.WorkshopEnergyLevel,Is.EqualTo(3));Assert.That(p.WorkshopDriveLevel,Is.EqualTo(4));
                Assert.That(p.AdvancedForms,Is.Empty);Assert.That(p.EvolutionSchema,Is.EqualTo(1));Assert.That(service.TryBank(7,999,999,out _),Is.True);
                Assert.That(service.TryBeginAttempt(out var token,out _),Is.True);Assert.That(token,Is.EqualTo(9));Assert.That(File.ReadAllText(disk.BackupPath),Is.EqualTo(json));
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [TestCase(3)] [TestCase(8)] public void AdvancedSavesRoundTripUpgradesEvolutionReceiptsAndBackupRecovery(int stage)
        {
            var directory=Path.Combine(Path.GetTempPath(),"DrillRunnerM8-"+Guid.NewGuid().ToString("N"));
            try
            {
                var disk=new FileProgressStore(directory);disk.Load();Assert.That(disk.TrySave(At(stage),out _),Is.True);
                var service=new ProgressionService(disk,Root);Assert.That(service.TryPurchase(UpgradeCategory.DrillPower,out _),Is.True);Assert.That(service.TryBeginAttempt(out var token,out _),Is.True);
                Assert.That(service.TryBank(token,200,3,out _),Is.True);var saved=service.Snapshot;var reload=new ProgressionService(new FileProgressStore(directory),Root);
                Assert.That(ProgressCodec.Encode(reload.Snapshot),Is.EqualTo(ProgressCodec.Encode(saved)));Assert.That(reload.TryBank(token,200,3,out _),Is.True);Assert.That(reload.Snapshot.Coins,Is.EqualTo(saved.Coins));
                File.WriteAllText(disk.Path,"corrupt M8 write");var blocked=new ProgressionService(new FileProgressStore(directory),Root);Assert.That(blocked.Writable,Is.False);
                Assert.That(blocked.Snapshot.FormId,Is.EqualTo(saved.FormId));Assert.That(new FileProgressStore(directory).RecoverBackup(Root,out _),Is.True);
                Assert.That(new ProgressionService(new FileProgressStore(directory),Root).Writable,Is.True);
            }
            finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
        [Test] public void EightDistinctVisualsAndAllEncounterTransitionsRespectTheOriginalEnvelope()
        {
            FullEvolutionSetup.Validate(Root);var library=AssetDatabase.LoadAssetAtPath<EncounterLibrary>(GameplayVarietySetup.LibraryPath);var models=new System.Collections.Generic.HashSet<GameObject>();
            for(var stage=1;stage<=8;stage++)
            {
                var form=Root.ForForm(ProgressionConfig.FormIds[stage-1]);Assert.That(models.Add(form.VisualPrefab),Is.True);Assert.That(form.Power.MaximumLevel,Is.EqualTo(5));
                var envelope=EncounterEnvelope.Accepted;
                foreach(var a in library.Patterns){Assert.That(EncounterSafety.Validate(a,library,envelope,out _),Is.True);foreach(var b in library.Patterns)Assert.That(EncounterSafety.Transition(a.Section,b.Section,library,envelope,out _),Is.True);}
            }
        }
        [Test] public void CircularResourceDependenciesAndStageSkippingConfigurationAreRejected()
        {
            var root=Object.Instantiate(Root);var next=Object.Instantiate(root.NextForm);root.NextForm=next;
            try
            {
                next.EvolutionResources=new[]{new ResourceCost(EvolutionResource.Iron,1)};Assert.Throws<ArgumentException>(()=>root.Validate());
                next.EvolutionResources=new[]{new ResourceCost(EvolutionResource.Copper,24),new ResourceCost(EvolutionResource.Copper,1)};Assert.Throws<ArgumentException>(()=>root.Validate());
                next.EvolutionResources=new[]{new ResourceCost(EvolutionResource.Copper,24)};next.NextForm=Root.ForForm("heavy_borer");Assert.Throws<ArgumentException>(()=>root.Validate());
            }
            finally{Object.DestroyImmediate(next);Object.DestroyImmediate(root);}
        }
        [Test] public void M8SetupPreservesAllFormBytesAndCustomSceneWorkOnRepeatedRuns()
        {
            const string path="Assets/_DrillRunner/Scenes/M8SetupTemporary.unity";var paths=Enumerable.Range(3,6).Select(FullEvolutionSetup.ConfigPath).Concat(new[]{WorkshopSetup.ConfigPath,EvolutionSetup.WorkshopPath,FoundationSetup.ScenePath}).ToArray();
            var bytes=paths.Select(File.ReadAllBytes).ToArray();Assert.That(AssetDatabase.CopyAsset(FoundationSetup.ScenePath,path),Is.True);
            try
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    var custom=new GameObject("M8 Custom Work");SceneManager.MoveGameObjectToScene(custom,scene);EditorSceneManager.SaveScene(scene);var before=File.ReadAllBytes(path);
                    FullEvolutionSetup.SetupScene(path);FullEvolutionSetup.SetupScene(path);Assert.That(File.ReadAllBytes(path),Is.EqualTo(before));Assert.That(custom,Is.Not.Null);
                    EditorSceneManager.MarkSceneDirty(scene);Assert.Throws<InvalidOperationException>(()=>FullEvolutionSetup.SetupScene(path));
                }
                finally{EditorSceneManager.CloseScene(scene,true);}
            }
            finally{AssetDatabase.DeleteAsset(path);}for(var i=0;i<paths.Length;i++)Assert.That(File.ReadAllBytes(paths[i]),Is.EqualTo(bytes[i]));
        }
        [Test] public void FutureProgressionEstimatesUseExplicitFundedResourcesAndAStatedYardAvoidanceIncomeModel()
        {
            var rows=new System.Collections.Generic.List<string>{"stage,model,runs,coinsEarned,upgradeCoins,meanRunMinutes"};
            for(var stage=3;stage<8;stage++)
            {
                var profile=At(stage);profile.Coins=0;var service=new ProgressionService(new MemoryProgressStore(profile),Root);var runs=0;long earned=0;double minutes=0;
                while(!service.Evolution(true).Ready&&runs<40)
                {
                    var attributes=service.Attributes;var seconds=attributes.Energy/0.7;var coins=(long)Math.Floor(attributes.Cruise*seconds/5);
                    Assert.That(service.TryBeginAttempt(out var token,out _),Is.True);Assert.That(service.TryBank(token,coins,0,out _),Is.True);earned+=coins;minutes+=seconds/60;runs++;
                    foreach(var category in new[]{UpgradeCategory.EnergyCapacity,UpgradeCategory.DrivePower,UpgradeCategory.DrillPower})while(service.TryPurchase(category,out _)){}
                }
                Assert.That(service.Evolution(true).Ready,Is.True);var form=service.ActiveForm;
                rows.Add(string.Join(",",stage,"pre-funded advanced resources; mathematical Yard no-drilling income",runs,earned,form.Power.Costs.Sum()+form.Energy.Costs.Sum()+form.Drive.Costs.Sum(),(minutes/runs).ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)));
                Assert.That(service.TryEvolve(true,out _),Is.True);Assert.That(service.ActiveForm.Stage,Is.EqualTo(stage+1));
            }
            File.WriteAllLines("Logs/M8-FutureEstimates.csv",rows);
        }
    }
}

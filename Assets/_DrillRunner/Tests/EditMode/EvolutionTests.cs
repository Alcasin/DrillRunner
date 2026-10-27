using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.EditMode
{
    public sealed class EvolutionTests
    {
        private ProgressionConfig config, next;
        private string directory;
        [SetUp] public void Prepare()
        {
            config = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ProgressionConfig>(WorkshopSetup.ConfigPath));
            next = Object.Instantiate(config.NextForm); config.NextForm = next;
            directory = Path.Combine(Path.GetTempPath(), "DrillRunner-M5-" + Guid.NewGuid().ToString("N"));
        }
        [TearDown] public void Clean()
        { Object.DestroyImmediate(config); Object.DestroyImmediate(next); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private static PlayerProgress Max(long copper = 12) => new PlayerProgress { PowerLevel = 5, EnergyLevel = 5, DriveLevel = 5, Coins = 10000, Copper = copper };
        private ProgressionService Service(PlayerProgress profile) => new ProgressionService(new MemoryProgressStore(profile), config);

        [TestCase(0, 0, 0, 12, false)] [TestCase(4, 5, 5, 12, false)] [TestCase(5, 4, 5, 12, false)]
        [TestCase(5, 5, 4, 12, false)] [TestCase(5, 5, 5, 11, false)] [TestCase(5, 5, 5, 12, true)]
        public void EligibilityRequiresEveryCategoryAndTheConfiguredCopper(int power, int energy, int drive, long copper, bool expected)
        {
            var service = Service(new PlayerProgress { PowerLevel = power, EnergyLevel = energy, DriveLevel = drive, Copper = copper });
            var state = service.Evolution(true); Assert.That(state.Ready, Is.EqualTo(expected));
            Assert.That(state.PowerComplete, Is.EqualTo(power == 5)); Assert.That(state.EnergyComplete, Is.EqualTo(energy == 5));
            Assert.That(state.DriveComplete, Is.EqualTo(drive == 5)); Assert.That(state.CopperAvailable, Is.EqualTo(copper >= config.EvolutionCopper));
            Assert.That(state.Description, Does.Contain("Copper " + copper + " / " + config.EvolutionCopper));
            Assert.That(service.TryEvolve(true, out _), Is.EqualTo(expected));
        }
        [Test] public void WorkshopPermissionAndAValidNextFormAreMandatory()
        {
            var service = Service(Max()); Assert.That(service.Evolution(false).Ready, Is.False);
            Assert.That(service.TryEvolve(false, out _), Is.False); Assert.That(service.Snapshot.Copper, Is.EqualTo(12));
            config.NextForm = null; var noNext = Service(Max()); Assert.That(noNext.Evolution(true).HasNext, Is.False);
            Assert.That(noNext.TryEvolve(true, out _), Is.False);
        }
        [Test] public void EvolutionSpendsOnlyCopperOnceAndRetainsAllPurchasedPocketHistory()
        {
            var profile = Max(18); profile.Revision = 40; profile.IssuedAttempt = 8; profile.BankedAttempt = 7;
            var service = Service(profile); var before = service.Attributes;
            Assert.That(service.TryEvolve(true, out _), Is.True); var saved = service.Snapshot;
            Assert.That(saved.FormId, Is.EqualTo(ProgressionConfig.WorkshopForm)); Assert.That(saved.FormSchema, Is.EqualTo(1));
            Assert.That(saved.Coins, Is.EqualTo(10000)); Assert.That(saved.Copper, Is.EqualTo(6)); Assert.That(saved.Revision, Is.EqualTo(41));
            Assert.That(saved.PowerLevel, Is.EqualTo(5)); Assert.That(saved.EnergyLevel, Is.EqualTo(5)); Assert.That(saved.DriveLevel, Is.EqualTo(5));
            Assert.That(saved.WorkshopPowerLevel, Is.Zero); Assert.That(saved.IssuedAttempt, Is.EqualTo(8)); Assert.That(saved.BankedAttempt, Is.EqualTo(7));
            var after = service.Attributes; Assert.That(after.Power, Is.GreaterThan(before.Power)); Assert.That(after.Energy, Is.GreaterThanOrEqualTo(before.Energy));
            Assert.That(after.Cruise, Is.GreaterThanOrEqualTo(before.Cruise)); Assert.That(after.MinimumBoost, Is.GreaterThanOrEqualTo(before.MinimumBoost));
            Assert.That(after.MaximumBoost, Is.GreaterThanOrEqualTo(before.MaximumBoost)); Assert.That(after.Recovery, Is.GreaterThanOrEqualTo(before.Recovery));
            for (var i = 0; i < 10; i++) Assert.That(service.TryEvolve(true, out _), Is.False);
            Assert.That(service.Snapshot.Revision, Is.EqualTo(41)); Assert.That(service.Snapshot.Copper, Is.EqualTo(6));
        }
        [Test] public void WorkshopTierChargesItsOwnPricesAndCannotRechargePocketUpgrades()
        {
            var service = Service(Max()); service.TryEvolve(true, out _);
            Assert.That(service.Attributes.Power, Is.EqualTo(12)); Assert.That(service.Attributes.Energy, Is.EqualTo(200)); Assert.That(service.Attributes.Cruise, Is.EqualTo(12.2f));
            long spent = 0;
            foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
            {
                var table = next.Table(category);
                for (var i = 0; i < table.MaximumLevel; i++) { Assert.That(service.TryPurchase(category, out _), Is.True); spent += table.Costs[i]; }
                Assert.That(service.TryPurchase(category, out _), Is.False); Assert.That(service.Snapshot.Level(category), Is.EqualTo(5));
            }
            Assert.That(service.Snapshot.Coins, Is.EqualTo(10000 - spent)); Assert.That(spent, Is.EqualTo(5230));
            Assert.That(service.Snapshot.PowerLevel, Is.EqualTo(5)); Assert.That(service.Snapshot.Copper, Is.Zero);
            Assert.That(service.Attributes.Power, Is.EqualTo(18)); Assert.That(service.Attributes.Energy, Is.EqualTo(300));
            Assert.That(service.Attributes.Cruise + service.Attributes.MaximumBoost, Is.EqualTo(26));
        }
        private sealed class SwitchStore : IProgressStore
        {
            private readonly MemoryProgressStore memory = new MemoryProgressStore(Max());
            public bool Fail = true;
            public ProgressLoad Load() => memory.Load();
            public bool TrySave(PlayerProgress progress, out string error)
            { if (Fail) { error = "Injected evolution save failure"; return false; } return memory.TrySave(progress, out error); }
        }
        [Test] public void FailedEvolutionSaveDoesNotPublishAnyPartOfTheTransaction()
        {
            var store = new SwitchStore(); var service = new ProgressionService(store, config); var before = service.Snapshot;
            Assert.That(service.TryEvolve(true, out _), Is.False); Assert.That(service.Snapshot.FormId, Is.EqualTo(ProgressionConfig.PocketForm));
            Assert.That(service.Snapshot.Copper, Is.EqualTo(before.Copper)); Assert.That(service.Snapshot.Coins, Is.EqualTo(before.Coins));
            Assert.That(service.Snapshot.Revision, Is.EqualTo(before.Revision)); Assert.That(service.Evolution(true).Ready, Is.True);
            store.Fail = false; Assert.That(service.TryEvolve(true, out _), Is.True); Assert.That(service.Snapshot.Copper, Is.Zero);
        }
        [Serializable] private sealed class LegacyProfile
        {
            public int Version = 1;
            public string ProfileId = "123456789012345678901234567890ab";
            public string FormId = "pocket_drill";
            public long Revision = 42, Coins = 1234, Copper = 17, IssuedAttempt = 8, BankedAttempt = 7;
            public int PowerLevel = 2, EnergyLevel = 3, DriveLevel = 4;
        }
        [Serializable] private sealed class Envelope { public int Version = 1; public string Payload, Checksum; }
        private static string LegacyJson()
        {
            var payload = JsonUtility.ToJson(new LegacyProfile());
            using (var sha = SHA256.Create()) return JsonUtility.ToJson(new Envelope { Payload = payload, Checksum = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))) });
        }
        [Test] public void AuthenticM4PayloadMigratesAdditivelyWithoutRewritingItOnLoad()
        {
            Directory.CreateDirectory(directory); var disk = new FileProgressStore(directory); var legacy = LegacyJson(); File.WriteAllText(disk.Path, legacy);
            Assert.That(legacy, Does.Not.Contain("FormSchema")); Assert.That(legacy, Does.Not.Contain("WorkshopPowerLevel"));
            var service = new ProgressionService(disk, config); var profile = service.Snapshot;
            Assert.That(service.Writable, Is.True); Assert.That(profile.Coins, Is.EqualTo(1234)); Assert.That(profile.Copper, Is.EqualTo(17));
            Assert.That(profile.ProfileId, Is.EqualTo("123456789012345678901234567890ab")); Assert.That(profile.Revision, Is.EqualTo(42));
            Assert.That(profile.FormId, Is.EqualTo(ProgressionConfig.PocketForm)); Assert.That(profile.PowerLevel, Is.EqualTo(2));
            Assert.That(profile.EnergyLevel, Is.EqualTo(3)); Assert.That(profile.DriveLevel, Is.EqualTo(4)); Assert.That(profile.WorkshopPowerLevel, Is.Zero);
            Assert.That(profile.FormSchema, Is.EqualTo(1)); Assert.That(File.ReadAllText(disk.Path), Is.EqualTo(legacy));
            Assert.That(service.TryBank(7, 999, 999, out _), Is.True); Assert.That(service.Snapshot.Coins, Is.EqualTo(1234));
            Assert.That(service.TryBeginAttempt(out var token, out _), Is.True); Assert.That(token, Is.EqualTo(9));
            var reload = new ProgressionService(new FileProgressStore(directory), config); Assert.That(reload.Snapshot.Coins, Is.EqualTo(1234));
            Assert.That(reload.Snapshot.BankedAttempt, Is.EqualTo(7)); Assert.That(File.ReadAllText(disk.BackupPath), Is.EqualTo(legacy));
        }
        [Test] public void EvolvedProfileAndNewLevelsSurviveDiskReloadAndBackupRecovery()
        {
            var disk = new FileProgressStore(directory); disk.Load(); disk.TrySave(Max(24), out _);
            var service = new ProgressionService(disk, config); service.TryEvolve(true, out _); service.TryPurchase(UpgradeCategory.DrillPower, out _);
            service.TryPurchase(UpgradeCategory.EnergyCapacity, out _);
            var loaded = new ProgressionService(new FileProgressStore(directory), config);
            Assert.That(loaded.Snapshot.FormId, Is.EqualTo(ProgressionConfig.WorkshopForm)); Assert.That(loaded.Attributes.Power, Is.EqualTo(13));
            Assert.That(loaded.Attributes.Energy, Is.EqualTo(220)); Assert.That(loaded.Snapshot.Copper, Is.EqualTo(12));
            File.WriteAllText(disk.Path, "interrupted write"); var corrupt = new ProgressionService(new FileProgressStore(directory), config);
            Assert.That(corrupt.Writable, Is.False); Assert.That(corrupt.Snapshot.FormId, Is.EqualTo(ProgressionConfig.WorkshopForm));
            Assert.That(new FileProgressStore(directory).RecoverBackup(config, out _), Is.True);
            var recovered = new ProgressionService(new FileProgressStore(directory), config); Assert.That(recovered.Attributes.Power, Is.EqualTo(13));
            Assert.That(recovered.Snapshot.Copper, Is.EqualTo(12));
        }
        [TestCase("unknown", 1, 0)] [TestCase("workshop_drill", 0, 0)] [TestCase("workshop_drill", 2, 0)]
        [TestCase("workshop_drill", 1, -1)] [TestCase("workshop_drill", 1, 6)] [TestCase("pocket_drill", 1, 1)]
        public void InvalidFormsSchemasAndWorkshopLevelsStayReadOnly(string form, int schema, int level)
        {
            var profile = Max(); profile.FormId = form; profile.FormSchema = schema; profile.WorkshopPowerLevel = level;
            var service = Service(profile); Assert.That(service.Writable, Is.False); Assert.That(service.TryEvolve(true, out _), Is.False);
        }
        [Test] public void EvolvedHistoryCannotSkipPocketPurchasesAndCopperOverflowCannotDuplicateEvolution()
        {
            var invalid = Max(); invalid.FormId = ProgressionConfig.WorkshopForm; invalid.FormSchema = 1; invalid.PowerLevel = 4;
            Assert.That(Service(invalid).Writable, Is.False);
            var service = Service(Max(long.MaxValue)); Assert.That(service.TryEvolve(true, out _), Is.True);
            Assert.That(service.Snapshot.Copper, Is.EqualTo(long.MaxValue - 12)); Assert.That(service.TryEvolve(true, out _), Is.False);
        }
        [Test] public void InvalidCostsRegressingStatsAndUnsupportedChainsAreRejected()
        {
            config.EvolutionCopper = 0; Assert.Throws<ArgumentException>(() => config.Validate()); config.EvolutionCopper = 12;
            next.Power.Values[0] = 9; Assert.Throws<ArgumentException>(() => config.Validate()); next.Power.Values[0] = 12;
            next.Energy.Values[0] = 170; Assert.Throws<ArgumentException>(() => config.Validate()); next.Energy.Values[0] = 200;
            next.SpeedCeiling = 40; Assert.Throws<ArgumentException>(() => config.Validate()); next.SpeedCeiling = 26;
            next.NextForm = config; Assert.Throws<ArgumentException>(() => config.Validate());
        }
        [Test] public void InvalidLiveConfigurationCannotAddCopperOrCommitAnEvolution()
        {
            var service = Service(Max()); config.EvolutionCopper = -12;
            Assert.That(service.Evolution(true).Ready, Is.False); Assert.That(service.TryEvolve(true, out _), Is.False);
            Assert.That(service.Snapshot.Copper, Is.EqualTo(12)); Assert.That(service.Snapshot.FormId, Is.EqualTo(ProgressionConfig.PocketForm));
            Assert.That(service.TryPurchase(UpgradeCategory.DrillPower, out _), Is.False);
        }
        [Test] public void InterruptedStagingDoesNotReplaceTheAuthoritativeProfileOrChargeCopper()
        {
            var disk = new FileProgressStore(directory); disk.Load(); disk.TrySave(Max(), out _);
            var hypothetical = Max(0); hypothetical.FormId = ProgressionConfig.WorkshopForm; hypothetical.FormSchema = 1;
            var staging = disk.Path + ".crash.tmp"; var staged = ProgressCodec.Encode(hypothetical); File.WriteAllText(staging, staged);
            var service = new ProgressionService(new FileProgressStore(directory), config);
            Assert.That(service.Snapshot.FormId, Is.EqualTo(ProgressionConfig.PocketForm)); Assert.That(service.Snapshot.Copper, Is.EqualTo(12));
            Assert.That(File.ReadAllText(staging), Is.EqualTo(staged)); Assert.That(service.TryEvolve(true, out _), Is.True);
            var committed = new ProgressionService(new FileProgressStore(directory), config);
            Assert.That(committed.Snapshot.FormId, Is.EqualTo(ProgressionConfig.WorkshopForm)); Assert.That(committed.TryEvolve(true, out _), Is.False);
        }
    }
}

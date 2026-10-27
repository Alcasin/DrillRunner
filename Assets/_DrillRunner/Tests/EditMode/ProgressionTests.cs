using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.EditMode
{
    public sealed class ProgressionTests
    {
        private ProgressionConfig config;
        private string directory;
        [SetUp] public void Prepare()
        { config = ScriptableObject.CreateInstance<ProgressionConfig>(); directory = Path.Combine(Path.GetTempPath(), "DrillRunner-M4-" + Guid.NewGuid().ToString("N")); }
        [TearDown] public void Clean()
        { Object.DestroyImmediate(config); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        private FileProgressStore Disk() => new FileProgressStore(directory);
        private ProgressionService Funded(long coins = 10000) => new ProgressionService(new MemoryProgressStore(new PlayerProgress { Coins = coins }), config);

        [Test] public void MissingSaveCreatesValidDefaultsWithoutWritingOrTouchingTheRealProfile()
        {
            var disk = Disk(); var service = new ProgressionService(disk, config); var p = service.Snapshot;
            Assert.That(service.Writable, Is.True); Assert.That(File.Exists(disk.Path), Is.False);
            Assert.That(p.Coins, Is.Zero); Assert.That(p.Copper, Is.Zero); Assert.That(p.PowerLevel, Is.Zero);
            Assert.That(p.Version, Is.EqualTo(1)); Assert.That(p.FormId, Is.EqualTo("pocket_drill"));
            Assert.That(service.Attributes.Power, Is.EqualTo(5)); Assert.That(service.Attributes.Energy, Is.EqualTo(100));
            p.Coins = 1000; Assert.That(service.Snapshot.Coins, Is.Zero, "Snapshots cannot mutate authority.");
            Assert.Throws<ArgumentException>(() => new FileProgressStore("relative"));
        }
        [Test] public void ActualDiskRoundTripPreservesPurchasedLevelsCurrencyAndDurableBankReceipt()
        {
            var service = new ProgressionService(Disk(), config);
            Assert.That(service.TryBeginAttempt(out var token, out _), Is.True);
            Assert.That(service.TryBank(token, 1000, 6, out _), Is.True);
            Assert.That(service.TryPurchase(UpgradeCategory.DrillPower, out _), Is.True);
            Assert.That(service.TryPurchase(UpgradeCategory.EnergyCapacity, out _), Is.True);
            var loaded = new ProgressionService(Disk(), config);
            Assert.That(loaded.Snapshot.Coins, Is.EqualTo(890)); Assert.That(loaded.Snapshot.Copper, Is.EqualTo(6));
            Assert.That(loaded.Attributes.Power, Is.EqualTo(6)); Assert.That(loaded.Attributes.Energy, Is.EqualTo(115));
            Assert.That(loaded.TryBank(token, 1000, 6, out _), Is.True);
            Assert.That(loaded.Snapshot.Coins, Is.EqualTo(890)); Assert.That(File.Exists(Disk().BackupPath), Is.True);
        }
        [Test] public void RapidPurchasesObeyCostsAffordabilityAndAllMaximumLevels()
        {
            var poor = Funded(159);
            Assert.That(poor.TryPurchase(UpgradeCategory.DrillPower, out _), Is.True);
            for (var i = 0; i < 8; i++) Assert.That(poor.TryPurchase(UpgradeCategory.DrillPower, out _), Is.False);
            Assert.That(poor.Snapshot.Coins, Is.EqualTo(99)); Assert.That(poor.Snapshot.PowerLevel, Is.EqualTo(1));
            var rich = Funded(); long total = 0;
            foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
            {
                var table = config.Table(category); var previous = table.Values[0];
                for (var i = 0; i < table.MaximumLevel; i++)
                {
                    Assert.That(rich.TryPurchase(category, out _), Is.True); total += table.Costs[i];
                    var now = config.Table(category).Values[rich.Snapshot.Level(category)]; Assert.That(now, Is.GreaterThan(previous)); previous = now;
                }
                var before = rich.Snapshot; Assert.That(rich.TryPurchase(category, out var error), Is.False); Assert.That(error, Does.Contain("MAX"));
                Assert.That(rich.Snapshot.Coins, Is.EqualTo(before.Coins));
            }
            Assert.That(rich.Snapshot.Coins, Is.EqualTo(10000 - total)); Assert.That(rich.Attributes.Power, Is.EqualTo(10));
            Assert.That(rich.Attributes.Energy, Is.EqualTo(180)); Assert.That(rich.Attributes.Cruise, Is.EqualTo(12));
            Assert.That(rich.Attributes.Cruise + rich.Attributes.MaximumBoost, Is.LessThanOrEqualTo(24));
        }
        [Test] public void InvalidCategoryInsufficientFundsAndInvalidTablesAreRejected()
        {
            var service = Funded(0); var before = service.Snapshot;
            Assert.That(service.TryPurchase((UpgradeCategory)99, out _), Is.False);
            Assert.That(service.TryPurchase(UpgradeCategory.DrivePower, out _), Is.False);
            Assert.That(service.Snapshot.Revision, Is.EqualTo(before.Revision));
            config.Power.Values[5] = 8; Assert.Throws<ArgumentException>(() => config.Validate());
            config.Power.Values[5] = 10; config.Drive.Values[5] = 13; Assert.Throws<ArgumentException>(() => config.Validate());
            config.Drive.Values[5] = 12; config.Energy.Costs[0] = -1; Assert.Throws<ArgumentException>(() => config.Validate());
        }
        [TestCase(-1, 0, 0)] [TestCase(0, -1, 0)] [TestCase(0, 0, -1)] [TestCase(0, 0, 6)]
        public void InvalidBalancesAndLevelsCannotBecomeWritableProgress(long coins, long copper, int level)
        {
            var profile = new PlayerProgress { Coins = coins, Copper = copper, PowerLevel = level };
            var service = new ProgressionService(new MemoryProgressStore(profile), config);
            Assert.That(service.Writable, Is.False); Assert.That(service.TryPurchase(UpgradeCategory.DrillPower, out _), Is.False);
        }
        [Test] public void BankingDuplicatesLateCallbacksAndInvalidRewardsNeverCreateExtraCurrency()
        {
            var service = Funded(0); Assert.That(service.TryBeginAttempt(out var first, out _), Is.True);
            Assert.That(service.TryBank(first, -1, 3, out _), Is.False); Assert.That(service.TryBank(first, 5, -1, out _), Is.False);
            Assert.That(service.TryBank(first + 1, 5, 3, out _), Is.False);
            Assert.That(service.TryBank(first, 5, 3, out _), Is.True);
            var revision = service.Snapshot.Revision;
            for (var i = 0; i < 10; i++) Assert.That(service.TryBank(first, 5, 3, out _), Is.True);
            Assert.That(service.Snapshot.Revision, Is.EqualTo(revision));
            Assert.That(service.TryBeginAttempt(out var second, out _), Is.True);
            Assert.That(service.TryBank(first, 900, 900, out _), Is.True);
            Assert.That(service.TryBank(second, 7, 6, out _), Is.True);
            Assert.That(service.Snapshot.Coins, Is.EqualTo(12)); Assert.That(service.Snapshot.Copper, Is.EqualTo(9));
        }
        [Test] public void OverflowRejectsTheEntireBankWithoutMutation()
        {
            var service = new ProgressionService(new MemoryProgressStore(new PlayerProgress { Coins = long.MaxValue, Copper = long.MaxValue }), config);
            Assert.That(service.TryBeginAttempt(out var token, out _), Is.True); var before = service.Snapshot;
            Assert.That(service.TryBank(token, 1, 0, out _), Is.False); Assert.That(service.TryBank(token, 0, 1, out _), Is.False);
            Assert.That(service.Snapshot.Revision, Is.EqualTo(before.Revision)); Assert.That(service.Snapshot.BankedAttempt, Is.Zero);
        }
        [Test] public void RevisionAndAttemptCounterLimitsRejectWithoutChangingState()
        {
            var revision = new ProgressionService(new MemoryProgressStore(new PlayerProgress { Revision = long.MaxValue, Coins = 100 }), config);
            Assert.That(revision.TryPurchase(UpgradeCategory.DrillPower, out _), Is.False); Assert.That(revision.Snapshot.Coins, Is.EqualTo(100));
            var attempts = new ProgressionService(new MemoryProgressStore(new PlayerProgress { IssuedAttempt = long.MaxValue }), config);
            Assert.That(attempts.TryBeginAttempt(out var token, out _), Is.False); Assert.That(token, Is.Zero);
            Assert.That(attempts.Snapshot.IssuedAttempt, Is.EqualTo(long.MaxValue));
        }
        [Test] public void ExclusiveWriterLockFailsSafelyAndTheSameBankCanBeRetried()
        {
            var disk = Disk(); var service = new ProgressionService(disk, config); service.TryBeginAttempt(out var token, out _);
            using (var held = new FileStream(disk.Path + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.That(service.TryBank(token, 100, 3, out _), Is.False);
                Assert.That(service.Snapshot.Coins, Is.Zero); Assert.That(service.Snapshot.BankedAttempt, Is.Zero);
            }
            Assert.That(service.TryBank(token, 100, 3, out _), Is.True); Assert.That(service.Snapshot.Coins, Is.EqualTo(100));
            Assert.That(new ProgressionService(Disk(), config).Snapshot.Copper, Is.EqualTo(3));
        }
        private sealed class FailingStore : IProgressStore
        {
            public ProgressLoad Load() => new ProgressLoad(new PlayerProgress { Coins = 100 }, true, "test");
            public bool TrySave(PlayerProgress progress, out string error) { error = "Injected disk failure"; return false; }
        }
        [Test] public void FailedPersistenceCannotPartiallyPurchaseOrIssueAnAttempt()
        {
            var service = new ProgressionService(new FailingStore(), config);
            Assert.That(service.TryPurchase(UpgradeCategory.DrillPower, out _), Is.False);
            Assert.That(service.Snapshot.Coins, Is.EqualTo(100)); Assert.That(service.Snapshot.PowerLevel, Is.Zero);
            Assert.That(service.TryBeginAttempt(out var token, out _), Is.False); Assert.That(token, Is.Zero); Assert.That(service.Snapshot.IssuedAttempt, Is.Zero);
        }
        [TestCase("{")] [TestCase("{}")] [TestCase("null")] [TestCase("{\"Version\":2,\"Payload\":\"future\"}")]
        public void CorruptOrUnsupportedPrimaryIsPreservedAndNeverSilentlyOverwritten(string contents)
        {
            Directory.CreateDirectory(directory); File.WriteAllText(Disk().Path, contents);
            var service = new ProgressionService(Disk(), config);
            Assert.That(service.Writable, Is.False); Assert.That(service.TryBeginAttempt(out _, out _), Is.False);
            Assert.That(File.ReadAllText(Disk().Path), Is.EqualTo(contents));
        }
        [Test] public void TruncatedPrimaryCanShowBackupReadOnlyAndConfirmedRecoveryArchivesIt()
        {
            var disk = Disk(); var service = new ProgressionService(disk, config);
            service.TryBeginAttempt(out var token, out _); service.TryBank(token, 100, 3, out _);
            service.TryPurchase(UpgradeCategory.EnergyCapacity, out _); // backup has 100 Coins, 3 Copper before this purchase.
            File.WriteAllText(disk.Path, "truncated primary");
            var damaged = new ProgressionService(Disk(), config);
            Assert.That(damaged.Writable, Is.False); Assert.That(damaged.Snapshot.Coins, Is.EqualTo(100));
            Assert.That(damaged.TryPurchase(UpgradeCategory.DrillPower, out _), Is.False);
            Assert.That(File.ReadAllText(disk.Path), Is.EqualTo("truncated primary"));
            Assert.That(Disk().RecoverBackup(config, out _), Is.True);
            var recovered = new ProgressionService(Disk(), config); Assert.That(recovered.Writable, Is.True);
            var archived = Directory.GetFiles(directory, "*.rejected-*"); Assert.That(archived.Length, Is.EqualTo(1));
            Assert.That(File.ReadAllText(archived[0]), Is.EqualTo("truncated primary")); Assert.That(recovered.Snapshot.Copper, Is.EqualTo(3));
        }
        [Test] public void IntegrityMismatchExternalEditsAndStaleWritersFailWithoutReplacingData()
        {
            var first = new ProgressionService(Disk(), config); first.TryBeginAttempt(out var token, out _);
            var stale = new ProgressionService(Disk(), config);
            first.TryBank(token, 100, 0, out _); var before = File.ReadAllText(Disk().Path);
            Assert.That(stale.TryBeginAttempt(out _, out _), Is.False); Assert.That(File.ReadAllText(Disk().Path), Is.EqualTo(before));
            Assert.That(ProgressCodec.TryDecode(before.Replace("Checksum", "ChangedChecksum"), out _, out _), Is.False);
            File.WriteAllText(Disk().Path, "external edit"); Assert.That(first.TryPurchase(UpgradeCategory.DrillPower, out _), Is.False);
            Assert.That(first.Snapshot.Coins, Is.EqualTo(100)); Assert.That(File.ReadAllText(Disk().Path), Is.EqualTo("external edit"));
            Assert.That(Directory.GetFiles(directory, "*.tmp"), Is.Empty);
        }
    }
}

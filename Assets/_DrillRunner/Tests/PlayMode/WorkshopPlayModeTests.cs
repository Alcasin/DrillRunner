using System;
using System.Collections;
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
    public sealed class WorkshopPlayModeTests
    {
        private string directory;
        private WorkshopController flow;
        private DrillMotor motor;
        private PrototypeHud hud;
        private TrackCatalog ownedCatalog;
        private const float Step = 1f / 120;
        [UnitySetUp] public IEnumerator Open()
        {
            directory = Path.Combine(Path.GetTempPath(), "DrillRunner-M4-Play-" + Guid.NewGuid().ToString("N"));
            var disk = new FileProgressStore(directory); disk.Load();
            Assert.That(disk.TrySave(new PlayerProgress { Coins = 1000, Copper = 12 }, out _), Is.True);
            WorkshopController.StoreFactory = () => new FileProgressStore(directory);
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null;
            Find(); Assert.That(flow.SavePath.StartsWith(directory, StringComparison.Ordinal), Is.True);
            Assert.That(flow.OpenWorkshop(), Is.True);
        }
        private void Find()
        {
            flow = Object.FindFirstObjectByType<WorkshopController>(); motor = flow.Run.Motor;
            motor.enabled = false; hud = Object.FindFirstObjectByType<PrototypeHud>();
        }
        [UnityTearDown] public IEnumerator Close()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            WorkshopController.StoreFactory = () => new MemoryProgressStore();
            if (ownedCatalog != null) Object.Destroy(ownedCatalog);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        private void Launch() { motor.Motion.Press(); motor.Motion.Step(1.5f); motor.Motion.Release(); }
        private void Until(Func<bool> condition, int ticks = 10000)
        {
            for (var i = 0; i < ticks && !condition(); i++)
            {
                motor.Simulate(Step);
                flow.Run.Track.UpdateCoverage(motor.Motion.Position.z, motor.ContactTarget);
            }
            Assert.That(condition(), Is.True, "State=" + motor.Motion.State);
        }
        private void Isolate(DrillObstacle target)
        {
            if (!motor.RegisteredObstacles.Contains(target)) ownedCatalog = LegacyTemplateFixture.Restore(flow.Run.Track, hud);
            foreach (var obstacle in motor.Obstacles) obstacle.gameObject.SetActive(obstacle == target);
            target.transform.position = new Vector3(0, 1, 5);
        }
        private void End()
        { motor.Energy.Consume(10000, 1); motor.Simulate(Step); }

        [UnityTest] public IEnumerator WorkshopPurchasesRefreshBalancesAndApplyAttributesWithoutRestartingTheEditor()
        {
            Assert.That(flow.View.Panel.activeSelf, Is.True); Assert.That(flow.View.Balance.text, Does.Contain("1000"));
            Assert.That(flow.View.Balance.text, Does.Contain("12"));
            foreach (var button in flow.View.PurchaseButtons) button.onClick.Invoke();
            Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(840)); Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(12));
            Assert.That(flow.View.Rows[0].text, Does.Contain("1 / 5")); Assert.That(flow.View.Balance.text, Does.Contain("840"));
            flow.View.StartButton.onClick.Invoke();
            Assert.That(flow.IsOpen, Is.False); Assert.That(flow.View.Panel.activeSelf, Is.False);
            Assert.That(motor.DrillPower, Is.EqualTo(6)); Assert.That(motor.Energy.Capacity, Is.EqualTo(115));
            Assert.That(motor.Energy.Remaining, Is.EqualTo(115)); Assert.That(motor.Settings.CruiseSpeed, Is.EqualTo(10.4f));
            Launch(); motor.Simulate(0.1f); Assert.That(motor.Motion.ForwardSpeed, Is.GreaterThan(20));
            Assert.That(flow.Purchase(UpgradeCategory.DrillPower), Is.False, "Cannot buy during a run.");
            Assert.That(flow.OpenWorkshop(), Is.False); yield return null;
        }
        [UnityTest] public IEnumerator PurchasedPowerNineActuallyDrillsStoneAndBreaksWoodFaster()
        {
            for (var i = 0; i < 4; i++) Assert.That(flow.Purchase(UpgradeCategory.DrillPower), Is.True);
            Assert.That(flow.StartRun(), Is.True); Assert.That(motor.DrillPower, Is.EqualTo(9));
            var wood = motor.Obstacles[0]; Isolate(wood); Launch(); Until(() => motor.Motion.State == RunState.Drilling);
            for (var i = 0; i < 60; i++) motor.Simulate(Step);
            Assert.That(wood.Durability.Remaining, Is.EqualTo(25).Within(0.1), "Power 5 leaves 45 HP after 0.5 seconds.");
            End(); Assert.That(flow.OpenWorkshop(), Is.True); Assert.That(flow.StartRun(), Is.True);
            var stone = motor.Obstacles[2]; Isolate(stone); Launch(); Until(() => motor.Motion.State == RunState.Drilling);
            Assert.That(motor.ContactTarget, Is.SameAs(stone)); Assert.That(stone.Durability.Hardness, Is.EqualTo(9));
            yield return Capture("StonePower9");
            Until(() => stone.Durability.IsDestroyed); Assert.That(flow.Run.Rewards.ObstacleCoins, Is.EqualTo(3));
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
        }
        [UnityTest] public IEnumerator MaximumDriveRemainsControlledAndCannotBypassStoneHardness()
        {
            for (var i = 0; i < 5; i++) Assert.That(flow.Purchase(UpgradeCategory.DrivePower), Is.True);
            Assert.That(flow.View.Rows[2].text, Does.Contain("MAX")); Assert.That(flow.View.PurchaseButtons[2].interactable, Is.False);
            var coins = flow.Service.Snapshot.Coins; flow.View.PurchaseButtons[2].onClick.Invoke(); Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(coins));
            Assert.That(flow.StartRun(), Is.True); Assert.That(motor.Settings.CruiseSpeed, Is.EqualTo(12)); Assert.That(motor.Settings.ResumeAcceleration, Is.EqualTo(22));
            Isolate(motor.Obstacles[2]); Launch(); Assert.That(motor.Motion.ForwardSpeed, Is.EqualTo(24));
            Until(() => motor.Motion.State == RunState.Blocked); Assert.That(motor.DrillPower, Is.EqualTo(5));
            Assert.That(motor.ContactTarget.Durability.Remaining, Is.EqualTo(240)); Assert.That(flow.Purchase(UpgradeCategory.DrillPower), Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator CompletedCopperRunBanksOnceAndRetryClearsOnlyTemporaryEarnings()
        {
            ownedCatalog = Object.Instantiate(flow.Run.Track.Catalog);
            ownedCatalog.Variants = new[] { ownedCatalog.Variants.Single(s => s.SectionId == "Copper") }; ownedCatalog.ResourceInterval = 1;
            flow.Run.Track.Catalog = ownedCatalog; Assert.That(flow.StartRun(), Is.True);
            foreach (var obstacle in motor.Obstacles) obstacle.gameObject.SetActive(false);
            Launch(); Until(() => motor.Motion.State == RunState.Drilling && motor.ContactTarget.Definition.RewardKind == ObstacleRewardKind.Copper);
            var copper = motor.ContactTarget; motor.Simulate(0.5f); Assert.That(flow.Run.Rewards.Copper, Is.Zero);
            Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(12));
            Until(() => copper.Durability.IsDestroyed); Assert.That(flow.Run.Rewards.Copper, Is.EqualTo(3)); Assert.That(flow.Run.Rewards.ObstacleCoins, Is.Zero);
            End(); yield return null;
            var coins = flow.Run.Rewards.Coins; var ledger = flow.Run.Rewards;
            Assert.That(flow.IsBanked, Is.True); Assert.That(flow.PendingBank, Is.False);
            Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(1000 + coins)); Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(15));
            Assert.That(hud.SummaryText.text, Does.Contain("3 Copper")); Assert.That(flow.View.Banking.text, Does.Contain("saved"));
            for (var i = 0; i < 5; i++) flow.Run.SendMessage("Complete");
            copper.Restore(); copper.Durability.Drill(10, 10, 100);
            Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(1000 + coins)); Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(15));
            yield return Capture("BankedResults");
            Assert.That(flow.OpenWorkshop(), Is.True); Assert.That(flow.View.Balance.text, Does.Contain("15"));
            Assert.That(flow.StartRun(), Is.True); Assert.That(flow.Run.Rewards.Coins, Is.Zero); Assert.That(flow.Run.Rewards.Copper, Is.Zero);
            Assert.That(ledger.Coins, Is.EqualTo(coins));
            for (var i = 0; i < 5; i++) hud.Restart();
            Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(1000 + coins));
            var disk = new ProgressionService(new FileProgressStore(directory), flow.Configuration);
            Assert.That(disk.Snapshot.Coins, Is.EqualTo(1000 + coins)); Assert.That(disk.Snapshot.Copper, Is.EqualTo(15));
        }
        [UnityTest] public IEnumerator ProgressSurvivesSceneReloadAndRetryUsesThePurchasedEnergyCapacity()
        {
            Assert.That(flow.Purchase(UpgradeCategory.EnergyCapacity), Is.True);
            Assert.That(flow.Purchase(UpgradeCategory.EnergyCapacity), Is.True);
            Assert.That(flow.StartRun(), Is.True); Launch(); motor.Simulate(0.5f); End();
            var saved = flow.Service.Snapshot;
            hud.RestartButton.onClick.Invoke(); Assert.That(motor.Energy.Capacity, Is.EqualTo(130)); Assert.That(motor.Energy.Remaining, Is.EqualTo(130));
            Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(saved.Coins));
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null; Find();
            Assert.That(motor.Energy.Capacity, Is.EqualTo(130)); Assert.That(flow.Service.Snapshot.EnergyLevel, Is.EqualTo(2));
            Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(saved.Coins)); Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(saved.Copper));
        }
        private sealed class SwitchStore : IProgressStore
        {
            private readonly MemoryProgressStore memory = new MemoryProgressStore(new PlayerProgress { Coins = 100 });
            public bool Fail;
            public ProgressLoad Load() => memory.Load();
            public bool TrySave(PlayerProgress progress, out string error)
            { if (Fail) { error = "Injected write failure"; return false; } return memory.TrySave(progress, out error); }
        }
        [UnityTest] public IEnumerator SaveFailureKeepsFrozenRewardsAndBlocksResetUntilTheBankCanBeSaved()
        {
            var store = new SwitchStore(); WorkshopController.StoreFactory = () => store;
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null; Find();
            flow.OpenWorkshop(); store.Fail = true;
            Assert.That(flow.Purchase(UpgradeCategory.DrillPower), Is.False);
            Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(100)); Assert.That(flow.Service.Snapshot.PowerLevel, Is.Zero);
            store.Fail = false; Assert.That(flow.StartRun(), Is.True);
            Launch(); motor.Simulate(0.5f); var coins = flow.Run.Rewards.Coins; Assert.That(coins, Is.GreaterThan(0));
            store.Fail = true; End(); yield return null;
            Assert.That(flow.PendingBank, Is.True); Assert.That(flow.IsBanked, Is.False); Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(100));
            var ledger = flow.Run.Rewards; hud.Restart(); Assert.That(flow.Run.Rewards, Is.SameAs(ledger)); Assert.That(flow.StartRun(), Is.False);
            Assert.That(flow.OpenWorkshop(), Is.True); Assert.That(flow.Purchase(UpgradeCategory.DrillPower), Is.False);
            Assert.That(flow.View.SaveButton.gameObject.activeSelf, Is.True);
            store.Fail = false; flow.View.SaveButton.onClick.Invoke();
            Assert.That(flow.PendingBank, Is.False); Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(100 + coins));
            Assert.That(flow.StartRun(), Is.True); Assert.That(flow.Run.Rewards.Coins, Is.Zero); yield return null;
        }
        [UnityTest] public IEnumerator CorruptProfileOpensAReadOnlyWorkshopAndCannotBeOverwrittenByStartOrPurchases()
        {
            var path = new FileProgressStore(directory).Path; File.WriteAllText(path, "truncated profile");
            var backup = path + ".bak"; if (File.Exists(backup)) File.Delete(backup);
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null; Find();
            Assert.That(flow.IsOpen, Is.True); Assert.That(flow.Service.Writable, Is.False);
            Assert.That(flow.View.StartButton.interactable, Is.False); Assert.That(flow.View.PurchaseButtons.All(b => !b.interactable), Is.True);
            Assert.That(flow.StartRun(), Is.False); Assert.That(flow.Purchase(UpgradeCategory.DrillPower), Is.False);
            Assert.That(File.ReadAllText(path), Is.EqualTo("truncated profile")); Assert.That(flow.View.Status.text, Does.Contain("preserved"));
        }
        [UnityTest] public IEnumerator RepeatedWorkshopNavigationCreatesNoDuplicateControllersAndActiveResetDiscardsUnbankedRewards()
        {
            for (var i = 0; i < 8; i++)
            {
                Assert.That(flow.StartRun(), Is.True); Launch(); motor.Simulate(0.5f);
                Assert.That(flow.Run.Rewards.Coins, Is.GreaterThan(0)); hud.Restart();
                Assert.That(flow.OpenWorkshop(), Is.True); Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(1000));
                yield return null;
                Assert.That(Object.FindObjectsByType<WorkshopController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<DrillMotor>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<RunProgress>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(flow.Run.Track.ActiveSections.Count, Is.EqualTo(6));
            }
            yield return Capture("Workshop");
        }
        [UnityTest] public IEnumerator TemporaryEditorFundingAndPowerOverridesCannotModifyTheRealProfile()
        {
            var original = File.ReadAllBytes(flow.SavePath);
            flow.UseTemporaryFunding(); Assert.That(flow.IsSandbox, Is.True);
            foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
                for (var i = 0; i < 5; i++) Assert.That(flow.Purchase(category), Is.True);
            yield return Capture("WorkshopMax");
            Assert.That(flow.StartRun(), Is.True); Assert.That(motor.DrillPower, Is.EqualTo(10)); Assert.That(motor.Energy.Capacity, Is.EqualTo(180));
            Launch(); motor.Simulate(0.5f); End(); Assert.That(flow.IsBanked, Is.True);
            Assert.That(File.ReadAllBytes(new FileProgressStore(directory).Path), Is.EqualTo(original));
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null; Find();
            Assert.That(flow.Service.Snapshot.PowerLevel, Is.Zero); Assert.That(motor.DrillPower, Is.EqualTo(5));
            var beforeQa = flow.Service.Snapshot.Coins;
            flow.StartQaRun(10, 8); Launch(); motor.Simulate(0.5f); End();
            Assert.That(flow.IsBanked, Is.True); Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(beforeQa));
            Assert.That(flow.Service.Snapshot.PowerLevel, Is.Zero);
        }
        private IEnumerator Capture(string state)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            var follower = Object.FindFirstObjectByType<RunnerCamera>(); follower.Snap(); var camera = follower.GetComponent<Camera>();
            var canvas = hud.GetComponent<Canvas>(); var texture = new RenderTexture(540, 960, 24); var image = new Texture2D(540, 960, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldPlane = canvas.planeDistance; var oldActive = RenderTexture.active;
            try
            {
                camera.targetTexture = texture; camera.aspect = 9f / 16; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 0.5f;
                yield return null; yield return null; RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/M4-" + state + ".png", image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane; camera.targetTexture = oldTarget;
                camera.ResetAspect(); RenderTexture.active = oldActive; texture.Release(); Object.Destroy(texture); Object.Destroy(image);
            }
        }
    }
}

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
    public sealed class EvolutionPlayModeTests
    {
        private string directory;
        private WorkshopController flow;
        private DrillMotor motor;
        private PrototypeHud hud;
        private TrackCatalog legacyCatalog;
        private const float Step = 1f / 120;
        [UnitySetUp] public IEnumerator Open()
        {
            directory = Path.Combine(Path.GetTempPath(), "DrillRunner-M5-Play-" + Guid.NewGuid().ToString("N"));
            var disk = new FileProgressStore(directory); disk.Load(); disk.TrySave(new PlayerProgress { Coins = 10000, Copper = 24 }, out _);
            WorkshopController.StoreFactory = () => new FileProgressStore(directory);
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null; Find(); flow.OpenWorkshop();
        }
        private void Find() { flow = Object.FindFirstObjectByType<WorkshopController>(); motor = flow.Run.Motor; motor.enabled = false; hud = Object.FindFirstObjectByType<PrototypeHud>(); }
        [UnityTearDown] public IEnumerator Close()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene"); WorkshopController.StoreFactory = () => new MemoryProgressStore();
            if (legacyCatalog != null) Object.Destroy(legacyCatalog);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
        private void MaxPocket()
        {
            foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
                for (var i = 0; i < 5; i++) Assert.That(flow.Purchase(category), Is.True);
        }
        private void SkipPresentation() { flow.enabled = false; flow.enabled = true; Assert.That(flow.IsEvolving, Is.False); }
        private void Launch() { motor.Motion.Press(); motor.Motion.Step(1.5f); motor.Motion.Release(); }
        private void Until(Func<bool> condition, int ticks = 5000)
        { for (var i = 0; i < ticks && !condition(); i++) motor.Simulate(Step); Assert.That(condition(), Is.True, "State=" + motor.Motion.State); }
        private void Isolate(DrillObstacle target, float z = 5)
        {
            if (!motor.RegisteredObstacles.Contains(target)) legacyCatalog = LegacyTemplateFixture.Restore(flow.Run.Track, hud);
            foreach (var obstacle in motor.RegisteredObstacles) obstacle.gameObject.SetActive(obstacle == target); target.transform.position = new Vector3(0, 1, z);
        }
        private void End() { motor.Energy.Consume(10000, 1); motor.Simulate(Step); }

        [UnityTest] public IEnumerator EvolutionUiShowsRequirementsAndRapidClicksOnlySpendCopperOnce()
        {
            Assert.That(flow.View.FormName.text, Does.Contain("POCKET DRILL")); Assert.That(flow.View.Requirements.text, Does.Contain("Power 0/5"));
            Assert.That(flow.View.EvolveButton.interactable, Is.False); flow.View.EvolveButton.onClick.Invoke();
            Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(24));
            MaxPocket(); Assert.That(flow.Evolution.Ready, Is.True); Assert.That(flow.View.EvolveButton.interactable, Is.True);
            Assert.That(flow.View.Requirements.text, Does.Contain("spend 12 Copper")); yield return Capture("EvolutionReady");
            var coins = flow.Service.Snapshot.Coins; var ledger = flow.Run.Rewards;
            flow.View.EvolveButton.onClick.Invoke(); Assert.That(flow.IsEvolving, Is.True); Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(12));
            Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(coins)); Assert.That(flow.StartRun(), Is.False);
            Assert.That(flow.Purchase(UpgradeCategory.DrillPower), Is.False); hud.Restart(); Assert.That(flow.Run.Rewards, Is.SameAs(ledger));
            for (var i = 0; i < 10; i++) flow.View.EvolveButton.onClick.Invoke();
            Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(12)); Assert.That(flow.Service.Snapshot.FormId, Is.EqualTo(ProgressionConfig.WorkshopForm));
            yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(flow.IsEvolving, Is.False); Assert.That(flow.Message, Does.Contain("WORKSHOP DRILL UNLOCKED"));
            Assert.That(flow.View.FormName.text, Does.Contain("WORKSHOP DRILL")); Assert.That(flow.View.Rows[0].text, Does.Contain("0 / 5"));
            Assert.That(flow.View.EvolveButton.gameObject.activeSelf, Is.False); Assert.That(flow.View.FormPreview.texture, Is.SameAs(flow.Configuration.NextForm.PreviewImage));
            yield return Capture("WorkshopDrillUnlocked");
        }
        [UnityTest] public IEnumerator InterruptedPresentationReloadRestoresTheCommittedFormAndItsNewUpgradeLevels()
        {
            MaxPocket(); Assert.That(flow.Evolve(), Is.True);
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null; Find(); flow.OpenWorkshop();
            Assert.That(flow.Service.Snapshot.FormId, Is.EqualTo(ProgressionConfig.WorkshopForm)); Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(12));
            Assert.That(flow.IsEvolving, Is.False); Assert.That(flow.Visuals.CurrentForm, Is.EqualTo(ProgressionConfig.WorkshopForm));
            Assert.That(flow.Purchase(UpgradeCategory.DrillPower), Is.True); Assert.That(flow.Purchase(UpgradeCategory.EnergyCapacity), Is.True);
            var saved = flow.Service.Snapshot;
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null; Find(); flow.OpenWorkshop();
            Assert.That(motor.DrillPower, Is.EqualTo(13)); Assert.That(motor.Energy.Capacity, Is.EqualTo(220));
            Assert.That(flow.Service.Snapshot.WorkshopPowerLevel, Is.EqualTo(1)); Assert.That(flow.Service.Snapshot.PowerLevel, Is.EqualTo(5));
            Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(saved.Coins)); Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(12));
            Assert.That(flow.Evolve(), Is.False);
        }
        [UnityTest] public IEnumerator WorkshopDrillUsesStrongerActualStatsOneVisualAndTheSameFootprintThroughRetry()
        {
            MaxPocket(); flow.Evolve(); SkipPresentation(); Assert.That(flow.StartRun(), Is.True);
            Assert.That(motor.DrillPower, Is.EqualTo(12)); Assert.That(motor.Energy.Capacity, Is.EqualTo(200)); Assert.That(motor.Energy.Remaining, Is.EqualTo(200));
            Assert.That(motor.Settings.CruiseSpeed, Is.EqualTo(12.2f)); Assert.That(motor.Settings.ResumeAcceleration, Is.EqualTo(24));
            Assert.That(flow.Visuals.ActiveVisual.name, Is.EqualTo("Workshop Drill Visual")); Assert.That(motor.transform.Find("Visuals").gameObject.activeSelf, Is.False);
            Assert.That(motor.GetComponent<BoxCollider>().size, Is.EqualTo(new Vector3(1.7f, 1.3f, 3)));
            Assert.That(flow.Visuals.ActiveVisual.GetComponentsInChildren<Collider>(true), Is.Empty);
            var wood = motor.Obstacles[0]; Isolate(wood); Launch(); Until(() => motor.Motion.State == RunState.Drilling);
            for (var i = 0; i < 60; i++) motor.Simulate(Step);
            Assert.That(wood.Durability.Remaining, Is.EqualTo(10).Within(0.15), "Max Pocket Power 10 leaves 20 HP after 0.5 seconds.");
            Until(() => wood.Durability.IsDestroyed); End(); yield return null; yield return Capture("WorkshopDrillResults");
            var coins = flow.Service.Snapshot.Coins; var copper = flow.Service.Snapshot.Copper;
            hud.RestartButton.onClick.Invoke(); Assert.That(motor.DrillPower, Is.EqualTo(12)); Assert.That(motor.Energy.Capacity, Is.EqualTo(200));
            Assert.That(flow.Visuals.CurrentForm, Is.EqualTo(ProgressionConfig.WorkshopForm)); Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(coins));
            Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(copper)); Assert.That(flow.Run.Rewards.Coins, Is.Zero);
            Assert.That(Object.FindObjectsByType<DrillMotor>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<DrillPointerInput>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator EvolutionIsUnavailableWhileMovingDrillingOrOutsideWorkshop()
        {
            MaxPocket(); Assert.That(flow.StartRun(), Is.True); Assert.That(flow.Evolve(), Is.False);
            Launch(); motor.Simulate(0.2f); Assert.That(flow.Evolve(), Is.False);
            Until(() => motor.Motion.State == RunState.Drilling); Assert.That(flow.Evolve(), Is.False);
            Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(24)); Assert.That(flow.Service.Snapshot.FormId, Is.EqualTo(ProgressionConfig.PocketForm));
            End(); flow.OpenWorkshop(); Assert.That(flow.Evolve(), Is.True); SkipPresentation(); yield return null;
        }
        private sealed class SwitchStore : IProgressStore
        {
            private readonly MemoryProgressStore memory = new MemoryProgressStore(new PlayerProgress { Coins = 10000, Copper = 24 });
            public bool Fail;
            public ProgressLoad Load() => memory.Load();
            public bool TrySave(PlayerProgress progress, out string error)
            { if (Fail) { error = "Injected evolution write failure"; return false; } return memory.TrySave(progress, out error); }
        }
        [UnityTest] public IEnumerator FailedEvolutionSaveKeepsPocketVisualCopperAndUiReadyForASafeRetry()
        {
            var store = new SwitchStore(); WorkshopController.StoreFactory = () => store;
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null; Find(); flow.OpenWorkshop(); MaxPocket();
            store.Fail = true; var coins = flow.Service.Snapshot.Coins; Assert.That(flow.Evolve(), Is.False);
            Assert.That(flow.IsEvolving, Is.False); Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(24)); Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(coins));
            Assert.That(flow.Visuals.CurrentForm, Is.EqualTo(ProgressionConfig.PocketForm)); Assert.That(flow.View.FormName.text, Does.Contain("POCKET DRILL"));
            Assert.That(flow.View.EvolveButton.interactable, Is.True); store.Fail = false; Assert.That(flow.Evolve(), Is.True); SkipPresentation();
        }
        [UnityTest] public IEnumerator NewDriveTierKeepsThinObstacleSweepsHardnessEscapeAndPortraitFramingSafe()
        {
            MaxPocket(); flow.Evolve(); SkipPresentation();
            for (var i = 0; i < 5; i++) Assert.That(flow.Purchase(UpgradeCategory.DrivePower), Is.True);
            flow.StartRun(); var stone = motor.Obstacles[2]; Isolate(stone, 7); stone.transform.localScale = new Vector3(1.8f, 2, 0.01f);
            Launch(); Assert.That(motor.Motion.ForwardSpeed, Is.EqualTo(26)); motor.Simulate(0.5f);
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Drilling)); Assert.That(stone.Durability.IsDestroyed, Is.False);
            var bounds = DrillContacts.WorldBounds(motor.GetComponent<BoxCollider>());
            Assert.That(bounds.max.z, Is.LessThanOrEqualTo(stone.WorldBounds.min.z - DrillContacts.Skin + 0.001f));
            End(); flow.StartQaRun(5, 200); Isolate(stone, 7); Launch(); motor.Simulate(0.5f);
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Blocked)); Assert.That(stone.Durability.Remaining, Is.EqualTo(240));
            motor.Motion.Press(); motor.Motion.Drag(new Vector2(-10000, 0), 540); Until(() => motor.Motion.State == RunState.Moving);
            var follower = Object.FindFirstObjectByType<RunnerCamera>(); var camera = follower.GetComponent<Camera>(); camera.aspect = 9f / 16;
            foreach (var desired in new[] { -2.15f, 2.15f })
            {
                motor.Motion.CancelPointer(); motor.Motion.Press(); motor.Motion.Drag(new Vector2((desired - motor.Motion.Position.x) * 540 / 8, 0), 540); motor.Simulate(0.6f);
                follower.Snap(); var screen = camera.WorldToViewportPoint(motor.transform.position + Vector3.up * 0.8f);
                Assert.That(screen.x, Is.InRange(0.08f, 0.92f)); Assert.That(screen.y, Is.InRange(0.15f, 0.5f));
                foreach (var renderer in flow.Visuals.ActiveVisual.GetComponentsInChildren<MeshRenderer>())
                { Assert.That(renderer.bounds.min.x, Is.GreaterThanOrEqualTo(motor.transform.position.x - 0.851f)); Assert.That(renderer.bounds.max.x, Is.LessThanOrEqualTo(motor.transform.position.x + 0.851f)); }
            }
            camera.ResetAspect(); yield return Capture("WorkshopDrillMoving");
        }
        [UnityTest] public IEnumerator TemporaryCopperFundingEvolutionAndNewUpgradesNeverTouchTheDiskProfile()
        {
            var path = flow.SavePath; var original = File.ReadAllBytes(path);
            flow.UseTemporaryFunding(0); MaxPocket(); Assert.That(flow.Evolve(), Is.False); Assert.That(flow.View.Requirements.text, Does.Contain("Copper 0 / 12"));
            flow.UseTemporaryFunding(24); Assert.That(flow.Evolve(), Is.True); SkipPresentation();
            foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory))) Assert.That(flow.Purchase(category), Is.True);
            Assert.That(flow.StartRun(), Is.True); Launch(); motor.Simulate(0.5f); End();
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null; Find();
            Assert.That(flow.Service.Snapshot.FormId, Is.EqualTo(ProgressionConfig.PocketForm)); Assert.That(flow.Service.Snapshot.PowerLevel, Is.Zero);
            Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(24)); Assert.That(flow.Visuals.CurrentForm, Is.EqualTo(ProgressionConfig.PocketForm));
        }
        [UnityTest] public IEnumerator GuidedRunsCanEarnTheCompleteFirstEvolutionWithoutAnyFundingOrBalanceEdits()
        {
            WorkshopController.StoreFactory = () => new MemoryProgressStore();
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null; Find(); flow.OpenWorkshop();
            long totalCoins = 0, totalCopper = 0; var runs = 0;
            while (!flow.Evolution.Ready && runs < 8)
            {
                runs++; flow.StartRun(); flow.Run.UseFixedSeed = true; flow.Run.ConfiguredSeed = 900 + runs; hud.Restart();
                var collectCopper = flow.Service.Snapshot.Copper < flow.Configuration.EvolutionCopper;
                Launch(); var ticks = 0;
                while (motor.Motion.State != RunState.RunEnded && ticks++ < 4000)
                {
                    // A guided effective player avoids ordinary obstacles, seeks at most one Ore per run until funded,
                    // and respects the same continuous steering/contact/energy rules as manual play.
                    var footprint = DrillContacts.WorldBounds(motor.GetComponent<BoxCollider>());
                    var target = motor.RegisteredObstacles.Where(o => o != null && o.IsSolid && o.WorldBounds.max.z >= footprint.min.z - 0.05f)
                        .OrderBy(o => o.WorldBounds.min.z).FirstOrDefault();
                    var desired = -2.1f;
                    if (target != null && target.WorldBounds.min.z < footprint.max.z + 25)
                    {
                        var group = motor.RegisteredObstacles.Where(o => o != null && o.IsSolid &&
                            Mathf.Abs(o.WorldBounds.min.z - target.WorldBounds.min.z) < 3).ToArray();
                        var ore = group.FirstOrDefault(o => o.Definition.RewardKind == ObstacleRewardKind.Copper);
                        desired = ore != null && collectCopper && flow.Run.Rewards.Copper == 0 ? ore.WorldBounds.center.x :
                            new[] { -2.1f, 0, 2.1f }.First(x => group.All(o => x + 0.85f <= o.WorldBounds.min.x - 0.05f || x - 0.85f >= o.WorldBounds.max.x + 0.05f));
                    }
                    motor.Motion.CancelPointer(); motor.Motion.Press(); motor.Motion.Drag(new Vector2((desired - motor.Motion.Position.x) * 540 / 8, 0), 540);
                    motor.Simulate(0.1f); flow.Run.Track.UpdateCoverage(motor.Motion.Position.z, motor.ContactTarget);
                    if (ticks % 100 == 0) yield return null;
                }
                Assert.That(motor.Motion.State, Is.EqualTo(RunState.RunEnded)); Assert.That(flow.IsBanked, Is.True);
                totalCoins += flow.Run.Rewards.Coins; totalCopper += flow.Run.Rewards.Copper; flow.OpenWorkshop();
                foreach (var category in new[] { UpgradeCategory.EnergyCapacity, UpgradeCategory.DrivePower, UpgradeCategory.DrillPower })
                    while (flow.Purchase(category)) { }
                Debug.Log("M5 guided run " + runs + ": " + flow.Run.Rewards.Distance.ToString("0") + " m, " + flow.Run.Rewards.Coins + " Coins, " + flow.Run.Rewards.Copper + " Copper; total " + totalCoins + "/" + totalCopper);
            }
            File.WriteAllText("Logs/M5-Economy.txt", "Guided runs: " + runs + "\nEarned Coins: " + totalCoins + "\nEarned Copper: " + totalCopper + "\n");
            Assert.That(flow.Evolution.Ready, Is.True, "The guided normal-economy journey should reach evolution within eight completed runs.");
            Assert.That(totalCoins, Is.GreaterThanOrEqualTo(2480)); Assert.That(totalCopper, Is.GreaterThanOrEqualTo(12));
            Assert.That(flow.IsSandbox, Is.False); Assert.That(flow.Evolve(), Is.True); SkipPresentation();
            Assert.That(flow.Service.Attributes.Power, Is.EqualTo(12)); Assert.That(flow.Purchase(UpgradeCategory.DrillPower), Is.True);
            Assert.That(flow.Service.Snapshot.WorkshopPowerLevel, Is.EqualTo(1)); Assert.That(flow.Service.Snapshot.PowerLevel, Is.EqualTo(5));
            Assert.That(flow.StartRun(), Is.True); Assert.That(motor.DrillPower, Is.EqualTo(13)); Assert.That(motor.Energy.Capacity, Is.EqualTo(200));
        }
        private IEnumerator Capture(string state)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            var follower = Object.FindFirstObjectByType<RunnerCamera>(); follower.Snap(); var camera = follower.GetComponent<Camera>(); var canvas = hud.GetComponent<Canvas>();
            var texture = new RenderTexture(540, 960, 24); var image = new Texture2D(540, 960, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldPlane = canvas.planeDistance; var oldActive = RenderTexture.active;
            try
            {
                camera.targetTexture = texture; camera.aspect = 9f / 16; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 0.5f;
                yield return null; yield return null; RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/M5-" + state + ".png", image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane; camera.targetTexture = oldTarget;
                camera.ResetAspect(); RenderTexture.active = oldActive; texture.Release(); Object.Destroy(texture); Object.Destroy(image);
            }
        }
    }
}

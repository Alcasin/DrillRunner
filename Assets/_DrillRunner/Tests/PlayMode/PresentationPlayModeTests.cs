using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public sealed class PresentationPlayModeTests
    {
        private DrillPresentation presentation;
        private WorkshopController flow;
        private DrillMotor motor;
        private PrototypeHud hud;
        private TrackCatalog ownedCatalog;
        private const float Step = 1f / 120;
        [UnitySetUp] public IEnumerator Open()
        {
            WorkshopController.StoreFactory = () => new MemoryProgressStore(new PlayerProgress { Coins = 10000, Copper = 24 });
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null;
            presentation = Object.FindFirstObjectByType<DrillPresentation>(); flow = Object.FindFirstObjectByType<WorkshopController>(); motor = flow.Run.Motor;
            hud = Object.FindFirstObjectByType<PrototypeHud>(); motor.enabled = false; motor.GetComponent<DrillPointerInput>().enabled = false;
            presentation.SendMessage("OnApplicationFocus", true); presentation.Sample();
        }
        [UnityTearDown] public IEnumerator Close()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene"); WorkshopController.StoreFactory = () => new MemoryProgressStore();
            if (ownedCatalog != null) Object.Destroy(ownedCatalog);
        }
        private void Launch() { motor.Motion.Press(); motor.Simulate(1.5f); motor.Motion.Release(); }
        private void Until(Func<bool> condition, int ticks = 6000)
        { for (var i = 0; i < ticks && !condition(); i++) motor.Simulate(Step); Assert.That(condition(), Is.True, "State=" + motor.Motion.State); }
        private void Isolate(DrillObstacle target, float z = 6)
        { foreach (var obstacle in motor.RegisteredObstacles) obstacle.gameObject.SetActive(obstacle == target); target.gameObject.SetActive(true); target.transform.position = new Vector3(0, 1, z); }
        private DrillObstacle Copper()
        { flow.Run.Track.UpdateCoverage(1, null); return motor.RegisteredObstacles.First(o => o.Definition.MaterialId == "copper_ore"); }
        private void End() { motor.Energy.Consume(10000, 1); motor.Simulate(Step); }

        [UnityTest] public IEnumerator RealChargeControlsPresentationAndLaunchSoundOccursOnce()
        {
            motor.Motion.Press(); motor.Simulate(0.75f);
            Assert.That(presentation.Charge, Is.EqualTo(motor.Motion.ChargeNormalized)); Assert.That(presentation.Charge, Is.EqualTo(0.5f).Within(0.001));
            Assert.That(presentation.Sounds.LoopCue, Is.EqualTo((int)FeedbackCue.Charge)); var starts = presentation.Sounds.LoopStarts;
            for (var i = 0; i < 10; i++) presentation.Sample(); Assert.That(presentation.Sounds.LoopStarts, Is.EqualTo(starts));
            yield return Capture("PocketCharging"); motor.Simulate(0.75f); yield return null;
            Assert.That(presentation.Charge, Is.EqualTo(1)); Assert.That(hud.ChargeFill.fillAmount, Is.EqualTo(1));
            motor.Motion.Release(); motor.Simulate(0.1f);
            Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.Launch], Is.EqualTo(1)); Assert.That(presentation.Sounds.LoopCue, Is.EqualTo(-1));
            for (var i = 0; i < 20; i++) presentation.Sample(); Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.Launch], Is.EqualTo(1));
            hud.Restart(); motor.Motion.Press(); motor.Motion.Release(); motor.Simulate(0.01f);
            Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.Launch], Is.EqualTo(2), "A valid quick tap also has launch feedback.");
        }
        [UnityTest] public IEnumerator ProgressiveWoodFeedbackBreaksOnceAndRestartClearsDamageParticlesAndLoops()
        {
            var wood = motor.Obstacles[0]; Isolate(wood); Launch(); Until(() => motor.Motion.State == RunState.Drilling);
            motor.Simulate(0.6f); Assert.That(presentation.Effects.IsDrilling, Is.True); Assert.That(presentation.Sounds.LoopCue, Is.EqualTo((int)FeedbackCue.Drill));
            Assert.That(wood.GetComponent<ObstacleAppearance>().DamageStage, Is.GreaterThan(0)); var starts = presentation.Sounds.LoopStarts;
            for (var i = 0; i < 20; i++) presentation.Sample(); Assert.That(presentation.Sounds.LoopStarts, Is.EqualTo(starts));
            yield return Capture("WoodDrilling"); Assert.That(presentation.Effects.ParticleCount, Is.GreaterThan(0)); Until(() => wood.Durability.IsDestroyed);
            Assert.That(presentation.Effects.IsDrilling, Is.False); Assert.That(presentation.Sounds.LoopCue, Is.EqualTo(-1));
            Assert.That(presentation.DestructionCues, Is.EqualTo(1)); Assert.That(flow.Run.Rewards.ObstacleCoins, Is.EqualTo(1));
            wood.Restore(); wood.Durability.Drill(5, 10, 100);
            Assert.That(presentation.DestructionCues, Is.EqualTo(1)); Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.Break], Is.EqualTo(1));
            Assert.That(flow.Run.Rewards.ObstacleCoins, Is.EqualTo(1)); hud.Restart();
            Assert.That(wood.GetComponent<ObstacleAppearance>().DamageStage, Is.Zero); Assert.That(presentation.Effects.ParticleCount, Is.Zero);
            Assert.That(presentation.Sounds.LoopCue, Is.EqualTo(-1)); yield return null;
        }
        [UnityTest] public IEnumerator BlockedFeedbackNeverShowsSuccessfulDrillingOrDamagesStone()
        {
            var stone = motor.Obstacles[2]; Isolate(stone); Launch(); Until(() => motor.Motion.State == RunState.Blocked);
            Assert.That(presentation.Effects.IsDrilling, Is.False); Assert.That(presentation.Sounds.LoopCue, Is.EqualTo(-1));
            Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.Blocked], Is.EqualTo(1)); motor.Simulate(1);
            Assert.That(stone.Durability.Remaining, Is.EqualTo(240)); Assert.That(stone.GetComponent<ObstacleAppearance>().DamageStage, Is.Zero);
            Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.Blocked], Is.EqualTo(1)); yield return Capture("StoneBlocked");
            motor.Motion.Press(); motor.Motion.Drag(new Vector2(-10000, 0), 540); Until(() => motor.Motion.State == RunState.Moving);
            Assert.That(presentation.Effects.IsDrilling, Is.False);
        }
        [UnityTest] public IEnumerator CopperBreakAndRewardFeedbackRespectTheLedgerAndResultsDoNotBankAgain()
        {
            var copper = Copper(); Isolate(copper); Launch(); Until(() => motor.Motion.State == RunState.Drilling); motor.Simulate(0.5f);
            Assert.That(flow.Run.Rewards.Copper, Is.Zero); Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.CopperReward], Is.Zero);
            yield return Capture("CopperDrilling"); Until(() => copper.Durability.IsDestroyed);
            Assert.That(flow.Run.Rewards.Copper, Is.EqualTo(3)); Assert.That(flow.Run.Rewards.ObstacleCoins, Is.Zero);
            Assert.That(presentation.Hud.Toast, Does.Contain("+3 COPPER")); Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.CopperBreak], Is.EqualTo(1));
            Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.CopperReward], Is.EqualTo(1)); yield return Capture("CopperBreak");
            End(); yield return null; var coins = flow.Service.Snapshot.Coins; var ore = flow.Service.Snapshot.Copper;
            for (var i = 0; i < 5; i++) { flow.Run.SendMessage("Complete"); presentation.Sample(); }
            Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.Finish], Is.EqualTo(1)); Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(coins));
            Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(ore)); Assert.That(presentation.Hud.ResultsCard.activeSelf, Is.True); yield return Capture("Results");
            flow.OpenWorkshop(); Assert.That(presentation.Sounds.LoopCue, Is.EqualTo(-1)); Assert.That(presentation.Effects.ParticleCount, Is.Zero);
            flow.StartRun(); Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(ore)); Assert.That(flow.Run.Rewards.Copper, Is.Zero);
        }
        [UnityTest] public IEnumerator WorkshopEvolutionAndUpgradeCuesFollowSuccessfulTransactionsOnly()
        {
            flow.OpenWorkshop(); yield return Capture("WorkshopPocket");
            foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory))) for (var i = 0; i < 5; i++) flow.Purchase(category);
            Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.Upgrade], Is.EqualTo(15)); flow.Purchase(UpgradeCategory.DrillPower);
            Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.Upgrade], Is.EqualTo(15)); var coins = flow.Service.Snapshot.Coins;
            yield return Capture("EvolutionReady");
            flow.Evolve(); yield return new WaitForSecondsRealtime(0.25f); yield return Capture("Evolution"); yield return new WaitForSecondsRealtime(0.9f);
            Assert.That(flow.Service.Snapshot.Copper, Is.EqualTo(12)); Assert.That(flow.Service.Snapshot.Coins, Is.EqualTo(coins));
            Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.Evolution], Is.EqualTo(1)); Assert.That(presentation.Sounds.LoopCue, Is.EqualTo(-1));
            yield return Capture("WorkshopEvo2");
            for (var i = 0; i < 6; i++) flow.Evolve(); Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.Evolution], Is.EqualTo(1));
            Assert.That(flow.StartRun(), Is.True);
            Assert.That(flow.Visuals.CurrentForm, Is.EqualTo(ProgressionConfig.WorkshopForm)); Assert.That(motor.DrillPower, Is.EqualTo(12));
            Launch(); motor.Simulate(0.1f); yield return Capture("WorkshopDrillMoving");
        }
        [UnityTest] public IEnumerator AllFourMaterialIdentitiesRenderUnderTheActualPortraitCamera()
        {
            foreach (var id in new[] { "wood", "brick", "stone", "copper_ore" })
            {
                hud.Restart(); var target = id == "copper_ore" ? Copper() : motor.Obstacles.First(o => o.Definition.MaterialId == id);
                Isolate(target); Launch(); motor.Simulate(0.02f); yield return Capture("Material-" + id);
                Assert.That(target.GetComponent<ObstacleAppearance>(), Is.Not.Null); Assert.That(target.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1));
            }
        }
        [UnityTest] public IEnumerator FocusDisableRetryAndNavigationStopLoopsWithoutDuplicatingPresentationObjects()
        {
            motor.Motion.Press(); motor.Simulate(0.2f); Assert.That(presentation.Sounds.LoopCue, Is.EqualTo((int)FeedbackCue.Charge));
            presentation.SendMessage("OnApplicationFocus", false); presentation.Sample(); Assert.That(presentation.Sounds.LoopCue, Is.EqualTo(-1));
            presentation.SendMessage("OnApplicationFocus", true); presentation.Sample(); Assert.That(presentation.Sounds.LoopCue, Is.EqualTo((int)FeedbackCue.Charge));
            presentation.enabled = false; Assert.That(presentation.Sounds.LoopCue, Is.EqualTo(-1)); presentation.enabled = true;
            for (var i = 0; i < 6; i++)
            {
                hud.Restart(); flow.OpenWorkshop(); flow.StartRun(); yield return null;
                Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length, Is.EqualTo(5));
                Assert.That(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(5));
                Assert.That(Object.FindObjectsByType<DrillPresentation>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(presentation.Sounds.LoopCue, Is.EqualTo(-1)); Assert.That(presentation.Effects.ParticleCount, Is.Zero);
            }
        }
        [UnityTest] public IEnumerator LongStreamingKeepsPresentationBoundedAndRecordsDesktopProfilerCounters()
        {
            ownedCatalog = Object.Instantiate(flow.Run.Track.Catalog); ownedCatalog.Variants = new[] { ownedCatalog.Fallback }; ownedCatalog.ResourceInterval = 0;
            flow.Run.Track.Catalog = ownedCatalog; motor.Drilling.MovingEnergyPerSecond = 0; hud.Restart();
            foreach (var obstacle in motor.Obstacles) obstacle.gameObject.SetActive(false); Launch();
            using (var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
            using (var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
            using (var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread"))
            {
                var camera = Object.FindFirstObjectByType<RunnerCamera>().GetComponent<Camera>(); var target = new RenderTexture(540, 960, 24);
                var oldTarget = camera.targetTexture; camera.targetTexture = target; camera.aspect = 9f / 16;
                var canvas = hud.GetComponent<Canvas>(); var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldPlane = canvas.planeDistance;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 0.5f;
                try
                {
                for (var i = 0; i < 160; i++)
                {
                    motor.Simulate(10); flow.Run.Track.UpdateCoverage(motor.Motion.Position.z, null);
                    if (i % 8 == 0) yield return null;
                    Assert.That(flow.Run.Track.ActiveSections.Count, Is.LessThanOrEqualTo(10)); Assert.That(presentation.Effects.ParticleCount, Is.LessThanOrEqualTo(112));
                }
                yield return null; yield return null;
                File.WriteAllText("Logs/M6-Profiler.txt", "Editor batch counters; not device FPS.\n" +
                    "GC bytes/frame: " + (gc.Valid ? gc.LastValue.ToString() : "unavailable") + "\nDraw calls: " + (draws.Valid ? draws.LastValue.ToString() : "unavailable") +
                    "\nMain thread ns: " + (main.Valid ? main.LastValue.ToString() : "unavailable") + "\nSections: " + flow.Run.Track.ActiveSections.Count + "\nAudio sources: 5\nParticle capacity: 112\n");
                }
                finally { canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane; camera.targetTexture = oldTarget; camera.ResetAspect(); target.Release(); Object.Destroy(target); }
            }
            Assert.That(motor.Motion.Position.z, Is.GreaterThan(15000)); Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length, Is.EqualTo(5));
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
                yield return new WaitForSecondsRealtime(0.12f);
                yield return null; yield return null; RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/M6-" + state + ".png", image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane; camera.targetTexture = oldTarget;
                camera.ResetAspect(); RenderTexture.active = oldActive; texture.Release(); Object.Destroy(texture); Object.Destroy(image);
            }
        }
    }
}

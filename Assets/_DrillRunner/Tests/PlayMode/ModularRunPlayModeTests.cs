using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public class ModularRunPlayModeTests
    {
        private DrillMotor motor;
        private RunProgress run;
        private TrackStreamer track;
        private PrototypeHud hud;
        private readonly List<Object> owned = new List<Object>();
        private const float Step = 1f / 120;

        [UnitySetUp]
        public IEnumerator Open()
        {
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null;
            motor = Object.FindFirstObjectByType<DrillMotor>(); run = Object.FindFirstObjectByType<RunProgress>();
            track = run.Track; hud = Object.FindFirstObjectByType<PrototypeHud>();
            motor.GetComponent<DrillPointerInput>().enabled = false;
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            yield return SceneManager.LoadSceneAsync("SampleScene");
            foreach (var value in owned) if (value != null) Object.Destroy(value);
            owned.Clear();
        }

        private void Launch() { motor.Motion.Press(); motor.Motion.Step(1.5f); motor.Motion.Release(); }
        private void Until(Func<bool> condition, int ticks = 6000)
        {
            for (var i = 0; i < ticks && !condition(); i++) motor.Simulate(Step);
            Assert.That(condition(), Is.True, "Expected transition; state=" + motor.Motion.State);
        }
        private void CatalogOnly(string id)
        {
            var copy = Object.Instantiate(track.Catalog); owned.Add(copy);
            copy.Variants = new[] { copy.Variants.Single(s => s.SectionId == id) };
            copy.ResourceInterval = id == "Copper" ? 1 : 0;
            track.Catalog = copy;
        }
        private void DisableOpening() { foreach (var obstacle in motor.Obstacles) obstacle.gameObject.SetActive(false); }
        private void AssertConnections()
        {
            var sorted = track.ActiveSections.OrderBy(s => s.StartZ).ToArray();
            for (var i = 1; i < sorted.Length; i++) Assert.That(sorted[i].StartZ, Is.EqualTo(sorted[i - 1].EndZ).Within(0.001f));
            Assert.That(track.ActiveSections.Count, Is.LessThanOrEqualTo(10));
            Assert.That(motor.RegisteredObstacles.Distinct().Count(), Is.EqualTo(motor.RegisteredObstacles.Count));
        }

        [UnityTest]
        public IEnumerator InitialCoverageConnectsAndStreamingUnregistersRetiredObstacles()
        {
            Assert.That(track.ActiveSections.Min(s => s.StartZ), Is.LessThanOrEqualTo(-40));
            Assert.That(track.ActiveSections.Max(s => s.EndZ), Is.GreaterThanOrEqualTo(200));
            AssertConnections();
            var old = motor.Obstacles[0]; track.UpdateCoverage(240, null);
            AssertConnections(); Assert.That(motor.RegisteredObstacles.Contains(old), Is.False);
            Assert.That(track.ActiveSections.Max(s => s.EndZ), Is.GreaterThanOrEqualTo(440));
            Assert.That(motor.RegisteredObstacles.Count, Is.EqualTo(track.ActiveSections.Sum(s => s.Obstacles.Length)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FixedSeedRestartsReplayAndNewSeedsVaryTheCourseWithoutLeakingObjects()
        {
            run.UseFixedSeed = true; run.ConfiguredSeed = 1337; hud.Restart(); track.UpdateCoverage(400, null);
            var a = track.ActiveSections.OrderBy(s => s.StartZ).Select(s => s.SectionId).ToArray();
            hud.Restart(); track.UpdateCoverage(400, null);
            Assert.That(track.ActiveSections.OrderBy(s => s.StartZ).Select(s => s.SectionId), Is.EqualTo(a));
            run.ConfiguredSeed = 904; hud.Restart(); track.UpdateCoverage(400, null);
            Assert.That(track.ActiveSections.OrderBy(s => s.StartZ).Select(s => s.SectionId), Is.Not.EqualTo(a));
            run.UseFixedSeed = false; hud.Restart(); var seed = run.Rewards.Seed; hud.Restart();
            Assert.That(run.Rewards.Seed, Is.Not.EqualTo(seed));
            for (var i = 0; i < 8; i++)
            {
                hud.Restart(); yield return null;
                Assert.That(track.ActiveSections.Count, Is.EqualTo(6));
                Assert.That(Object.FindObjectsByType<TrackSection>(FindObjectsSortMode.None).Length, Is.EqualTo(6));
                Assert.That(motor.RegisteredObstacles.Count, Is.EqualTo(4));
            }
        }

        [UnityTest]
        public IEnumerator VehicleTraversesBeyondTheOldTwentyKilometerLimitWithBoundedSections()
        {
            CatalogOnly("Open"); motor.Drilling.MovingEnergyPerSecond = 0; hud.Restart(); DisableOpening(); Launch();
            for (var i = 0; i < 305; i++)
            {
                motor.Simulate(10);
                if (i % 10 == 0) { AssertConnections(); yield return null; }
            }
            Assert.That(motor.transform.position.z, Is.GreaterThan(30000));
            Assert.That(track.ActiveSections.Max(s => s.EndZ), Is.GreaterThan(motor.transform.position.z + 150));
            Assert.That(run.Rewards.Distance, Is.GreaterThan(30000)); Assert.That(motor.RegisteredObstacles.Count, Is.Zero);
            AssertConnections();
        }

        [UnityTest]
        public IEnumerator NormalDestructionRewardsOnceAndDistanceStaysStillDuringDrillingAndSidewaysMotion()
        {
            motor.enabled = false; Launch(); Until(() => motor.Motion.State == RunState.Drilling);
            var wood = motor.ContactTarget; var distance = run.Rewards.Distance;
            for (var i = 0; i < 60; i++) motor.Simulate(Step);
            Assert.That(run.Rewards.Distance, Is.EqualTo(distance)); Assert.That(run.Rewards.ObstacleCoins, Is.Zero);
            Until(() => wood.Durability.IsDestroyed);
            Assert.That(run.Rewards.ObstacleCoins, Is.EqualTo(1));
            wood.Restore(); wood.Durability.Drill(5, 10, 100);
            Assert.That(run.Rewards.ObstacleCoins, Is.EqualTo(1));
            Assert.That(run.Rewards.DistanceCoins, Is.GreaterThan(run.Rewards.ObstacleCoins));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpawnedCopperUsesM2DrillingAwardsOnlyCopperAndFreezesTheCompletedSummary()
        {
            CatalogOnly("Copper"); hud.Restart(); DisableOpening(); Launch();
            Until(() => motor.Motion.State == RunState.Drilling && motor.ContactTarget.Definition.RewardKind == ObstacleRewardKind.Copper);
            var copper = motor.ContactTarget;
            for (var i = 0; i < 60; i++) motor.Simulate(Step);
            Assert.That(run.Rewards.Copper, Is.Zero); Assert.That(run.Rewards.ObstacleCoins, Is.Zero);
            yield return Capture("CopperDrilling");
            Until(() => copper.Durability.IsDestroyed);
            Assert.That(run.Rewards.Copper, Is.EqualTo(3)); Assert.That(run.Rewards.ObstacleCoins, Is.Zero);
            var beforeEnd = track.GeneratedCount;
            motor.Energy.Consume(1000, 1); motor.Simulate(Step); yield return null;
            Assert.That(run.Rewards.IsCompleted, Is.True); Assert.That(hud.SummaryText.gameObject.activeSelf, Is.True);
            Assert.That(hud.SummaryText.text, Does.Contain("3 Copper"));
            var distance = run.Rewards.Distance; var coins = run.Rewards.Coins;
            copper.Restore(); copper.Durability.Drill(5, 10, 100); motor.Simulate(1);
            Assert.That(run.Rewards.Distance, Is.EqualTo(distance)); Assert.That(run.Rewards.Coins, Is.EqualTo(coins));
            Assert.That(run.Rewards.Copper, Is.EqualTo(3)); Assert.That(track.GeneratedCount, Is.EqualTo(beforeEnd));
            yield return Capture("Summary");
            hud.Restart();
            Assert.That(run.Rewards.Distance, Is.Zero); Assert.That(run.Rewards.Coins, Is.Zero); Assert.That(run.Rewards.Copper, Is.Zero);
            copper.Restore(); copper.Durability.Drill(5, 10, 100); // Deferred old object has no current-run registration.
            Assert.That(run.Rewards.Copper, Is.Zero); Assert.That(motor.RegisteredObstacles.Contains(copper), Is.False);
        }

        [UnityTest]
        public IEnumerator GeneratedStoneBlocksAndAllowsFullFootprintEscapeWithoutDistanceRewardsWhileStopped()
        {
            CatalogOnly("StoneBypass"); hud.Restart(); DisableOpening(); Launch();
            Until(() => motor.Motion.State == RunState.Blocked);
            var stone = motor.ContactTarget; var distance = run.Rewards.Distance;
            motor.Motion.Press(); motor.Motion.Drag(new Vector2(-10000, 0), 540);
            Until(() => motor.Motion.State == RunState.Moving, 240);
            Assert.That(stone.Durability.Remaining, Is.EqualTo(240));
            Assert.That(run.Rewards.Distance, Is.EqualTo(distance).Within(0.002));
            Assert.That(motor.transform.position.x + 0.85f, Is.LessThan(stone.WorldBounds.min.x));
            yield return Capture("StoneEscape");
        }

        [UnityTest]
        public IEnumerator CurrentDrillingSectionIsProtectedAndRestartClearsPartialDamageAndEveryRegistration()
        {
            CatalogOnly("Copper"); hud.Restart(); DisableOpening(); Launch();
            Until(() => motor.Motion.State == RunState.Drilling && motor.ContactTarget.Definition.MaterialId == "copper_ore");
            var target = motor.ContactTarget;
            var section = track.ActiveSections.Single(s => s.Contains(target));
            motor.Simulate(0.2f); Assert.That(target.Durability.Fraction, Is.LessThan(1));
            track.UpdateCoverage(section.EndZ + track.BehindDistance + 160, target);
            Assert.That(track.ActiveSections.Contains(section), Is.True); Assert.That(motor.RegisteredObstacles.Contains(target), Is.True);
            hud.Restart();
            Assert.That(motor.ContactTarget, Is.Null); Assert.That(motor.RegisteredObstacles.Contains(target), Is.False);
            Assert.That(run.Rewards.Distance, Is.Zero); Assert.That(run.Rewards.Copper, Is.Zero);
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready)); Assert.That(motor.Energy.Remaining, Is.EqualTo(100));
            yield return null; AssertConnections();
        }

        [UnityTest]
        public IEnumerator RestartFromMovingBlockedDrillingAndEndedDoesNotAccumulateSectionsOrRewards()
        {
            motor.enabled = false;
            Launch(); motor.Simulate(0.5f); Assert.That(run.Rewards.Distance, Is.GreaterThan(0)); hud.Restart();
            Launch(); Until(() => motor.Motion.State == RunState.Drilling); hud.Restart();
            foreach (var obstacle in motor.Obstacles) obstacle.gameObject.SetActive(obstacle == motor.Obstacles[2]);
            motor.Obstacles[2].transform.position = new Vector3(0, 1, 5);
            Launch(); Until(() => motor.Motion.State == RunState.Blocked); hud.Restart();
            motor.Drilling.EnergyCapacity = 0.7f; hud.Restart(); Launch(); Until(() => motor.Motion.State == RunState.RunEnded); hud.Restart();
            for (var i = 0; i < 5; i++)
            {
                hud.Restart(); yield return null;
                Assert.That(run.Rewards.Distance, Is.Zero); Assert.That(run.Rewards.Coins, Is.Zero); Assert.That(run.Rewards.Copper, Is.Zero);
                Assert.That(run.Rewards.IsCompleted, Is.False); Assert.That(hud.SummaryText.gameObject.activeSelf, Is.False);
                Assert.That(track.ActiveSections.Count, Is.EqualTo(6)); Assert.That(motor.RegisteredObstacles.Count, Is.EqualTo(4));
                Assert.That(motor.transform.position, Is.EqualTo(motor.StartPosition));
            }
        }

        [UnityTest]
        public IEnumerator InvalidStreamingConfigurationFailsClosedAndBadCoverageInputIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => track.UpdateCoverage(float.NaN, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => track.UpdateCoverage(float.MaxValue, null));
            var invalid = Object.Instantiate(track.Catalog); owned.Add(invalid); invalid.Width = float.NaN; track.Catalog = invalid;
            LogAssert.Expect(LogType.Error, new Regex("Cannot initialize modular run")); hud.Restart();
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.RunEnded)); Assert.That(run.Rewards.IsCompleted, Is.True);
            Assert.That(run.Rewards.Coins, Is.Zero); yield return null;
        }

        private IEnumerator Capture(string state)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            var follower = Object.FindFirstObjectByType<RunnerCamera>(); follower.Snap();
            var camera = follower.GetComponent<Camera>(); var canvas = hud.GetComponent<Canvas>();
            var texture = new RenderTexture(540, 960, 24); var image = new Texture2D(540, 960, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldPlane = canvas.planeDistance;
            var oldActive = RenderTexture.active;
            try
            {
                camera.targetTexture = texture; camera.aspect = 9f / 16;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 0.5f;
                yield return null; yield return null;
                RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/M3-" + state + ".png", image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane;
                camera.targetTexture = oldTarget; camera.ResetAspect(); RenderTexture.active = oldActive;
                texture.Release(); Object.Destroy(texture); Object.Destroy(image);
            }
        }
    }
}

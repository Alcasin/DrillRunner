using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public class DrillingPlayModeTests
    {
        private DrillMotor motor;
        private PrototypeHud hud;
        private RunnerCamera follower;
        private DrillObstacle wood, brick, stone;
        private TrackCatalog legacyCatalog;
        private const float Step = 1f / 120;

        [UnitySetUp]
        public IEnumerator OpenScene()
        {
            yield return HistoricalRunFixture.LoadScene();
            motor = Object.FindFirstObjectByType<DrillMotor>();
            hud = Object.FindFirstObjectByType<PrototypeHud>();
            follower = Object.FindFirstObjectByType<RunnerCamera>();
            yield return null;
            motor.enabled = false; // Tests drive the authoritative motor clock explicitly.
            motor.GetComponent<DrillPointerInput>().enabled = false;
            wood = motor.Obstacles[0]; brick = motor.Obstacles[1]; stone = motor.Obstacles[2];
            legacyCatalog = LegacyTemplateFixture.Restore(Object.FindFirstObjectByType<TrackStreamer>(), hud);
        }

        [UnityTearDown] public IEnumerator CloseScene() { yield return HistoricalRunFixture.UnloadScene(); if (legacyCatalog != null) Object.Destroy(legacyCatalog); }

        private void Advance(float seconds)
        {
            var ticks = Mathf.RoundToInt(seconds * 120);
            for (var i = 0; i < ticks; i++) motor.Simulate(Step);
        }

        private void Until(Func<bool> condition, float limit = 20)
        {
            for (var i = 0; i < limit * 120 && !condition(); i++) motor.Simulate(Step);
            Assert.That(condition(), Is.True, "Expected transition before timeout; state=" + motor.Motion.State);
        }

        private void Launch()
        {
            motor.Motion.Press(); motor.Motion.Step(1.5f); motor.Motion.Release();
        }

        private void Isolate(DrillObstacle target, float z = 5)
        {
            foreach (var obstacle in motor.Obstacles) obstacle.gameObject.SetActive(obstacle == target);
            target.transform.position = new Vector3(0, 1, z);
        }

        [UnityTest]
        public IEnumerator FrontalWoodDrillsProgressivelyLocksSteeringAndResumesOnceWithNoStackedEnergy()
        {
            var breaks = 0; wood.Broken += _ => breaks++;
            Launch(); Until(() => motor.Motion.State == RunState.Drilling);
            Assert.That(motor.ContactTarget, Is.SameAs(wood));
            var contact = motor.transform.position; var energy = motor.Energy.Remaining;
            motor.Motion.Press(); motor.Motion.Drag(new Vector2(100, 0), 540);
            Advance(0.5f);
            Assert.That(motor.transform.position, Is.EqualTo(contact));
            Assert.That(motor.Motion.IsDragging, Is.False);
            Assert.That(wood.Durability.Remaining, Is.EqualTo(45).Within(0.001));
            Assert.That(energy - motor.Energy.Remaining, Is.EqualTo(4).Within(0.001));
            yield return Capture("Drilling");
            Until(() => wood.Durability.IsDestroyed);
            Assert.That(breaks, Is.EqualTo(1)); Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            Assert.That(motor.Motion.IsDragging, Is.True);
            Assert.That(wood.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(wood.GetComponent<BoxCollider>().enabled, Is.False);
            Advance(1);
            Assert.That(motor.transform.position.z, Is.GreaterThan(contact.z));
            Assert.That(motor.Motion.ForwardSpeed, Is.InRange(9, 10.001f));
            Assert.That(breaks, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator MaximumLaunchCannotTunnelThroughThinStoneAndBlockedSteeringEscapesWithoutSideClipping()
        {
            Isolate(stone); stone.transform.localScale = new Vector3(1.8f, 2, 0.01f);
            Launch(); Until(() => motor.Motion.State == RunState.Blocked, 2);
            var contactZ = motor.transform.position.z;
            Assert.That(DrillContacts.WorldBounds(motor.GetComponent<BoxCollider>()).max.z,
                Is.LessThanOrEqualTo(stone.WorldBounds.min.z - DrillContacts.Skin + 1e-4));
            var energy = motor.Energy.Remaining;
            Advance(2);
            Assert.That(motor.transform.position.z, Is.EqualTo(contactZ));
            Assert.That(stone.Durability.Remaining, Is.EqualTo(stone.Durability.Maximum));
            Assert.That(energy - motor.Energy.Remaining, Is.EqualTo(1).Within(0.001));
            yield return Capture("Blocked");
            motor.Motion.Press(); motor.Motion.Drag(new Vector2(-10000, 0), 540);
            Until(() => motor.Motion.State == RunState.Moving, 2);
            Assert.That(motor.transform.position.x, Is.LessThan(-1.7f));
            Advance(0.2f);
            motor.Motion.Drag(new Vector2(10000, 0), 540); Advance(0.1f);
            var body = DrillContacts.WorldBounds(motor.GetComponent<BoxCollider>());
            if (body.min.z < stone.WorldBounds.max.z)
                Assert.That(body.max.x, Is.LessThanOrEqualTo(stone.WorldBounds.min.x - DrillContacts.Skin + 1e-4));
            Advance(2);
            Assert.That(motor.transform.position.z, Is.GreaterThan(stone.WorldBounds.max.z));
            Assert.That(stone.Durability.IsDestroyed, Is.False);
        }

        [UnityTest]
        public IEnumerator SideGrazingDrillableMaterialDoesNotDamageOrLockAndCanBeEscaped()
        {
            Isolate(wood, 12); Launch(); motor.Motion.Press(); motor.Motion.Drag(new Vector2(70, 0), 540);
            Until(() => motor.Motion.State == RunState.Blocked, 3);
            Assert.That(motor.ContactIsTooHard, Is.False);
            Assert.That(motor.Motion.IsDragging, Is.True);
            Assert.That(wood.Durability.Remaining, Is.EqualTo(70));
            motor.Motion.Drag(new Vector2(1000, 0), 540);
            Until(() => motor.Motion.State == RunState.Moving, 2);
            Assert.That(wood.Durability.Remaining, Is.EqualTo(70));
            yield return null;
        }

        [UnityTest]
        public IEnumerator HigherQaPowerCanDestroyStoneAndResetRestoresIt()
        {
            Isolate(stone); motor.Drilling.DrillPower = 10; motor.ResetRun();
            var breaks = 0; stone.Broken += _ => breaks++;
            Launch(); Until(() => motor.Motion.State == RunState.Drilling, 2);
            var energy = motor.Energy.Remaining;
            Until(() => stone.Durability.IsDestroyed, 4);
            Assert.That(breaks, Is.EqualTo(1));
            Assert.That(energy - motor.Energy.Remaining, Is.EqualTo(43.2f).Within(0.05));
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            motor.ResetRun();
            Assert.That(stone.Durability.Remaining, Is.EqualTo(240));
            Assert.That(stone.IsSolid, Is.True); Assert.That(stone.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(motor.ContactTarget, Is.Null); Assert.That(motor.Energy.Remaining, Is.EqualTo(100));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReadyAndChargingUseNoEnergyAndMovingUsesOnlyItsOperatingRate()
        {
            foreach (var obstacle in motor.Obstacles) obstacle.gameObject.SetActive(false);
            Advance(2); Assert.That(motor.Energy.Remaining, Is.EqualTo(100));
            motor.Motion.Press(); Advance(2); Assert.That(motor.Energy.Remaining, Is.EqualTo(100));
            motor.Motion.Release(); Advance(2);
            Assert.That(motor.Energy.Remaining, Is.EqualTo(98.6f).Within(0.001));
            Assert.Throws<ArgumentOutOfRangeException>(() => motor.Simulate(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => motor.Simulate(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => motor.Simulate(float.MaxValue));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MovingDepletionEndsExactlyOnceAndRestartRestoresEveryRunState()
        {
            motor.Drilling.EnergyCapacity = 0.7f; motor.ResetRun();
            var endings = 0; motor.RunEnded += () => endings++;
            Launch(); Advance(2);
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.RunEnded));
            Assert.That(motor.Energy.Remaining, Is.Zero); Assert.That(endings, Is.EqualTo(1));
            var ended = motor.transform.position;
            motor.Motion.Press(); motor.Motion.Release(); Advance(2);
            Assert.That(motor.transform.position, Is.EqualTo(ended)); Assert.That(endings, Is.EqualTo(1));
            yield return Capture("RunEnded");
            for (var i = 0; i < 3; i++) hud.Restart();
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
            Assert.That(motor.transform.position, Is.EqualTo(motor.StartPosition));
            Assert.That(motor.Energy.Remaining, Is.EqualTo(0.7f));
            Assert.That(motor.ContactTarget, Is.Null);
            Assert.That(Object.FindObjectsByType<DrillObstacle>(FindObjectsSortMode.None).Length, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator DepletionDuringDrillingPreservesPartialDurabilityAndDoesNotEmitBreak()
        {
            Isolate(wood); motor.Drilling.EnergyCapacity = 3; motor.ResetRun();
            var breaks = 0; var endings = 0; wood.Broken += _ => breaks++; motor.RunEnded += () => endings++;
            Launch(); Until(() => motor.Motion.State == RunState.RunEnded, 2);
            Assert.That(wood.Durability.Remaining, Is.InRange(0.01f, 69.9f));
            Assert.That(wood.IsSolid, Is.True); Assert.That(breaks, Is.Zero); Assert.That(endings, Is.EqualTo(1));
            var remaining = wood.Durability.Remaining; var position = motor.transform.position;
            Advance(1); Assert.That(wood.Durability.Remaining, Is.EqualTo(remaining));
            Assert.That(motor.transform.position, Is.EqualTo(position));
            hud.Restart();
            Assert.That(wood.Durability.Remaining, Is.EqualTo(70));
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready)); Assert.That(motor.Energy.Remaining, Is.EqualTo(3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BlockedDepletionEndsWithoutDamageAndRestartsWithoutALatchedSteeringLock()
        {
            Isolate(stone); motor.Drilling.EnergyCapacity = 1; motor.ResetRun();
            Launch(); Until(() => motor.Motion.State == RunState.Blocked, 1);
            hud.Restart(); Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
            Launch(); Until(() => motor.Motion.State == RunState.RunEnded, 4);
            Assert.That(stone.Durability.Remaining, Is.EqualTo(240));
            hud.Restart(); motor.Motion.Press(); motor.Motion.Release(); motor.Motion.Press();
            Assert.That(motor.Motion.IsDragging, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NearbyObstaclesKeepOneTargetAndRestartDuringDrillingRestoresBoth()
        {
            foreach (var obstacle in motor.Obstacles) obstacle.gameObject.SetActive(obstacle == wood || obstacle == brick);
            wood.transform.position = brick.transform.position = new Vector3(0, 1, 5);
            wood.transform.localScale = brick.transform.localScale = new Vector3(1.6f, 2, 0.5f);
            Launch(); Until(() => motor.Motion.State == RunState.Drilling, 2);
            Assert.That(motor.ContactTarget, Is.SameAs(wood)); Advance(0.5f);
            Assert.That(motor.ContactTarget, Is.SameAs(wood)); Assert.That(brick.Durability.Remaining, Is.EqualTo(140));
            hud.Restart();
            Assert.That(motor.ContactTarget, Is.Null); Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
            Assert.That(wood.Durability.Remaining, Is.EqualTo(70)); Assert.That(brick.Durability.Remaining, Is.EqualTo(140));
            Launch(); Until(() => wood.Durability.IsDestroyed, 4);
            Until(() => motor.ContactTarget == brick, 1);
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Drilling));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ContactDamageAndEnergyMatchAcrossRenderFramePartitions()
        {
            Isolate(wood); Launch();
            for (var i = 0; i < 30; i++) motor.Simulate(1f / 30);
            var position = motor.transform.position; var energy = motor.Energy.Remaining; var remaining = wood.Durability.Remaining;
            motor.ResetRun(); Launch();
            for (var i = 0; i < 120; i++) motor.Simulate(1f / 120);
            Assert.That(motor.transform.position.z, Is.EqualTo(position.z).Within(1e-4));
            Assert.That(motor.Energy.Remaining, Is.EqualTo(energy).Within(1e-4));
            Assert.That(wood.Durability.Remaining, Is.EqualTo(remaining).Within(1e-4));
            yield return null;
        }

        [UnityTest]
        public IEnumerator MovingOrDisablingTheActiveTargetReleasesDrillingWithoutRemoteDamage()
        {
            Isolate(wood); Launch(); motor.Motion.Press();
            Until(() => motor.Motion.State == RunState.Drilling, 2);
            var remaining = wood.Durability.Remaining;
            wood.transform.position += Vector3.forward * 10;
            Advance(0.1f);
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            Assert.That(motor.ContactTarget, Is.Null); Assert.That(motor.Motion.IsDragging, Is.True);
            Assert.That(wood.Durability.Remaining, Is.EqualTo(remaining));
            motor.ResetRun(); Isolate(wood); Launch();
            Until(() => motor.Motion.State == RunState.Drilling, 2);
            wood.enabled = false; Advance(0.1f);
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            Assert.That(motor.ContactTarget, Is.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissingMaterialFailsClosedAsASolidEscapableObstacle()
        {
            Isolate(wood);
            typeof(DrillObstacle).GetField("definition", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(wood, null);
            LogAssert.Expect(LogType.Warning, new Regex("Drill obstacle cannot be drilled"));
            wood.Restore();
            Launch(); Until(() => motor.Motion.State == RunState.Blocked, 2);
            Assert.That(wood.IsSolid, Is.True); Assert.That(wood.Durability, Is.Null);
            var position = motor.transform.position;
            Advance(0.5f); Assert.That(motor.transform.position, Is.EqualTo(position));
            Assert.That(motor.Energy.Remaining, Is.InRange(0.01f, 100));
            motor.Motion.Press(); motor.Motion.Drag(new Vector2(10000, 0), 540);
            Until(() => motor.Motion.State == RunState.Moving, 2);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InvalidRunConfigurationIsRejectedBeforeResetAndStartupStopsSafely()
        {
            Launch(); Advance(0.1f);
            var position = motor.transform.position; var energy = motor.Energy.Remaining;
            motor.Drilling.DrillPower = float.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(() => motor.ResetRun());
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            Assert.That(motor.transform.position, Is.EqualTo(position)); Assert.That(motor.Energy.Remaining, Is.EqualTo(energy));
            motor.Drilling.DrillPower = 5; motor.ResetRun();
            var invalidRoot = new GameObject("Invalid M2 Startup Test");
            invalidRoot.SetActive(false); invalidRoot.AddComponent<BoxCollider>();
            var invalid = invalidRoot.AddComponent<DrillMotor>();
            invalid.Drilling.EnergyCapacity = float.NaN;
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("Cannot initialize DrillMotor"));
                invalidRoot.SetActive(true); yield return null;
                Assert.That(invalid.enabled, Is.False); Assert.That(invalid.Motion.State, Is.EqualTo(RunState.RunEnded));
                Assert.That(invalidRoot.transform.position, Is.EqualTo(Vector3.zero));
                invalid.Drilling.EnergyCapacity = 100; invalid.ResetRun();
                Assert.That(invalid.enabled, Is.True); Assert.That(invalid.Motion.State, Is.EqualTo(RunState.Ready));
                Assert.That(invalid.Energy.Remaining, Is.EqualTo(100));
            }
            finally { Object.Destroy(invalidRoot); }
        }

        private IEnumerator Capture(string state)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            follower.Snap();
            var camera = follower.GetComponent<Camera>(); var canvas = hud.GetComponent<Canvas>();
            var texture = new RenderTexture(540, 960, 24); var image = new Texture2D(540, 960, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
            var oldPlane = canvas.planeDistance; var oldActive = RenderTexture.active;
            try
            {
                camera.targetTexture = texture; camera.aspect = 9f / 16;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 0.5f;
                yield return null; yield return null;
                RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/M2-" + state + ".png", image.EncodeToPNG());
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

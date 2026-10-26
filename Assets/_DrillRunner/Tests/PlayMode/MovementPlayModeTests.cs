using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DrillRunner.Tests.PlayMode
{
    public class MovementPlayModeTests : InputTestFixture
    {
        private DrillMotor motor;
        private DrillPointerInput input;
        private RunnerCamera follower;
        private PrototypeHud hud;
        private Mouse mouse;

        // In Test Framework 1.6, UnitySetUp wraps NUnit SetUp. Initialize the isolated
        // input system before loading subscribers, and restore it after unloading them.
        public override void Setup() { }
        public override void TearDown() { }

        [UnitySetUp]
        public IEnumerator OpenPrototype()
        {
            base.Setup();
            yield return SceneManager.LoadSceneAsync("Gameplay");
            motor = Object.FindFirstObjectByType<DrillMotor>();
            input = motor.GetComponent<DrillPointerInput>();
            follower = Object.FindFirstObjectByType<RunnerCamera>();
            hud = Object.FindFirstObjectByType<PrototypeHud>();
            mouse = InputSystem.AddDevice<Mouse>();
            input.SendMessage("OnApplicationFocus", true);
            yield return null; // All actions enabled, scene started, neutral pointer accepted.
            Set(mouse.position, new Vector2(Screen.width * 0.5f, Screen.height * 0.4f));
        }

        [UnityTearDown]
        public IEnumerator ClosePrototype()
        {
            // Destroy subscribers before InputTestFixture restores the real input system.
            yield return SceneManager.LoadSceneAsync("SampleScene");
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator MouseLaunchesOnceSteersOnlyWhenDraggingAndRestartsWithoutDrift()
        {
            var start = motor.transform.position;
            Set(mouse.delta, new Vector2(200, 0)); yield return null;
            Assert.That(motor.transform.position, Is.EqualTo(start));
            Press(mouse.leftButton);
            yield return new WaitForSeconds(1.65f);
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Charging));
            Assert.That(motor.Motion.ChargeNormalized, Is.EqualTo(1));
            Assert.That(hud.ChargeFill.fillAmount, Is.EqualTo(1));
            Assert.That(hud.StateText.text, Is.EqualTo("CHARGING"));
            Release(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            yield return new WaitForSeconds(0.1f);
            Assert.That(motor.transform.position.z, Is.GreaterThan(start.z));
            Set(mouse.delta, new Vector2(200, 0)); yield return null;
            Assert.That(motor.transform.position.x, Is.EqualTo(start.x));
            Press(mouse.leftButton); yield return null;
            Assert.That(motor.transform.position.x, Is.EqualTo(start.x));
            Set(mouse.delta, new Vector2(Screen.width * 0.2f, 500));
            yield return new WaitForSeconds(0.1f);
            Assert.That(motor.transform.position.x, Is.GreaterThan(0));
            Release(mouse.leftButton); yield return null;
            var x = motor.transform.position.x;
            yield return new WaitForSeconds(0.1f);
            Assert.That(motor.transform.position.x, Is.EqualTo(x));
            hud.Restart(); yield return null;
            Assert.That(motor.transform.position, Is.EqualTo(start));
            Assert.That(motor.Motion.ForwardSpeed, Is.Zero);
            Assert.That(motor.Motion.ChargeNormalized, Is.Zero);
            Assert.That(hud.StateText.text, Is.EqualTo("READY"));
            Assert.That(Vector3.Distance(follower.transform.position, start + follower.FollowOffset), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator TouchCancellationNeverLaunchesAndPrimaryFingerDoesNotTransferToASecondFinger()
        {
            var screen = InputSystem.AddDevice<Touchscreen>();
            var point = new Vector2(Screen.width / 2f, Screen.height / 3f);
            // Touch helpers otherwise update immediately outside the player loop,
            // causing the next dynamic update to clear delta before MonoBehaviour.Update.
            BeginTouch(1, point, queueEventOnly: true, screen: screen); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Charging));
            MoveTouch(1, point + Vector2.right * 100, new Vector2(100, 0), queueEventOnly: true, screen: screen); yield return null;
            Assert.That(motor.transform.position.x, Is.Zero);
            CancelTouch(1, point, queueEventOnly: true, screen: screen); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
            Assert.That(motor.Motion.ForwardSpeed, Is.Zero);
            yield return null;
            BeginTouch(2, point, queueEventOnly: true, screen: screen); yield return null;
            BeginTouch(3, point + Vector2.right * 50, queueEventOnly: true, screen: screen); yield return null;
            EndTouch(2, point, queueEventOnly: true, screen: screen); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            MoveTouch(3, point + Vector2.right * 200, new Vector2(150, 0), queueEventOnly: true, screen: screen); yield return null;
            Assert.That(motor.transform.position.x, Is.Zero);
            EndTouch(3, point, queueEventOnly: true, screen: screen); yield return null;
            BeginTouch(4, point, queueEventOnly: true, screen: screen); yield return null;
            yield return null;
            Assert.That(motor.Motion.IsDragging, Is.True, "A fresh primary finger must start a drag after all old fingers lift.");
            MoveTouch(4, point - Vector2.right * 60, new Vector2(-60, 0), queueEventOnly: true, screen: screen);
            yield return new WaitForSeconds(0.05f);
            Assert.That(motor.transform.position.x, Is.LessThan(0));
            EndTouch(4, point, queueEventOnly: true, screen: screen); yield return null;
            var x = motor.transform.position.x;
            yield return null;
            Assert.That(motor.transform.position.x, Is.EqualTo(x));
        }

        [UnityTest]
        public IEnumerator RestartDuringAHeldChargeOrDragRequiresAFreshPress()
        {
            Press(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Charging));
            hud.Restart(); yield return null;
            Set(mouse.delta, new Vector2(100, 0)); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
            Release(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
            Press(mouse.leftButton); yield return null;
            Release(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            Press(mouse.leftButton); yield return null;
            Set(mouse.delta, new Vector2(80, 0)); yield return null;
            hud.Restart(); yield return null;
            Release(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
            Assert.That(motor.transform.position, Is.EqualTo(motor.StartPosition));
            Assert.That(motor.Motion.IsDragging, Is.False);
        }

        [UnityTest]
        public IEnumerator OverlappingMouseAndTouchDoNotStealTheOwnedPointerOrHoldLaunchOpen()
        {
            var screen = InputSystem.AddDevice<Touchscreen>();
            var point = new Vector2(Screen.width / 2f, Screen.height / 3f);
            Press(mouse.leftButton); yield return null;
            BeginTouch(1, point, queueEventOnly: true, screen: screen); yield return null;
            Release(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            MoveTouch(1, point + Vector2.right * 100, new Vector2(100, 0), queueEventOnly: true, screen: screen);
            yield return null;
            Assert.That(motor.transform.position.x, Is.Zero);
            EndTouch(1, point, queueEventOnly: true, screen: screen); yield return null;
            Assert.That(motor.Motion.IsDragging, Is.False);
            Press(mouse.leftButton); yield return null;
            Set(mouse.delta, new Vector2(80, 0)); yield return null;
            yield return null;
            Assert.That(motor.transform.position.x, Is.GreaterThan(0));
            Release(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.ForwardSpeed, Is.InRange(10, 24));
        }

        [UnityTest]
        public IEnumerator FocusLossDeviceResetAndEnableCyclesCancelWithoutGhostLaunches()
        {
            Press(mouse.leftButton); yield return null;
            InputSystem.ResetDevice(mouse); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
            yield return null;
            Press(mouse.leftButton); yield return null;
            input.SendMessage("OnApplicationFocus", false);
            Release(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
            input.SendMessage("OnApplicationFocus", true); yield return null;
            for (var i = 0; i < 3; i++)
            {
                Press(mouse.leftButton); yield return null;
                input.enabled = false; yield return null;
                Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
                Release(mouse.leftButton);
                input.enabled = true; yield return null;
            }
            Press(mouse.leftButton); yield return null;
            Release(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            Assert.That(motor.Motion.ForwardSpeed, Is.InRange(10, 24));
            hud.Restart(); yield return null;
            Assert.That(Object.FindObjectsByType<DrillMotor>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ActualRestartButtonIgnoresGameplayPressesAndLeavesTheRunReady()
        {
            Press(mouse.leftButton); yield return null;
            Release(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Moving));
            var screenPoint = RectTransformUtility.WorldToScreenPoint(null, hud.RestartButton.transform.position);
            Set(mouse.position, screenPoint); yield return null;
            Press(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.IsDragging, Is.False);
            Release(mouse.leftButton); yield return null;
            yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
            Assert.That(motor.transform.position, Is.EqualTo(motor.StartPosition));
            Assert.That(motor.Motion.ChargeNormalized, Is.Zero);
            Assert.That(motor.Motion.Release(), Is.False);
            // Repeating a UI click cannot charge or launch the drill.
            Press(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
            Release(mouse.leftButton); yield return null;
            Assert.That(motor.Motion.State, Is.EqualTo(RunState.Ready));
        }

        [UnityTest]
        public IEnumerator PortraitCameraRendersAllRunStatesAndKeepsTheDrillReadableAtBothBounds()
        {
            var camera = follower.GetComponent<Camera>();
            var rotation = camera.transform.rotation;
            yield return Capture(camera, "Ready");
            Press(mouse.leftButton);
            yield return new WaitForSeconds(0.75f);
            yield return Capture(camera, "Charging");
            Release(mouse.leftButton); yield return null;
            Press(mouse.leftButton); yield return null;
            foreach (var direction in new[] { -1, 1 })
            {
                Set(mouse.delta, new Vector2(direction * Screen.width * 2, 0));
                yield return new WaitForSeconds(0.45f);
                Assert.That(Mathf.Abs(motor.transform.position.x) + motor.Settings.FootprintHalfWidth, Is.LessThanOrEqualTo(3.0001f));
                Assert.That(Quaternion.Angle(rotation, camera.transform.rotation), Is.LessThan(0.01f));
                var oldAspect = camera.aspect;
                camera.aspect = 9f / 16;
                var screen = camera.WorldToViewportPoint(motor.transform.position + Vector3.up * 0.65f);
                camera.aspect = oldAspect;
                Assert.That(screen.x, Is.InRange(0.08f, 0.92f));
                Assert.That(screen.y, Is.InRange(0.15f, 0.5f));
            }
            Release(mouse.leftButton); yield return null;
            yield return Capture(camera, "Moving");
            hud.Restart(); yield return null;
            Assert.That(Vector3.Distance(camera.transform.position, motor.StartPosition + follower.FollowOffset), Is.LessThan(0.001f));
        }

        private IEnumerator Capture(Camera camera, string state)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                yield break;
            var canvas = hud.GetComponent<Canvas>();
            var texture = new RenderTexture(540, 960, 24);
            var image = new Texture2D(540, 960, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture;
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            var oldPlane = canvas.planeDistance;
            var oldActive = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                camera.aspect = 9f / 16;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 0.5f;
                yield return null;
                // End-of-frame coroutines are not dispatched in Editor batch mode.
                // Two frame yields let URP finish rendering the assigned target.
                yield return null;
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0);
                image.Apply();
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes("Logs/M1-" + state + ".png", image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane;
                camera.targetTexture = oldTarget;
                camera.ResetAspect();
                RenderTexture.active = oldActive;
                texture.Release();
                Object.Destroy(texture); Object.Destroy(image);
            }
        }
    }
}

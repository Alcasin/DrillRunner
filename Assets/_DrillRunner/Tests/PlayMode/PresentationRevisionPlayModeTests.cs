using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.PlayMode
{
    public sealed class PresentationRevisionPlayModeTests
    {
        private WorkshopController flow;
        private DrillMotor motor;
        private DrillPresentation presentation;
        private PrototypeHud hud;
        [UnitySetUp] public IEnumerator Open()
        {
            WorkshopController.StoreFactory = () => new MemoryProgressStore(new PlayerProgress { Coins = 10000, Copper = 24 });
            yield return SceneManager.LoadSceneAsync("Gameplay"); yield return null;
            flow = Object.FindFirstObjectByType<WorkshopController>(); motor = flow.Run.Motor; motor.enabled = false;
            motor.GetComponent<DrillPointerInput>().enabled = false; presentation = Object.FindFirstObjectByType<DrillPresentation>();
            hud = Object.FindFirstObjectByType<PrototypeHud>(); presentation.SendMessage("OnApplicationFocus", true);
        }
        [UnityTearDown] public IEnumerator Close()
        { yield return SceneManager.LoadSceneAsync("SampleScene"); WorkshopController.StoreFactory = () => new MemoryProgressStore(); }
        private void Launch() { motor.Motion.Press(); motor.Simulate(1.5f); motor.Motion.Release(); motor.Simulate(0.01f); }
        private void Until(Func<bool> condition)
        { for (var i = 0; i < 6000 && !condition(); i++) motor.Simulate(1f / 120); Assert.That(condition(), Is.True); }
        private DrillObstacle Target(string id)
        {
            flow.Run.Track.UpdateCoverage(1, null); var target = motor.RegisteredObstacles.First(o => o.Definition.MaterialId == id);
            foreach (var obstacle in motor.RegisteredObstacles) obstacle.gameObject.SetActive(obstacle == target);
            target.gameObject.SetActive(true); target.transform.position = new Vector3(0, 1, 6); return target;
        }
        private IEnumerator Evolve()
        {
            flow.OpenWorkshop(); foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory))) for (var i = 0; i < 5; i++) Assert.That(flow.Purchase(category), Is.True);
            Assert.That(flow.Evolve(), Is.True); yield return new WaitForSecondsRealtime(0.25f); yield return Capture("Evolution");
            yield return new WaitForSecondsRealtime(0.9f); yield return Capture("WorkshopPreview"); Assert.That(flow.StartRun(), Is.True);
        }
        [UnityTest] public IEnumerator PortraitProjectionExposesTheCuttingSurfaceAndKeepsBothEscapeEdgesVisible()
        {
            var follower = Object.FindFirstObjectByType<RunnerCamera>(); var camera = follower.GetComponent<Camera>();
            for (var form = 0; form < 2; form++)
            {
                if (form == 1) yield return Evolve();
                Launch(); motor.Simulate(0.1f); follower.Snap(); var oldAspect = camera.aspect; camera.aspect = 9f / 16;
                try
                {
                    var origin = motor.transform.position; var tip = camera.WorldToViewportPoint(origin + new Vector3(0, 0.94f, 1.83f));
                    var collarTop = camera.WorldToViewportPoint(origin + new Vector3(0, 0.94f + 0.53f * (form == 0 ? 0.84f : 1.04f), 0.02f));
                    Assert.That((tip.y - collarTop.y) * 960, Is.GreaterThan(18), "The cone projects past the rear collar rather than hiding behind a disk.");
                    foreach (var x in new[] { -3f, 3f })
                    { var edge = camera.WorldToViewportPoint(new Vector3(x, 0, origin.z + 4)); Assert.That(edge.x, Is.InRange(0.01f, 0.99f)); }
                }
                finally { camera.aspect = oldAspect; }
                yield return Capture(form == 0 ? "PocketSilhouette" : "WorkshopSilhouette"); hud.Restart();
            }
        }
        [UnityTest] public IEnumerator BothFormsExposeAnimatedBitsInReadyChargeMovementAndContactWithoutMovingTheFootprint()
        {
            var collider = motor.GetComponent<BoxCollider>(); var center = collider.center; var size = collider.size;
            for (var form = 0; form < 2; form++)
            {
                if (form == 1) yield return Evolve();
                var name = form == 0 ? "Pocket" : "Workshop"; yield return Capture(name + "Ready");
                var rotor = flow.Visuals.ActiveVisual.transform.Find("Drill Rotor"); var before = rotor.localRotation;
                motor.Motion.Press(); motor.Simulate(0.75f); yield return Capture(name + "Charging");
                Assert.That(Quaternion.Angle(before, rotor.localRotation), Is.GreaterThan(0.1f));
                Assert.That(Vector3.Dot(rotor.forward, motor.transform.forward), Is.GreaterThan(0.999f));
                motor.Motion.Release(); motor.Simulate(0.15f); yield return Capture(name + "Moving");
                Assert.That(collider.center, Is.EqualTo(center)); Assert.That(collider.size, Is.EqualTo(size));
                Assert.That(flow.Visuals.ActiveVisual.activeInHierarchy, Is.True);
                Assert.That(motor.transform.Cast<Transform>().Count(t => t.gameObject.activeSelf && (t.name == "Visuals" || t.name == "Workshop Drill Visual")), Is.EqualTo(1));
                hud.Restart(); var wood = Target("wood"); Launch(); Until(() => motor.Motion.State == RunState.Drilling); motor.Simulate(0.15f);
                Assert.That(wood.Durability.Fraction, Is.LessThan(1)); yield return Capture(name + "Drilling"); hud.Restart();
            }
        }
        [UnityTest] public IEnumerator FourRealMaterialBreaksProduceBoundedFragmentsWithImmediateCollisionReleaseAndAnimation()
        {
            yield return Evolve();
            foreach (var id in new[] { "wood", "brick", "stone", "copper_ore" })
            {
                hud.Restart(); var target = Target(id); var cues = presentation.DestructionCues; Launch(); Until(() => motor.Motion.State == RunState.Drilling);
                motor.Simulate(0.15f); Assert.That(presentation.Effects.IsDrilling, Is.True); yield return Capture(id + "Contact");
                Until(() => target.Durability.IsDestroyed); Assert.That(target.IsSolid, Is.False); Assert.That(target.GetComponent<BoxCollider>().enabled, Is.False);
                Assert.That(presentation.DestructionCues, Is.EqualTo(cues + 1)); Assert.That(presentation.Effects.IsDrilling, Is.False);
                yield return Capture(id + "BreakEarly", 0.04f); var early = presentation.Effects.ParticleCount;
                Assert.That(early, Is.GreaterThan(10)); Assert.That(early, Is.LessThanOrEqualTo(112));
                yield return Capture(id + "BreakLate", 0.24f); Assert.That(presentation.DestructionCues, Is.EqualTo(cues + 1));
                yield return new WaitForSecondsRealtime(1); Assert.That(presentation.Effects.ParticleCount, Is.Zero);
            }
        }
        [UnityTest] public IEnumerator CopperResponseRequiresConfirmedLedgerChangeOccursOnceAndClearsOnEndAndRetry()
        {
            var target = Target("copper_ore"); Launch(); Until(() => motor.Motion.State == RunState.Drilling); motor.Simulate(0.2f);
            Assert.That(presentation.Effects.CopperResponses, Is.Zero); Assert.That(presentation.Hud.Toast, Is.Empty);
            Until(() => target.Durability.IsDestroyed); Assert.That(flow.Run.Rewards.Copper, Is.EqualTo(3)); Assert.That(presentation.Effects.CopperResponses, Is.EqualTo(1));
            Assert.That(presentation.Hud.Toast, Is.EqualTo("+3 COPPER")); yield return Capture("CopperReward", 0.06f);
            target.Restore(); target.Durability.Drill(12, 10, 10); presentation.Sample();
            Assert.That(presentation.Effects.CopperResponses, Is.EqualTo(1)); Assert.That(flow.Run.Rewards.Copper, Is.EqualTo(3));
            Assert.That(presentation.Sounds.CueCounts[(int)FeedbackCue.CopperReward], Is.EqualTo(1));
            motor.Energy.Consume(10000, 1); motor.Simulate(0.01f); Assert.That(presentation.Effects.ParticleCount, Is.Zero); Assert.That(presentation.Hud.Toast, Is.Empty);
            Assert.That(flow.StartRun(), Is.True); yield return null; Assert.That(presentation.Effects.ParticleCount, Is.Zero); Assert.That(presentation.Hud.Toast, Is.Empty);
        }
        [UnityTest] public IEnumerator BlockedHasNoDamageParticlesAndLargerHudTracksAuthoritativeValues()
        {
            var stone = Target("stone"); Launch(); Until(() => motor.Motion.State == RunState.Blocked); yield return Capture("Blocked");
            Assert.That(stone.Durability.Remaining, Is.EqualTo(240)); Assert.That(presentation.Effects.ParticleCount, Is.Zero);
            Assert.That(hud.StateText.text, Is.EqualTo("BLOCKED")); Assert.That(hud.EnergyFill.fillAmount, Is.EqualTo(motor.Energy.Fraction).Within(0.0001));
            Assert.That(hud.DurabilityFill.fillAmount, Is.Zero); Assert.That(hud.RewardText.fontSize, Is.GreaterThanOrEqualTo(22));
            Assert.That(hud.transform.Find("M4 Effective Attributes").GetComponent<Text>().text, Does.Contain("POWER 5"));
            Assert.That(hud.EnergyFill.transform.parent.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(19));
            hud.Restart(); yield return Capture("Yard"); Assert.That(presentation.Sounds.LoopCue, Is.EqualTo(-1));
        }
        [UnityTest] public IEnumerator AudioWiringAndRealRuntimeBuffersAreMeasuredWithoutAssumingRecordingCaptureWorks()
        {
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.enabled), Is.EqualTo(1));
            Assert.That(AudioListener.volume, Is.GreaterThan(0)); Assert.That(AudioListener.pause, Is.False);
            var sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None); Assert.That(sources.Length, Is.EqualTo(5));
            var samples = new float[1024]; var log = "Unity Editor runtime audio probe; no physical listening performed.\n";
            var dsp = AudioSettings.dspTime;
            foreach (FeedbackCue cue in Enum.GetValues(typeof(FeedbackCue)))
            {
                presentation.Sounds.Play(cue); yield return new WaitForSecondsRealtime(0.07f);
                var source = sources.Single(s => s.clip != null && s.clip.name == "M6" + cue && !s.loop);
                Assert.That(source.mute, Is.False); Assert.That(source.volume, Is.GreaterThan(0)); Assert.That(source.outputAudioMixerGroup, Is.Null);
                source.GetOutputData(samples, 0); var sourcePeak = samples.Max(v => Mathf.Abs(v));
                AudioListener.GetOutputData(samples, 0); var listenerPeak = samples.Max(v => Mathf.Abs(v));
                log += cue + ": playing=" + source.isPlaying + ", timeSamples=" + source.timeSamples + ", sourcePeak=" + sourcePeak + ", listenerPeak=" + listenerPeak + "\n";
            }
            motor.Motion.Press(); motor.Simulate(0.5f); yield return new WaitForSecondsRealtime(0.2f); var starts = presentation.Sounds.LoopStarts;
            for (var i = 0; i < 20; i++) presentation.Sample(); Assert.That(presentation.Sounds.LoopStarts, Is.EqualTo(starts));
            var loop = sources.Single(s => s.loop); loop.GetOutputData(samples, 0); var loopPeak = samples.Max(v => Mathf.Abs(v));
            AudioListener.GetOutputData(samples, 0); var chargeListenerPeak = samples.Max(v => Mathf.Abs(v));
            log += "Charge loop playing=" + loop.isPlaying + ", timeSamples=" + loop.timeSamples + ", sourcePeak=" + loopPeak + ", listenerPeak=" + chargeListenerPeak + ", DSP delta=" + (AudioSettings.dspTime - dsp) + ", frequency=" + AudioSettings.outputSampleRate + "\n";
            hud.Restart(); var wood = Target("wood"); Launch(); Until(() => motor.Motion.State == RunState.Drilling); motor.Simulate(0.1f);
            yield return new WaitForSecondsRealtime(0.2f); loop.GetOutputData(samples, 0); loopPeak = samples.Max(v => Mathf.Abs(v));
            AudioListener.GetOutputData(samples, 0); log += "Actual drilling loop playing=" + loop.isPlaying + ", cue=" + presentation.Sounds.LoopCue + ", sourcePeak=" + loopPeak + ", listenerPeak=" + samples.Max(v => Mathf.Abs(v)) + "\n";
            hud.Restart(); Assert.That(sources.All(s => !s.isPlaying), Is.True); Assert.That(presentation.Sounds.LoopCue, Is.EqualTo(-1));
            File.WriteAllText("Logs/M6B-Audio.txt", log);
        }
        private IEnumerator Capture(string name, float delay = 0.12f)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) yield break;
            var follower = Object.FindFirstObjectByType<RunnerCamera>(); follower.Snap(); var camera = follower.GetComponent<Camera>(); var canvas = hud.GetComponent<Canvas>();
            var texture = new RenderTexture(540, 960, 24); var image = new Texture2D(540, 960, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture; var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; var oldPlane = canvas.planeDistance; var oldActive = RenderTexture.active;
            try
            {
                camera.targetTexture = texture; camera.aspect = 9f / 16; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 0.5f;
                yield return new WaitForSecondsRealtime(delay); yield return null; yield return null;
                RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 540, 960), 0, 0); image.Apply(); File.WriteAllBytes("Logs/M6B-" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldPlane; camera.targetTexture = oldTarget;
                camera.ResetAspect(); RenderTexture.active = oldActive; texture.Release(); Object.Destroy(texture); Object.Destroy(image);
            }
        }
    }
}

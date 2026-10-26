using System;
using NUnit.Framework;
using UnityEngine;

namespace DrillRunner.Tests.EditMode
{
    public class MovementTests
    {
        private static DrillMotion NewMotion() => new DrillMotion(new DrillMovementSettings(), Vector3.zero);
        private static DrillMotion Launch(float duration = 1.5f)
        {
            var motion = NewMotion();
            motion.Press();
            motion.Step(duration);
            Assert.That(motion.Release(), Is.True);
            return motion;
        }

        [Test]
        public void ReadyIsStationaryAndReleaseRequiresAValidPress()
        {
            var motion = NewMotion();
            Assert.That(motion.Release(), Is.False);
            motion.Drag(new Vector2(500, 500), 540);
            motion.Step(10);
            Assert.That(motion.State, Is.EqualTo(RunState.Ready));
            Assert.That(motion.Position, Is.EqualTo(Vector3.zero));
            Assert.That(motion.ForwardSpeed, Is.Zero);
        }

        [Test]
        public void ChargeIncreasesAndStaysAtMaximumWithoutWrappingOrSteering()
        {
            var motion = NewMotion();
            motion.Press();
            motion.Step(0.75f);
            Assert.That(motion.ChargeNormalized, Is.EqualTo(0.5f).Within(1e-6));
            motion.Press(); // Repeated press must not restart charge.
            motion.Drag(new Vector2(100, 300), 540);
            motion.Step(1000);
            Assert.That(motion.ChargeNormalized, Is.EqualTo(1));
            motion.Step(1000);
            Assert.That(motion.ChargeNormalized, Is.EqualTo(1));
            Assert.That(motion.Position, Is.EqualTo(Vector3.zero));
        }

        [TestCase(0f)]
        [TestCase(0.001f)]
        [TestCase(1.5f)]
        public void ShortAndFullPressesLaunchOnlyOnceAndKeepCruising(float hold)
        {
            var motion = Launch(hold);
            var speed = motion.ForwardSpeed;
            Assert.That(speed, Is.InRange(12, 22));
            Assert.That(motion.Release(), Is.False);
            motion.Press();
            Assert.That(motion.Release(), Is.False);
            Assert.That(motion.ForwardSpeed, Is.EqualTo(speed));
            motion.Step(30);
            Assert.That(motion.State, Is.EqualTo(RunState.Moving));
            Assert.That(motion.Position.z, Is.GreaterThan(300));
            Assert.That(motion.ForwardSpeed, Is.EqualTo(10).Within(1e-5));
        }

        [Test]
        public void HigherChargeProducesStrongerBoostAndDistance()
        {
            var low = Launch(0.1f);
            var high = Launch(1.5f);
            Assert.That(high.ForwardSpeed, Is.GreaterThan(low.ForwardSpeed));
            high.Step(0.5f); low.Step(0.5f);
            Assert.That(high.Position.z, Is.GreaterThan(low.Position.z));
        }

        [Test]
        public void CancelingChargeReturnsToReadyAndCannotLaunchOnLateRelease()
        {
            var motion = NewMotion();
            motion.Press(); motion.Step(0.5f); motion.CancelPointer();
            Assert.That(motion.State, Is.EqualTo(RunState.Ready));
            Assert.That(motion.ChargeNormalized, Is.Zero);
            Assert.That(motion.Release(), Is.False);
            motion.Press();
            Assert.That(motion.Release(), Is.True);
        }

        [Test]
        public void BoostDecaysMonotonicallyWithinSpeedLimits()
        {
            var motion = Launch();
            var previous = motion.ForwardSpeed;
            for (var i = 0; i < 600; i++)
            {
                motion.Step(1f / 60);
                Assert.That(motion.ForwardSpeed, Is.InRange(10, 24));
                Assert.That(motion.ForwardSpeed, Is.LessThanOrEqualTo(previous));
                previous = motion.ForwardSpeed;
            }
            Assert.That(motion.ForwardSpeed, Is.EqualTo(10).Within(1e-4));
        }

        [Test]
        public void ExactBoostIntegrationIsIndependentOfFramePartition()
        {
            var coarse = Launch(); var fine = Launch(); var oneStep = Launch();
            for (var i = 0; i < 30; i++) coarse.Step(1f / 30);
            for (var i = 0; i < 120; i++) fine.Step(1f / 120);
            oneStep.Step(1);
            Assert.That(coarse.Position.z, Is.EqualTo(fine.Position.z).Within(1e-4));
            Assert.That(coarse.Position.z, Is.EqualTo(oneStep.Position.z).Within(1e-4));
            Assert.That(coarse.ForwardSpeed, Is.EqualTo(fine.ForwardSpeed).Within(1e-4));
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void SteeringIsContinuousAndConstrainsWholeFootprintOnBothSides(float direction)
        {
            var motion = Launch();
            motion.Press();
            motion.Drag(new Vector2(direction * 50, 900), 540);
            motion.Step(0.02f);
            Assert.That(motion.Position.x * direction, Is.InRange(0.01f, 0.75f));
            motion.Drag(new Vector2(direction * 100000, 0), 540);
            motion.Step(3);
            Assert.That(motion.Position.x, Is.EqualTo(direction * 2.15f).Within(1e-5));
            Assert.That(Mathf.Abs(motion.Position.x) + 0.85f, Is.LessThanOrEqualTo(3.00001f));
            Assert.That(motion.Position.y, Is.Zero);
        }

        [Test]
        public void StartingADragHasNoJumpAndReleaseStopsAllLateralMotion()
        {
            var motion = Launch();
            motion.Drag(new Vector2(100, 0), 540); motion.Step(0.1f);
            Assert.That(motion.Position.x, Is.Zero);
            motion.Press(); motion.Step(0.1f);
            Assert.That(motion.Position.x, Is.Zero);
            motion.Drag(new Vector2(100, 0), 540); motion.Step(0.02f);
            var x = motion.Position.x;
            motion.Release(); motion.Step(1);
            Assert.That(motion.Position.x, Is.EqualTo(x));
        }

        [Test]
        public void VerticalDeltaIsIgnoredAndSteeringIsResolutionIndependent()
        {
            var low = Launch(); var high = Launch();
            low.Press(); high.Press();
            low.Drag(new Vector2(0, 10000), 540); low.Step(0.1f);
            Assert.That(low.Position.x, Is.Zero);
            low.Drag(new Vector2(54, 20), 540);
            high.Drag(new Vector2(108, -400), 1080);
            low.Step(0.1f); high.Step(0.1f);
            Assert.That(low.Position.x, Is.EqualTo(high.Position.x).Within(1e-6));
        }

        [Test]
        public void SteeringResponseDoesNotDependOnStepPartition()
        {
            var a = Launch(); var b = Launch(); a.Press(); b.Press();
            a.Drag(new Vector2(50, 0), 540); b.Drag(new Vector2(50, 0), 540);
            a.Step(0.1f);
            for (var i = 0; i < 10; i++) b.Step(0.01f);
            Assert.That(a.Position.x, Is.EqualTo(b.Position.x).Within(1e-6));
        }

        [Test]
        public void TemporarySteeringRestrictionClearsQueuedMotionButKeepsForwardMotion()
        {
            var motion = Launch(); motion.Press(); motion.Drag(new Vector2(100, 0), 540);
            motion.SetSteeringEnabled(false); motion.Step(0.1f);
            Assert.That(motion.Position.x, Is.Zero);
            Assert.That(motion.Position.z, Is.GreaterThan(0));
            motion.SetSteeringEnabled(true); motion.Step(0.1f);
            Assert.That(motion.Position.x, Is.Zero);
            motion.Drag(new Vector2(-20, 0), 540); motion.Step(0.1f);
            Assert.That(motion.Position.x, Is.LessThan(0));
        }

        [Test]
        public void ResetRestoresPositionSpeedChargeReadinessAndDrag()
        {
            var motion = Launch(); motion.Press(); motion.Drag(new Vector2(50, 0), 540); motion.Step(1);
            motion.Reset(); motion.Drag(new Vector2(100, 0), 540); motion.Step(1);
            Assert.That(motion.Position, Is.EqualTo(Vector3.zero));
            Assert.That(motion.ForwardSpeed, Is.Zero);
            Assert.That(motion.ChargeNormalized, Is.Zero);
            Assert.That(motion.IsDragging, Is.False);
            Assert.That(motion.Release(), Is.False);
            motion.Press(); Assert.That(motion.Release(), Is.True);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1f)]
        public void InvalidTimeIsRejectedWithoutCorruptingMovement(float invalid)
        {
            var motion = Launch(); var before = motion.Position;
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Step(invalid));
            Assert.That(motion.Position, Is.EqualTo(before));
            Assert.That(float.IsNaN(motion.ForwardSpeed), Is.False);
        }

        [Test]
        public void InvalidSettingsAndInputAreRejectedWithoutNaNPositions()
        {
            var settings = new DrillMovementSettings { ChargeDuration = 0 };
            Assert.Throws<ArgumentOutOfRangeException>(() => new DrillMotion(settings, Vector3.zero));
            settings = new DrillMovementSettings { CruiseSpeed = float.NaN };
            Assert.Throws<ArgumentOutOfRangeException>(() => new DrillMotion(settings, Vector3.zero));
            settings = new DrillMovementSettings { MaximumSpeed = 11 };
            Assert.Throws<ArgumentException>(() => new DrillMotion(settings, Vector3.zero));
            settings = new DrillMovementSettings { FootprintHalfWidth = 3 };
            Assert.Throws<ArgumentException>(() => new DrillMotion(settings, Vector3.zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DrillMotion(new DrillMovementSettings(), new Vector3(float.NaN, 0, 0)));
            var motion = Launch(); motion.Press();
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Drag(Vector2.one, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Drag(new Vector2(float.NaN, 0), 540));
            motion.Drag(new Vector2(float.MaxValue, 0), 1); motion.Step(1);
            Assert.That(motion.Position.x, Is.InRange(-2.15f, 2.15f));
        }

        [Test]
        public void ValidatedSettingsAreCapturedInsteadOfMutatingDuringARun()
        {
            var settings = new DrillMovementSettings();
            var motion = new DrillMotion(settings, Vector3.zero);
            settings.CruiseSpeed = float.NaN;
            motion.Press(); motion.Release(); motion.Step(1);
            Assert.That(motion.Position.z, Is.GreaterThan(10));
        }

        [Test]
        public void CameraDampingIsStableBoundedAndFrameIndependent()
        {
            var single = RunnerCamera.Damp(0, 20, 8, 1);
            var partitioned = 0f;
            for (var i = 0; i < 120; i++) partitioned = RunnerCamera.Damp(partitioned, 20, 8, 1f / 120);
            Assert.That(partitioned, Is.EqualTo(single).Within(1e-4));
            Assert.That(single, Is.InRange(0, 20));
            Assert.That(RunnerCamera.Damp(0, 20, 8, 1000), Is.EqualTo(20));
            Assert.That(RunnerCamera.Damp(-2, 2, 8, 0), Is.EqualTo(-2));
            Assert.Throws<ArgumentOutOfRangeException>(() => RunnerCamera.Damp(float.NaN, 0, 8, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => RunnerCamera.Damp(0, 1, 0, 1));
        }
    }
}

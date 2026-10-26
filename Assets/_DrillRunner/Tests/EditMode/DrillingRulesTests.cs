using System;
using NUnit.Framework;
using UnityEngine;

namespace DrillRunner.Tests.EditMode
{
    public class DrillingRulesTests
    {
        private MaterialDefinition definition;

        [SetUp]
        public void CreateMaterial()
        {
            definition = ScriptableObject.CreateInstance<MaterialDefinition>();
            definition.MaterialId = "test"; definition.DisplayName = "Test Material";
            definition.Hardness = 5; definition.Durability = 100; definition.DrillingEnergyPerSecond = 12;
        }

        [TearDown] public void Cleanup() => UnityEngine.Object.DestroyImmediate(definition);

        [TestCase(4f, 100f)]
        [TestCase(5f, 50f)]
        [TestCase(8f, 20f)]
        public void HardnessIsAnAbsoluteThresholdIndependentOfMovement(float power, float remaining)
        {
            var target = new ObstacleDurability(definition);
            target.Drill(power, 10, 1);
            Assert.That(target.Remaining, Is.EqualTo(remaining).Within(1e-6));
            Assert.That(target.IsDestroyed, Is.False);
        }

        [Test]
        public void DamageIsFrameIndependentClampedAndEmitsOneDestructionPerRun()
        {
            var a = new ObstacleDurability(definition); var b = new ObstacleDurability(definition);
            a.Drill(5, 10, 1.5);
            for (var i = 0; i < 150; i++) b.Drill(5, 10, 0.01);
            Assert.That(a.Remaining, Is.EqualTo(b.Remaining).Within(1e-5));
            var breaks = 0; a.Destroyed += () => breaks++;
            a.Drill(5, 10, 100); a.Drill(50, 10, 100);
            Assert.That(a.Remaining, Is.Zero); Assert.That(breaks, Is.EqualTo(1));
            a.Reset(); Assert.That(a.Remaining, Is.EqualTo(100));
            a.Drill(5, 10, 2); Assert.That(breaks, Is.EqualTo(2));
        }

        [Test]
        public void HigherPowerShortensDrillingTimeAndReducesTotalMaterialEnergy()
        {
            var target = new ObstacleDurability(definition);
            var low = target.TimeToBreak(5, 10); var high = target.TimeToBreak(10, 10);
            Assert.That(high, Is.LessThan(low));
            Assert.That(high * target.EnergyRate, Is.LessThan(low * target.EnergyRate));
            Assert.That(target.TimeToBreak(4, 10), Is.EqualTo(double.PositiveInfinity));
        }

        [Test]
        public void MaterialSnapshotDoesNotDamageOrMutateItsSharedDefinition()
        {
            var target = new ObstacleDurability(definition);
            target.Drill(5, 10, 0.5);
            Assert.That(definition.Durability, Is.EqualTo(100));
            definition.Durability = 999;
            Assert.That(target.Maximum, Is.EqualTo(100));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidHardnessOrMaterialEnergyIsRejected(float value)
        {
            definition.Hardness = value;
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObstacleDurability(definition));
            definition.Hardness = 5; definition.DrillingEnergyPerSecond = value;
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObstacleDurability(definition));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDurabilityIsRejected(float value)
        {
            definition.Durability = value;
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObstacleDurability(definition));
        }

        [Test]
        public void MissingMaterialIdentityAndInvalidDamageAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => new ObstacleDurability(null));
            definition.MaterialId = " "; Assert.Throws<ArgumentException>(() => definition.Validate());
            definition.MaterialId = "test";
            var target = new ObstacleDurability(definition);
            Assert.Throws<ArgumentOutOfRangeException>(() => target.Drill(5, float.NaN, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => target.Drill(5, 10, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => target.Drill(5, 10, -1));
            Assert.That(target.Remaining, Is.EqualTo(100));
        }

        [Test]
        public void EnergyIsFrameIndependentAndDepletionFiresOnceUntilReset()
        {
            var a = new RunEnergy(100); var b = new RunEnergy(100);
            a.Consume(12, 3);
            for (var i = 0; i < 300; i++) b.Consume(12, 0.01);
            Assert.That(a.Remaining, Is.EqualTo(64));
            Assert.That(b.Remaining, Is.EqualTo(a.Remaining).Within(1e-5));
            var ends = 0; a.Depleted += () => ends++;
            a.Consume(100, 100); a.Consume(100, 100);
            Assert.That(a.Remaining, Is.Zero); Assert.That(ends, Is.EqualTo(1));
            a.Reset(); Assert.That(a.Remaining, Is.EqualTo(100));
            a.Consume(100, 1); Assert.That(ends, Is.EqualTo(2));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void EnergyRejectsInvalidRatesTimeAndCapacity(float invalid)
        {
            var energy = new RunEnergy(100);
            Assert.Throws<ArgumentOutOfRangeException>(() => energy.Consume(invalid, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => energy.Consume(1, invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunEnergy(invalid));
            Assert.That(energy.Remaining, Is.EqualTo(100));
        }

        [Test]
        public void ZeroRateAndTimeAreSafeAndInvalidRunSettingsFailBeforeSimulation()
        {
            var energy = new RunEnergy(100); energy.Consume(0, 100); energy.Consume(100, 0);
            Assert.That(energy.Remaining, Is.EqualTo(100));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunEnergy(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DrillingSettings { DamagePerPower = 0 }.Validate());
            Assert.Throws<ArgumentOutOfRangeException>(() => new DrillingSettings { MovingEnergyPerSecond = -1 }.Validate());
        }

        [Test]
        public void ContactStatesHaveOneAuthorityAndReleaseLocksOnCompletionEndAndReset()
        {
            var motion = new DrillMotion(new DrillMovementSettings(), Vector3.zero);
            motion.Press(); motion.Release(); motion.Press(); motion.EnterContact(true);
            Assert.That(motion.State, Is.EqualTo(RunState.Drilling)); Assert.That(motion.IsDragging, Is.False);
            motion.Drag(new Vector2(100, 0), 540); motion.Step(1);
            Assert.That(motion.Position, Is.EqualTo(Vector3.zero)); Assert.That(motion.ForwardSpeed, Is.Zero);
            Assert.That(motion.ResumeMoving(), Is.True); Assert.That(motion.ResumeMoving(), Is.False);
            Assert.That(motion.IsDragging, Is.True); motion.Step(0.2f);
            Assert.That(motion.ForwardSpeed, Is.InRange(0.01f, 10));
            motion.EnterContact(false); Assert.That(motion.IsDragging, Is.True);
            motion.Drag(new Vector2(50, 0), 540); motion.Step(0.2f);
            Assert.That(motion.Position.x, Is.GreaterThan(0)); Assert.That(motion.ForwardSpeed, Is.Zero);
            Assert.That(motion.EndRun(), Is.True); Assert.That(motion.EndRun(), Is.False);
            var ended = motion.Position; motion.Press(); motion.Release(); motion.Step(1);
            Assert.That(motion.Position, Is.EqualTo(ended));
            motion.Reset(); Assert.That(motion.State, Is.EqualTo(RunState.Ready));
            motion.Press(); Assert.That(motion.Release(), Is.True);
        }

        [Test]
        public void SweepsStopThinFrontalObstaclesAndLateralEntryWithoutGrazingLocks()
        {
            var body = new Bounds(new Vector3(0, 0.65f, 0.35f), new Vector3(1.7f, 1.3f, 3));
            var thin = new Bounds(new Vector3(0, 1, 10), new Vector3(1.8f, 2, 0.01f));
            var travel = DrillContacts.SweepForward(body, thin, 100);
            Assert.That(body.max.z + travel, Is.EqualTo(thin.min.z - DrillContacts.Skin).Within(1e-5));
            Assert.That(DrillContacts.CentralTip(new Vector3(0, 0.65f, body.max.z + travel), thin), Is.True);
            Assert.That(DrillContacts.CentralTip(new Vector3(0.85f, 0.65f, body.max.z + travel), thin), Is.False);
            body.center = new Vector3(2.1f, 0.65f, 10);
            Assert.That(DrillContacts.SweepForward(body, thin, 100), Is.EqualTo(100));
            var sideways = DrillContacts.SweepX(body, thin, -4);
            Assert.That(body.min.x + sideways, Is.GreaterThanOrEqualTo(thin.max.x + DrillContacts.Skin - 1e-5));
            body.center = new Vector3(-2.1f, 0.65f, 10);
            sideways = DrillContacts.SweepX(body, thin, 4);
            Assert.That(body.max.x + sideways, Is.LessThanOrEqualTo(thin.min.x - DrillContacts.Skin + 1e-5));
            body.center = new Vector3(thin.max.x + body.extents.x - 0.001f, 0.65f, 0.35f);
            Assert.That(DrillContacts.SweepForward(body, thin, 100), Is.LessThan(100), "Even a 1 mm footprint overlap must block forward passage.");
            Assert.Throws<ArgumentOutOfRangeException>(() => DrillContacts.SweepX(body, thin, float.NaN));
        }
    }
}

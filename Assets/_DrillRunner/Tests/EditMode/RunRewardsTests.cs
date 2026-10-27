using System;
using NUnit.Framework;

namespace DrillRunner.Tests.EditMode
{
    public class RunRewardsTests
    {
        private static RunRewards NewRun() => new RunRewards(1337, 0, new RewardSettings());

        [Test]
        public void ReadyIgnoresProgressAndClaimsUntilTheRunStarts()
        {
            var run = NewRun(); run.RecordForward(100);
            Assert.That(run.Claim(1, ObstacleRewardKind.Coins, 2, true), Is.False);
            Assert.That(run.Distance, Is.Zero); Assert.That(run.Coins, Is.Zero);
        }

        [Test]
        public void MonotonicForwardDistancePaysEachIntervalOnceAndIgnoresBacktracking()
        {
            var run = NewRun(); run.Start();
            run.RecordForward(-10); run.RecordForward(4.9); Assert.That(run.Coins, Is.Zero);
            run.RecordForward(5); run.RecordForward(5); run.RecordForward(0);
            Assert.That(run.DistanceCoins, Is.EqualTo(1));
            run.RecordForward(15); Assert.That(run.DistanceCoins, Is.EqualTo(3));
            run.RecordForward(12); Assert.That(run.Distance, Is.EqualTo(15));
        }

        [Test]
        public void SmallProgressPartitionsDoNotMissOrDuplicateDistanceCoins()
        {
            var a = NewRun(); var b = NewRun(); a.Start(); b.Start();
            for (var i = 0; i <= 10000; i++) a.RecordForward(i / 100.0);
            b.RecordForward(100);
            Assert.That(a.Coins, Is.EqualTo(20)); Assert.That(a.Coins, Is.EqualTo(b.Coins));
        }

        [Test]
        public void NormalDestructionAddsConfiguredCoinsExactlyOnceAndPartialDamagePaysNothing()
        {
            var run = NewRun(); run.Start(); run.RecordForward(50);
            Assert.That(run.Claim(1, ObstacleRewardKind.Coins, 1, false), Is.False);
            Assert.That(run.ObstacleCoins, Is.Zero);
            Assert.That(run.Claim(1, ObstacleRewardKind.Coins, 1, true), Is.True);
            Assert.That(run.Claim(1, ObstacleRewardKind.Coins, 1, true), Is.False);
            run.Claim(2, ObstacleRewardKind.Coins, 2, true);
            Assert.That(run.DistanceCoins, Is.EqualTo(10)); Assert.That(run.ObstacleCoins, Is.EqualTo(3));
            Assert.That(run.Coins, Is.EqualTo(13));
        }

        [Test]
        public void CopperAwardsOnlyResourcesAfterFullDestruction()
        {
            var run = NewRun(); run.Start();
            Assert.That(run.Claim(1, ObstacleRewardKind.Copper, 3, false), Is.False);
            Assert.That(run.Claim(1, ObstacleRewardKind.Copper, 3, true), Is.True);
            Assert.That(run.Claim(1, ObstacleRewardKind.Copper, 3, true), Is.False);
            Assert.That(run.Copper, Is.EqualTo(3)); Assert.That(run.Coins, Is.Zero);
        }

        [Test]
        public void CompletionFreezesEveryCounterAndASeparateAttemptStartsEmpty()
        {
            var run = NewRun(); run.Start(); run.RecordForward(20); run.Claim(1, ObstacleRewardKind.Copper, 3, true);
            run.Complete(); run.Complete(); run.Start(); run.RecordForward(500);
            Assert.That(run.Claim(2, ObstacleRewardKind.Coins, 10, true), Is.False);
            Assert.That(run.Distance, Is.EqualTo(20)); Assert.That(run.Coins, Is.EqualTo(4)); Assert.That(run.Copper, Is.EqualTo(3));
            var next = NewRun(); Assert.That(next.Distance, Is.Zero); Assert.That(next.Coins, Is.Zero); Assert.That(next.Copper, Is.Zero);
        }

        [Test]
        public void InvalidRewardInputAndOverflowAreRejectedBeforeMutation()
        {
            Assert.Throws<ArgumentException>(() => new RunRewards(0, 0, new RewardSettings { DistancePerPayout = 0 }));
            var run = NewRun(); run.Start();
            Assert.Throws<ArgumentOutOfRangeException>(() => run.RecordForward(double.NaN));
            Assert.Throws<ArgumentException>(() => run.Claim(0, ObstacleRewardKind.Coins, 1, true));
            Assert.Throws<ArgumentException>(() => run.Claim(1, ObstacleRewardKind.Copper, -1, true));
            Assert.Throws<OverflowException>(() => run.RecordForward(double.MaxValue));
            Assert.That(run.Distance, Is.Zero); Assert.That(run.Coins, Is.Zero);
        }
    }
}

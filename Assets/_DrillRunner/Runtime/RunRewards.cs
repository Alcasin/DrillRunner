using System;
using System.Collections.Generic;

namespace DrillRunner
{
    [Serializable]
    public sealed class RewardSettings
    {
        public double DistancePerPayout = 5;
        public int CoinsPerPayout = 1;
        public void Validate()
        {
            if (double.IsNaN(DistancePerPayout) || double.IsInfinity(DistancePerPayout) || DistancePerPayout <= 0 || CoinsPerPayout < 0)
                throw new ArgumentException("Distance reward interval must be finite and positive; Coins must be nonnegative.");
        }
    }

    /// <summary>Single in-memory ledger for one attempt. No balances are persisted.</summary>
    public sealed class RunRewards
    {
        private readonly double origin, interval;
        private readonly int payout;
        private readonly HashSet<long> claimed = new HashSet<long>();
        private double progress;
        public int Seed { get; }
        public double Distance => progress;
        public long DistanceCoins { get; private set; }
        public long ObstacleCoins { get; private set; }
        public long Copper { get; private set; }
        public long Coins => DistanceCoins + ObstacleCoins;
        public bool IsActive { get; private set; }
        public bool IsCompleted { get; private set; }

        public RunRewards(int seed, double startZ, RewardSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.Validate();
            if (double.IsNaN(startZ) || double.IsInfinity(startZ)) throw new ArgumentOutOfRangeException(nameof(startZ));
            Seed = seed; origin = startZ; interval = settings.DistancePerPayout; payout = settings.CoinsPerPayout;
        }

        public void Start() { if (!IsCompleted) IsActive = true; }

        public void RecordForward(double worldZ)
        {
            if (double.IsNaN(worldZ) || double.IsInfinity(worldZ)) throw new ArgumentOutOfRangeException(nameof(worldZ));
            if (!IsActive || IsCompleted) return;
            var next = Math.Max(progress, Math.Max(0, worldZ - origin));
            var intervals = Math.Floor(next / interval + 1e-9);
            if (intervals > long.MaxValue / Math.Max(1, payout)) throw new OverflowException("Distance reward exceeds the supported run range.");
            var coins = checked((long)intervals * payout);
            if (coins > long.MaxValue - ObstacleCoins) throw new OverflowException("Run Coin total overflow.");
            progress = next; DistanceCoins = coins;
        }

        public bool Claim(long encounterId, ObstacleRewardKind kind, int amount, bool completelyDestroyed)
        {
            if (encounterId <= 0 || amount < 0 || !Enum.IsDefined(typeof(ObstacleRewardKind), kind)) throw new ArgumentException("Invalid obstacle reward.");
            if (!IsActive || IsCompleted || !completelyDestroyed || claimed.Contains(encounterId)) return false;
            var total = kind == ObstacleRewardKind.Copper ? checked(Copper + amount) : checked(Coins + amount);
            claimed.Add(encounterId);
            if (kind == ObstacleRewardKind.Copper) Copper = total; else ObstacleCoins += amount;
            return true;
        }

        public void Complete() { IsCompleted = true; IsActive = false; }
    }
}

using System;
using System.Collections.Generic;

namespace DrillRunner
{
    [Serializable]
    public sealed class RewardSettings
    {
        public double DistancePerPayout = 5;
        public int CoinsPerPayout = 1;
        public int ObstacleCoinMultiplier = 1;
        public void Validate()
        {
            if (double.IsNaN(DistancePerPayout) || double.IsInfinity(DistancePerPayout) || DistancePerPayout <= 0 || CoinsPerPayout < 0 || ObstacleCoinMultiplier < 1 || ObstacleCoinMultiplier > 1000)
                throw new ArgumentException("Distance reward interval must be finite and positive; Coins must be nonnegative.");
        }
    }

    /// <summary>Single in-memory ledger for one attempt. No balances are persisted.</summary>
    public sealed class RunRewards
    {
        private readonly double origin, interval;
        private readonly int payout, obstacleMultiplier;
        private readonly HashSet<long> claimed = new HashSet<long>();
        private double progress;
        public int Seed { get; }
        public string RegionId { get; }
        public double Distance => progress;
        public long DistanceCoins { get; private set; }
        public long ObstacleCoins { get; private set; }
        private readonly long[] resources = new long[5];
        public long Copper => resources[0];
        public long Iron => resources[1];
        public long Quartz => resources[2];
        public long AlloyCore => resources[3];
        public long EnergyShard => resources[4];
        public long Resource(ObstacleRewardKind kind) => MaterialDefinition.IsResource(kind) ? resources[(int)kind - 1] : 0;
        public string ResourceText()
        {
            var text = "";
            for (var i = 0; i < resources.Length; i++)
            {
                bool contextual = RegionId == RegionIds.Yard ? i == 0 : RegionId == RegionIds.Quarry ? i == 1 : RegionId == RegionIds.Advanced ? i == 2 || i == 3 : i == 4;
                if (contextual || resources[i] > 0) text += (text.Length == 0 ? "" : " · ") + resources[i] + " " + ResourceCost.Name((EvolutionResource)i);
            }
            return text;
        }
        public long Coins => DistanceCoins + ObstacleCoins;
        public bool IsActive { get; private set; }
        public bool IsCompleted { get; private set; }

        public RunRewards(int seed, double startZ, RewardSettings settings, string regionId = RegionIds.Yard)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.Validate();
            if (double.IsNaN(startZ) || double.IsInfinity(startZ)) throw new ArgumentOutOfRangeException(nameof(startZ));
            if (!RegionIds.Implemented(regionId)) throw new ArgumentException("Unimplemented run region.");
            RegionId = regionId; Seed = seed; origin = startZ; interval = settings.DistancePerPayout; payout = settings.CoinsPerPayout; obstacleMultiplier = settings.ObstacleCoinMultiplier;
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
            var normalCoins = checked((long)amount * obstacleMultiplier);
            var special = MaterialDefinition.IsResource(kind);
            var total = special ? checked(Resource(kind) + amount) : checked(Coins + normalCoins);
            claimed.Add(encounterId);
            if (special) resources[(int)kind - 1] = total; else ObstacleCoins += normalCoins;
            return true;
        }

        public void Complete() { IsCompleted = true; IsActive = false; }
    }
}

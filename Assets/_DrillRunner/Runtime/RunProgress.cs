using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrillRunner
{
    [DisallowMultipleComponent]
    public sealed class RunProgress : MonoBehaviour
    {
        [SerializeField] private DrillMotor motor;
        [SerializeField] private TrackStreamer track;
        [SerializeField] private RewardSettings rewards = new RewardSettings();
        [SerializeField] private bool useFixedSeed = true;
        [SerializeField] private int configuredSeed = 1337;
        private readonly Dictionary<DrillObstacle, RewardBinding> bindings = new Dictionary<DrillObstacle, RewardBinding>();
        private System.Random attemptSeeds;
        private long nextId;
        private bool started;
        public RunRewards Rewards { get; private set; }
        public bool UseFixedSeed { get => useFixedSeed; set => useFixedSeed = value; }
        public int ConfiguredSeed { get => configuredSeed; set => configuredSeed = value; }
        public TrackStreamer Track => track;
        public DrillMotor Motor => motor;

        private readonly struct RewardBinding
        {
            public readonly long Id;
            public readonly ObstacleRewardKind Kind;
            public readonly int Amount;
            public RewardBinding(long id, MaterialDefinition material)
            { Id = id; Kind = material.RewardKind; Amount = material.RewardAmount; }
        }

        private void OnEnable()
        {
            if (motor == null || track == null) return;
            motor.ResetPerformed += BeginAttempt; motor.RunEnded += Complete;
            motor.SimulationStarting += Observe; motor.SimulationAdvanced += AfterSimulation;
            motor.ObstacleRegistered += Bind; motor.ObstacleUnregistered += Unbind; motor.ObstacleBroken += OnBroken;
        }

        private void Start() { started = true; BeginAttempt(); }

        private void OnDisable()
        {
            if (motor == null) return;
            motor.ResetPerformed -= BeginAttempt; motor.RunEnded -= Complete;
            motor.SimulationStarting -= Observe; motor.SimulationAdvanced -= AfterSimulation;
            motor.ObstacleRegistered -= Bind; motor.ObstacleUnregistered -= Unbind; motor.ObstacleBroken -= OnBroken;
        }

        private void BeginAttempt()
        {
            if (!started) return;
            Rewards?.Complete(); bindings.Clear(); nextId = 0;
            if (attemptSeeds == null) attemptSeeds = new System.Random(Environment.TickCount);
            var seed = useFixedSeed ? configuredSeed : attemptSeeds.Next();
            if (!useFixedSeed && Rewards != null && seed == Rewards.Seed) seed = unchecked(seed + 1);
            try
            {
                Rewards = new RunRewards(seed, motor.StartPosition.z, rewards);
                track.Begin(seed);
                foreach (var obstacle in motor.RegisteredObstacles) Bind(obstacle);
                if (motor.Motion.State == RunState.RunEnded) Rewards.Complete();
                Debug.Log("DrillRunner attempt seed: " + seed, this);
            }
            catch (ArgumentException exception)
            {
                Rewards?.Complete(); motor.Motion.EndRun();
                Debug.LogError("Cannot initialize modular run: " + exception.Message, this);
            }
        }

        private void Observe()
        {
            if (Rewards == null || Rewards.IsCompleted) return;
            var state = motor.Motion.State;
            if (state == RunState.Moving || state == RunState.Drilling || state == RunState.Blocked) Rewards.Start();
            Rewards.RecordForward(motor.Motion.Position.z);
        }

        private void AfterSimulation()
        {
            Observe();
            if (motor.enabled && Rewards != null && !Rewards.IsCompleted)
                track.UpdateCoverage(Mathf.Max(0, motor.Motion.Position.z), motor.ContactTarget);
        }

        private void Complete()
        { Observe(); Rewards?.RecordForward(motor.Motion.Position.z); Rewards?.Complete(); }

        private void Bind(DrillObstacle obstacle)
        {
            if (obstacle != null && obstacle.Definition != null && !bindings.ContainsKey(obstacle))
                bindings.Add(obstacle, new RewardBinding(++nextId, obstacle.Definition));
        }
        private void Unbind(DrillObstacle obstacle) => bindings.Remove(obstacle);

        private void OnBroken(DrillObstacle obstacle)
        {
            Observe();
            if (Rewards != null && bindings.TryGetValue(obstacle, out var binding))
                Rewards.Claim(binding.Id, binding.Kind, binding.Amount, obstacle.Durability != null && obstacle.Durability.IsDestroyed);
        }
    }
}

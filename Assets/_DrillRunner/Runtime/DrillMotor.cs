using System;
using System.Collections.Generic;
using UnityEngine;

namespace DrillRunner
{
    [DisallowMultipleComponent]
    public sealed class DrillMotor : MonoBehaviour
    {
        [SerializeField] private DrillMovementSettings settings = new DrillMovementSettings();
        [SerializeField] private DrillingSettings drilling = new DrillingSettings();
        [SerializeField] private BoxCollider footprint;
        [SerializeField] private Transform drillTip;
        [SerializeField] private DrillObstacle[] obstacles = Array.Empty<DrillObstacle>();
        private DrillingSettings runSettings;
        private bool initializationFailed;
        private readonly List<DrillObstacle> registered = new List<DrillObstacle>(32);
        private const double MaximumStep = 1.0 / 120.0;
        public DrillMovementSettings Settings => settings;
        public DrillingSettings Drilling => drilling;
        public RunEnergy Energy { get; private set; }
        public DrillObstacle ContactTarget { get; private set; }
        public DrillObstacle[] Obstacles => obstacles;
        public IReadOnlyList<DrillObstacle> RegisteredObstacles => registered;
        public float DrillPower => runSettings != null ? runSettings.DrillPower : 0;
        public bool ContactIsTooHard => ContactTarget != null &&
            (ContactTarget.Durability == null || runSettings.DrillPower < ContactTarget.Durability.Hardness);
        public DrillMotion Motion { get; private set; }
        public Vector3 StartPosition { get; private set; }
        public event Action ResetPerformed;
        public event Action RunEnded;
        public event Action SimulationAdvanced;
        public event Action SimulationStarting;
        public event Action<DrillObstacle> ObstacleRegistered;
        public event Action<DrillObstacle> ObstacleUnregistered;
        public event Action<DrillObstacle> ObstacleBroken;
        public Func<bool> ResetGuard { get; set; }
#if UNITY_EDITOR
        public RunDiagnostics Diagnostics { get; private set; }
#endif

        private void Awake()
        {
            StartPosition = transform.position;
            Motion = new DrillMotion(settings, StartPosition);
            if (footprint == null)
                footprint = GetComponent<BoxCollider>();
            try
            {
                DrillContacts.WorldBounds(footprint);
                foreach (var obstacle in obstacles) RegisterObstacle(obstacle);
                ResetDrilling();
            }
            catch (ArgumentException exception)
            {
                initializationFailed = true;
                Motion.EndRun();
                enabled = false;
                Debug.LogError("Cannot initialize DrillMotor: " + exception.Message, this);
            }
        }

        private void Update()
        {
            Simulate(Time.deltaTime);
        }

        /// <summary>Bounded contact/energy substeps, including the final fractional interval.</summary>
        public void Simulate(float deltaTime)
        {
            if (!DrillMovementSettings.Finite(deltaTime) || deltaTime < 0 || deltaTime > 10)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Expected a finite interval from 0 to 10 seconds.");
            SimulationStarting?.Invoke();
            double remaining = deltaTime;
            while (remaining > 1e-10)
            {
                var duration = Math.Min(remaining, MaximumStep);
                Tick(duration);
                remaining -= duration;
                if (Motion.State == RunState.RunEnded)
                    break;
            }
            SimulationAdvanced?.Invoke();
        }

        private void Tick(double seconds)
        {
            if (Motion.State == RunState.Ready || Motion.State == RunState.Charging)
            {
                Motion.Step((float)seconds);
                return;
            }
            if (Motion.State == RunState.RunEnded)
                return;
            while (seconds > 1e-10)
            {
                if (Energy.IsEmpty) { FinishRun(); return; }
                if (Motion.State == RunState.Drilling)
                {
                    if (ContactTarget == null || !ContactTarget.IsSolid || ContactTarget.Durability == null ||
                        !DrillContacts.CentralTip(TipPosition(), ContactTarget.WorldBounds))
                    {
                        ContactTarget = null;
                        Motion.ResumeMoving();
                        continue;
                    }
                    var target = ContactTarget.Durability;
                    var drillingRate = DrillingEnergyRate(target.EnergyRate);
                    var duration = Math.Min(seconds, Math.Min(target.TimeToBreak(runSettings.DrillPower, runSettings.DamagePerPower),
                        Energy.TimeUntilEmpty(drillingRate)));
                    target.Drill(runSettings.DrillPower, runSettings.DamagePerPower, duration);
                    Energy.Consume(drillingRate, duration);
#if UNITY_EDITOR
                    Diagnostics.Record(RunState.Drilling, duration, drillingRate);
#endif
                    seconds -= duration;
                    if (Energy.IsEmpty) { FinishRun(); return; }
                    if (target.IsDestroyed)
                    {
                        ContactTarget = null;
                        Motion.ResumeMoving();
                    }
                }
                else
                {
                    var blocked = Motion.State == RunState.Blocked;
                    var rate = blocked ? runSettings.BlockedEnergyPerSecond : runSettings.MovingEnergyPerSecond;
                    var duration = Math.Min(seconds, Energy.TimeUntilEmpty(rate));
                    MoveWithContacts((float)duration, blocked);
                    Energy.Consume(rate, duration); // Exactly the state's rate, never movement + drilling together.
#if UNITY_EDITOR
                    Diagnostics.Record(blocked ? RunState.Blocked : RunState.Moving, duration, rate);
#endif
                    seconds -= duration;
                    if (Energy.IsEmpty) { FinishRun(); return; }
                }
            }
        }

        private void MoveWithContacts(float seconds, bool wasBlocked)
        {
            var before = Motion.Position;
            var desired = Motion.PredictPosition(seconds);
            var box = DrillContacts.WorldBounds(footprint);
            var dx = desired.x - before.x;
            foreach (var obstacle in registered)
                if (obstacle != null && obstacle.IsSolid)
                    dx = DrillContacts.SweepX(box, obstacle.WorldBounds, dx);
            var lateralClipped = Mathf.Abs(dx - (desired.x - before.x)) > 1e-6f;
            box.center += Vector3.right * dx;
            var wantedZ = wasBlocked ? 0.02f : Mathf.Max(0, desired.z - before.z);
            var dz = wantedZ;
            DrillObstacle hit = null;
            foreach (var obstacle in registered)
            {
                if (obstacle == null || !obstacle.IsSolid)
                    continue;
                var allowed = DrillContacts.SweepForward(box, obstacle.WorldBounds, dz);
                if (allowed < dz)
                {
                    dz = allowed;
                    hit = obstacle;
                }
            }
            Motion.Step(seconds);
            Motion.ResolvePosition(new Vector3(before.x + dx, before.y, wasBlocked ? before.z : before.z + dz), lateralClipped);
            transform.position = Motion.Position;
            if (hit != null)
            {
#if UNITY_EDITOR
                if (ContactTarget != hit) Diagnostics.Contacts++;
                if (Diagnostics.FirstContactSeconds < 0) Diagnostics.FirstContactSeconds = Diagnostics.Seconds + seconds;
#endif
                ContactTarget = hit;
                var tip = TipPosition();
                var drillable = hit.Durability != null && runSettings.DrillPower >= hit.Durability.Hardness &&
                                DrillContacts.CentralTip(tip, hit.WorldBounds);
                Motion.EnterContact(drillable);
            }
            else if (wasBlocked)
            {
                ContactTarget = null;
                Motion.ResumeMoving();
            }
        }

        private void FinishRun()
        {
            if (Motion.EndRun())
            {
#if UNITY_EDITOR
                Diagnostics.EndedByEnergy = Energy.IsEmpty;
#endif
                RunEnded?.Invoke();
            }
        }

        private Vector3 TipPosition()
        {
            if (drillTip != null) return drillTip.position;
            var box = DrillContacts.WorldBounds(footprint);
            return new Vector3(box.center.x, box.center.y, box.max.z);
        }

        private void ResetDrilling(DrillingSettings validatedSettings = null)
        {
            runSettings = validatedSettings ?? drilling.Snapshot();
            Energy = new RunEnergy(runSettings.EnergyCapacity);
#if UNITY_EDITOR
            Diagnostics = new RunDiagnostics();
#endif
            ContactTarget = null;
            foreach (var obstacle in registered)
                if (obstacle != null)
                    obstacle.Restore();
        }

        public void RegisterObstacle(DrillObstacle obstacle)
        {
            if (obstacle == null || registered.Contains(obstacle)) return;
            registered.Add(obstacle);
            obstacle.Broken += OnObstacleBroken;
            ObstacleRegistered?.Invoke(obstacle);
        }

        public void UnregisterObstacle(DrillObstacle obstacle)
        {
            if (obstacle == null || !registered.Remove(obstacle)) return;
            obstacle.Broken -= OnObstacleBroken;
            if (ContactTarget == obstacle)
            {
                ContactTarget = null;
                Motion?.ResumeMoving();
            }
            ObstacleUnregistered?.Invoke(obstacle);
        }

        private void OnObstacleBroken(DrillObstacle obstacle)
        {
#if UNITY_EDITOR
            Diagnostics.Destroyed++;
#endif
            ObstacleBroken?.Invoke(obstacle);
        }

        private void OnDestroy()
        {
            foreach (var obstacle in registered)
                if (obstacle != null) obstacle.Broken -= OnObstacleBroken;
            registered.Clear();
        }

        public void ResetRun()
        {
            if (ResetGuard != null && !ResetGuard()) return;
            var validatedSettings = drilling.Snapshot(); // Reject bad edits before changing an existing run.
            var nextMotion = new DrillMotion(settings, StartPosition);
            Motion = nextMotion;
            transform.position = StartPosition;
            ResetDrilling(validatedSettings);
            if (initializationFailed)
            {
                initializationFailed = false;
                enabled = true;
            }
            ResetPerformed?.Invoke();
        }

        public void ApplyAttributes(EffectiveAttributes attributes)
        {
            if (Motion.State != RunState.Ready && Motion.State != RunState.RunEnded)
                throw new InvalidOperationException("Apply progression only between runs.");
            var movement = settings.Snapshot(); var damage = drilling.Snapshot();
            movement.CruiseSpeed = attributes.Cruise; movement.MinimumLaunchBoost = attributes.MinimumBoost;
            movement.MaximumLaunchBoost = attributes.MaximumBoost; movement.ResumeAcceleration = attributes.Recovery;
            movement.MaximumSpeed = attributes.SpeedCeiling;
            damage.DrillPower = attributes.Power; damage.EnergyCapacity = attributes.Energy;
            movement.Validate(); damage.Validate();
            settings = movement; drilling = damage;
        }
        public float DrillingEnergyRate(float materialRate)
        {
            DrillRules.NonNegative(materialRate, nameof(materialRate));
            var tuning = runSettings ?? drilling;
            return tuning.DrillingBaseEnergyPerSecond + materialRate * tuning.DrillingMaterialScale;
        }
        public void ApplyConsumption(FormConsumption profile, float chargeSeconds, float boostDecay = 1.8f)
        {
            if (Motion.State != RunState.Ready && Motion.State != RunState.RunEnded) throw new InvalidOperationException("Apply consumption only between runs.");
            profile.Validate(); DrillMovementSettings.Positive(chargeSeconds, nameof(chargeSeconds)); DrillMovementSettings.Positive(boostDecay, nameof(boostDecay));
            var damage = drilling.Snapshot(); var movement = settings.Snapshot();
            damage.MovingEnergyPerSecond = profile.Moving; damage.BlockedEnergyPerSecond = profile.Blocked;
            damage.DrillingBaseEnergyPerSecond = profile.DrillingBase; damage.DrillingMaterialScale = profile.MaterialScale;
            movement.ChargeDuration = chargeSeconds; movement.BoostDecayRate = boostDecay; damage.Validate(); movement.Validate(); drilling = damage; settings = movement;
        }
    }
}

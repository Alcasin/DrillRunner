using System;
using UnityEngine;

namespace DrillRunner
{
    public enum RunState { Ready, Charging, Moving, Drilling, Blocked, RunEnded }

    [Serializable]
    public sealed class DrillMovementSettings
    {
        [Min(0.01f)] public float ChargeDuration = 1.5f;
        [Min(0.01f)] public float CruiseSpeed = 10f;
        [Min(0)] public float MinimumLaunchBoost = 2f;
        [Min(0)] public float MaximumLaunchBoost = 12f;
        [Min(0.01f)] public float MaximumSpeed = 24f;
        [Min(0.01f)] public float BoostDecayRate = 1.8f;
        [Min(0.01f)] public float TrackWidth = 6f;
        [Min(0.01f)] public float FootprintHalfWidth = 0.85f;
        public float TrackCenterX;
        [Min(0.01f)] public float SteeringWorldUnitsPerScreenWidth = 8f;
        [Min(0.01f)] public float SteeringResponse = 18f;
        [Min(0.01f)] public float ResumeAcceleration = 12f;

        public void Validate()
        {
            Positive(ChargeDuration, nameof(ChargeDuration));
            Positive(CruiseSpeed, nameof(CruiseSpeed));
            Positive(MaximumSpeed, nameof(MaximumSpeed));
            Positive(BoostDecayRate, nameof(BoostDecayRate));
            Positive(TrackWidth, nameof(TrackWidth));
            Positive(FootprintHalfWidth, nameof(FootprintHalfWidth));
            Positive(SteeringWorldUnitsPerScreenWidth, nameof(SteeringWorldUnitsPerScreenWidth));
            Positive(SteeringResponse, nameof(SteeringResponse));
            Positive(ResumeAcceleration, nameof(ResumeAcceleration));
            if (!Finite(TrackCenterX) || !Finite(MinimumLaunchBoost) || !Finite(MaximumLaunchBoost) ||
                MinimumLaunchBoost < 0 || MaximumLaunchBoost < MinimumLaunchBoost ||
                (double)CruiseSpeed + MaximumLaunchBoost > MaximumSpeed || TrackWidth <= 2 * FootprintHalfWidth ||
                !Finite(TrackCenterX - TrackWidth / 2) || !Finite(TrackCenterX + TrackWidth / 2))
                throw new ArgumentException("Invalid track footprint or launch speed range.");
        }

        internal DrillMovementSettings Snapshot()
        {
            Validate();
            return (DrillMovementSettings)MemberwiseClone();
        }

        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        internal static void Positive(float value, string name)
        {
            if (!Finite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(name, "Expected a finite positive value.");
        }
    }

    /// <summary>Time-based movement rules, independent of scene objects and input callbacks.</summary>
    public sealed class DrillMotion
    {
        private readonly DrillMovementSettings settings;
        private readonly Vector3 start;
        private double x, z, boost, chargeSeconds, cruiseVelocity;
        private float targetX;
        private bool pointerHeld;
        private bool steeringEnabled = true;

        public RunState State { get; private set; }
        public Vector3 Position => new Vector3((float)x, start.y, (float)z);
        public float ChargeNormalized => (float)(chargeSeconds / settings.ChargeDuration);
        public float ForwardSpeed => State == RunState.Moving ? (float)(cruiseVelocity + boost) : 0;
        public bool IsDragging => (State == RunState.Moving || State == RunState.Blocked) && pointerHeld && steeringEnabled;
        public float MinimumX => settings.TrackCenterX - settings.TrackWidth / 2 + settings.FootprintHalfWidth;
        public float MaximumX => settings.TrackCenterX + settings.TrackWidth / 2 - settings.FootprintHalfWidth;

        public DrillMotion(DrillMovementSettings settings, Vector3 start)
        {
            this.settings = (settings ?? throw new ArgumentNullException(nameof(settings))).Snapshot();
            if (!DrillMovementSettings.Finite(start) || start.x < MinimumX || start.x > MaximumX)
                throw new ArgumentOutOfRangeException(nameof(start), "Start must be finite and inside track bounds.");
            this.start = start;
            Reset();
        }

        public void Press()
        {
            if (pointerHeld || State == RunState.RunEnded)
                return;
            pointerHeld = true;
            targetX = (float)x;
            if (State == RunState.Ready)
                State = RunState.Charging;
        }

        public bool Release()
        {
            if (!pointerHeld)
                return false;
            pointerHeld = false;
            targetX = (float)x;
            if (State != RunState.Charging)
                return false;
            boost = Mathf.Lerp(settings.MinimumLaunchBoost, settings.MaximumLaunchBoost, ChargeNormalized);
            cruiseVelocity = settings.CruiseSpeed;
            State = RunState.Moving;
            return true;
        }

        public void CancelPointer()
        {
            pointerHeld = false;
            targetX = (float)x;
            if (State == RunState.Charging)
            {
                State = RunState.Ready;
                chargeSeconds = 0;
            }
        }

        public void SetSteeringEnabled(bool enabled)
        {
            steeringEnabled = enabled;
            targetX = (float)x;
        }

        // Delta is a per-update displacement: normalize by width, never multiply by dt.
        public void Drag(Vector2 pixelDelta, float screenWidth)
        {
            if (!DrillMovementSettings.Finite(pixelDelta.x) || !DrillMovementSettings.Finite(pixelDelta.y) ||
                !DrillMovementSettings.Finite(screenWidth) || screenWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(pixelDelta), "Expected finite delta and positive screen width.");
            if (!IsDragging)
                return;
            var desired = (double)targetX + (double)pixelDelta.x / screenWidth * settings.SteeringWorldUnitsPerScreenWidth;
            targetX = (float)Math.Max(MinimumX, Math.Min(MaximumX, desired));
        }

        public void Step(float deltaTime)
        {
            if (!DrillMovementSettings.Finite(deltaTime) || deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (State == RunState.Charging)
            {
                chargeSeconds = Math.Min(settings.ChargeDuration, chargeSeconds + deltaTime);
                return;
            }
            if ((State != RunState.Moving && State != RunState.Blocked) || deltaTime == 0)
                return;

            // Integrate exponential boost exactly, including distance during its decay.
            var decay = Math.Exp(-(double)settings.BoostDecayRate * deltaTime);
            var nextZ = State == RunState.Moving ? z + ForwardDistance(deltaTime) : z;
            if (nextZ > float.MaxValue || nextZ < -float.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Movement would exceed representable world coordinates.");
            z = nextZ;
            boost *= decay;
            if (State == RunState.Moving)
                cruiseVelocity = Math.Min(settings.CruiseSpeed, cruiseVelocity + settings.ResumeAcceleration * (double)deltaTime);
            if (IsDragging)
                x += (targetX - x) * (1 - Math.Exp(-(double)settings.SteeringResponse * deltaTime));
            x = Math.Max(MinimumX, Math.Min(MaximumX, x));
        }

        public Vector3 PredictPosition(float deltaTime)
        {
            if (!DrillMovementSettings.Finite(deltaTime) || deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            var nextX = IsDragging ? x + (targetX - x) * (1 - Math.Exp(-(double)settings.SteeringResponse * deltaTime)) : x;
            var nextZ = State == RunState.Moving ? z + ForwardDistance(deltaTime) : z;
            if (nextZ > float.MaxValue || nextZ < -float.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            return new Vector3((float)nextX, start.y, (float)nextZ);
        }

        private double ForwardDistance(float dt)
        {
            var rampTime = Math.Min(dt, (settings.CruiseSpeed - cruiseVelocity) / settings.ResumeAcceleration);
            return cruiseVelocity * rampTime + 0.5 * settings.ResumeAcceleration * rampTime * rampTime +
                   settings.CruiseSpeed * (dt - rampTime) +
                   boost * (1 - Math.Exp(-(double)settings.BoostDecayRate * dt)) / settings.BoostDecayRate;
        }

        internal void ResolvePosition(Vector3 position, bool stopLateralTarget)
        {
            if (!DrillMovementSettings.Finite(position) || position.y != start.y || position.x < MinimumX - 0.001f || position.x > MaximumX + 0.001f)
                throw new ArgumentOutOfRangeException(nameof(position));
            x = Mathf.Clamp(position.x, MinimumX, MaximumX);
            z = position.z;
            if (stopLateralTarget)
                targetX = (float)x;
        }

        public void EnterContact(bool drillable)
        {
            if (State != RunState.Moving && State != RunState.Blocked)
                return;
            State = drillable ? RunState.Drilling : RunState.Blocked;
            boost = cruiseVelocity = 0;
            if (drillable)
                targetX = (float)x;
        }

        public bool ResumeMoving()
        {
            if (State != RunState.Drilling && State != RunState.Blocked)
                return false;
            State = RunState.Moving;
            boost = cruiseVelocity = 0;
            targetX = (float)x;
            return true;
        }

        public bool EndRun()
        {
            if (State == RunState.RunEnded)
                return false;
            State = RunState.RunEnded;
            boost = cruiseVelocity = 0;
            CancelPointer();
            return true;
        }

        public void Reset()
        {
            x = start.x;
            z = start.z;
            targetX = start.x;
            boost = chargeSeconds = cruiseVelocity = 0;
            pointerHeld = false;
            steeringEnabled = true;
            State = RunState.Ready;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace DrillRunner
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class DrillPresentation : MonoBehaviour
    {
        [SerializeField] private DrillMotor motor;
        [SerializeField] private DrillVisuals visuals;
        [SerializeField] private WorkshopController flow;
        [SerializeField] private RunProgress run;
        [SerializeField] private PresentationAudio sounds;
        [SerializeField] private PresentationEffects effects;
        [SerializeField] private PresentationHud hud;
        [SerializeField] private int visualRevision;
        public int VisualRevision => visualRevision;
        private readonly HashSet<DrillObstacle> broken = new HashSet<DrillObstacle>();
        private GameObject boundVisual;
        private Transform rotor, housing;
        private Vector3 housingPosition;
        private Quaternion housingRotation;
        private float spin, time, launchResponse;
        private RunState previous = (RunState)(-1);
        private bool wasOpen;
        private long previousCopper, previousCoins;
        private bool focused = true, paused;
        public RunState State { get; private set; }
        public float Charge { get; private set; }
        public float RotorSpeed { get; private set; }
        public int DestructionCues { get; private set; }
        public PresentationAudio Sounds => sounds;
        public PresentationEffects Effects => effects;
        public PresentationHud Hud => hud;
        private void OnEnable()
        {
            motor.SimulationAdvanced += Sample; motor.ResetPerformed += ResetFeedback; motor.ObstacleBroken += OnBroken;
            motor.ObstacleUnregistered += Unregistered; flow.Feedback += WorkshopCue;
        }
        private void OnDisable()
        {
            motor.SimulationAdvanced -= Sample; motor.ResetPerformed -= ResetFeedback; motor.ObstacleBroken -= OnBroken;
            motor.ObstacleUnregistered -= Unregistered; flow.Feedback -= WorkshopCue;
            sounds.StopAll(); effects.StopAll(); RestorePose();
        }
        private void Unregistered(DrillObstacle obstacle) => broken.Remove(obstacle);
        private void ResetFeedback()
        {
            broken.Clear(); previousCopper = previousCoins = 0; previous = (RunState)(-1); launchResponse = 0; spin = 0;
            sounds.StopAll(); effects.StopAll(); hud.ClearToast(); RestorePose(); boundVisual = null;
        }
        private void WorkshopCue(WorkshopFeedback cue)
        {
            if (!focused || paused) return;
            if (cue == WorkshopFeedback.Opened) { sounds.StopAll(); effects.StopAll(); hud.ClearToast(); }
            if (cue == WorkshopFeedback.Purchased) sounds.Play(FeedbackCue.Upgrade);
            if (cue == WorkshopFeedback.EvolutionStarted) sounds.SetLoop((int)FeedbackCue.Charge, 1.35f);
            if (cue == WorkshopFeedback.EvolutionCompleted) { sounds.SetLoop(-1); sounds.Play(FeedbackCue.Evolution); }
        }
        private void OnBroken(DrillObstacle obstacle)
        {
            if (!focused || paused || flow.IsOpen || run.Rewards == null || run.Rewards.IsCompleted || !run.Rewards.IsActive || obstacle.Durability == null || !obstacle.Durability.IsDestroyed || !broken.Add(obstacle)) return;
            var copper = obstacle.Definition.RewardKind == ObstacleRewardKind.Copper;
            var confirmedCopper = copper && run.Rewards.Copper > previousCopper;
            effects.Burst(obstacle.Definition.MaterialId, obstacle.WorldBounds, confirmedCopper); sounds.Play(copper ? FeedbackCue.CopperBreak : FeedbackCue.Break); DestructionCues++;
            if (confirmedCopper)
            { hud.Reward(run.Rewards.Copper - previousCopper, true); sounds.Play(FeedbackCue.CopperReward); }
            else if (!copper && run.Rewards.ObstacleCoins > previousCoins) hud.Reward(run.Rewards.ObstacleCoins - previousCoins, false);
            previousCopper = run.Rewards.Copper; previousCoins = run.Rewards.ObstacleCoins;
        }
        public void Sample()
        {
            if (motor.Motion == null) return;
            State = motor.Motion.State; Charge = motor.Motion.ChargeNormalized;
            if (!focused || paused) { sounds.StopAll(); effects.StopAll(); return; }
            var active = !flow.IsOpen; var target = motor.ContactTarget;
            var drilling = active && State == RunState.Drilling && target != null && target.IsSolid;
            var point = motor.transform.position + new Vector3(0, 0.95f, 1.82f);
            effects.SetDrilling(drilling, target != null ? target.Definition != null ? target.Definition.MaterialId : "stone" : "wood", point);
            if (drilling && target.Durability != null)
            { var appearance = target.GetComponent<ObstacleAppearance>(); if (appearance != null) appearance.SetDamage(target.Durability.Fraction); }
            if (State != previous || wasOpen != flow.IsOpen)
            {
                if (active && State == RunState.Moving && (previous == RunState.Charging || previous == RunState.Ready || previous == (RunState)(-1)))
                { sounds.Play(FeedbackCue.Launch); launchResponse = 0.25f; }
                if (active && State == RunState.Blocked) sounds.Play(FeedbackCue.Blocked);
                if (active && State == RunState.RunEnded) { launchResponse = 0.15f; effects.StopAll(); hud.ClearToast(); sounds.Play(FeedbackCue.Finish); }
                previous = State; wasOpen = flow.IsOpen;
            }
            var loop = flow.IsEvolving ? (int)FeedbackCue.Charge : !active ? -1 : State == RunState.Charging ? (int)FeedbackCue.Charge : drilling ? (int)FeedbackCue.Drill : -1;
            var materialPitch = target == null || target.Definition == null ? 1 : target.Definition.MaterialId == "wood" ? 1.1f : target.Definition.MaterialId == "brick" ? 0.95f : target.Definition.MaterialId == "stone" ? 0.8f : 1.2f;
            sounds.SetLoop(loop, flow.IsEvolving ? 1.35f : State == RunState.Charging ? 0.75f + Charge * 0.65f : materialPitch);
        }
        private void LateUpdate()
        {
            Sample(); var active = visuals.ActiveVisual;
            if (active != boundVisual)
            {
                RestorePose(); boundVisual = active; rotor = active != null ? active.transform.Find("Drill Rotor") : null;
                housing = active != null ? active.transform.Find("Motor Housing") : null;
                if (housing != null) { housingPosition = housing.localPosition; housingRotation = housing.localRotation; }
            }
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f); time += dt;
            RotorSpeed = flow.IsOpen ? 70 : State == RunState.Charging ? 120 + Charge * 1200 : State == RunState.Drilling ? 1500 : State == RunState.Moving ? 750 : State == RunState.Ready ? 60 : 0;
            spin = (spin + RotorSpeed * dt) % 360;
            if (rotor != null) rotor.localRotation = Quaternion.AngleAxis(spin, Vector3.forward);
            launchResponse = Mathf.Max(0, launchResponse - dt);
            if (housing != null)
            {
                var working = State == RunState.Drilling && !flow.IsOpen;
                housing.localPosition = housingPosition + new Vector3(working ? Mathf.Sin(time * 90) * 0.006f : 0, Mathf.Sin(time * 5) * 0.004f, 0);
                housing.localRotation = housingRotation * Quaternion.Euler(launchResponse * 6 - (State == RunState.Charging ? Charge * 1.5f : 0), 0, 0);
            }
        }
        private void RestorePose()
        {
            if (housing != null) { housing.localPosition = housingPosition; housing.localRotation = housingRotation; }
            if (rotor != null) rotor.localRotation = Quaternion.identity;
        }
        private void OnApplicationFocus(bool focus) { focused = focus; if (!focus) { sounds.StopAll(); effects.StopAll(); } }
        private void OnApplicationPause(bool pause) { paused = pause; if (pause) { sounds.StopAll(); effects.StopAll(); } }
    }
}

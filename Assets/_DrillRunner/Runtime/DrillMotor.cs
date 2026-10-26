using System;
using UnityEngine;

namespace DrillRunner
{
    [DisallowMultipleComponent]
    public sealed class DrillMotor : MonoBehaviour
    {
        [SerializeField] private DrillMovementSettings settings = new DrillMovementSettings();
        public DrillMovementSettings Settings => settings;
        public DrillMotion Motion { get; private set; }
        public Vector3 StartPosition { get; private set; }
        public event Action ResetPerformed;

        private void Awake()
        {
            StartPosition = transform.position;
            Motion = new DrillMotion(settings, StartPosition);
        }

        private void Update()
        {
            Motion.Step(Time.deltaTime);
            transform.position = Motion.Position;
        }

        public void ResetRun()
        {
            Motion.Reset();
            transform.position = StartPosition;
            ResetPerformed?.Invoke();
        }
    }
}

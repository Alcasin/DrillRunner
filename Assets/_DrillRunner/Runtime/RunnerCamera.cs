using System;
using UnityEngine;

namespace DrillRunner
{
    [DisallowMultipleComponent]
    public sealed class RunnerCamera : MonoBehaviour
    {
        [SerializeField] private DrillMotor target;
        [SerializeField] private Vector3 followOffset = new Vector3(0, 6, -9);
        [SerializeField, Min(0.01f)] private float forwardResponsiveness = 8f;
        [SerializeField, Min(0.01f)] private float horizontalResponsiveness = 5f;
        [SerializeField, Range(0, 1)] private float horizontalFollow = 0.25f;
        [SerializeField, Min(0)] private float lookAhead = 6f;
        [SerializeField] private float lookHeight = 0.65f;
        public DrillMotor Target => target;
        public Vector3 FollowOffset => followOffset;

        private void OnEnable()
        {
            if (target != null)
                target.ResetPerformed += Snap;
        }

        private void Start() => Snap();

        private void OnDisable()
        {
            if (target != null)
                target.ResetPerformed -= Snap;
        }

        private Vector3 DesiredPosition()
        {
            var position = target.transform.position;
            position.x = Mathf.Lerp(target.StartPosition.x, position.x, horizontalFollow);
            return position + followOffset;
        }

        public void Snap()
        {
            if (target == null)
                return;
            transform.position = DesiredPosition();
            transform.rotation = Quaternion.LookRotation(new Vector3(0, lookHeight - followOffset.y, lookAhead - followOffset.z));
        }

        private void LateUpdate()
        {
            if (target == null)
                return;
            var desired = DesiredPosition();
            var position = transform.position;
            position.x = Damp(position.x, desired.x, horizontalResponsiveness, Time.deltaTime);
            position.y = Damp(position.y, desired.y, forwardResponsiveness, Time.deltaTime);
            position.z = Damp(position.z, desired.z, forwardResponsiveness, Time.deltaTime);
            transform.position = position;
        }

        public static float Damp(float current, float desired, float responsiveness, float deltaTime)
        {
            DrillMovementSettings.Positive(responsiveness, nameof(responsiveness));
            if (!DrillMovementSettings.Finite(current) || !DrillMovementSettings.Finite(desired) ||
                !DrillMovementSettings.Finite(deltaTime) || deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            return (float)((double)desired + ((double)current - desired) * Math.Exp(-(double)responsiveness * deltaTime));
        }
    }
}

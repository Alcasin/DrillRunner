using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace DrillRunner
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class DrillPointerInput : MonoBehaviour
    {
        [SerializeField] private DrillMotor motor;
        [SerializeField] private InputActionAsset actionsSource;
        [SerializeField] private GraphicRaycaster uiRaycaster;
        private InputActionAsset actions;
        private InputAction charge, steer;
        private InputDevice owner;
        private TouchControl ownedTouch;
        private int ownedTouchId;
        private bool live, focused = true, pendingRelease, skipDelta, waitForNeutral;
        private PointerEventData uiPointer;
        private EventSystem uiEventSystem;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>(8);

        public DrillMotor Motor => motor;
        public InputActionAsset ActionsSource => actionsSource;
        public GraphicRaycaster UiRaycaster => uiRaycaster;

        private void OnEnable()
        {
            if (motor == null || actionsSource == null)
            {
                Debug.LogError("DrillPointerInput needs a motor and DrillRunner input asset.", this);
                enabled = false;
                return;
            }
            actions = Instantiate(actionsSource);
            charge = actions.FindAction("Gameplay/Charge", true);
            steer = actions.FindAction("Gameplay/Steer", true);
            charge.performed += OnPressed;
            charge.canceled += OnCanceled;
            InputSystem.onDeviceChange += OnDeviceChange;
            live = true;
            waitForNeutral = true;
            actions.Enable();
        }

        private void OnDisable()
        {
            live = false; // Disabling an action is cancellation, never a launch release.
            CancelGesture();
            InputSystem.onDeviceChange -= OnDeviceChange;
            if (actions == null)
                return;
            charge.performed -= OnPressed;
            charge.canceled -= OnCanceled;
            actions.Disable();
            Destroy(actions);
            actions = null;
        }

        private void OnPressed(InputAction.CallbackContext context)
        {
            if (!live || !focused || waitForNeutral || owner != null || motor.Motion == null)
                return;
            Vector2 position;
            if (context.control.device is Mouse mouse)
                position = mouse.position.ReadValue();
            else if (context.control.device is Touchscreen screen)
            {
                // The primary-touch callback can precede the physical touch-slot update.
                // Capture its ID now and resolve the slot after the input update finishes.
                ownedTouchId = screen.primaryTouch.touchId.ReadValue();
                if (screen.primaryTouch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Began)
                    return;
                position = screen.primaryTouch.position.ReadValue();
            }
            else
                return;

            if (IsOverUi(position))
            {
                ownedTouch = null;
                return;
            }
            owner = context.control.device;
            pendingRelease = false;
            skipDelta = true;
            motor.Motion.Press();
        }

        private void OnCanceled(InputAction.CallbackContext context)
        {
            if (live && focused && owner == context.control.device)
                pendingRelease = true;
        }

        private void Update()
        {
            if (!live || !focused)
                return;
            if (waitForNeutral)
            {
                foreach (var control in charge.controls)
                    if (control is ButtonControl button && button.isPressed)
                        return;
                waitForNeutral = false;
            }
            if (owner == null)
                return;

            if (owner is Touchscreen screen)
            {
                if (ownedTouch == null)
                    foreach (var touch in screen.touches)
                        if (touch.touchId.ReadValue() == ownedTouchId)
                            ownedTouch = touch;
                if (ownedTouch == null || ownedTouch.touchId.ReadValue() != ownedTouchId)
                {
                    CancelGesture();
                    return;
                }
            }

            // A primary touch may stay active while another finger is down. Own the
            // original finger so lifting/canceling it never transfers the drag.
            if (ownedTouch != null)
            {
                var phase = ownedTouch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    CancelGesture();
                    return;
                }
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
                    pendingRelease = true;
            }
            // Another device may keep the shared Button action actuated after the
            // owned mouse releases. Gesture ownership follows the physical pointer.
            if (owner is Mouse ownedMouse && !ownedMouse.leftButton.isPressed)
                pendingRelease = true;
            if (pendingRelease)
            {
                var realRelease = ownedTouch != null
                    ? ownedTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended
                    : owner is Mouse mouse && !mouse.leftButton.isPressed;
                if (realRelease)
                    motor.Motion.Release();
                else
                    motor.Motion.CancelPointer();
                ClearPointer();
                return;
            }
            if (skipDelta)
            {
                skipDelta = false;
                return;
            }
            if (steer.activeControl != null && steer.activeControl.device == owner)
                motor.Motion.Drag(steer.ReadValue<Vector2>(), Mathf.Max(1, Screen.width));
        }

        private bool IsOverUi(Vector2 position)
        {
            if (uiRaycaster == null || EventSystem.current == null)
                return false;
            if (uiPointer == null || uiEventSystem != EventSystem.current)
            {
                uiEventSystem = EventSystem.current;
                uiPointer = new PointerEventData(EventSystem.current);
            }
            uiPointer.position = position;
            uiHits.Clear();
            uiRaycaster.Raycast(uiPointer, uiHits);
            return uiHits.Count != 0;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (owner == device && (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected ||
                change == InputDeviceChange.Disabled || change == InputDeviceChange.SoftReset || change == InputDeviceChange.HardReset))
                CancelGesture();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
            if (!hasFocus)
                CancelGesture();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                CancelGesture();
        }

        private void ClearPointer()
        {
            owner = null;
            ownedTouch = null;
            ownedTouchId = 0;
            pendingRelease = skipDelta = false;
        }

        private void CancelGesture()
        {
            motor?.Motion?.CancelPointer();
            ClearPointer();
            waitForNeutral = true;
        }

        public void Restart()
        {
            CancelGesture();
            motor.ResetRun();
        }
    }
}

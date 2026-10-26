using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DrillRunner.Tests.EditMode
{
    // InputTestFixture isolates synthetic devices from the user's real Editor input.
    public class FoundationInputTests : InputTestFixture
    {
        private InputActionAsset actions;
        private InputAction charge;
        private InputAction steer;

        public override void Setup()
        {
            base.Setup();
            var source = AssetDatabase.LoadAssetAtPath<InputActionAsset>(FoundationSetup.InputPath);
            Assert.That(source, Is.Not.Null);
            actions = Object.Instantiate(source);
            charge = actions.FindAction("Gameplay/Charge", true);
            steer = actions.FindAction("Gameplay/Steer", true);
            actions.Enable();
        }

        public override void TearDown()
        {
            if (actions != null)
            {
                actions.Disable();
                Object.DestroyImmediate(actions);
            }
            base.TearDown();
        }

        [Test]
        public void MouseCanPressHoldDragAndRelease()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var presses = 0;
            var releases = 0;
            charge.performed += _ => presses++;
            charge.canceled += _ => releases++;

            Press(mouse.leftButton);
            Assert.That(charge.IsPressed(), Is.True);
            InputSystem.Update();
            Assert.That(charge.IsPressed(), Is.True);
            Set(mouse.delta, new Vector2(30, -5));
            Assert.That(steer.ReadValue<Vector2>().x, Is.EqualTo(30));
            Release(mouse.leftButton);
            Assert.That(charge.IsPressed(), Is.False);
            Assert.That(presses, Is.EqualTo(1));
            Assert.That(releases, Is.EqualTo(1));
        }

        [Test]
        public void PrimaryTouchCanPressHoldDragAndRelease()
        {
            var screen = InputSystem.AddDevice<Touchscreen>();
            var presses = 0;
            var releases = 0;
            charge.performed += _ => presses++;
            charge.canceled += _ => releases++;

            BeginTouch(1, new Vector2(100, 200), screen: screen);
            Assert.That(charge.IsPressed(), Is.True);
            InputSystem.Update();
            Assert.That(charge.IsPressed(), Is.True);
            MoveTouch(1, new Vector2(124, 200), new Vector2(24, 0), screen: screen);
            Assert.That(steer.ReadValue<Vector2>().x, Is.EqualTo(24));
            EndTouch(1, new Vector2(124, 200), screen: screen);
            Assert.That(charge.IsPressed(), Is.False);
            Assert.That(presses, Is.EqualTo(1));
            Assert.That(releases, Is.EqualTo(1));
        }

        [Test]
        public void DisablingActionsCancelsAChargeWithoutChangingTheSourceAsset()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var canceled = false;
            charge.canceled += _ => canceled = true;
            Press(mouse.leftButton);
            actions.Disable();
            Assert.That(canceled, Is.True);
            Assert.That(charge.IsPressed(), Is.False);
            Assert.That(AssetDatabase.LoadAssetAtPath<InputActionAsset>(FoundationSetup.InputPath).enabled, Is.False);
        }
    }
}

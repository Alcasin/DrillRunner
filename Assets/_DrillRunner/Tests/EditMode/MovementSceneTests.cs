using System.IO;
using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DrillRunner.Tests.EditMode
{
    public class MovementSceneTests
    {
        [Test]
        public void GameplayHasOneWiredDrillCameraHudAndUiInputWithACompleteFootprint()
        {
            var scene = EditorSceneManager.OpenPreviewScene(FoundationSetup.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects();
                var motor = roots.SelectMany(r => r.GetComponentsInChildren<DrillMotor>(true)).Single();
                var input = motor.GetComponent<DrillPointerInput>();
                Assert.That(input.Motor, Is.SameAs(motor));
                Assert.That(input.ActionsSource, Is.Not.Null);
                Assert.That(input.UiRaycaster, Is.Not.Null);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<RunnerCamera>(true)).Single().Target, Is.SameAs(motor));
                var hud = roots.SelectMany(r => r.GetComponentsInChildren<PrototypeHud>(true)).Single();
                Assert.That(hud.Input, Is.SameAs(input));
                Assert.That(hud.RestartButton, Is.Not.Null);
                Assert.That(hud.ChargeFill, Is.Not.Null);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<InputSystemUIInputModule>(true)).Single().actionsAsset, Is.Not.Null);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<Rigidbody>(true)), Is.Empty);
                var collider = motor.GetComponent<BoxCollider>();
                Assert.That(collider.size.x / 2, Is.EqualTo(motor.Settings.FootprintHalfWidth));
                foreach (var renderer in motor.GetComponentsInChildren<MeshRenderer>())
                {
                    Assert.That(renderer.bounds.min.x, Is.GreaterThanOrEqualTo(collider.bounds.min.x - 0.001f));
                    Assert.That(renderer.bounds.max.x, Is.LessThanOrEqualTo(collider.bounds.max.x + 0.001f));
                    Assert.That(renderer.bounds.min.y, Is.GreaterThanOrEqualTo(-0.001f));
                }
                Assert.That(motor.transform.forward, Is.EqualTo(Vector3.forward));
                Assert.That(roots.Single(r => r.name == "Track Reference (Temporary)").transform.localScale.z,
                    Is.GreaterThanOrEqualTo(MovementPrototypeSetup.TrackLength));
                Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(motor.gameObject), Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void RepeatedM1SetupPreservesCustomObjectsSceneBytesAndExistingAssetGuids()
        {
            const string copy = "Assets/_DrillRunner/Scenes/M1SetupTestTemporary.unity";
            Assert.That(AssetDatabase.CopyAsset(FoundationSetup.ScenePath, copy), Is.True);
            var original = File.ReadAllBytes(FoundationSetup.ScenePath);
            var prefab = File.ReadAllBytes(MovementPrototypeSetup.PrefabPath);
            try
            {
                var scene = EditorSceneManager.OpenScene(copy, OpenSceneMode.Additive);
                try
                {
                    var sentinel = new GameObject("User Authored Sentinel");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sentinel, scene);
                    sentinel.transform.position = new Vector3(17, 3, 8);
                    EditorSceneManager.SaveScene(scene);
                    var before = File.ReadAllBytes(copy);
                    var guid = AssetDatabase.AssetPathToGUID(copy);
                    MovementPrototypeSetup.SetupScene(copy);
                    MovementPrototypeSetup.SetupScene(copy);
                    Assert.That(sentinel.transform.position, Is.EqualTo(new Vector3(17, 3, 8)));
                    Assert.That(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DrillMotor>()).Count(), Is.EqualTo(1));
                    Assert.That(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RunnerCamera>()).Count(), Is.EqualTo(1));
                    Assert.That(File.ReadAllBytes(copy), Is.EqualTo(before));
                    Assert.That(AssetDatabase.AssetPathToGUID(copy), Is.EqualTo(guid));
                    EditorSceneManager.MarkSceneDirty(scene);
                    Assert.Throws<System.InvalidOperationException>(() => MovementPrototypeSetup.SetupScene(copy));
                    Assert.That(scene.isDirty, Is.True);
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            finally { AssetDatabase.DeleteAsset(copy); }
            Assert.That(File.ReadAllBytes(FoundationSetup.ScenePath), Is.EqualTo(original));
            Assert.That(File.ReadAllBytes(MovementPrototypeSetup.PrefabPath), Is.EqualTo(prefab));
        }
    }
}

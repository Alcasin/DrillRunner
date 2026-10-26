using System.IO;
using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrillRunner.Tests.EditMode
{
    public class DrillingSceneTests
    {
        [Test]
        public void EncounterHasThreeValidMaterialsFourSolidObstaclesAndWiredFeedback()
        {
            var scene = EditorSceneManager.OpenPreviewScene(FoundationSetup.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects();
                var motor = roots.SelectMany(r => r.GetComponentsInChildren<DrillMotor>(true)).Single();
                var obstacles = roots.SelectMany(r => r.GetComponentsInChildren<DrillObstacle>(true)).ToArray();
                Assert.That(obstacles.Length, Is.EqualTo(4));
                Assert.That(motor.Obstacles.Distinct().Count(), Is.EqualTo(4));
                Assert.That(motor.Obstacles, Is.EquivalentTo(obstacles));
                Assert.That(obstacles.Select(o => o.Definition.MaterialId).Distinct(), Is.EquivalentTo(new[] { "wood", "brick", "stone" }));
                foreach (var obstacle in obstacles)
                {
                    obstacle.Definition.Validate();
                    var bounds = DrillContacts.WorldBounds(obstacle.GetComponent<BoxCollider>());
                    Assert.That(bounds.min.y, Is.EqualTo(0).Within(1e-5));
                    Assert.That(bounds.size.x + motor.Settings.FootprintHalfWidth * 4, Is.LessThan(motor.Settings.TrackWidth));
                    Assert.That(obstacle.GetComponent<BoxCollider>().isTrigger, Is.False);
                    var canDrill = motor.Drilling.DrillPower >= obstacle.Definition.Hardness;
                    Assert.That(canDrill, Is.EqualTo(obstacle.Definition.MaterialId != "stone"));
                }
                var box = motor.GetComponent<BoxCollider>();
                Assert.That(motor.transform.Find("Drill Contact Tip").localPosition.z, Is.EqualTo(box.center.z + box.size.z / 2));
                var hud = roots.SelectMany(r => r.GetComponentsInChildren<PrototypeHud>(true)).Single();
                Assert.That(hud.EnergyFill, Is.Not.Null); Assert.That(hud.DurabilityFill, Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void RepeatedM2SetupPreservesCustomWorkGuidsAndScenePrefabAndMaterialBytes()
        {
            const string copy = "Assets/_DrillRunner/Scenes/M2SetupTestTemporary.unity";
            Assert.That(AssetDatabase.CopyAsset(FoundationSetup.ScenePath, copy), Is.True);
            var paths = new[] { MovementPrototypeSetup.PrefabPath, FoundationSetup.ScenePath,
                FoundationSetup.InputPath, FoundationSetup.Root + "/ScriptableObjects/Wood.asset",
                FoundationSetup.Root + "/ScriptableObjects/Brick.asset", FoundationSetup.Root + "/ScriptableObjects/Stone.asset" };
            var contents = paths.Select(File.ReadAllBytes).ToArray();
            try
            {
                var scene = EditorSceneManager.OpenScene(copy, OpenSceneMode.Additive);
                try
                {
                    var sentinel = new GameObject("M2 User Work");
                    SceneManager.MoveGameObjectToScene(sentinel, scene);
                    sentinel.transform.position = new Vector3(20, 4, 2);
                    EditorSceneManager.SaveScene(scene);
                    var before = File.ReadAllBytes(copy); var guid = AssetDatabase.AssetPathToGUID(copy);
                    DrillingPrototypeSetup.SetupScene(copy); DrillingPrototypeSetup.SetupScene(copy);
                    Assert.That(sentinel.transform.position, Is.EqualTo(new Vector3(20, 4, 2)));
                    Assert.That(File.ReadAllBytes(copy), Is.EqualTo(before));
                    Assert.That(AssetDatabase.AssetPathToGUID(copy), Is.EqualTo(guid));
                    Assert.That(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DrillObstacle>(true)).Count(), Is.EqualTo(4));
                    EditorSceneManager.MarkSceneDirty(scene);
                    Assert.Throws<System.InvalidOperationException>(() => DrillingPrototypeSetup.SetupScene(copy));
                    Assert.That(scene.isDirty, Is.True);
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            finally { AssetDatabase.DeleteAsset(copy); }
            for (var i = 0; i < paths.Length; i++) Assert.That(File.ReadAllBytes(paths[i]), Is.EqualTo(contents[i]), paths[i]);
        }
    }
}

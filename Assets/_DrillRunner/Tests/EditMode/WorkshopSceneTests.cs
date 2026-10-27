using System;
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
    public sealed class WorkshopSceneTests
    {
        [Test] public void GameplayHasOneWiredWorkshopAndAValidPocketConfiguration()
        {
            var scene = EditorSceneManager.OpenPreviewScene(FoundationSetup.ScenePath);
            try
            {
                var flow = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WorkshopController>(true)).Single();
                flow.Configuration.Validate(); Assert.That(flow.Run, Is.Not.Null); Assert.That(flow.View, Is.Not.Null);
                Assert.That(flow.View.Panel.activeSelf, Is.False, "Editor retains direct legacy gameplay entry.");
                Assert.That(flow.View.Rows.Length, Is.EqualTo(3)); Assert.That(flow.View.PurchaseButtons.All(b => b != null), Is.True);
                Assert.That(flow.View.StartButton, Is.Not.Null); Assert.That(flow.View.OpenButton, Is.Not.Null); Assert.That(flow.View.SaveButton, Is.Not.Null);
                Assert.That(flow.View.GameplayGroups.Length, Is.GreaterThan(0)); Assert.That(flow.View.GameplayGroups.All(g => g != null), Is.True);
                Assert.That(flow.Configuration.Power.Values.Last(), Is.GreaterThanOrEqualTo(9));
                Assert.That(flow.Run.Track.Catalog.InitialPower, Is.EqualTo(5), "M3 safety remains certified for the base drill.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        [Test] public void M4SetupRerunsPreserveUserWorkGuidsSceneAndTunedConfiguration()
        {
            const string copy = "Assets/_DrillRunner/Scenes/M4SetupTemporary.unity";
            var original = File.ReadAllBytes(FoundationSetup.ScenePath); var config = File.ReadAllBytes(WorkshopSetup.ConfigPath);
            Assert.That(AssetDatabase.CopyAsset(FoundationSetup.ScenePath, copy), Is.True);
            try
            {
                var scene = EditorSceneManager.OpenScene(copy, OpenSceneMode.Additive);
                try
                {
                    var sentinel = new GameObject("User M4 Work"); SceneManager.MoveGameObjectToScene(sentinel, scene); sentinel.transform.position = new Vector3(8, 3, 17);
                    EditorSceneManager.SaveScene(scene); var bytes = File.ReadAllBytes(copy); var guid = AssetDatabase.AssetPathToGUID(copy);
                    WorkshopSetup.SetupScene(copy); WorkshopSetup.SetupScene(copy);
                    Assert.That(File.ReadAllBytes(copy), Is.EqualTo(bytes)); Assert.That(AssetDatabase.AssetPathToGUID(copy), Is.EqualTo(guid));
                    Assert.That(sentinel.transform.position, Is.EqualTo(new Vector3(8, 3, 17)));
                    EditorSceneManager.MarkSceneDirty(scene); Assert.Throws<InvalidOperationException>(() => WorkshopSetup.SetupScene(copy));
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            finally { AssetDatabase.DeleteAsset(copy); }
            Assert.That(File.ReadAllBytes(FoundationSetup.ScenePath), Is.EqualTo(original)); Assert.That(File.ReadAllBytes(WorkshopSetup.ConfigPath), Is.EqualTo(config));
        }
    }
}

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
    public sealed class EvolutionSceneTests
    {
        [Test] public void TwoAuthoredFormsHaveDistinctModelsWithinTheOriginalFootprintAndOneWiredController()
        {
            var root = AssetDatabase.LoadAssetAtPath<ProgressionConfig>(WorkshopSetup.ConfigPath); root.Validate();
            Assert.That(root.NextForm.FormId, Is.EqualTo(ProgressionConfig.WorkshopForm)); Assert.That(root.NextForm.NextForm, Is.Null);
            Assert.That(root.EvolutionCopper, Is.EqualTo(12)); Assert.That(root.VisualPrefab, Is.Not.SameAs(root.NextForm.VisualPrefab));
            foreach (var form in new[] { root, root.NextForm })
            { EvolutionSetup.ValidateVisual(form.VisualPrefab); Assert.That(form.PreviewImage, Is.Not.Null); }
            Assert.That(root.NextForm.VisualPrefab.transform.Find("Motor Housing"), Is.Not.Null);
            var scene = EditorSceneManager.OpenPreviewScene(FoundationSetup.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects(); var flow = roots.SelectMany(r => r.GetComponentsInChildren<WorkshopController>(true)).Single();
                Assert.That(flow.Visuals, Is.Not.Null); Assert.That(flow.View.EvolveButton, Is.Not.Null); Assert.That(flow.View.Requirements, Is.Not.Null);
                Assert.That(flow.View.FormPreview, Is.Not.Null); Assert.That(flow.View.FormName, Is.Not.Null);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<DrillMotor>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Count(), Is.EqualTo(1));
                Assert.That(flow.Run.Motor.GetComponent<BoxCollider>().size, Is.EqualTo(new Vector3(1.7f, 1.3f, 3)));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        [Test] public void ExistingCatalogPairsRetainSteeringClearanceAtTheConservativeNewSpeedCeiling()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TrackCatalog>(ModularTrackSetup.CatalogPath);
            foreach (var a in catalog.Variants)
            foreach (var b in catalog.Variants)
            {
                var end = a.ReadPlacements().Select(p => p.Bounds.max.z).DefaultIfEmpty(0).Max();
                var begin = b.ReadPlacements().Select(p => p.Bounds.min.z).DefaultIfEmpty(40).Min();
                Assert.That(4.3 * Math.Exp(-18 * (40 - end + begin - 3) / 26), Is.LessThan(TrackSafety.Margin));
            }
        }
        [Test] public void M5SetupIsIdempotentAndPreservesCustomWorkAssetBytesAndGuids()
        {
            const string copy = "Assets/_DrillRunner/Scenes/M5SetupTemporary.unity";
            var paths = new[] { FoundationSetup.ScenePath, WorkshopSetup.ConfigPath, EvolutionSetup.WorkshopPath, EvolutionSetup.PocketVisualPath, EvolutionSetup.WorkshopVisualPath };
            var bytes = paths.Select(File.ReadAllBytes).ToArray(); Assert.That(AssetDatabase.CopyAsset(FoundationSetup.ScenePath, copy), Is.True);
            try
            {
                var scene = EditorSceneManager.OpenScene(copy, OpenSceneMode.Additive);
                try
                {
                    var sentinel = new GameObject("User M5 Work"); SceneManager.MoveGameObjectToScene(sentinel, scene); sentinel.transform.position = new Vector3(3, 7, 11);
                    EditorSceneManager.SaveScene(scene); var before = File.ReadAllBytes(copy); var guid = AssetDatabase.AssetPathToGUID(copy);
                    EvolutionSetup.SetupScene(copy); EvolutionSetup.SetupScene(copy);
                    Assert.That(File.ReadAllBytes(copy), Is.EqualTo(before)); Assert.That(AssetDatabase.AssetPathToGUID(copy), Is.EqualTo(guid));
                    Assert.That(sentinel.transform.position, Is.EqualTo(new Vector3(3, 7, 11)));
                    EditorSceneManager.MarkSceneDirty(scene); Assert.Throws<InvalidOperationException>(() => EvolutionSetup.SetupScene(copy));
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            finally { AssetDatabase.DeleteAsset(copy); }
            for (var i = 0; i < paths.Length; i++) Assert.That(File.ReadAllBytes(paths[i]), Is.EqualTo(bytes[i]));
        }
    }
}

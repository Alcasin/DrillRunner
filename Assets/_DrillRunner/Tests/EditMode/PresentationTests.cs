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
    public sealed class PresentationTests
    {
        [Test] public void RefinedFormsKeepTheFootprintHaveDistinctMaterialsAndBoundedMeshComplexity()
        {
            foreach (var path in new[] { EvolutionSetup.PocketVisualPath, EvolutionSetup.WorkshopVisualPath })
            {
                var visual = AssetDatabase.LoadAssetAtPath<GameObject>(path); EvolutionSetup.ValidateVisual(visual);
                Assert.That(visual.transform.Find("Drill Rotor"), Is.Not.Null); Assert.That(visual.transform.Find("Motor Housing"), Is.Not.Null);
                var renderers = visual.GetComponentsInChildren<MeshRenderer>(); Assert.That(renderers.Length, Is.LessThanOrEqualTo(12));
                Assert.That(renderers.Select(r => r.sharedMaterial).Distinct().Count(), Is.GreaterThanOrEqualTo(5));
                Assert.That(visual.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3), Is.LessThan(20000));
            }
        }
        [Test] public void OriginalLocalSoundsHaveSafePeakLevelsAndCompleteCueCoverage()
        {
            var theme = AssetDatabase.LoadAssetAtPath<PresentationTheme>(PresentationAssets.ThemePath); theme.Validate();
            Assert.That(theme.Sounds.Distinct().Count(), Is.EqualTo(10));
            foreach (var clip in theme.Sounds)
            {
                Assert.That(clip.channels, Is.EqualTo(1)); Assert.That(clip.frequency, Is.EqualTo(22050)); Assert.That(clip.length, Is.InRange(0.2f, 1.01f));
                var samples = new float[clip.samples]; Assert.That(clip.GetData(samples, 0), Is.True);
                var peak = samples.Max(s => Mathf.Abs(s)); Assert.That(peak, Is.InRange(0.1f, 0.601f));
                Assert.That(Mathf.Abs(samples.Average()), Is.LessThan(0.02f));
                Assert.That(Mathf.Abs(samples[0]), Is.LessThan(0.01f)); Assert.That(Mathf.Abs(samples[samples.Length - 1]), Is.LessThan(0.01f));
            }
        }
        [Test] public void DecorativeSectionsAndMaterialDetailsDoNotAddCollidersOrChangeSafeRoutes()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TrackCatalog>(ModularTrackSetup.CatalogPath);
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { ModularTrackSetup.SectionFolder }))
            {
                var section = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)).GetComponent<TrackSection>();
                Assert.That(SectionSequence.IsSafe(section, catalog, 0.85f), Is.True, section.SectionId);
                Assert.That(section.transform.Find("M6 Yard Details").GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(section.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1 + section.Obstacles.Length));
                foreach (var obstacle in section.Obstacles)
                {
                    Assert.That(obstacle.GetComponent<ObstacleAppearance>(), Is.Not.Null);
                    Assert.That(obstacle.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1));
                }
            }
        }
        [Test] public void GameplayHasOnePresentationDriverFiveAudioSourcesAndFiveCappedParticleSystems()
        {
            var scene = EditorSceneManager.OpenPreviewScene(FoundationSetup.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects(); var driver = roots.SelectMany(r => r.GetComponentsInChildren<DrillPresentation>(true)).Single();
                Assert.That(driver.Sounds.VoiceCount, Is.EqualTo(4)); Assert.That(driver.Effects.SystemCount, Is.EqualTo(5)); Assert.That(driver.Hud, Is.Not.Null);
                var particles = roots.SelectMany(r => r.GetComponentsInChildren<ParticleSystem>(true)).ToArray();
                Assert.That(particles.Sum(p => p.main.maxParticles), Is.EqualTo(112)); Assert.That(particles.All(p => !p.main.playOnAwake), Is.True);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<AudioSource>(true)).Count(), Is.EqualTo(5));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<DrillMotor>(true)).Count(), Is.EqualTo(1));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        [Test] public void M6SetupPreservesCustomObjectsGuidsAssetsAndDirtySceneWorkOnReruns()
        {
            const string copy = "Assets/_DrillRunner/Scenes/M6SetupTemporary.unity";
            var paths = new[] { FoundationSetup.ScenePath, PresentationAssets.ThemePath, EvolutionSetup.PocketVisualPath, EvolutionSetup.WorkshopVisualPath, WorkshopSetup.ConfigPath };
            var bytes = paths.Select(File.ReadAllBytes).ToArray(); Assert.That(AssetDatabase.CopyAsset(FoundationSetup.ScenePath, copy), Is.True);
            try
            {
                var scene = EditorSceneManager.OpenScene(copy, OpenSceneMode.Additive);
                try
                {
                    var sentinel = new GameObject("M6 User Work"); SceneManager.MoveGameObjectToScene(sentinel, scene); sentinel.transform.position = new Vector3(11, 2, 17);
                    EditorSceneManager.SaveScene(scene); var before = File.ReadAllBytes(copy); var guid = AssetDatabase.AssetPathToGUID(copy);
                    VisualPolishSetup.SetupScene(copy); VisualPolishSetup.SetupScene(copy);
                    Assert.That(File.ReadAllBytes(copy), Is.EqualTo(before)); Assert.That(AssetDatabase.AssetPathToGUID(copy), Is.EqualTo(guid));
                    Assert.That(sentinel.transform.position, Is.EqualTo(new Vector3(11, 2, 17)));
                    EditorSceneManager.MarkSceneDirty(scene); Assert.Throws<InvalidOperationException>(() => VisualPolishSetup.SetupScene(copy));
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            finally { AssetDatabase.DeleteAsset(copy); }
            for (var i = 0; i < paths.Length; i++) Assert.That(File.ReadAllBytes(paths[i]), Is.EqualTo(bytes[i]));
        }
    }
}

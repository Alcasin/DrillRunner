using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DrillRunner.Tests.EditMode
{
    public sealed class PresentationRevisionTests
    {
        [Test] public void BothExposedRotorsPointForwardAndKeepTheAcceptedCollisionFootprint()
        {
            foreach (var path in new[] { EvolutionSetup.PocketVisualPath, EvolutionSetup.WorkshopVisualPath })
            {
                var visual = AssetDatabase.LoadAssetAtPath<GameObject>(path); EvolutionSetup.ValidateVisual(visual);
                var rotor = visual.transform.Find("Drill Rotor"); var housing = visual.transform.Find("Motor Housing").GetComponent<MeshRenderer>();
                Assert.That(Vector3.Dot(rotor.forward, Vector3.forward), Is.EqualTo(1).Within(0.0001));
                var bounds = rotor.GetComponentsInChildren<MeshRenderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                Assert.That(bounds.max.z, Is.InRange(1.8f, 1.85f)); Assert.That(bounds.size.z, Is.GreaterThan(1.4f));
                Assert.That(bounds.max.y - housing.bounds.max.y, Is.GreaterThan(0.4f), "Upper rotating flutes clear the lowered rear housing.");
            }
            var drill = AssetDatabase.LoadAssetAtPath<GameObject>(MovementPrototypeSetup.PrefabPath); var collider = drill.GetComponent<BoxCollider>();
            Assert.That(collider.size, Is.EqualTo(new Vector3(1.7f, 1.3f, 3)));
            Assert.That(collider.center, Is.EqualTo(new Vector3(0, 0.65f, 0.35f)));
        }
        [Test] public void YardUsesThreeSharedColliderFreeVariantsAndPreservesAuthoredSurfaces()
        {
            var variants = AssetDatabase.FindAssets("t:Prefab", new[] { ModularTrackSetup.SectionFolder }).Select(g =>
            {
                var section = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                var details = section.transform.Find("M6B Yard Variant"); Assert.That(details, Is.Not.Null);
                Assert.That(details.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(details.GetComponentsInChildren<MeshRenderer>().Length, Is.LessThanOrEqualTo(4));
                Assert.That(section.transform.Find("Surface").GetComponent<BoxCollider>().size, Is.EqualTo(Vector3.one));
                return details.GetComponentsInChildren<MeshFilter>().First().sharedMesh;
            }).Distinct().Count();
            Assert.That(variants, Is.EqualTo(3));
        }
        [Test] public void RevisedSceneKeepsOneEnabledListenerAndExistingFiveTwoDimensionalSources()
        {
            var scene = EditorSceneManager.OpenPreviewScene(FoundationSetup.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects(); var listeners = roots.SelectMany(r => r.GetComponentsInChildren<AudioListener>(true)).ToArray();
                Assert.That(listeners.Length, Is.EqualTo(1)); Assert.That(listeners[0].enabled, Is.True);
                var sources = roots.SelectMany(r => r.GetComponentsInChildren<AudioSource>(true)).ToArray(); Assert.That(sources.Length, Is.EqualTo(5));
                Assert.That(sources.All(s => s.enabled && !s.mute && !s.playOnAwake && s.spatialBlend == 0 && s.outputAudioMixerGroup == null), Is.True);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<DrillPresentation>(true)).Single().VisualRevision, Is.EqualTo(5));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}

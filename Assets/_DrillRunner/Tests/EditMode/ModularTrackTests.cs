using System;
using System.IO;
using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DrillRunner.Tests.EditMode
{
    public class ModularTrackTests
    {
        private TrackCatalog Catalog => AssetDatabase.LoadAssetAtPath<TrackCatalog>(ModularTrackSetup.CatalogPath);
        private static TrackPlacement Obstacle(float x, float z, float width, float hardness, ObstacleRewardKind kind = ObstacleRewardKind.Coins)
            => new TrackPlacement(new Bounds(new Vector3(x, 1, z), new Vector3(width, 2, 0.8f)), hardness, kind);
        private static bool Safe(params TrackPlacement[] placements) => TrackSafety.Validate(6, 40, 0.85f, 5, placements, out _);

        [Test]
        public void AuthoredModulesHaveConsistentConnectionsAndCertifiedRoutes()
        {
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { ModularTrackSetup.SectionFolder });
            Assert.That(paths.Length, Is.EqualTo(11));
            foreach (var guid in paths)
            {
                var section = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)).GetComponent<TrackSection>();
                Assert.That(section.Length, Is.EqualTo(40)); Assert.That(section.Width, Is.EqualTo(6));
                Assert.That(SectionSequence.IsSafe(section, Catalog, 0.85f), Is.True, section.name);
            }
        }

        [Test]
        public void SameSeedReplaysSequenceDifferentSeedsVaryAndGlobalRandomIsUntouched()
        {
            var state = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(919); var expected = UnityEngine.Random.value; UnityEngine.Random.InitState(919);
                var a = new SectionSequence(1337, Catalog, 0.85f); var b = new SectionSequence(1337, Catalog, 0.85f);
                var c = new SectionSequence(904, Catalog, 0.85f);
                var first = Enumerable.Range(0, 200).Select(_ => a.NextIndex()).ToArray();
                var same = Enumerable.Range(0, 200).Select(_ => b.NextIndex()).ToArray();
                var other = Enumerable.Range(0, 200).Select(_ => c.NextIndex()).ToArray();
                Assert.That(same, Is.EqualTo(first)); Assert.That(other, Is.Not.EqualTo(first)); Assert.That(first.Distinct().Count(), Is.GreaterThanOrEqualTo(5));
                Assert.That(UnityEngine.Random.value, Is.EqualTo(expected));
                for (var i = 0; i < first.Length; i += 3)
                    Assert.That(Catalog.Variants[first[i]].ReadPlacements().Any(p => p.Reward == ObstacleRewardKind.Copper), Is.True);
            }
            finally { UnityEngine.Random.state = state; }
        }

        [Test]
        public void StoneHasRealBypassAndVisualGapNarrowerThanTheFootprintIsRejected()
        {
            Assert.That(Safe(Obstacle(0, 20, 1.8f, 9)), Is.True);
            Assert.That(Safe(Obstacle(0, 20, 6, 9)), Is.False);
            Assert.That(Safe(Obstacle(-1.7f, 20, 1.8f, 9), Obstacle(1.7f, 20, 1.8f, 9)), Is.False);
        }

        [Test]
        public void CopperMustBeDrillableAndHaveACertifiedReachableApproach()
        {
            Assert.That(Safe(Obstacle(0, 20, 1.2f, 4, ObstacleRewardKind.Copper)), Is.True);
            Assert.That(Safe(Obstacle(0, 20, 1.2f, 9, ObstacleRewardKind.Copper)), Is.False);
            Assert.That(Safe(Obstacle(2.3f, 20, 1, 4, ObstacleRewardKind.Copper)), Is.False);
            Assert.That(Safe(Obstacle(0, 14, 1.8f, 9), Obstacle(0, 27, 1.2f, 4, ObstacleRewardKind.Copper)), Is.False);
            Assert.That(Safe(Obstacle(-1.6f, 20, 1, 9), Obstacle(1.5f, 20, 0.8f, 4, ObstacleRewardKind.Copper)), Is.True);
        }

        [Test]
        public void BoundaryClearanceMakesAllConsecutiveCatalogPairsPhysicallyTraversable()
        {
            foreach (var a in Catalog.Variants)
            foreach (var b in Catalog.Variants)
            {
                Assert.That(SectionSequence.IsSafe(a, Catalog, 0.85f) && SectionSequence.IsSafe(b, Catalog, 0.85f), Is.True);
                var end = a.ReadPlacements().Select(p => p.Bounds.max.z).DefaultIfEmpty(0).Max();
                var begin = b.ReadPlacements().Select(p => p.Bounds.min.z).DefaultIfEmpty(40).Min();
                var freeTravel = 40 - end + begin - 3;
                var residual = 4.3 * Math.Exp(-18 * freeTravel / 24);
                Assert.That(residual, Is.LessThan(TrackSafety.Margin), a.SectionId + " -> " + b.SectionId);
            }
            Assert.That(Safe(Obstacle(0, 3, 1.8f, 9)), Is.False);
            Assert.That(Safe(Obstacle(0, 37, 1.8f, 9)), Is.False);
        }

        [Test]
        public void UnsafeSelectedVariantFallsBackAndInvalidCatalogIsRejected()
        {
            var catalog = Object.Instantiate(Catalog);
            var root = Object.Instantiate(Catalog.Variants.First(s => s.SectionId == "StoneBypass").gameObject);
            try
            {
                root.transform.Find("Surface").localScale = new Vector3(1, 0.5f, 40);
                catalog.Variants = new[] { root.GetComponent<TrackSection>(), Catalog.Fallback }; catalog.ResourceInterval = 0;
                var sequence = new SectionSequence(42, catalog, 0.85f);
                var picks = Enumerable.Range(0, 100).Select(_ => sequence.NextIndex()).ToArray();
                Assert.That(picks, Has.Member(-1)); Assert.That(picks, Has.No.Member(0));
                root.transform.Find("Surface").localScale = new Vector3(6, 0.5f, 40);
                root.transform.Find("Surface").GetComponent<MeshRenderer>().enabled = false;
                Assert.That(SectionSequence.IsSafe(root.GetComponent<TrackSection>(), catalog, 0.85f), Is.False, "An invisible floor must not be accepted.");
                catalog.Fallback = root.GetComponent<TrackSection>();
                Assert.Throws<ArgumentException>(() => new SectionSequence(42, catalog, 0.85f));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void GameplayUsesModulesAndPreservesInactiveM2ReferenceAndStableWiring()
        {
            var scene = EditorSceneManager.OpenPreviewScene(FoundationSetup.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.That(roots.Single(r => r.name == "Track Reference (Temporary)").activeSelf, Is.False);
                Assert.That(roots.Single(r => r.name == "M1 Track Guides").activeSelf, Is.False);
                var run = roots.SelectMany(r => r.GetComponentsInChildren<RunProgress>(true)).Single();
                Assert.That(run.Motor, Is.Not.Null); Assert.That(run.Track.StarterSections.Length, Is.EqualTo(4));
                Assert.That(run.Track.Catalog, Is.SameAs(Catalog));
                var hud = roots.SelectMany(r => r.GetComponentsInChildren<PrototypeHud>(true)).Single();
                Assert.That(hud.RewardText, Is.Not.Null); Assert.That(hud.SummaryText, Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void RepeatedM3SetupPreservesSceneGuidsCustomWorkAndRewardTuning()
        {
            const string copy = "Assets/_DrillRunner/Scenes/M3SetupTemporary.unity";
            Assert.That(AssetDatabase.CopyAsset(FoundationSetup.ScenePath, copy), Is.True);
            var paths = new[] { FoundationSetup.ScenePath, ModularTrackSetup.CatalogPath, FoundationSetup.Root + "/ScriptableObjects/Wood.asset" };
            var bytes = paths.Select(File.ReadAllBytes).ToArray();
            try
            {
                var scene = EditorSceneManager.OpenScene(copy, OpenSceneMode.Additive);
                try
                {
                    var sentinel = new GameObject("User M3 Work"); SceneManager.MoveGameObjectToScene(sentinel, scene);
                    sentinel.transform.position = new Vector3(17, 4, 2); EditorSceneManager.SaveScene(scene);
                    var before = File.ReadAllBytes(copy); var guid = AssetDatabase.AssetPathToGUID(copy);
                    ModularTrackSetup.SetupScene(copy); ModularTrackSetup.SetupScene(copy);
                    Assert.That(File.ReadAllBytes(copy), Is.EqualTo(before)); Assert.That(AssetDatabase.AssetPathToGUID(copy), Is.EqualTo(guid));
                    Assert.That(sentinel.transform.position, Is.EqualTo(new Vector3(17, 4, 2)));
                    EditorSceneManager.MarkSceneDirty(scene); Assert.Throws<InvalidOperationException>(() => ModularTrackSetup.SetupScene(copy));
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            finally { AssetDatabase.DeleteAsset(copy); }
            for (var i = 0; i < paths.Length; i++) Assert.That(File.ReadAllBytes(paths[i]), Is.EqualTo(bytes[i]));
        }
    }
}

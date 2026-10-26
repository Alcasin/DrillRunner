using System;
using System.IO;
using System.Linq;
using DrillRunner.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DrillRunner.Tests.EditMode
{
    public class FoundationTests
    {
        [Test]
        public void GameplayIsEnabledInGlobalAndEffectiveBuildScenes()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(FoundationSetup.ScenePath), Is.Not.Null);
            Assert.That(EditorBuildSettings.globalScenes.Count(s => s.path == FoundationSetup.ScenePath && s.enabled), Is.EqualTo(1));
            Assert.That(EditorBuildSettings.scenes.Count(s => s.path == FoundationSetup.ScenePath && s.enabled), Is.EqualTo(1));
        }

        [Test]
        public void GameplayOpensWithCameraLightingAndUrpTrack()
        {
            var scene = EditorSceneManager.OpenPreviewScene(FoundationSetup.ScenePath);
            try
            {
                var roots = scene.GetRootGameObjects();
                var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).Single();
                Assert.That(camera.CompareTag("MainCamera"), Is.True);
                Assert.That(camera.GetComponent<AudioListener>(), Is.Not.Null);
                Assert.That(camera.GetComponent<UniversalAdditionalCameraData>(), Is.Not.Null);
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<Light>())
                    .Any(l => l.type == LightType.Directional && l.enabled), Is.True);
                var track = roots.Single(r => r.name == "Track Reference (Temporary)");
                var material = track.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.That(material, Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>(FoundationSetup.MaterialPath)));
                Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                    .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)), Is.Zero);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void RuntimeBuildHasNoEditorOrTestAssemblyDependencies()
        {
            var player = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            Assert.That(player.Any(a => a.name == "DrillRunner.Runtime"), Is.True);
            Assert.That(player.Any(a => a.name == "DrillRunner.Editor" || a.name == "DrillRunner.EditModeTests"), Is.False);
            var references = System.Reflection.Assembly.Load("DrillRunner.Runtime").GetReferencedAssemblies();
            Assert.That(references.Any(a => a.Name.StartsWith("UnityEditor", StringComparison.Ordinal) ||
                                           a.Name.Contains("TestRunner") || a.Name.StartsWith("nunit", StringComparison.Ordinal)), Is.False);
            var editor = CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                .Single(a => a.name == "DrillRunner.Editor");
            Assert.That(editor.flags.HasFlag(AssemblyFlags.EditorAssembly), Is.True);
        }

        [Test]
        public void PortraitAndModernInputAreConfiguredWithoutChangingUrp()
        {
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.Portrait));
            Assert.That(PlayerSettings.defaultScreenWidth * 16, Is.EqualTo(PlayerSettings.defaultScreenHeight * 9));
            Assert.That(PlayerSettings.defaultIsNativeResolution, Is.False);
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.True);
            Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown ||
                        PlayerSettings.allowedAutorotateToLandscapeLeft || PlayerSettings.allowedAutorotateToLandscapeRight, Is.False);
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.InstanceOf<UniversalRenderPipelineAsset>());
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Assert.Pass("Only modern Input System is active.");
#else
            Assert.Fail("Use Input System Package (New), with the legacy gameplay input backend disabled.");
#endif
        }

        [Test]
        public void EssentialAssetsHaveStableReferenceableGuids()
        {
            var paths = new[] { FoundationSetup.ScenePath, FoundationSetup.InputPath, FoundationSetup.MaterialPath };
            foreach (var path in paths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                Assert.That(guid, Has.Length.EqualTo(32), path);
                Assert.That(AssetDatabase.GUIDToAssetPath(guid), Is.EqualTo(path));
            }
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(FoundationSetup.InputPath);
            Assert.That(actions.FindAction("Gameplay/Charge", true).type, Is.EqualTo(InputActionType.Button));
            Assert.That(actions.FindAction("Gameplay/Steer", true).expectedControlType, Is.EqualTo("Vector2"));
        }

        [Test]
        public void RepeatedSetupPreservesSavedAssetsGuidsBuildEntriesAndUnsavedScenes()
        {
            var paths = new[] { FoundationSetup.ScenePath, FoundationSetup.InputPath, FoundationSetup.MaterialPath,
                FoundationSetup.ScenePath + ".meta", FoundationSetup.InputPath + ".meta", FoundationSetup.MaterialPath + ".meta",
                "ProjectSettings/ProjectSettings.asset", "ProjectSettings/EditorBuildSettings.asset" };
            var contents = paths.Select(File.ReadAllBytes).ToArray();
            var previousActive = SceneManager.GetActiveScene();
            var scratch = EditorSceneManager.NewPreviewScene();
            try
            {
                var sentinel = new GameObject("M0 Test - Unsaved User Work");
                SceneManager.MoveGameObjectToScene(sentinel, scratch);
                EditorSceneManager.MarkSceneDirty(scratch);
                var scenes = EditorSceneManager.GetSceneManagerSetup();
                var buildEntries = EditorBuildSettings.scenes.Select(s => s.path + ":" + s.enabled).ToArray();
                FoundationSetup.Setup();
                FoundationSetup.Setup();
                Assert.That(sentinel != null && scratch.isLoaded && scratch.isDirty, Is.True);
                var after = EditorSceneManager.GetSceneManagerSetup();
                Assert.That(after.Select(s => (s.path, s.isLoaded, s.isActive)),
                    Is.EqualTo(scenes.Select(s => (s.path, s.isLoaded, s.isActive))));
                Assert.That(EditorBuildSettings.scenes.Select(s => s.path + ":" + s.enabled), Is.EqualTo(buildEntries));
                for (var i = 0; i < paths.Length; i++)
                    Assert.That(File.ReadAllBytes(paths[i]), Is.EqualTo(contents[i]), paths[i] + " changed during repeated setup.");
            }
            finally
            {
                if (previousActive.IsValid() && previousActive.isLoaded)
                    SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.ClosePreviewScene(scratch);
            }
        }
    }
}

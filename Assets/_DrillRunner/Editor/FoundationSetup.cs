using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace DrillRunner.Editor
{
    /// <summary>Explicit, repeatable M0 setup. Existing asset contents are never rebuilt.</summary>
    public static class FoundationSetup
    {
        public const string Root = "Assets/_DrillRunner";
        public const string ScenePath = Root + "/Scenes/Gameplay.unity";
        public const string InputPath = Root + "/Settings/DrillRunner.inputactions";
        public const string MaterialPath = Root + "/Materials/TrackReference.mat";

        [MenuItem("Tools/DrillRunner/Setup Project Foundation")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before setting up the foundation.");

            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                throw new InvalidOperationException("The active quality level must use the existing URP asset.");

#if !ENABLE_INPUT_SYSTEM
            throw new InvalidOperationException(
                "Enable Input System Package (New) in Player > Active Input Handling, then restart Unity.");
#else
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (actions == null || actions.FindAction("Gameplay/Charge") == null ||
                actions.FindAction("Gameplay/Steer") == null)
                throw new InvalidOperationException("Missing or invalid M0 input asset: " + InputPath);

            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Materials");
            EnsureGameplayScene();
            ConfigurePortrait();

            // Unity 6 separates global scenes from an active build profile's override.
            var globalScenes = IncludeGameplay(EditorBuildSettings.globalScenes);
            if (globalScenes != null)
                EditorBuildSettings.globalScenes = globalScenes;

            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile != null && profile.overrideGlobalScenes)
            {
                var profileScenes = IncludeGameplay(profile.scenes);
                if (profileScenes != null)
                {
                    profile.scenes = profileScenes;
                    EditorUtility.SetDirty(profile);
                    AssetDatabase.SaveAssetIfDirty(profile);
                    Debug.Log("DrillRunner: included Gameplay in the active build profile override.");
                }
            }

            Debug.Log("DrillRunner M0 setup complete. Existing assets and open scenes preserved. " +
                      "Open " + ScenePath + "; choose a 9:16 aspect ratio in Game View.");
#endif
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void EnsureGameplayScene()
        {
            if (File.Exists(ScenePath))
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                    throw new InvalidOperationException("Existing Gameplay scene could not be imported; left untouched.");
                return;
            }

            // Do not recreate an asset at a path whose GUID still exists.
            if (File.Exists(ScenePath + ".meta"))
                throw new InvalidOperationException("Gameplay scene is missing but its .meta remains. Restore the scene first.");

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                if (File.Exists(MaterialPath) || File.Exists(MaterialPath + ".meta"))
                    throw new InvalidOperationException("Existing track material could not be loaded; left untouched.");
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    throw new InvalidOperationException("URP/Lit shader is unavailable. Check URP package import.");
                material = new Material(shader) { name = "TrackReference" };
                material.SetColor("_BaseColor", new Color(0.23f, 0.28f, 0.32f));
                AssetDatabase.CreateAsset(material, MaterialPath);
                AssetDatabase.SaveAssetIfDirty(material);
            }

            var previousActive = SceneManager.GetActiveScene();
            // Batch startup supplies an empty untitled scene. Unity disallows creating
            // another scene additively beside it; replace only that clean, empty batch scene.
            var emptyBatchScene = Application.isBatchMode && SceneManager.sceneCount == 1 &&
                                  previousActive.IsValid() && previousActive.path == "" &&
                                  previousActive.rootCount == 0 && !previousActive.isDirty;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                emptyBatchScene ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0, 7, -10);
                cameraObject.transform.LookAt(new Vector3(0, 0, 9));
                var camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 55;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 150;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.17f, 0.23f);
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

                var lightObject = new GameObject("Directional Light", typeof(Light));
                lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                light.shadows = LightShadows.Soft;
                lightObject.AddComponent<UniversalAdditionalLightData>();

                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Track Reference (Temporary)";
                ground.transform.position = new Vector3(0, -0.25f, 15);
                ground.transform.localScale = new Vector3(6, 0.5f, 50);
                ground.GetComponent<MeshRenderer>().sharedMaterial = material;
                ground.isStatic = true;

                RenderSettings.sun = light;
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.55f);
                RenderSettings.fog = false;

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new IOException("Could not save " + ScenePath);
                Debug.Log("DrillRunner: created " + ScenePath + " using Unity scene APIs.");
            }
            finally
            {
                if (previousActive.IsValid() && previousActive.isLoaded)
                    SceneManager.SetActiveScene(previousActive);
                if (!emptyBatchScene)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ConfigurePortrait()
        {
            if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait &&
                PlayerSettings.defaultScreenWidth == 540 && PlayerSettings.defaultScreenHeight == 960 &&
                !PlayerSettings.defaultIsNativeResolution && PlayerSettings.allowedAutorotateToPortrait &&
                !PlayerSettings.allowedAutorotateToPortraitUpsideDown &&
                !PlayerSettings.allowedAutorotateToLandscapeLeft && !PlayerSettings.allowedAutorotateToLandscapeRight)
                return;

            Debug.Log("DrillRunner: applying portrait orientation, 540 x 960 default window size, " +
                      "native-resolution override off, and portrait-only autorotation flags.");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }

        // Null means no write is needed. Preserve unrelated entries and their ordering/enabled flags.
        private static EditorBuildSettingsScene[] IncludeGameplay(EditorBuildSettingsScene[] scenes)
        {
            var result = new List<EditorBuildSettingsScene>(scenes);
            var existing = result.FirstOrDefault(item => item.path == ScenePath);
            if (existing != null && existing.enabled)
                return null;
            if (existing != null)
            {
                existing.enabled = true;
                Debug.Log("DrillRunner: enabled Gameplay in the build scene list.");
            }
            else
            {
                result.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                Debug.Log("DrillRunner: added Gameplay first in the build scene list; existing entries preserved.");
            }
            return result.ToArray();
        }
    }
}

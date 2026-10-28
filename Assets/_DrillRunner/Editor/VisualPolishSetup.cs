using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace DrillRunner.Editor
{
    public static class VisualPolishSetup
    {
        private static void RefineParticles(ParticleSystem particles, bool loop)
        {
            var main = particles.main; var emission = particles.emission;
            if (loop) { main.startSize = 0.075f; main.startLifetime = 0.35f; emission.rateOverTime = 40; }
            else
            {
                var renderer = particles.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = PresentationAssets.Cube;
                main.startRotation3D = true; main.startRotationX = new ParticleSystem.MinMaxCurve(-3, 3); main.startRotationY = new ParticleSystem.MinMaxCurve(-3, 3);
                main.startRotationZ = new ParticleSystem.MinMaxCurve(-3, 3);
            }
        }
        [MenuItem("Tools/DrillRunner/Setup M6 Visual Polish")]
        public static void Setup() => SetupScene(FoundationSetup.ScenePath);
        public static void SetupScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before M6 setup.");
            var scene = SceneManager.GetSceneByPath(path); var opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save the scene before M6 setup; unsaved edits preserved.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) throw new InvalidOperationException("Accepted M5 scene required.");
            var previous = SceneManager.GetActiveScene(); if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var roots = scene.GetRootGameObjects(); var existing = roots.SelectMany(r => r.GetComponentsInChildren<DrillPresentation>(true)).ToArray();
                if (existing.Length > 1) throw new InvalidOperationException("Duplicate presentation drivers; preserved for review.");
                if (existing.Length == 1)
                {
                    var driver = existing[0]; var theme = AssetDatabase.LoadAssetAtPath<PresentationTheme>(PresentationAssets.ThemePath); theme.Validate();
                    if (driver.Sounds == null || driver.Effects == null || driver.Hud == null || driver.Sounds.VoiceCount != 4 || driver.Effects.SystemCount != 5)
                        throw new InvalidOperationException("Incomplete M6 references; preserved for review.");
                    if (driver.VisualRevision < 5)
                    {
                        ApplyRevision(scene, driver);
                        Debug.Log("DrillRunner M6-B targeted revision integrated. Collision, balance, audio assets and saves preserved.");
                    }
                    else Debug.Log("DrillRunner M6-B already configured; scene and assets unchanged.");
                    return;
                }
                var flow = roots.SelectMany(r => r.GetComponentsInChildren<WorkshopController>(true)).Single();
                var hud = roots.SelectMany(r => r.GetComponentsInChildren<PrototypeHud>(true)).Single();
                var themeAsset = PresentationAssets.EnsureTheme(); PresentationAssets.BuildModels(); PresentationWorld.Obstacles(); PresentationWorld.Sections();
                RefreshPreview(flow.Configuration.VisualPrefab, FoundationSetup.Root + "/Textures/PocketDrillPreview.png");
                RefreshPreview(flow.Configuration.NextForm.VisualPrefab, FoundationSetup.Root + "/Textures/WorkshopDrillPreview.png");
                var audio = flow.gameObject.AddComponent<PresentationAudio>(); var audioData = new SerializedObject(audio); Ref(audioData, "theme", themeAsset);
                var loop = Voice(flow.transform, "M6 Mechanical Loop"); Ref(audioData, "loop", loop);
                var voices = audioData.FindProperty("voices"); voices.arraySize = 4;
                for (var i = 0; i < 4; i++) voices.GetArrayElementAtIndex(i).objectReferenceValue = Voice(flow.transform, "M6 Voice " + i);
                audioData.ApplyModifiedProperties();
                var effects = flow.gameObject.AddComponent<PresentationEffects>(); var effectsData = new SerializedObject(effects); Ref(effectsData, "theme", themeAsset);
                Ref(effectsData, "contact", Particles(flow.transform, "M6 Contact Chips", themeAsset, true, 24));
                var bursts = effectsData.FindProperty("bursts"); bursts.arraySize = 4;
                for (var i = 0; i < 4; i++) bursts.GetArrayElementAtIndex(i).objectReferenceValue = Particles(flow.transform, "M6 Break Burst " + i, themeAsset, false, 22);
                effectsData.ApplyModifiedProperties(); var feedbackHud = PresentationUi.Build(hud, flow);
                var driverComponent = flow.gameObject.AddComponent<DrillPresentation>(); var driverData = new SerializedObject(driverComponent);
                Ref(driverData, "motor", flow.Run.Motor); Ref(driverData, "visuals", flow.Visuals); Ref(driverData, "flow", flow); Ref(driverData, "run", flow.Run);
                Ref(driverData, "sounds", audio); Ref(driverData, "effects", effects); Ref(driverData, "hud", feedbackHud); driverData.ApplyModifiedProperties();
                var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.7f, 0.82f, 0.82f);
                var light = roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).Single(); light.color = new Color(1, 0.94f, 0.82f); light.intensity = 1.25f;
                light.shadows = LightShadows.Soft; light.shadowStrength = 0.65f; light.shadowBias = 0.05f; light.transform.rotation = Quaternion.Euler(42, -35, 0);
                RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(0.73f, 0.84f, 0.9f);
                RenderSettings.ambientEquatorColor = new Color(0.48f, 0.61f, 0.64f); RenderSettings.ambientGroundColor = new Color(0.29f, 0.37f, 0.4f);
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 145; RenderSettings.fogEndDistance = 220; RenderSettings.fogColor = camera.backgroundColor;
                ApplyRevision(scene, driverComponent);
                Debug.Log("DrillRunner M6 visual polish setup complete. Gameplay/progression data and player saves were not changed.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            }
        }
        private static void ApplyRevision(Scene scene, DrillPresentation driver)
        {
            PresentationAssets.BuildModels(); PresentationWorld.ReviseYard();
            var flow = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WorkshopController>(true)).Single();
            var hud = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PrototypeHud>(true)).Single();
            RefreshPreview(flow.Configuration.VisualPrefab, FoundationSetup.Root + "/Textures/PocketDrillPreview.png");
            RefreshPreview(flow.Configuration.NextForm.VisualPrefab, FoundationSetup.Root + "/Textures/WorkshopDrillPreview.png");
            PresentationUi.ReviseGameplay(hud);
            const string fragmentPath = FoundationSetup.Root + "/Materials/M6BFragments.mat";
            var fragmentMaterial = AssetDatabase.LoadAssetAtPath<Material>(fragmentPath);
            if (fragmentMaterial == null)
            {
                PresentationAssets.RequireNew(fragmentPath);
                fragmentMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit")) { name = "M6BFragments" };
                fragmentMaterial.SetTexture("_BaseMap", Texture2D.whiteTexture); fragmentMaterial.SetColor("_BaseColor", Color.white);
                fragmentMaterial.SetFloat("_Surface", 0); fragmentMaterial.SetFloat("_Metallic", 0); fragmentMaterial.SetFloat("_Smoothness", 0.15f);
                fragmentMaterial.SetFloat("_SrcBlend", 1); fragmentMaterial.SetFloat("_DstBlend", 0); fragmentMaterial.SetFloat("_ZWrite", 1);
                fragmentMaterial.renderQueue = 2000; AssetDatabase.CreateAsset(fragmentMaterial, fragmentPath); AssetDatabase.SaveAssetIfDirty(fragmentMaterial);
            }
            foreach (var particles in driver.Effects.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particles.main; var renderer = particles.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = fragmentMaterial;
                renderer.renderMode = ParticleSystemRenderMode.Mesh; renderer.mesh = PresentationAssets.Cube; renderer.maxParticleSize = 0.04f;
                main.startRotation3D = true; main.startRotationX = new ParticleSystem.MinMaxCurve(-3, 3); main.startRotationY = new ParticleSystem.MinMaxCurve(-3, 3); main.startRotationZ = new ParticleSystem.MinMaxCurve(-3, 3);
                var rotation = particles.rotationOverLifetime; rotation.enabled = true; rotation.separateAxes = true; rotation.x = 2; rotation.y = 3; rotation.z = 4;
                if (main.loop) { main.startLifetime = 0.42f; main.startSpeed = 2; var emission = particles.emission; emission.rateOverTime = 38; }
                else
                {
                    var size = particles.sizeOverLifetime; size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.65f, 0.85f), new Keyframe(1, 0)));
                    main.gravityModifier = 1.1f;
                }
            }
            var data = new SerializedObject(driver); data.FindProperty("visualRevision").intValue = 5; data.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save M6-B scene.");
        }
        private static AudioSource Voice(Transform parent, string name)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false); var source = root.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.volume = 0.2f; source.dopplerLevel = 0; source.priority = 100; return source;
        }
        private static ParticleSystem Particles(Transform parent, string name, PresentationTheme theme, bool loop, int capacity)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false); var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.loop = loop; main.playOnAwake = false; main.maxParticles = capacity;
            main.startLifetime = loop ? 0.3f : 0.65f; main.startSpeed = loop ? 1.3f : 2.4f; main.startSize = 0.055f; main.gravityModifier = 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.useUnscaledTime = false; particles.useAutoRandomSeed = false; particles.randomSeed = (uint)(1301 + capacity + (loop ? 1 : 3));
            var emission = particles.emission; emission.enabled = loop; emission.rateOverTime = loop ? 28 : 0;
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = loop ? 0.08f : 0.2f;
            var size = particles.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
            var renderer = particles.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = theme.ParticleMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; renderer.maxParticleSize = 0.025f;
            RefineParticles(particles, loop); return particles;
        }
        private static void RefreshPreview(GameObject prefab, string path)
        {
            var utility = new PreviewRenderUtility(); Texture2D texture = null;
            try
            {
                utility.AddSingleGO(Object.Instantiate(prefab)); utility.cameraFieldOfView = 24;
                utility.camera.transform.position = new Vector3(2.6f, 1.9f, 3.8f); utility.camera.transform.LookAt(new Vector3(0, 0.75f, 0.15f));
                utility.camera.clearFlags = CameraClearFlags.SolidColor; utility.camera.backgroundColor = new Color(0.96f, 0.98f, 0.97f);
                utility.lights[0].intensity = 1.4f; utility.lights[0].transform.rotation = Quaternion.Euler(40, 35, 0); utility.lights[1].intensity = 1; utility.ambientColor = new Color(0.6f, 0.68f, 0.7f);
                utility.BeginStaticPreview(new Rect(0, 0, 512, 224)); utility.Render(true); texture = utility.EndStaticPreview();
                File.WriteAllBytes(path, texture.EncodeToPNG()); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            finally { if (texture != null) Object.DestroyImmediate(texture); utility.Cleanup(); }
        }
        private static void Ref(SerializedObject data, string name, Object value) => data.FindProperty(name).objectReferenceValue = value;
    }
}

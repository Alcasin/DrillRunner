using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DrillRunner.Editor
{
    public static class EvolutionSetup
    {
        private const string Root = FoundationSetup.Root;
        public const string WorkshopPath = Root + "/ScriptableObjects/WorkshopDrillProgression.asset";
        public const string PocketVisualPath = Root + "/Prefabs/VisualPocketDrill.prefab";
        public const string WorkshopVisualPath = Root + "/Prefabs/VisualWorkshopDrill.prefab";
        [MenuItem("Tools/DrillRunner/Setup M5 Evolution")]
        public static void Setup() => SetupScene(FoundationSetup.ScenePath);

        public static void SetupScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before M5 setup.");
            var scene = SceneManager.GetSceneByPath(path); var opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save the scene before M5 setup; unsaved work preserved.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) throw new InvalidOperationException("The accepted M4 Gameplay scene is required.");
            var previous = SceneManager.GetActiveScene(); if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var flow = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WorkshopController>(true)).Single();
                var motor = flow.Run.Motor; var view = flow.View; var config = flow.Configuration;
                if (view.EvolveButton != null)
                {
                    config.Validate();
                    if (config.NextForm == null || config.VisualPrefab == null || config.PreviewImage == null || config.NextForm.VisualPrefab == null ||
                        config.NextForm.PreviewImage == null || flow.Visuals == null || view.Requirements == null || view.FormPreview == null || view.FormName == null)
                        throw new InvalidOperationException("Incomplete evolution references; preserved for review.");
                    ValidateVisual(config.VisualPrefab); ValidateVisual(config.NextForm.VisualPrefab);
                    Debug.Log("DrillRunner M5 already configured; scene and all assets unchanged."); return;
                }
                var pocket = EnsurePocket(motor); var workshop = EnsureWorkshop();
                var next = AssetDatabase.LoadAssetAtPath<ProgressionConfig>(WorkshopPath);
                if (next == null)
                {
                    RequireNew(WorkshopPath); next = ScriptableObject.CreateInstance<ProgressionConfig>();
                    next.FormId = ProgressionConfig.WorkshopForm; next.DisplayName = "Workshop Drill"; next.SpeedCeiling = 26; next.EvolutionCopper = 0;
                    next.Power = new UpgradeTable { Costs = new[] { 150, 230, 340, 480, 650 }, Values = new[] { 12f, 13, 14, 15, 16, 18 } };
                    next.Energy = new UpgradeTable { Costs = new[] { 130, 210, 310, 440, 600 }, Values = new[] { 200f, 220, 240, 260, 280, 300 } };
                    next.Drive = new UpgradeTable { Costs = new[] { 130, 210, 310, 440, 600 }, Values = new[] { 12.2f, 12.35f, 12.5f, 12.65f, 12.8f, 13 } };
                    next.MinimumBoost = new[] { 3.25f, 3.4f, 3.6f, 3.8f, 4, 4.25f };
                    next.MaximumBoost = new[] { 12.25f, 12.4f, 12.5f, 12.6f, 12.75f, 13 };
                    next.Recovery = new[] { 24f, 26, 28, 30, 32, 34 };
                    next.VisualPrefab = workshop; next.PreviewImage = EnsurePreview(workshop, "WorkshopDrillPreview"); next.Validate();
                    AssetDatabase.CreateAsset(next, WorkshopPath); AssetDatabase.SaveAssetIfDirty(next);
                }
                var data = new SerializedObject(config);
                data.FindProperty("FormId").stringValue = ProgressionConfig.PocketForm; data.FindProperty("DisplayName").stringValue = "Pocket Drill";
                Ref(data, "VisualPrefab", pocket); Ref(data, "PreviewImage", EnsurePreview(pocket, "PocketDrillPreview")); Ref(data, "NextForm", next);
                if (data.FindProperty("EvolutionCopper").intValue <= 0) data.FindProperty("EvolutionCopper").intValue = 12;
                data.ApplyModifiedProperties(); config.Validate(); AssetDatabase.SaveAssetIfDirty(config);
                var visuals = motor.GetComponent<DrillVisuals>(); if (visuals == null) visuals = motor.gameObject.AddComponent<DrillVisuals>();
                var visualsData = new SerializedObject(visuals); Ref(visualsData, "pocketVisual", motor.transform.Find("Visuals").gameObject); visualsData.ApplyModifiedProperties();
                var flowData = new SerializedObject(flow); Ref(flowData, "visuals", visuals); flowData.ApplyModifiedProperties();
                var panel = view.Panel.transform;
                var display = panel.Find("Pocket Drill Presentation").GetComponent<RectTransform>();
                display.anchoredPosition = new Vector2(0, -117); display.sizeDelta = new Vector2(492, 87);
                display.Find("Placeholder Body").gameObject.SetActive(false); display.Find("Placeholder Tip").gameObject.SetActive(false);
                var formName = display.Find("Form").GetComponent<Text>(); formName.fontSize = 19;
                formName.rectTransform.anchoredPosition = new Vector2(0, -1); formName.rectTransform.sizeDelta = new Vector2(480, 28);
                var preview = Rect(display, "M5 Form Preview", 24, 250, 62).gameObject.AddComponent<RawImage>(); preview.texture = config.PreviewImage; preview.raycastTarget = false;
                for (var i = 0; i < 3; i++)
                {
                    var card = view.Rows[i].transform.parent.GetComponent<RectTransform>(); card.anchoredPosition = new Vector2(0, -(211 + i * 137)); card.sizeDelta = new Vector2(492, 128);
                    var row = view.Rows[i]; row.fontSize = 19; row.rectTransform.anchoredPosition = new Vector2(0, -2); row.rectTransform.sizeDelta = new Vector2(480, 73);
                    var button = view.PurchaseButtons[i].GetComponent<RectTransform>(); button.anchoredPosition = new Vector2(0, -81); button.sizeDelta = new Vector2(450, 39);
                    var buttonLabel = button.GetComponentInChildren<Text>(); buttonLabel.fontSize = 20; buttonLabel.rectTransform.sizeDelta = new Vector2(438, 39);
                }
                var evolution = Rect(panel, "M5 Evolution", 631, 492, 129); evolution.gameObject.AddComponent<Image>().color = new Color(0.075f, 0.13f, 0.17f);
                var requirements = Label(evolution, "Requirements", 1, 17, 79); requirements.text = "Complete every Pocket Drill upgrade and collect Copper";
                var evolveRect = Rect(evolution, "Evolve", 85, 450, 36); var image = evolveRect.gameObject.AddComponent<Image>();
                var evolve = evolveRect.gameObject.AddComponent<Button>(); evolve.targetGraphic = image;
                var colors = view.StartButton.colors; colors.normalColor = new Color(0.75f, 0.43f, 0.1f); colors.highlightedColor = new Color(0.95f, 0.65f, 0.2f); evolve.colors = colors;
                Label(evolveRect, "Label", 0, 21, 36).text = "EVOLVE";
                var viewData = new SerializedObject(view); Ref(viewData, "evolve", evolve); Ref(viewData, "requirements", requirements);
                Ref(viewData, "formName", formName); Ref(viewData, "formPreview", preview); viewData.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save M5 Gameplay.");
                Debug.Log("DrillRunner M5 evolution, form visuals, two progression tiers, and Workshop setup complete. No player save was accessed.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject EnsurePocket(DrillMotor motor)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PocketVisualPath); if (existing != null) { ValidateVisual(existing); return existing; }
            RequireNew(PocketVisualPath); var copy = Object.Instantiate(motor.transform.Find("Visuals").gameObject);
            try { copy.name = "Pocket Drill Visual"; copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); ValidateVisual(copy); return PrefabUtility.SaveAsPrefabAsset(copy, PocketVisualPath); }
            finally { Object.DestroyImmediate(copy); }
        }
        private static GameObject EnsureWorkshop()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(WorkshopVisualPath); if (existing != null) { ValidateVisual(existing); return existing; }
            RequireNew(WorkshopVisualPath); var root = new GameObject("Workshop Drill Visual");
            try
            {
                var hull = Material("WorkshopHull", new Color(0.035f, 0.53f, 0.58f), 0.35f);
                var housing = Material("WorkshopHousing", new Color(0.075f, 0.14f, 0.2f), 0.5f);
                var accent = Material("WorkshopAccent", new Color(1, 0.58f, 0.12f), 0.4f);
                var steel = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/DrillSteel.mat");
                var tread = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/DrillTreads.mat");
                Part(root, "Reinforced Hull", PrimitiveType.Cube, new Vector3(0, 0.69f, -0.14f), new Vector3(1.46f, 0.8f, 1.95f), hull);
                Part(root, "Left Track", PrimitiveType.Cube, new Vector3(-0.66f, 0.28f, -0.15f), new Vector3(0.3f, 0.52f, 2), tread);
                Part(root, "Right Track", PrimitiveType.Cube, new Vector3(0.66f, 0.28f, -0.15f), new Vector3(0.3f, 0.52f, 2), tread);
                Part(root, "Motor Housing", PrimitiveType.Cube, new Vector3(0, 1.19f, -0.38f), new Vector3(1.1f, 0.6f, 1.05f), housing);
                Part(root, "Copper Top Plate", PrimitiveType.Cube, new Vector3(0, 1.51f, -0.38f), new Vector3(0.95f, 0.06f, 0.7f), accent);
                Part(root, "Front Brace", PrimitiveType.Cube, new Vector3(0, 0.73f, 0.65f), new Vector3(1.5f, 0.32f, 0.19f), accent);
                for (var side = -1; side <= 1; side += 2)
                for (var i = 0; i < 3; i++) Part(root, "Cooling Vent " + side + " " + i, PrimitiveType.Cube,
                    new Vector3(side * 0.56f, 1.2f, -0.7f + i * 0.26f), new Vector3(0.03f, 0.3f, 0.12f), steel);
                for (var i = 0; i < 5; i++)
                {
                    var diameter = 1.15f - i * 0.23f;
                    var ring = Part(root, "Reinforced Drill Step " + i, PrimitiveType.Cylinder, new Vector3(0, 0.69f, 0.82f + i * 0.2f),
                        new Vector3(diameter, 0.13f, diameter), i % 2 == 0 ? steel : accent);
                    ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }
                ValidateVisual(root); return PrefabUtility.SaveAsPrefabAsset(root, WorkshopVisualPath);
            }
            finally { Object.DestroyImmediate(root); }
        }
        public static void ValidateVisual(GameObject visual)
        {
            if (visual.GetComponentsInChildren<Collider>(true).Length != 0 || visual.GetComponentsInChildren<MonoBehaviour>(true).Length != 0 ||
                visual.GetComponentsInChildren<Rigidbody>(true).Length != 0 || visual.GetComponentsInChildren<Camera>(true).Length != 0 || visual.transform.localScale != Vector3.one)
                throw new ArgumentException("Form prefabs must contain only visual geometry at unit root scale.");
            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) throw new ArgumentException("Form visual is empty.");
            foreach (var renderer in renderers)
            {
                var bounds = renderer.bounds; var offset = visual.transform.position;
                if (bounds.min.x - offset.x < -0.85f - 0.001f || bounds.max.x - offset.x > 0.85f + 0.001f ||
                    bounds.min.y - offset.y < -0.001f || bounds.min.z - offset.z < -1.15f - 0.001f || bounds.max.z - offset.z > 1.85f + 0.001f)
                    throw new ArgumentException("Decorative geometry exceeds the accepted horizontal footprint.");
            }
        }
        private static GameObject Part(GameObject parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            if (material == null) throw new ArgumentException("Missing drill material.");
            var part = GameObject.CreatePrimitive(type); part.name = name; part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = position; part.transform.localScale = scale; part.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>()); return part;
        }
        private static Material Material(string name, Color color, float metallic)
        {
            var path = Root + "/Materials/" + name + ".mat"; var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
            RequireNew(path); var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color); material.SetFloat("_Metallic", metallic); AssetDatabase.CreateAsset(material, path); AssetDatabase.SaveAssetIfDirty(material); return material;
        }
        private static Texture2D EnsurePreview(GameObject prefab, string name)
        {
            var path = Root + "/Textures/" + name + ".png"; var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (existing != null) return existing;
            RequireNew(path); var utility = new PreviewRenderUtility(); Texture2D texture = null;
            try
            {
                var clone = Object.Instantiate(prefab); utility.AddSingleGO(clone);
                utility.cameraFieldOfView = 30; utility.camera.transform.position = new Vector3(3.8f, 2.6f, 5.8f);
                utility.camera.transform.LookAt(new Vector3(0, 0.7f, 0.2f));
                utility.camera.clearFlags = CameraClearFlags.SolidColor; utility.camera.backgroundColor = new Color(0.075f, 0.13f, 0.17f);
                utility.lights[0].intensity = 1.4f; utility.lights[0].transform.rotation = Quaternion.Euler(35, 35, 0);
                utility.lights[1].intensity = 1; utility.ambientColor = Color.gray;
                utility.BeginStaticPreview(new Rect(0, 0, 480, 160)); utility.Render(true); texture = utility.EndStaticPreview();
                File.WriteAllBytes(path, texture.EncodeToPNG()); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            finally { if (texture != null) Object.DestroyImmediate(texture); utility.Cleanup(); }
        }
        private static RectTransform Rect(Transform parent, string name, float top, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1); rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private static Text Label(Transform parent, string name, float top, int size, float height)
        {
            var text = Rect(parent, name, top, 480, height).gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false; return text;
        }
        private static void Ref(SerializedObject data, string property, Object value) => data.FindProperty(property).objectReferenceValue = value;
        private static void RequireNew(string path)
        { if (File.Exists(path) || File.Exists(path + ".meta")) throw new InvalidOperationException("Restore existing evolution asset before setup: " + path); }
    }
}

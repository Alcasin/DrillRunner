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
    public static class FullEvolutionSetup
    {
        public const string Root = FoundationSetup.Root;
        public static readonly string[] Names = { "Industrial Drill", "Heavy Borer", "Hydraulic Drill", "Crystal Core Drill", "Fusion Drill", "Quantum Drill" };
        public static string ConfigPath(int stage) => Root + "/ScriptableObjects/" + Names[stage - 3].Replace(" ", "") + "Progression.asset";
        [MenuItem("Tools/DrillRunner/Setup M8 Full Evolution")]
        public static void Setup() => SetupScene(FoundationSetup.ScenePath);
        public static void SetupScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before M8 setup.");
            var scene = SceneManager.GetSceneByPath(path); var opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save the scene before M8 setup; unsaved work preserved.");
            var previous = SceneManager.GetActiveScene(); if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var flow = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WorkshopController>(true)).Single(); var config = flow.Configuration;
                if (config.NextForm.NextForm != null && flow.View.ResourceSummary != null)
                { Validate(config); Debug.Log("M8 already configured: eight forms, all references and assets preserved."); return; }
                var forms = new ProgressionConfig[6];
                for (var i = 0; i < 6; i++)
                {
                    forms[i] = AssetDatabase.LoadAssetAtPath<ProgressionConfig>(ConfigPath(i + 3));
                    if (forms[i] != null) continue;
                    PresentationAssets.RequireNew(ConfigPath(i + 3)); var form = ScriptableObject.CreateInstance<ProgressionConfig>();
                    form.FormId = ProgressionConfig.FormIds[i + 2]; form.DisplayName = Names[i]; form.EvolutionCopper = 0; form.SpeedCeiling = 26;
                    Tables(form, i); form.EvolutionResources = Costs(i + 3); form.VisualPrefab = FullEvolutionModels.Create(i + 3);
                    form.PreviewImage = Preview(form.VisualPrefab, i + 3); AssetDatabase.CreateAsset(form, ConfigPath(i + 3)); forms[i] = form;
                }
                for (var i = 0; i < forms.Length - 1; i++)
                {
                    if (forms[i].NextForm != null && forms[i].NextForm != forms[i + 1]) throw new InvalidOperationException("Existing custom evolution link preserved for review.");
                    if (forms[i].NextForm == forms[i + 1]) continue;
                    forms[i].NextForm = forms[i + 1]; EditorUtility.SetDirty(forms[i]);
                }
                var workshop = config.NextForm;
                if (workshop.NextForm != null && workshop.NextForm != forms[0]) throw new InvalidOperationException("Custom Workshop evolution preserved.");
                workshop.NextForm = forms[0]; workshop.EvolutionResources = new[] { new ResourceCost(EvolutionResource.Copper, 24) }; EditorUtility.SetDirty(workshop);
                Validate(config); AssetDatabase.SaveAssets();
                var view = flow.View;
                if (view.ResourceSummary == null)
                {
                    var parent = view.FormName.transform.parent; var text = new GameObject("M8 Owned Resources", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                    text.transform.SetParent(parent, false); var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1);
                    rect.anchoredPosition = new Vector2(0, -115); rect.sizeDelta = new Vector2(480, 29);
                    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 13; text.color = new Color(0.065f, 0.17f, 0.23f);
                    text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
                    var data = new SerializedObject(view); data.FindProperty("evolutionResources").objectReferenceValue = text; data.ApplyModifiedProperties();
                }
                EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save M8 integration.");
                Debug.Log("M8 integrated: eight forms, five resources, one Workshop/motor, no advanced resource spawns. No player save accessed.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            }
        }
        public static void Validate(ProgressionConfig root)
        {
            root.Validate(); var form = root; var count = 0;
            while (form != null)
            {
                if (form.Stage != ++count || form.PreviewImage == null || form.VisualPrefab == null) throw new ArgumentException("Incomplete M8 form chain or preview.");
                EvolutionSetup.ValidateVisual(form.VisualPrefab); form = form.NextForm;
            }
            if (count != 8) throw new ArgumentException("M8 requires exactly eight forms.");
        }
        private static ResourceCost[] Costs(int stage)
        {
            switch (stage)
            {
                case 3: return new[] { new ResourceCost(EvolutionResource.Iron, 30) };
                case 4: return new[] { new ResourceCost(EvolutionResource.Iron, 50), new ResourceCost(EvolutionResource.Copper, 12) };
                case 5: return new[] { new ResourceCost(EvolutionResource.Quartz, 40) };
                case 6: return new[] { new ResourceCost(EvolutionResource.AlloyCore, 24), new ResourceCost(EvolutionResource.Quartz, 20) };
                case 7: return new[] { new ResourceCost(EvolutionResource.EnergyShard, 30), new ResourceCost(EvolutionResource.AlloyCore, 12) };
                default: return Array.Empty<ResourceCost>();
            }
        }
        private static void Tables(ProgressionConfig form, int i)
        {
            float[][] power = { new[] {20f,22,24,26,28,30}, new[] {33f,36,39,42,45,48}, new[] {52f,56,60,65,70,75}, new[] {82f,88,94,101,108,115}, new[] {125f,135,145,156,168,180}, new[] {195f,210,225,242,260,280} };
            float[][] energy = { new[] {320f,345,370,400,430,460}, new[] {480f,510,540,575,610,650}, new[] {680f,720,760,805,850,900}, new[] {950f,1000,1060,1120,1180,1250}, new[] {1320f,1390,1460,1540,1620,1700}, new[] {1800f,1890,1980,2080,2190,2300} };
            int[][] p = { new[] {500,750,1000,1400,1850}, new[] {700,1000,1400,1900,2500}, new[] {1100,1500,2100,2800,3700}, new[] {2600,3500,4700,6200,8000}, new[] {4000,5400,7200,9500,12500}, new[] {5400,7200,9600,12600,16500} };
            int[][] e = { new[] {450,650,900,1250,1650}, new[] {600,900,1250,1700,2250}, new[] {950,1350,1900,2550,3400}, new[] {2400,3200,4300,5700,7400}, new[] {3600,4900,6600,8800,11500}, new[] {4900,6600,8900,11800,15400} };
            int[][] d = { new[] {400,600,850,1200,1600}, new[] {550,850,1200,1650,2200}, new[] {900,1300,1800,2450,3250}, new[] {2300,3100,4200,5500,7200}, new[] {3400,4700,6400,8500,11000}, new[] {4600,6300,8500,11200,14600} };
            float[][] boost = {new[] {4.5f,4.75f,5,5.25f,5.5f,5.75f},new[] {6f,6.25f,6.5f,6.75f,7,7.25f},new[] {7.5f,7.75f,8,8.25f,8.5f,8.75f},new[] {9f,9.2f,9.4f,9.6f,9.8f,10},new[] {10.25f,10.45f,10.65f,10.85f,11.05f,11.25f},new[] {11.5f,11.75f,12,12.25f,12.5f,12.75f}};
            float[][] recovery = {new[] {36f,38,40,42,44,46},new[] {48f,50,52,54,56,58},new[] {60f,62,64,66,68,70},new[] {72f,74,76,78,80,82},new[] {84f,86,88,90,92,94},new[] {96f,98,100,102,104,106}};
            form.Power = new UpgradeTable { Costs = p[i], Values = power[i] }; form.Energy = new UpgradeTable { Costs = e[i], Values = energy[i] };
            form.Drive = new UpgradeTable { Costs = d[i], Values = new[] {13f,13,13,13,13,13} }; form.MinimumBoost = boost[i];
            form.MaximumBoost = new[] {13f,13,13,13,13,13}; form.Recovery = recovery[i];
        }
        public static void RefreshPreviews()
        {
            for (var stage = 3; stage <= 8; stage++)
                Preview(AssetDatabase.LoadAssetAtPath<ProgressionConfig>(ConfigPath(stage)).VisualPrefab, stage, true);
        }
        private static Texture2D Preview(GameObject prefab, int stage, bool refresh = false)
        {
            var path = Root + "/Textures/M8Evo" + stage + "Preview.png"; var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (existing != null && !refresh) return existing;
            if (existing == null) PresentationAssets.RequireNew(path); var utility = new PreviewRenderUtility(); Texture2D image = null;
            try
            {
                utility.AddSingleGO(Object.Instantiate(prefab)); utility.cameraFieldOfView = 24;
                utility.camera.transform.position = new Vector3(2.6f, 1.9f, 3.8f); utility.camera.transform.LookAt(new Vector3(0, 0.75f, 0.15f));
                utility.camera.clearFlags = CameraClearFlags.SolidColor; utility.camera.backgroundColor = new Color(0.84f,0.91f,0.91f);
                utility.lights[0].intensity = 1.4f; utility.lights[0].transform.rotation = Quaternion.Euler(35,35,0); utility.lights[1].intensity = 1; utility.ambientColor = Color.gray;
                utility.BeginStaticPreview(new Rect(0,0,512,224)); utility.Render(true); image = utility.EndStaticPreview();
                File.WriteAllBytes(path, image.EncodeToPNG()); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            finally { if (image != null) Object.DestroyImmediate(image); utility.Cleanup(); }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DrillRunner.Editor
{
    public static class WorkshopSetup
    {
        public const string ConfigPath = FoundationSetup.Root + "/ScriptableObjects/PocketDrillProgression.asset";
        [MenuItem("Tools/DrillRunner/Setup M4 Workshop")]
        public static void Setup() => SetupScene(FoundationSetup.ScenePath);

        public static void SetupScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before M4 setup.");
            var scene = SceneManager.GetSceneByPath(path); var opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save the scene before M4 setup; unsaved edits preserved.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) throw new InvalidOperationException("Accepted M3 scene is required.");
            var previous = SceneManager.GetActiveScene();
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var roots = scene.GetRootGameObjects();
                var existing = roots.SelectMany(r => r.GetComponentsInChildren<WorkshopController>(true)).ToArray();
                if (existing.Length > 1) throw new InvalidOperationException("Duplicate Workshop controllers; preserved for review.");
                if (existing.Length == 1)
                {
                    var current = existing[0]; current.Configuration.Validate();
                    if (current.Run == null || current.View == null || current.View.StartButton == null || current.View.Panel == null ||
                        current.View.Rows.Length != 3 || current.View.Rows.Any(r => r == null) || current.View.PurchaseButtons.Length != 3 ||
                        current.View.PurchaseButtons.Any(b => b == null)) throw new InvalidOperationException("Incomplete Workshop references; preserved for review.");
                    if (current.View.GameplayGroups == null || current.View.GameplayGroups.Length == 0 || current.View.GameplayGroups.Any(g => g == null))
                    {
                        var visibility = new SerializedObject(current.View);
                        Array(visibility, "gameplayGroups", VisibilityGroups(hud: roots.SelectMany(r => r.GetComponentsInChildren<PrototypeHud>(true)).Single(),
                            current.View.Panel.transform, current.View.SaveButton.transform)); visibility.ApplyModifiedProperties();
                        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                        Debug.Log("DrillRunner M4 missing HUD visibility groups restored through Editor APIs."); return;
                    }
                    Debug.Log("DrillRunner M4 setup already configured; scene and assets unchanged."); return;
                }
                var run = roots.SelectMany(r => r.GetComponentsInChildren<RunProgress>(true)).Single();
                var hud = roots.SelectMany(r => r.GetComponentsInChildren<PrototypeHud>(true)).Single();
                var config = AssetDatabase.LoadAssetAtPath<ProgressionConfig>(ConfigPath);
                if (config == null)
                {
                    if (File.Exists(ConfigPath) || File.Exists(ConfigPath + ".meta")) throw new InvalidOperationException("Restore existing progression asset before setup.");
                    config = ScriptableObject.CreateInstance<ProgressionConfig>(); config.Validate();
                    AssetDatabase.CreateAsset(config, ConfigPath); AssetDatabase.SaveAssetIfDirty(config);
                }
                config.Validate();
                var root = new GameObject("M4 Workshop"); var flow = root.AddComponent<WorkshopController>(); var view = root.AddComponent<WorkshopView>();
                var gameplay = hud.GetComponent<RectTransform>();
                var panel = Rect(hud.transform, "M4 Workshop Panel", 0, 0, 0); Stretch(panel);
                var background = panel.gameObject.AddComponent<Image>(); background.color = new Color(0.035f, 0.065f, 0.09f, 1); background.raycastTarget = true;
                Label(panel, "Title", "WORKSHOP", 26, 34, 48);
                var balance = Label(panel, "Permanent Balances", "COINS 0       COPPER 0", 77, 23, 40);
                var display = Rect(panel, "Pocket Drill Presentation", 122, 492, 110); Image(display, new Color(0.07f, 0.14f, 0.18f));
                Label(display, "Form", "POCKET DRILL  /  FORM 1", 6, 21, 32);
                var body = Rect(display, "Placeholder Body", 53, 120, 30); Image(body, new Color(0.08f, 0.65f, 0.67f));
                var tip = Rect(display, "Placeholder Tip", 50, 25, 25); tip.anchoredPosition = new Vector2(70, -50); tip.localRotation = Quaternion.Euler(0, 0, 45); Image(tip, new Color(0.95f, 0.68f, 0.2f));
                var rows = new Text[3]; var prices = new Text[3]; var purchases = new Button[3];
                for (var i = 0; i < 3; i++)
                {
                    var card = Rect(panel, "Upgrade " + (UpgradeCategory)i, 248 + i * 169, 492, 157); Image(card, new Color(0.075f, 0.13f, 0.17f));
                    rows[i] = Label(card, "Attributes", "UPGRADE", 9, 22, 83);
                    purchases[i] = Button(card, "Purchase", "UPGRADE", 101, 450, 44, out prices[i]);
                }
                var start = Button(panel, "Start Run", "START RUN", 775, 492, 60, out _);
                var status = Label(panel, "Save Status", "Progress loads at startup", 893, 17, 60); status.horizontalOverflow = HorizontalWrapMode.Wrap;
                var attributes = Label(gameplay, "M4 Effective Attributes", "POWER 5     ENERGY 100 / 100", 251, 19, 36);
                var banking = Label(gameplay, "M4 Banking", "", 385, 18, 70); banking.horizontalOverflow = HorizontalWrapMode.Wrap;
                var open = Button(gameplay, "Open Workshop", "WORKSHOP", 470, 260, 48, out _);
                var save = Button(gameplay, "Retry Save", "RETRY SAVE", 530, 260, 44, out _); save.gameObject.SetActive(false);
                var groups = VisibilityGroups(hud, panel, save.transform);
                var restartLabel = hud.RestartButton.GetComponentInChildren<Text>();
                if (restartLabel != null) restartLabel.text = "RETRY / QA RESET";
                var viewData = new SerializedObject(view);
                Ref(viewData, "controller", flow); Ref(viewData, "panel", panel.gameObject); Ref(viewData, "gameplay", gameplay.gameObject);
                Ref(viewData, "balance", balance); Ref(viewData, "status", status); Ref(viewData, "banking", banking);
                Ref(viewData, "startRun", start); Ref(viewData, "openWorkshop", open); Ref(viewData, "retrySave", save);
                Array(viewData, "rows", rows); Array(viewData, "prices", prices); Array(viewData, "purchase", purchases); viewData.ApplyModifiedProperties();
                Array(viewData, "gameplayGroups", groups); viewData.ApplyModifiedProperties();
                var controllerData = new SerializedObject(flow);
                Ref(controllerData, "configuration", config); Ref(controllerData, "run", run); Ref(controllerData, "input", hud.Input); Ref(controllerData, "view", view); controllerData.ApplyModifiedProperties();
                var hudData = new SerializedObject(hud); Ref(hudData, "workshop", flow); Ref(hudData, "attributesText", attributes); hudData.ApplyModifiedProperties();
                panel.gameObject.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the M4 scene.");
                Debug.Log("DrillRunner M4 Workshop and persistence setup complete. Editor opens the convenient gameplay entry; players open Workshop.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static CanvasGroup[] VisibilityGroups(PrototypeHud hud, Transform panel, Transform save)
        {
            return hud.transform.Cast<Transform>().Where(t => t != panel && t != save).Select(t =>
            {
                var group = t.GetComponent<CanvasGroup>();
                return group != null ? group : t.gameObject.AddComponent<CanvasGroup>();
            }).ToArray();
        }
        private static RectTransform Rect(Transform parent, string name, float top, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -top); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private static void Image(RectTransform rect, Color color)
        { var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; }
        private static Text Label(Transform parent, string name, string value, float top, int size, float height)
        {
            var label = Rect(parent, name, top, 480, height).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.text = value; label.fontSize = size;
            label.color = new Color(0.92f, 0.96f, 0.98f); label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false; return label;
        }
        private static Button Button(Transform parent, string name, string value, float top, float width, float height, out Text label)
        {
            var rect = Rect(parent, name, top, width, height); var image = rect.gameObject.AddComponent<Image>(); image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = new Color(0.08f, 0.47f, 0.5f); colors.highlightedColor = new Color(0.12f, 0.62f, 0.65f);
            colors.pressedColor = new Color(0.05f, 0.3f, 0.33f); colors.disabledColor = new Color(0.15f, 0.21f, 0.24f); button.colors = colors;
            label = Label(rect, "Label", value, 0, 21, height); label.rectTransform.sizeDelta = new Vector2(width - 12, height); return button;
        }
        private static void Ref(SerializedObject data, string property, UnityEngine.Object value) => data.FindProperty(property).objectReferenceValue = value;
        private static void Array<T>(SerializedObject data, string property, T[] values) where T : UnityEngine.Object
        { var p = data.FindProperty(property); p.arraySize = values.Length; for (var i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
    }
}

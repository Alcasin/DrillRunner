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
    public static class DrillingPrototypeSetup
    {
        private const string Root = FoundationSetup.Root;
        public const string EncounterName = "M2 Encounter";

        [MenuItem("Tools/DrillRunner/Setup M2 Drilling Prototype")]
        public static void Setup() => SetupScene(FoundationSetup.ScenePath);

        public static void SetupScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before M2 setup.");
            var scene = SceneManager.GetSceneByPath(path);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (!openedHere && scene.isDirty)
                throw new InvalidOperationException("Save the Gameplay scene before M2 setup; unsaved work was preserved.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                throw new InvalidOperationException("The M1 Gameplay scene is required.");

            EnsureDrillPrefab();
            var wood = EnsureMaterial("wood", "Wood", 2, 70, 8, new Color(0.7f, 0.4f, 0.16f));
            var brick = EnsureMaterial("brick", "Brick", 5, 140, 12, new Color(0.8f, 0.25f, 0.13f));
            var stone = EnsureMaterial("stone", "Stone", 9, 240, 18, new Color(0.62f, 0.68f, 0.76f));
            var woodPrefab = EnsureObstaclePrefab(wood, new Vector3(1.6f, 2, 0.65f));
            var brickPrefab = EnsureObstaclePrefab(brick, new Vector3(1.8f, 2, 0.8f));
            var stonePrefab = EnsureObstaclePrefab(stone, new Vector3(1.8f, 2, 0.8f));
            var previous = SceneManager.GetActiveScene();
            if (openedHere)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var roots = scene.GetRootGameObjects();
                var motor = roots.SelectMany(r => r.GetComponentsInChildren<DrillMotor>(true)).Single();
                var hud = roots.SelectMany(r => r.GetComponentsInChildren<PrototypeHud>(true)).Single();
                var changed = false;
                changed |= ConfigureMotor(motor);
                var encounter = roots.SingleOrDefault(r => r.name == EncounterName);
                if (encounter == null)
                {
                    encounter = new GameObject(EncounterName);
                    Undo.RegisterCreatedObjectUndo(encounter, "Create M2 encounter");
                    Spawn(woodPrefab, "Wood 01", 35, encounter.transform);
                    Spawn(brickPrefab, "Brick 02", 65, encounter.transform);
                    Spawn(stonePrefab, "Stone 03", 95, encounter.transform);
                    Spawn(woodPrefab, "Wood 04", 130, encounter.transform);
                    changed = true;
                }
                var targets = encounter.GetComponentsInChildren<DrillObstacle>(true);
                if (targets.Length == 0)
                    throw new InvalidOperationException("Existing M2 Encounter is empty; preserved for review.");
                foreach (var target in targets)
                {
                    if (target.Definition == null)
                        throw new InvalidOperationException("Missing material on " + target.name);
                    target.Definition.Validate();
                    DrillContacts.WorldBounds(target.GetComponent<BoxCollider>());
                }
                var serialized = new SerializedObject(motor);
                var list = serialized.FindProperty("obstacles");
                var existing = motor.Obstacles;
                if (existing.Any(o => o == null) || existing.Distinct().Count() != existing.Length)
                    throw new InvalidOperationException("Existing obstacle references contain nulls or duplicates; preserved for review.");
                foreach (var target in targets)
                    if (!existing.Contains(target))
                    {
                        list.InsertArrayElementAtIndex(list.arraySize);
                        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = target;
                        changed = true;
                    }
                serialized.ApplyModifiedProperties();
                changed |= EnsureHud(hud);
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new IOException("Could not save M2 scene: " + path);
                }
                Debug.Log("DrillRunner M2 scene ready: " + path + (changed ? " (saved)." : " (already configured; unchanged)."));
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
                if (openedHere && SceneManager.sceneCount > 1)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void EnsureDrillPrefab()
        {
            var contents = PrefabUtility.LoadPrefabContents(MovementPrototypeSetup.PrefabPath);
            try
            {
                var motor = contents.GetComponent<DrillMotor>();
                if (motor == null)
                    throw new InvalidOperationException("M1 drill prefab has no motor.");
                if (ConfigureMotor(motor))
                    PrefabUtility.SaveAsPrefabAsset(contents, MovementPrototypeSetup.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static bool ConfigureMotor(DrillMotor motor)
        {
            var box = motor.GetComponent<BoxCollider>();
            DrillContacts.WorldBounds(box);
            var changed = false;
            var tip = motor.transform.Find("Drill Contact Tip");
            if (tip == null)
            {
                tip = new GameObject("Drill Contact Tip").transform;
                tip.SetParent(motor.transform, false);
                tip.localPosition = new Vector3(box.center.x, box.center.y, box.center.z + box.size.z / 2);
                changed = true;
            }
            changed |= ReferenceIfMissing(motor, "footprint", box);
            changed |= ReferenceIfMissing(motor, "drillTip", tip);
            motor.Drilling.Validate();
            return changed;
        }

        private static MaterialDefinition EnsureMaterial(string id, string name, float hardness, float durability, float energy, Color color)
        {
            var path = Root + "/ScriptableObjects/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<MaterialDefinition>(path);
            if (existing != null)
            {
                existing.Validate();
                return existing;
            }
            RequireNewPath(path);
            var definition = ScriptableObject.CreateInstance<MaterialDefinition>();
            definition.MaterialId = id; definition.DisplayName = name; definition.Hardness = hardness;
            definition.Durability = durability; definition.DrillingEnergyPerSecond = energy; definition.FeedbackColor = color;
            definition.Validate();
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssetIfDirty(definition);
            return definition;
        }

        private static GameObject EnsureObstaclePrefab(MaterialDefinition definition, Vector3 size)
        {
            var path = Root + "/Prefabs/Obstacle" + definition.DisplayName + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;
            RequireNewPath(path);
            var materialPath = Root + "/Materials/Obstacle" + definition.DisplayName + ".mat";
            var visual = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (visual == null)
            {
                RequireNewPath(materialPath);
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP/Lit is missing.");
                visual = new Material(shader) { name = "Obstacle " + definition.DisplayName };
                visual.SetColor("_BaseColor", definition.FeedbackColor);
                AssetDatabase.CreateAsset(visual, materialPath);
                AssetDatabase.SaveAssetIfDirty(visual);
            }
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                root.name = "Obstacle " + definition.DisplayName;
                root.transform.localScale = size;
                root.GetComponent<MeshRenderer>().sharedMaterial = visual;
                var obstacle = root.AddComponent<DrillObstacle>();
                ReferenceIfMissing(obstacle, "definition", definition);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void Spawn(GameObject prefab, string name, float z, Transform parent)
        {
            var obstacle = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obstacle.name = name;
            obstacle.transform.position = new Vector3(0, 1, z);
        }

        private static bool EnsureHud(PrototypeHud hud)
        {
            var panel = hud.transform.Find("M2 Feedback");
            var changed = false;
            if (panel == null)
            {
                panel = new GameObject("M2 Feedback", typeof(RectTransform)).transform;
                panel.SetParent(hud.transform, false);
                var rect = (RectTransform)panel;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
                CreateBar(panel, "Energy", 50, new Color(0.1f, 0.85f, 0.75f), 1);
                CreateLabel(panel, "Energy Label", 80, "ENERGY");
                CreateBar(panel, "Durability", 110, new Color(1, 0.65f, 0.15f), 0);
                CreateLabel(panel, "Material Label", 140, "DRILL PROGRESS");
                changed = true;
            }
            changed |= ReferenceIfMissing(hud, "energyFill", panel.Find("Energy/Fill").GetComponent<Image>());
            changed |= ReferenceIfMissing(hud, "durabilityFill", panel.Find("Durability/Fill").GetComponent<Image>());
            changed |= ReferenceIfMissing(hud, "materialText", panel.Find("Material Label").GetComponent<Text>());
            return changed;
        }

        private static void CreateBar(Transform parent, string name, float y, Color color, float initial)
        {
            var background = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            background.transform.SetParent(parent, false);
            SetRect(background.rectTransform, y, 20);
            background.color = new Color(0.03f, 0.06f, 0.08f, 0.95f); background.raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fill.transform.SetParent(background.transform, false);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.sizeDelta = new Vector2(-6, -6);
            fill.color = color; fill.raycastTarget = false;
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = initial;
        }

        private static void CreateLabel(Transform parent, string name, float y, string value)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            SetRect(label.rectTransform, y, 26);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 17;
            label.color = Color.white; label.alignment = TextAnchor.MiddleCenter; label.text = value; label.raycastTarget = false;
        }

        private static void SetRect(RectTransform rect, float y, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(440, height);
        }

        private static bool ReferenceIfMissing(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            if (value == null) throw new InvalidOperationException("Missing M2 reference " + property);
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty(property);
            if (field.objectReferenceValue == value) return false;
            if (field.objectReferenceValue != null)
                throw new InvalidOperationException("Existing reference differs: " + target.name + "." + property + "; preserved for review.");
            field.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
            return true;
        }

        private static void RequireNewPath(string path)
        {
            if (File.Exists(path) || File.Exists(path + ".meta"))
                throw new InvalidOperationException("Restore the existing asset before setup: " + path);
        }
    }
}

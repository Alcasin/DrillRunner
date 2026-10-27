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
    public static class ModularTrackSetup
    {
        private const string Root = FoundationSetup.Root;
        public const string CatalogPath = Root + "/ScriptableObjects/WorkshopYardCatalog.asset";
        public const string SectionFolder = Root + "/Prefabs/TrackSections";

        [MenuItem("Tools/DrillRunner/Setup M3 Modular Track")]
        public static void Setup() => SetupScene(FoundationSetup.ScenePath);

        public static void SetupScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before M3 setup.");
            var scene = SceneManager.GetSceneByPath(path); var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (!openedHere && scene.isDirty) throw new InvalidOperationException("Save the scene before M3 setup; unsaved edits were preserved.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) throw new InvalidOperationException("The accepted M2 scene is required.");
            if (!AssetDatabase.IsValidFolder(SectionFolder)) AssetDatabase.CreateFolder(Root + "/Prefabs", "TrackSections");
            var oldActive = SceneManager.GetActiveScene();
            if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var roots = scene.GetRootGameObjects();
                var motor = roots.SelectMany(r => r.GetComponentsInChildren<DrillMotor>(true)).Single();
                var hud = roots.SelectMany(r => r.GetComponentsInChildren<PrototypeHud>(true)).Single();
                var existing = roots.SelectMany(r => r.GetComponentsInChildren<RunProgress>(true)).ToArray();
                if (existing.Length > 1) throw new InvalidOperationException("Multiple run trackers exist; none were removed.");
                if (existing.Length == 1)
                {
                    if (existing[0].Motor != motor || existing[0].Track == null || existing[0].Track.Catalog == null ||
                        existing[0].Track.StarterSections.Length != 4 || existing[0].Track.StarterSections.Any(s => s == null) ||
                        hud.RewardText == null || hud.SummaryText == null)
                        throw new InvalidOperationException("Existing modular-run references are incomplete; preserved for review.");
                    new SectionSequence(1337, existing[0].Track.Catalog, motor.Settings.FootprintHalfWidth);
                    Debug.Log("DrillRunner M3 setup already configured; assets and scene unchanged.");
                    return;
                }

                ConfigureReward("Wood", ObstacleRewardKind.Coins, 1);
                ConfigureReward("Brick", ObstacleRewardKind.Coins, 2);
                ConfigureReward("Stone", ObstacleRewardKind.Coins, 3);
                var copperPrefab = EnsureCopper();
                var open = EnsureSection("Open", Array.Empty<Placement>());
                var wood = LoadObstacle("Wood"); var brick = LoadObstacle("Brick"); var stone = LoadObstacle("Stone");
                var variants = new[]
                {
                    open,
                    EnsureSection("WoodOffset", new[] { new Placement(wood, -0.9f, 20, 1.2f) }),
                    EnsureSection("BrickOffset", new[] { new Placement(brick, 0.8f, 20, 1.3f) }),
                    EnsureSection("Mixed", new[] { new Placement(wood, -0.9f, 14, 1.2f), new Placement(brick, 0.9f, 28, 1.3f) }),
                    EnsureSection("StoneBypass", new[] { new Placement(stone, 0, 20, 1.8f) }),
                    EnsureSection("Copper", new[] { new Placement(copperPrefab, 0, 20, 1.2f) }),
                    EnsureSection("RiskReward", new[] { new Placement(stone, -1.6f, 20, 1), new Placement(copperPrefab, 1.5f, 20, 0.8f) })
                };
                var catalog = AssetDatabase.LoadAssetAtPath<TrackCatalog>(CatalogPath);
                if (catalog == null)
                {
                    RequireNew(CatalogPath); catalog = ScriptableObject.CreateInstance<TrackCatalog>();
                    catalog.Fallback = open; catalog.Variants = variants; AssetDatabase.CreateAsset(catalog, CatalogPath);
                    AssetDatabase.SaveAssetIfDirty(catalog);
                }
                new SectionSequence(1337, catalog, motor.Settings.FootprintHalfWidth);
                var encounter = roots.Single(r => r.name == DrillingPrototypeSetup.EncounterName);
                var starterScenes = new TrackSection[4]; var templates = new TrackSection[4];
                if (motor.Obstacles.Length != 4) throw new InvalidOperationException("Expected the accepted four-obstacle opening; existing layout was preserved.");
                for (var i = 0; i < 4; i++)
                {
                    var sectionRoot = new GameObject("Starter " + i);
                    sectionRoot.transform.SetParent(encounter.transform, false); sectionRoot.transform.position = Vector3.forward * (i * 40);
                    motor.Obstacles[i].transform.SetParent(sectionRoot.transform, true);
                    var section = AddSectionGeometry(sectionRoot, "Starter" + i, new[] { motor.Obstacles[i] });
                    if (!SectionSequence.IsSafe(section, catalog, motor.Settings.FootprintHalfWidth))
                        throw new InvalidOperationException("Accepted opening does not satisfy modular route safety.");
                    var prefabPath = SectionFolder + "/Starter" + i + ".prefab";
                    RequireNew(prefabPath);
                    var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(sectionRoot, prefabPath, InteractionMode.AutomatedAction);
                    starterScenes[i] = sectionRoot.GetComponent<TrackSection>(); templates[i] = prefab.GetComponent<TrackSection>();
                }
                // Preserve the old reference assets/objects for earlier milestone QA;
                // they provide no runtime surface, collision, rails or distance marks.
                roots.Single(r => r.name == "Track Reference (Temporary)").SetActive(false);
                roots.Single(r => r.name == "M1 Track Guides").SetActive(false);
                var runRoot = new GameObject("M3 Workshop Run");
                var streamer = runRoot.AddComponent<TrackStreamer>(); var progress = runRoot.AddComponent<RunProgress>();
                var streamData = new SerializedObject(streamer);
                Ref(streamData, "motor", motor); Ref(streamData, "catalog", catalog);
                SetArray(streamData.FindProperty("starters"), starterScenes); SetArray(streamData.FindProperty("starterTemplates"), templates);
                streamData.ApplyModifiedProperties();
                var progressData = new SerializedObject(progress); Ref(progressData, "motor", motor); Ref(progressData, "track", streamer); progressData.ApplyModifiedProperties();
                CreateHud(hud, progress);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save M3 scene.");
                Debug.Log("DrillRunner M3 modular track, rewards, and Copper setup complete.");
            }
            finally
            {
                if (oldActive.IsValid() && oldActive.isLoaded) SceneManager.SetActiveScene(oldActive);
                if (openedHere && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private readonly struct Placement
        {
            public readonly GameObject Prefab; public readonly float X, Z, Width;
            public Placement(GameObject prefab, float x, float z, float width) { Prefab = prefab; X = x; Z = z; Width = width; }
        }

        private static GameObject LoadObstacle(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Obstacle" + name + ".prefab");

        private static TrackSection EnsureSection(string id, Placement[] placements)
        {
            var path = SectionFolder + "/" + id + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing.GetComponent<TrackSection>();
            RequireNew(path); var root = new GameObject(id);
            try
            {
                var targets = new DrillObstacle[placements.Length];
                for (var i = 0; i < placements.Length; i++)
                {
                    var p = placements[i]; if (p.Prefab == null) throw new InvalidOperationException("Missing M2 obstacle prefab.");
                    var obstacle = (GameObject)PrefabUtility.InstantiatePrefab(p.Prefab, root.transform);
                    obstacle.transform.localPosition = new Vector3(p.X, 1, p.Z);
                    var scale = obstacle.transform.localScale; scale.x = p.Width; obstacle.transform.localScale = scale;
                    targets[i] = obstacle.GetComponent<DrillObstacle>();
                }
                AddSectionGeometry(root, id, targets);
                return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<TrackSection>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static TrackSection AddSectionGeometry(GameObject root, string id, DrillObstacle[] targets)
        {
            var trackMaterial = AssetDatabase.LoadAssetAtPath<Material>(FoundationSetup.MaterialPath);
            var edgeMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/TrackEdges.mat");
            var markMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/TrackDistanceMarks.mat");
            var floor = Cube(root.transform, "Surface", new Vector3(0, -0.25f, 20), new Vector3(6, 0.5f, 40), trackMaterial, true);
            Cube(root.transform, "Left Edge", new Vector3(-3.075f, 0.05f, 20), new Vector3(0.15f, 0.1f, 40), edgeMaterial, false);
            Cube(root.transform, "Right Edge", new Vector3(3.075f, 0.05f, 20), new Vector3(0.15f, 0.1f, 40), edgeMaterial, false);
            for (var z = 5; z < 40; z += 10) Cube(root.transform, "Distance Mark " + z, new Vector3(0, 0.01f, z), new Vector3(0.07f, 0.01f, 2), markMaterial, false);
            var entrance = new GameObject("Entrance").transform; entrance.SetParent(root.transform, false);
            var exit = new GameObject("Exit").transform; exit.SetParent(root.transform, false); exit.localPosition = Vector3.forward * 40;
            var section = root.AddComponent<TrackSection>(); var data = new SerializedObject(section);
            data.FindProperty("sectionId").stringValue = id; Ref(data, "entrance", entrance); Ref(data, "exit", exit); Ref(data, "surface", floor.GetComponent<BoxCollider>());
            SetArray(data.FindProperty("obstacles"), targets); data.ApplyModifiedProperties();
            return section;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool collider)
        {
            if (material == null) throw new InvalidOperationException("Missing M1 track material.");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name; cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position; cube.transform.localScale = scale; cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
            return cube;
        }

        private static void ConfigureReward(string name, ObstacleRewardKind kind, int amount)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MaterialDefinition>(Root + "/ScriptableObjects/" + name + ".asset");
            if (asset == null) throw new InvalidOperationException("Missing M2 material " + name);
            var data = new SerializedObject(asset); data.FindProperty("RewardKind").enumValueIndex = (int)kind;
            data.FindProperty("RewardAmount").intValue = amount; data.ApplyModifiedProperties(); AssetDatabase.SaveAssetIfDirty(asset);
        }

        private static GameObject EnsureCopper()
        {
            const string dataPath = Root + "/ScriptableObjects/CopperOre.asset";
            var definition = AssetDatabase.LoadAssetAtPath<MaterialDefinition>(dataPath);
            if (definition == null)
            {
                RequireNew(dataPath); definition = ScriptableObject.CreateInstance<MaterialDefinition>();
                definition.MaterialId = "copper_ore"; definition.DisplayName = "Copper Ore"; definition.Hardness = 4; definition.Durability = 90;
                definition.DrillingEnergyPerSecond = 10; definition.RewardKind = ObstacleRewardKind.Copper; definition.RewardAmount = 3;
                definition.FeedbackColor = new Color(0.12f, 0.7f, 0.64f); definition.Validate(); AssetDatabase.CreateAsset(definition, dataPath); AssetDatabase.SaveAssetIfDirty(definition);
            }
            var path = Root + "/Prefabs/ObstacleCopperOre.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing;
            RequireNew(path); var materialPath = Root + "/Materials/ObstacleCopperOre.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                RequireNew(materialPath); material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Copper Ore" };
                material.SetColor("_BaseColor", definition.FeedbackColor); material.SetFloat("_Metallic", 0.6f);
                AssetDatabase.CreateAsset(material, materialPath); AssetDatabase.SaveAssetIfDirty(material);
            }
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                root.name = "Copper Ore"; root.transform.localScale = new Vector3(1.2f, 2, 0.8f); root.GetComponent<MeshRenderer>().sharedMaterial = material;
                var obstacle = root.AddComponent<DrillObstacle>(); var data = new SerializedObject(obstacle); Ref(data, "definition", definition); data.ApplyModifiedProperties();
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CreateHud(PrototypeHud hud, RunProgress progress)
        {
            var label = Label(hud.transform, "M3 Rewards", 210, 21, 40);
            label.text = "0 m    COINS 0    COPPER 0";
            var summary = Label(hud.transform, "M3 Summary", 300, 23, 80); summary.text = "RUN COMPLETE"; summary.gameObject.SetActive(false);
            var data = new SerializedObject(hud); Ref(data, "runProgress", progress); Ref(data, "rewardText", label); Ref(data, "summaryText", summary); data.ApplyModifiedProperties();
        }

        private static Text Label(Transform parent, string name, float fromTop, int size, float height)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>(); label.transform.SetParent(parent, false);
            var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -fromTop); rect.sizeDelta = new Vector2(510, height);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = size; label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white; label.raycastTarget = false; return label;
        }

        private static void Ref(SerializedObject data, string name, UnityEngine.Object value)
        { if (value == null) throw new InvalidOperationException("Missing M3 reference: " + name); data.FindProperty(name).objectReferenceValue = value; }
        private static void SetArray<T>(SerializedProperty property, T[] values) where T : UnityEngine.Object
        { property.arraySize = values.Length; for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; }
        private static void RequireNew(string path)
        { if (File.Exists(path) || File.Exists(path + ".meta")) throw new InvalidOperationException("Existing asset needs restoration: " + path); }
    }
}

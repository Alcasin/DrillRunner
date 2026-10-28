using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DrillRunner.Editor
{
    public static class GameplayVarietySetup
    {
        public const string Folder = FoundationSetup.Root + "/Prefabs/Encounters";
        public const string DataFolder = FoundationSetup.Root + "/ScriptableObjects/Encounters";
        public const string LibraryPath = DataFolder + "/WorkshopYardEncounters.asset";
        [Serializable] private sealed class ReferenceData { public string Source = "M7 at cab310f plus uncommitted M7"; public ReferencePattern[] Patterns; public ReferenceSequence[] Sequences; }
        [Serializable] private sealed class ReferencePattern
        {
            public string Id; public int Category, Pressure, MinimumOrdinal, EarlyWeight, Weight, LateWeight; public float BypassX; public ReferencePlacement[] Obstacles;
        }
        [Serializable] private sealed class ReferencePlacement { public string Material; public Vector3 Position, Scale; }
        [Serializable] private sealed class ReferenceSequence { public int Seed; public string[] Ids; }
        public static void ExportM7Reference()
        {
            const string path = FoundationSetup.Root + "/Tests/Fixtures/M7-Reference.json";
            if (File.Exists(path)) throw new InvalidOperationException("The frozen M7 reference already exists; refusing to overwrite it.");
            EnsureFolder(FoundationSetup.Root + "/Tests", "Fixtures");
            var library = AssetDatabase.LoadAssetAtPath<EncounterLibrary>(LibraryPath); Validate(library);
            var reference = new ReferenceData();
            reference.Patterns = library.Patterns.Select(p => new ReferencePattern { Id = p.Section.SectionId, Category = (int)p.Category, Pressure = p.Pressure,
                MinimumOrdinal = p.MinimumOrdinal, EarlyWeight = p.EarlyWeight, Weight = p.Weight, LateWeight = p.LateWeight, BypassX = p.BypassX,
                Obstacles = p.Section.Obstacles.Select(o => new ReferencePlacement { Material = o.Definition.MaterialId, Position = o.transform.localPosition, Scale = o.transform.localScale }).ToArray() }).ToArray();
            reference.Sequences = new[] { 1337, 904, 2026 }.Select(seed =>
            { var sequence = new EncounterSequence(seed, library, EncounterEnvelope.Accepted); return new ReferenceSequence { Seed = seed, Ids = Enumerable.Range(0, 256).Select(_ => sequence.Next().SectionId).ToArray() }; }).ToArray();
            File.WriteAllText(path, JsonUtility.ToJson(reference, true)); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Frozen original M7 geometry, metadata and three golden sequences for equivalent-policy comparison.");
        }
        private readonly struct Placement
        {
            public readonly string Material; public readonly float X, Z, Width;
            public Placement(string material, float x, float z, float width) { Material = material; X = x; Z = z; Width = width; }
        }

        [MenuItem("Tools/DrillRunner/Setup M7 Gameplay Variety")]
        public static void Setup() => SetupScene(FoundationSetup.ScenePath);
        public static void SetupScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before M7 setup.");
            var scene = SceneManager.GetSceneByPath(path); var opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save the scene before M7 setup; unsaved work is preserved.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) throw new InvalidOperationException("Accepted M6-B scene required.");
            var previous = SceneManager.GetActiveScene(); if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var track = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TrackStreamer>(true)).Single();
                if (track.Encounters != null)
                {
                    if (track.Encounters.Revision < 2) RevisePatterns(track.Encounters);
                    Validate(track.Encounters); Debug.Log("DrillRunner M7-B configured; scene references preserved, repeat setup makes no changes."); return;
                }
                if (scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DrillPresentation>(true)).Single().VisualRevision != 5)
                    throw new InvalidOperationException("Complete the accepted M6-B integration first.");
                EnsureFolder(FoundationSetup.Root + "/Prefabs", "Encounters"); EnsureFolder(FoundationSetup.Root + "/ScriptableObjects", "Encounters");
                var library = AssetDatabase.LoadAssetAtPath<EncounterLibrary>(LibraryPath);
                if (library == null)
                {
                    PresentationAssets.RequireNew(LibraryPath); library = ScriptableObject.CreateInstance<EncounterLibrary>(); library.Catalog = track.Catalog;
                    var patterns = new System.Collections.Generic.List<EncounterDefinition>();
                    foreach (var side in new[] { -1, 1 })
                    {
                        var suffix = side < 0 ? "Left" : "Right";
                        patterns.Add(Pattern("Easy" + suffix, EncounterCategory.EasyFlow, "Two soft targets; the opposite edge stays open. Maintain momentum or collect small Coin rewards.",
                            0, 0, 4, 3, 2, -side * 2.05f, new Placement("Wood", side * 0.65f, 20, 0.9f), new Placement("Wood", side * 0.65f, 30, 0.9f)));
                        patterns.Add(Pattern("Dense" + suffix, EncounterCategory.DenseDrilling, "Three splinter targets or one Brick: choose a drilling line or preserve Energy on the clear outside edge.",
                            2, 3, 0, 3, 4, side * 2.05f, new Placement("Wood", side * 0.65f, 20, 0.7f), new Placement("Wood", side * 0.65f, 26, 0.7f),
                            new Placement("Wood", side * 0.65f, 32, 0.7f), new Placement("Brick", -side * 0.9f, 26, 0.7f)));
                        patterns.Add(Pattern("OreOpen" + suffix, EncounterCategory.RiskReward, "Unprotected Ore is accessible at starting Power: spend Energy for Copper or use the clear opposite edge.",
                            0, 0, 3, 2, 2, -side * 2.05f, new Placement("CopperOre", side * 1.1f, 22, 0.9f), new Placement("Wood", -side * 0.5f, 28, 0.8f)));
                        patterns.Add(Pattern("OreGuard" + suffix, EncounterCategory.RiskReward, "Brick then Ore on one aligned route. At Power 5 mining costs 51.6 Energy; the bypass costs no drilling Energy.",
                            2, 3, 0, 2, 2, -side * 2.05f, new Placement("Brick", side * 0.8f, 20, 0.9f), new Placement("CopperOre", side * 0.8f, 30, 0.9f)));
                        patterns.Add(Pattern("Escape" + suffix, EncounterCategory.PrecisionEscape, "Read the staggered Stone pair early and commit to the wide clear side. BLOCKED still allows lateral escape.",
                            2, 3, 0, 2, 2, -side * 2.05f, new Placement("Stone", side * 0.15f, 21, 1.3f), new Placement("Stone", side * 0.9f, 30, 1)));
                        patterns.Add(Pattern("Power" + suffix, EncounterCategory.PowerTest, "Parallel Wood and Stone choices: avoid Stone below Power 9, drill it after upgrading, or use the free edge.",
                            1, 2, 1, 2, 3, -side * 2.05f, new Placement("Stone", side * 1, 24, 1), new Placement("Wood", -side * 0.5f, 24, 0.8f)));
                    }
                    patterns.Add(Pattern("RecoveryOpen", EncounterCategory.Recovery, "Open deck gives pacing relief and preparation time. Energy does not regenerate.", 0, 0, 1, 1, 2, -2.05f));
                    patterns.Add(Pattern("RecoveryOre", EncounterCategory.Recovery, "Low-pressure open deck with one optional Ore. Supports reward cadence without forcing a costly guard.",
                        0, 0, 1, 1, 1, -2.05f, new Placement("CopperOre", 1.1f, 24, 0.9f)));
                    library.Patterns = patterns.ToArray(); Validate(library); AssetDatabase.CreateAsset(library, LibraryPath); AssetDatabase.SaveAssetIfDirty(library);
                }
                if (library.Revision < 2) RevisePatterns(library);
                Validate(library);
                if (library.Catalog != track.Catalog) throw new InvalidOperationException("M7 library/catalog mismatch; existing references preserved.");
                var data = new SerializedObject(track); data.FindProperty("encounters").objectReferenceValue = library; data.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save M7 integration.");
                Debug.Log("DrillRunner M7: 14 validated encounters, six categories, existing streamer and M6-B presentation integrated.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            }
        }
        private static void RevisePatterns(EncounterLibrary library)
        {
            foreach (var definition in library.Patterns)
            {
                var path = AssetDatabase.GetAssetPath(definition.Section); var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var section = root.GetComponent<TrackSection>(); var side = definition.name.EndsWith("Left", StringComparison.Ordinal) ? -1 : 1;
                    switch (definition.Category)
                    {
                        case EncounterCategory.EasyFlow:
                            definition.Arrangement = EncounterArrangement.FlowArc;
                            section.Obstacles[0].transform.localPosition = new Vector3(side * 0.3f, 1, 20);
                            section.Obstacles[1].transform.localPosition = new Vector3(side * 1f, 1, 30); break;
                        case EncounterCategory.DenseDrilling:
                            definition.Arrangement = EncounterArrangement.SplitCluster; definition.MinimumOrdinal = 1; definition.EarlyWeight = 2;
                            section.Obstacles.First(o => o.Definition.MaterialId == "brick").transform.localPosition = new Vector3(-side * 0.9f, 1, 20); break;
                        case EncounterCategory.RiskReward:
                            var guarded = definition.name.StartsWith("OreGuard", StringComparison.Ordinal);
                            definition.Arrangement = guarded ? EncounterArrangement.GuardedChoice : EncounterArrangement.OreChoice;
                            if (!guarded)
                                section.Obstacles.First(o => o.Definition.MaterialId == "wood").transform.localPosition = new Vector3(-side * 0.65f, 1, 22);
                            else if (section.Obstacles.Length == 2)
                            {
                                var wood = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FoundationSetup.Root + "/Prefabs/ObstacleWood.prefab"), root.transform);
                                wood.transform.localPosition = new Vector3(-side * 0.65f, 1, 20); wood.transform.localScale = new Vector3(0.8f, 2, 0.8f);
                                var data = new SerializedObject(section); var array = data.FindProperty("obstacles"); array.arraySize = 3; array.GetArrayElementAtIndex(2).objectReferenceValue = wood.GetComponent<DrillObstacle>(); data.ApplyModifiedProperties();
                            }
                            break;
                        case EncounterCategory.PrecisionEscape:
                            definition.Arrangement = EncounterArrangement.StoneStagger;
                            section.Obstacles[0].transform.localPosition = new Vector3(side * 0.4f, 1, 21);
                            section.Obstacles[1].transform.localPosition = new Vector3(side * 1f, 1, 30); break;
                        case EncounterCategory.PowerTest:
                            definition.Arrangement = EncounterArrangement.PowerFork; definition.MinimumOrdinal = 3; definition.EarlyWeight = 0; break;
                        case EncounterCategory.Recovery:
                            var empty = section.Obstacles.Length == 0;
                            definition.Arrangement = empty ? EncounterArrangement.QuietDeck : EncounterArrangement.OreChoice;
                            definition.MinimumOrdinal = empty ? 3 : 2; definition.EarlyWeight = empty ? 0 : 1;
                            if (empty) definition.LateWeight = 1; break;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path); EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            library.IntroductionSections = 1; library.OpeningBufferSections = 0; library.FirstSelectionCopper = false;
            library.CalmFirstEncounter = true; library.ReliefAfterHighPressure = true; library.ArrangementMemory = 2; library.EmptyRecoverySpacing = 4;
            library.Revision = 2; Validate(library); EditorUtility.SetDirty(library); AssetDatabase.SaveAssetIfDirty(library);
            Debug.Log("M7-B: one 40m introduction, early choices and Copper before 150m, distinct shapes and guarded/safe alternatives integrated.");
        }
        public static void Validate(EncounterLibrary library)
        {
            foreach (var pattern in library.Patterns)
                if (!EncounterSafety.Validate(pattern, library, EncounterEnvelope.Accepted, out var reason)) throw new ArgumentException(pattern.name + ": " + reason);
            foreach (var a in library.Patterns) foreach (var b in library.Patterns)
                if (!EncounterSafety.Transition(a.Section, b.Section, library, EncounterEnvelope.Accepted, out _)) throw new ArgumentException("Unsafe encounter transition.");
            var plan = new EncounterSequence(1337, library, EncounterEnvelope.Accepted);
            if (plan.ValidPatterns != library.Patterns.Length) throw new ArgumentException("Some encounter definitions were rejected.");
        }
        private static EncounterDefinition Pattern(string id, EncounterCategory category, string intent, int pressure, int minimum, int early, int weight, int late, float bypass, params Placement[] placements)
        {
            var path = DataFolder + "/" + id + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<EncounterDefinition>(path); if (existing != null) return existing;
            PresentationAssets.RequireNew(path); var definition = ScriptableObject.CreateInstance<EncounterDefinition>();
            definition.Section = Section(id, placements); definition.Category = category; definition.Intent = intent; definition.Pressure = pressure;
            definition.MinimumOrdinal = minimum; definition.EarlyWeight = early; definition.Weight = weight; definition.LateWeight = late; definition.BypassX = bypass;
            AssetDatabase.CreateAsset(definition, path); AssetDatabase.SaveAssetIfDirty(definition); return definition;
        }
        private static TrackSection Section(string id, Placement[] placements)
        {
            var path = Folder + "/" + id + ".prefab"; var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (existing != null) return existing.GetComponent<TrackSection>();
            PresentationAssets.RequireNew(path);
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModularTrackSetup.SectionFolder + "/Open.prefab")); root.name = id;
            try
            {
                var targets = new DrillObstacle[placements.Length];
                for (var i = 0; i < placements.Length; i++)
                {
                    var p = placements[i]; var source = AssetDatabase.LoadAssetAtPath<GameObject>(FoundationSetup.Root + "/Prefabs/Obstacle" + p.Material + ".prefab");
                    var target = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform); target.transform.localPosition = new Vector3(p.X, 1, p.Z);
                    target.transform.localScale = new Vector3(p.Width, 2, 0.8f); targets[i] = target.GetComponent<DrillObstacle>();
                }
                var data = new SerializedObject(root.GetComponent<TrackSection>()); data.FindProperty("sectionId").stringValue = id;
                var array = data.FindProperty("obstacles"); array.arraySize = targets.Length;
                for (var i = 0; i < targets.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = targets[i]; data.ApplyModifiedProperties();
                return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<TrackSection>();
            }
            finally { Object.DestroyImmediate(root); }
        }
        private static void EnsureFolder(string parent, string name) { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }
    }
}

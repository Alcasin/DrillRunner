using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DrillRunner.Editor
{
    public static class PresentationWorld
    {
        private const string Root = FoundationSetup.Root;
        public static void Combine(Transform container, string assetName, bool refresh = false)
        {
            var sources = container.GetComponentsInChildren<MeshRenderer>(); var groups = sources.GroupBy(r => r.sharedMaterial).ToArray();
            for (var i = 0; i < groups.Length; i++)
            {
                var group = groups[i].ToArray();
                var instances = group.Select(r => new CombineInstance { mesh = r.GetComponent<MeshFilter>().sharedMesh,
                    transform = container.worldToLocalMatrix * r.transform.localToWorldMatrix }).ToArray();
                var mesh = PresentationAssets.MeshAsset(assetName + i, () => { var merged = new Mesh(); merged.CombineMeshes(instances); return merged; }, refresh);
                PresentationAssets.Part(container, "Shared Batch " + i, Vector3.zero, Vector3.one, groups[i].Key, mesh);
            }
            foreach (var source in sources) Object.DestroyImmediate(source.gameObject);
        }
        public static void Obstacles()
        {
            foreach (var name in new[] { "Wood", "Brick", "Stone", "CopperOre" })
            {
                var path = Root + "/Prefabs/Obstacle" + name + ".prefab"; var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (root.transform.Find("M6 Surface") != null) continue;
                    var details = new GameObject("M6 Surface").transform; details.SetParent(root.transform, false);
                    root.GetComponent<MeshFilter>().sharedMesh = PresentationAssets.Bevel;
                    var material = name == "Wood" ? PresentationAssets.Material("M6WoodBase", new Color(0.48f, 0.25f, 0.1f), 0) :
                        name == "Brick" ? PresentationAssets.Material("M6Mortar", new Color(0.72f, 0.56f, 0.43f), 0) :
                        name == "Stone" ? PresentationAssets.Material("M6Slate", new Color(0.35f, 0.44f, 0.51f), 0.1f) :
                        PresentationAssets.Material("M6Ore", new Color(0.12f, 0.3f, 0.32f), 0.2f);
                    root.GetComponent<MeshRenderer>().sharedMaterial = material;
                    if (name == "Wood")
                    {
                        var wood = PresentationAssets.Material("M6WoodFace", new Color(0.78f, 0.48f, 0.22f), 0);
                        var grain = PresentationAssets.Material("M6WoodGrain", new Color(0.43f, 0.23f, 0.1f), 0);
                        for (var row = 0; row < 3; row++)
                        {
                            PresentationAssets.Part(details, "Plank " + row, new Vector3(0, -0.32f + row * 0.32f, -0.499f), new Vector3(0.94f, 0.29f, 0.025f), wood);
                            for (var line = 0; line < 2; line++) PresentationAssets.Part(details, "Grain", new Vector3(line * 0.14f - 0.1f, -0.37f + row * 0.32f + line * 0.09f, -0.517f), new Vector3(0.63f, 0.009f, 0.008f), grain);
                        }
                        foreach (var x in new[] { -0.37f, 0.37f }) PresentationAssets.Part(details, "Wood Rivet", new Vector3(x, 0, -0.523f), new Vector3(0.055f, 0.055f, 0.015f), PresentationAssets.Metal);
                    }
                    else if (name == "Brick")
                    {
                        var brick = PresentationAssets.Material("M6BrickFace", new Color(0.75f, 0.29f, 0.18f), 0);
                        for (var row = 0; row < 3; row++) for (var col = 0; col < 2; col++)
                            PresentationAssets.Part(details, "Brick Segment", new Vector3(-0.235f + col * 0.47f, -0.32f + row * 0.32f, -0.499f), new Vector3(0.435f, 0.275f, 0.036f), brick);
                    }
                    else if (name == "Stone")
                    {
                        var face = PresentationAssets.Material("M6SlateLight", new Color(0.55f, 0.65f, 0.69f), 0.1f);
                        for (var i = 0; i < 3; i++)
                        {
                            var line = PresentationAssets.Part(details, "Slate Vein", new Vector3(-0.06f + i * 0.05f, -0.24f + i * 0.24f, -0.505f), new Vector3(0.68f, 0.025f, 0.015f), face);
                            line.transform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -16 : 14);
                        }
                    }
                    else
                    {
                        for (var i = 0; i < 5; i++)
                        {
                            var crystal = PresentationAssets.Part(details, "Copper Crystal", new Vector3((i % 3 - 1) * 0.27f, i < 3 ? -0.1f : 0.22f, -0.49f), new Vector3(0.16f, 0.22f, 0.1f), i % 2 == 0 ? PresentationAssets.Copper : PresentationAssets.Paint);
                            crystal.transform.localRotation = Quaternion.Euler(0, 0, 35 + i * 11);
                        }
                        PresentationAssets.Part(details, "Ore Top Crystal", new Vector3(0.1f, 0.48f, 0.02f), new Vector3(0.3f, 0.03f, 0.48f), PresentationAssets.Copper);
                    }
                    Combine(details, "M6" + name + "Detail");
                    var cracks = new GameObject[3];
                    for (var i = 0; i < 3; i++)
                    {
                        var crack = PresentationAssets.Part(details, "Damage Stage " + i, new Vector3((i - 1) * 0.13f, 0.04f, -0.56f), new Vector3(0.018f, 0.57f, 0.01f), PresentationAssets.Dark);
                        crack.transform.localRotation = Quaternion.Euler(0, 0, -28 + i * 29); crack.SetActive(false); cracks[i] = crack;
                    }
                    var appearance = root.AddComponent<ObstacleAppearance>(); var data = new SerializedObject(appearance);
                    data.FindProperty("details").objectReferenceValue = details.gameObject; var array = data.FindProperty("cracks"); array.arraySize = 3;
                    for (var i = 0; i < 3; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = cracks[i]; data.ApplyModifiedProperties();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        public static void Sections()
        {
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { ModularTrackSetup.SectionFolder }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var floor = PresentationAssets.Material("M6Deck", new Color(0.36f, 0.52f, 0.54f), 0.12f);
            var ground = PresentationAssets.Material("M6YardGround", new Color(0.6f, 0.71f, 0.69f), 0);
            foreach (var path in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (root.transform.Find("M6 Yard Details") != null) continue;
                    root.transform.Find("Surface").GetComponent<MeshRenderer>().sharedMaterial = floor;
                    foreach (var child in root.transform.Cast<Transform>())
                    {
                        if (child.name.StartsWith("Distance Mark", StringComparison.Ordinal)) child.gameObject.SetActive(false);
                        if (child.name.EndsWith("Edge", StringComparison.Ordinal)) child.GetComponent<MeshRenderer>().sharedMaterial = PresentationAssets.Dark;
                    }
                    var details = new GameObject("M6 Yard Details").transform; details.SetParent(root.transform, false);
                    PresentationAssets.Part(details, "Workshop Yard Base", new Vector3(0, -0.48f, 20), new Vector3(14, 0.3f, 40), ground, PresentationAssets.Cube);
                    foreach (var z in new[] { 0.06f, 10, 20, 30 }) PresentationAssets.Part(details, "Deck Joint", new Vector3(0, 0.012f, z), new Vector3(5.85f, 0.012f, 0.045f), PresentationAssets.Cream);
                    foreach (var side in new[] { -1, 1 })
                    {
                        PresentationAssets.Part(details, "Boundary Inlay", new Vector3(side * 2.93f, 0.013f, 20), new Vector3(0.08f, 0.015f, 40), PresentationAssets.Paint);
                        foreach (var z in new[] { 5f, 15, 25, 35 })
                        {
                            PresentationAssets.Part(details, "Shoulder Bolt", new Vector3(side * 3.3f, 0.08f, z), new Vector3(0.3f, 0.3f, 0.32f), PresentationAssets.Dark);
                            PresentationAssets.Part(details, "Shoulder Cap", new Vector3(side * 3.3f, 0.25f, z), new Vector3(0.28f, 0.05f, 0.28f), PresentationAssets.Paint);
                        }
                        PresentationAssets.Part(details, "Workshop Crate", new Vector3(side * 5.3f, 0.07f, side < 0 ? 12 : 28), new Vector3(1.15f, 1.1f, 1.2f), PresentationAssets.Teal);
                        PresentationAssets.Part(details, "Crate Lid", new Vector3(side * 5.3f, 0.65f, side < 0 ? 12 : 28), new Vector3(1.2f, 0.08f, 1.25f), PresentationAssets.Metal);
                    }
                    Combine(details, "M6Yard"); PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        public static void ReviseYard()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { ModularTrackSetup.SectionFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (root.transform.Find("M6B Yard Variant") != null) continue;
                    // Authored, name-based variants never consume gameplay or Unity random state.
                    var hash = 0; foreach (var c in root.GetComponent<TrackSection>().SectionId) hash = (hash * 31 + c) & 0x7fffffff;
                    var variant = hash % 3; var details = new GameObject("M6B Yard Variant").transform; details.SetParent(root.transform, false);
                    for (var i = 0; i < 3; i++)
                    {
                        var z = 5 + i * 13 + variant;
                        PresentationAssets.Part(details, "Cross Deck Panel", new Vector3(0, 0.018f, z), new Vector3(5.6f, 0.008f, 1.3f), PresentationAssets.Material("M6Deck", Color.gray, 0.12f), PresentationAssets.Cube);
                        foreach (var x in new[] { -2.65f, 2.65f })
                            PresentationAssets.Part(details, "Panel Fastener", new Vector3(x, 0.027f, z), new Vector3(0.11f, 0.012f, 0.18f), PresentationAssets.Metal, PresentationAssets.Cube);
                    }
                    var side = variant == 1 ? -1 : 1; var equipmentZ = 8 + variant * 9;
                    PresentationAssets.Part(details, "Workshop Compressor", new Vector3(side * 4.8f, 0.25f, equipmentZ), new Vector3(1.3f, 1.35f, variant == 2 ? 2.5f : 1.4f), PresentationAssets.Dark);
                    PresentationAssets.Part(details, "Compressor Panel", new Vector3(side * 4.8f, 0.97f, equipmentZ), new Vector3(1.1f, 0.12f, 1.2f), PresentationAssets.Teal);
                    for (var i = 0; i < 3; i++)
                        PresentationAssets.Part(details, "Side Pipe", new Vector3(-side * (4.3f + i * 0.22f), 0.07f + i * 0.1f, 17 + variant * 3), new Vector3(0.11f, 0.11f, 7 + variant * 2), PresentationAssets.Metal);
                    Combine(details, "M6BYardVariant" + variant + "_"); PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
    }
}

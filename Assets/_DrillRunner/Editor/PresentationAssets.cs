using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DrillRunner.Editor
{
    public static class PresentationAssets
    {
        public const string Root = FoundationSetup.Root;
        public const string ThemePath = Root + "/ScriptableObjects/M6Presentation.asset";
        public static Mesh Bevel => MeshAsset("M6Bevel", RoundedBox);
        public static Material Paint => Material("M6Paint", new Color(1, 0.58f, 0.1f), 0.12f);
        public static Material Teal => Material("M6Teal", new Color(0.025f, 0.48f, 0.53f), 0.2f);
        public static Material Dark => Material("M6Carbon", new Color(0.055f, 0.105f, 0.15f), 0.1f);
        public static Material Metal => Material("M6Steel", new Color(0.68f, 0.81f, 0.85f), 0.75f);
        public static Material BitSteel => Material("M6BBitSteel", new Color(0.84f, 0.9f, 0.94f), 0.2f);
        public static Material Rubber => Material("M6Rubber", new Color(0.055f, 0.075f, 0.085f), 0);
        public static Material Cream => Material("M6Ivory", new Color(0.88f, 0.92f, 0.9f), 0.05f);
        public static Material Copper => Material("M6Copper", new Color(0.92f, 0.4f, 0.12f), 0.65f);
        private static Mesh cylinder;
        private static Mesh cube;
        public static Mesh Cube
        {
            get { if (cube == null) { var temporary = GameObject.CreatePrimitive(PrimitiveType.Cube); cube = temporary.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(temporary); } return cube; }
        }
        private static Mesh Cylinder
        {
            get
            {
                if (cylinder == null) { var temporary = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cylinder = temporary.GetComponent<MeshFilter>().sharedMesh; Object.DestroyImmediate(temporary); }
                return cylinder;
            }
        }
        public static Material Material(string name, Color color, float metallic)
        {
            var path = Root + "/Materials/" + name + ".mat"; var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing != null) return existing;
            RequireNew(path); var result = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            result.SetColor("_BaseColor", color); result.SetFloat("_Metallic", metallic); result.SetFloat("_Smoothness", metallic > 0.5f ? 0.58f : 0.32f);
            AssetDatabase.CreateAsset(result, path); AssetDatabase.SaveAssetIfDirty(result); return result;
        }
        public static Mesh MeshAsset(string name, Func<Mesh> build, bool refresh = false)
        {
            var path = Root + "/Models/" + name + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                if (refresh) { var replacement = build(); replacement.name = name; EditorUtility.CopySerialized(replacement, existing); Object.DestroyImmediate(replacement); EditorUtility.SetDirty(existing); AssetDatabase.SaveAssetIfDirty(existing); }
                return existing;
            }
            RequireNew(path); var mesh = build(); mesh.name = name; AssetDatabase.CreateAsset(mesh, path); AssetDatabase.SaveAssetIfDirty(mesh); return mesh;
        }
        private static Mesh RoundedBox()
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            var grid = new[] { -0.5f, -0.46f, -0.4f, 0.4f, 0.46f, 0.5f };
            var axes = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var face in axes)
            {
                var u = Mathf.Abs(face.y) > 0.5f ? Vector3.right : Vector3.up; var v = Vector3.Cross(face, u); var start = vertices.Count;
                for (var y = 0; y < 6; y++) for (var x = 0; x < 6; x++)
                {
                    var p = face * 0.5f + u * grid[x] + v * grid[y];
                    var core = new Vector3(Mathf.Clamp(p.x, -0.4f, 0.4f), Mathf.Clamp(p.y, -0.4f, 0.4f), Mathf.Clamp(p.z, -0.4f, 0.4f));
                    var n = (p - core).normalized; vertices.Add(core + n * 0.1f); normals.Add(n); uv.Add(new Vector2(x / 5f, y / 5f));
                }
                for (var y = 0; y < 5; y++) for (var x = 0; x < 5; x++)
                { var a = start + y * 6 + x; triangles.AddRange(new[] { a, a + 1, a + 7, a, a + 7, a + 6 }); }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); return mesh;
        }
        private static Mesh Helix()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            const int rings = 16, sides = 24;
            for (var z = 0; z <= rings; z++) for (var a = 0; a < sides; a++)
            {
                var t = z / (float)rings; var angle = a * Mathf.PI * 2 / sides;
                var radius = (0.5f * (1 - t) + 0.015f) * (0.84f + 0.16f * Mathf.Cos(angle * 3 - t * Mathf.PI * 7));
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, t));
            }
            for (var z = 0; z < rings; z++) for (var a = 0; a < sides; a++)
            {
                var i = z * sides + a; var j = z * sides + (a + 1) % sides;
                triangles.AddRange(new[] { i, j, i + sides, j, j + sides, i + sides });
            }
            var center = vertices.Count; vertices.Add(Vector3.zero);
            for (var a = 0; a < sides; a++) triangles.AddRange(new[] { center, (a + 1) % sides, a });
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private static Mesh FluteStripes()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); const int segments = 24;
            for (var flute = 0; flute < 3; flute++)
            {
                var start = vertices.Count;
                for (var i = 0; i <= segments; i++) for (var edge = 0; edge < 2; edge++)
                {
                    var t = 0.02f + i / (float)segments * 0.93f; var angle = flute * Mathf.PI * 2 / 3 + t * Mathf.PI * 7 / 3 + (edge == 0 ? -0.1f : 0.1f);
                    var radius = (0.5f * (1 - t) + 0.015f) * (0.84f + 0.16f * Mathf.Cos(angle * 3 - t * Mathf.PI * 7)) + 0.006f;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, t));
                }
                for (var i = 0; i < segments; i++) { var a = start + i * 2; triangles.AddRange(new[] { a, a + 1, a + 2, a + 1, a + 3, a + 2 }); }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private static Mesh DriveRing()
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); const int segments = 32;
            for (var i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2 / segments; var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                vertices.Add(radial * 0.53f + Vector3.back * 0.5f); vertices.Add(radial * 0.47f + Vector3.back * 0.5f);
                vertices.Add(radial * 0.53f + Vector3.forward * 0.5f); vertices.Add(radial * 0.47f + Vector3.forward * 0.5f);
            }
            for (var i = 0; i < segments; i++)
            {
                var a = i * 4; var b = (i + 1) % segments * 4;
                triangles.AddRange(new[] { a, b, a + 2, b, b + 2, a + 2, a + 1, a + 3, b + 1, b + 1, a + 3, b + 3,
                    a, a + 1, b, b, a + 1, b + 1, a + 2, b + 2, a + 3, b + 2, b + 3, a + 3 });
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        public static GameObject Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Mesh mesh = null)
        {
            var result = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); result.transform.SetParent(parent, false);
            result.transform.localPosition = position; result.transform.localScale = scale;
            result.GetComponent<MeshFilter>().sharedMesh = mesh != null ? mesh : Bevel; result.GetComponent<MeshRenderer>().sharedMaterial = material; return result;
        }
        public static void BuildModels()
        {
            foreach (var path in new[] { EvolutionSetup.PocketVisualPath, EvolutionSetup.WorkshopVisualPath, MovementPrototypeSetup.PrefabPath })
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var target = path == MovementPrototypeSetup.PrefabPath ? root.transform.Find("Visuals") : root.transform;
                    foreach (var child in target.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                    var advanced = path == EvolutionSetup.WorkshopVisualPath; Model(target, advanced);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        private static void Model(Transform target, bool advanced)
        {
            var paint = advanced ? Teal : Paint; var hullWidth = advanced ? 1.46f : 1.28f;
            Part(target, "Painted Chassis", new Vector3(0, 0.51f, -0.36f), new Vector3(hullWidth, 0.44f, 1.42f), paint);
            Part(target, "Armored Sill", new Vector3(0, 0.32f, -0.15f), new Vector3(hullWidth, 0.18f, 1.95f), Dark);
            foreach (var side in new[] { -1, 1 })
            {
                Part(target, "Rubber Track " + side, new Vector3(side * 0.64f, 0.26f, -0.15f), new Vector3(0.32f, 0.47f, 1.95f), Rubber);
                for (var i = 0; i < 6; i++) Part(target, "Track Cleat " + side + " " + i, new Vector3(side * 0.64f, 0.48f, -0.93f + i * 0.31f), new Vector3(0.3f, 0.055f, 0.11f), Dark);
                Part(target, "Side Armor " + side, new Vector3(side * 0.7f, 0.69f, -0.05f), new Vector3(0.07f, 0.24f, 1.5f), advanced ? Metal : Paint);
                for (var i = 0; i < 3; i++) Part(target, "Housing Vent " + side + " " + i, new Vector3(side * (advanced ? 0.56f : 0.47f), 0.85f, -0.84f + i * 0.16f), new Vector3(0.025f, 0.14f, 0.07f), Metal);
            }
            Part(target, "Motor Housing", new Vector3(0, advanced ? 0.86f : 0.82f, -0.67f), new Vector3(advanced ? 1.08f : 0.89f, advanced ? 0.34f : 0.28f, 0.76f), advanced ? Dark : paint);
            Part(target, "Motor Top Cap", new Vector3(0, advanced ? 1.05f : 0.98f, -0.67f), new Vector3(advanced ? 0.98f : 0.75f, 0.055f, 0.6f), advanced ? Copper : Cream);
            Part(target, "Drive Coupling", new Vector3(0, 0.89f, -0.12f), new Vector3(advanced ? 1.28f : 1.12f, 0.3f, 0.22f), advanced ? Copper : Dark);
            foreach (var side in new[] { -1, 1 })
                Part(target, "Bit Support " + side, new Vector3(side * 0.46f, 0.66f, 0.08f), new Vector3(0.13f, 0.38f, 0.45f), Metal);
            var rotor = new GameObject("Drill Rotor").transform; rotor.SetParent(target, false); rotor.localPosition = new Vector3(0, 0.94f, 0.02f);
            var bitScale = new Vector3(advanced ? 1.04f : 0.84f, advanced ? 1.04f : 0.84f, 1.81f);
            Part(rotor, "Fluted Steel Bit", Vector3.zero, bitScale, BitSteel, MeshAsset("M6HelicalBit", Helix, true));
            Part(rotor, "Helical Cutting Edges", Vector3.zero, bitScale, Dark, MeshAsset("M6BFluteStripes", FluteStripes));
            // One offset bright tooth makes rotation readable from behind without changing the spin axis.
            Part(rotor, "Index Tooth", new Vector3(0.26f, 0.25f, 0.43f), new Vector3(0.09f, 0.11f, 0.38f), advanced ? Copper : Cream);
            {
                Part(rotor, "Open Drive Collar", new Vector3(0, 0, 0.02f), new Vector3(bitScale.x, bitScale.y, 0.08f), advanced ? Copper : Dark,
                    MeshAsset("M6BDriveRing", DriveRing));
            }
            var assembly = new GameObject("Static Assembly").transform; assembly.SetParent(target, false);
            foreach (var child in target.Cast<Transform>().ToArray()) if (child != rotor && child != assembly && child.name != "Motor Housing") child.SetParent(assembly, false);
            PresentationWorld.Combine(assembly, advanced ? "M6WorkshopStatic" : "M6PocketStatic", true);
            PresentationWorld.Combine(rotor, advanced ? "M6WorkshopRotor" : "M6PocketRotor", true);
        }
        public static PresentationTheme EnsureTheme()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PresentationTheme>(ThemePath); if (existing != null) return existing;
            RequireNew(ThemePath); var theme = ScriptableObject.CreateInstance<PresentationTheme>();
            for (var i = 0; i < 10; i++) theme.Sounds[i] = Sound((FeedbackCue)i);
            var sprite = Sprite("M6Particle", true);
            var particlePath = Root + "/Materials/M6Particles.mat"; var particle = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            particle.SetTexture("_BaseMap", sprite.texture); particle.SetColor("_BaseColor", Color.white); particle.SetFloat("_Surface", 1);
            particle.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); particle.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            particle.SetFloat("_ZWrite", 0); particle.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); particle.renderQueue = 3000;
            RequireNew(particlePath); AssetDatabase.CreateAsset(particle, particlePath); AssetDatabase.SaveAssetIfDirty(particle); theme.ParticleMaterial = particle;
            theme.Validate(); AssetDatabase.CreateAsset(theme, ThemePath); AssetDatabase.SaveAssetIfDirty(theme); return theme;
        }
        public static Sprite Sprite(string name, bool radial = false)
        {
            var path = Root + "/UI/" + name + ".png"; var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path); if (existing != null) return existing;
            RequireNew(path); const int size = 64; var image = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f - 32, y + 0.5f - 32); float alpha;
                if (radial) alpha = Mathf.Clamp01((30 - p.magnitude) / 12);
                else { var q = new Vector2(Mathf.Max(Mathf.Abs(p.x) - 19, 0), Mathf.Max(Mathf.Abs(p.y) - 19, 0)); alpha = Mathf.Clamp01(12.5f - q.magnitude); }
                image.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); Object.DestroyImmediate(image); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.spriteBorder = radial ? Vector4.zero : new Vector4(18, 18, 18, 18); importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static AudioClip Sound(FeedbackCue cue)
        {
            var path = Root + "/Audio/M6" + cue + ".wav"; var existing = AssetDatabase.LoadAssetAtPath<AudioClip>(path); if (existing != null) return existing;
            RequireNew(path); const int rate = 22050; var loop = cue == FeedbackCue.Charge || cue == FeedbackCue.Drill;
            var seconds = loop ? 1f : cue == FeedbackCue.Evolution ? 0.75f : cue == FeedbackCue.CopperReward ? 0.42f : 0.25f;
            var length = Mathf.RoundToInt(seconds * rate); var samples = new short[length]; uint random = 8191; var filtered = 0d;
            for (var i = 0; i < length; i++)
            {
                var t = i / (double)rate; var u = t / seconds; random = random * 1664525 + 1013904223;
                var noise = (random / (double)uint.MaxValue * 2 - 1); filtered = filtered * 0.8 + noise * 0.2;
                double signal;
                if (cue == FeedbackCue.Charge) signal = (Math.Sin(2 * Math.PI * 140 * t) * 0.23 + Math.Sin(2 * Math.PI * 280 * t) * 0.06) * (0.8 + 0.2 * Math.Sin(2 * Math.PI * 4 * t));
                else if (cue == FeedbackCue.Drill) signal = Math.Sin(2 * Math.PI * 110 * t) * 0.18 + Math.Sin(2 * Math.PI * 330 * t) * 0.08 + filtered * 0.2;
                else if (cue == FeedbackCue.Break || cue == FeedbackCue.CopperBreak) signal = (filtered * 0.75 + Math.Sin(2 * Math.PI * (cue == FeedbackCue.Break ? 180 : 620) * t) * 0.28) * Math.Exp(-u * 8);
                else if (cue == FeedbackCue.Launch) signal = (Math.Sin(2 * Math.PI * (220 * t + 600 * t * t)) * 0.3 + filtered * 0.35) * Math.Sin(Math.PI * u);
                else if (cue == FeedbackCue.Blocked) signal = Math.Sin(2 * Math.PI * 125 * t) * 0.34 * Math.Exp(-u * 10) + Math.Sin(2 * Math.PI * 100 * t) * 0.1 * Math.Sin(Math.PI * u);
                else
                {
                    var step = (int)(u * 3); var frequency = cue == FeedbackCue.Finish ? 420 - step * 100 : cue == FeedbackCue.Evolution ? 440 + step * 220 : 660 + step * 165;
                    var note = u * 3 - step;
                    signal = (Math.Sin(2 * Math.PI * frequency * t) * 0.28 + Math.Sin(2 * Math.PI * frequency * 2 * t) * 0.05) * Math.Sin(Math.PI * note) * Math.Exp(-note * 2);
                }
                var edge = Math.Min(1, Math.Min(t / 0.008, (seconds - t) / 0.012)); signal *= Math.Max(0, edge);
                samples[i] = (short)(Math.Max(-0.6, Math.Min(0.6, signal)) * short.MaxValue);
            }
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2); foreach (var sample in samples) writer.Write(sample);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings; settings.loadType = AudioClipLoadType.DecompressOnLoad; settings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings; importer.forceToMono = true; importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        public static void RequireNew(string path)
        { if (File.Exists(path) || File.Exists(path + ".meta")) throw new InvalidOperationException("Existing asset needs restoration: " + path); }
    }
}

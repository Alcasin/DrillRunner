using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DrillRunner.Editor
{
    public static class MovementPrototypeSetup
    {
        public const string PrefabPath = FoundationSetup.Root + "/Prefabs/DrillPrototype.prefab";
        public const float TrackLength = 20000;
        private const string TemplateInputPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Tools/DrillRunner/Setup M1 Movement Prototype")]
        public static void Setup() => SetupScene(FoundationSetup.ScenePath);

        // An explicit path also permits testing setup against a disposable scene copy.
        public static void SetupScene(string scenePath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before M1 setup.");
            var scene = SceneManager.GetSceneByPath(scenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (!openedHere && scene.isDirty)
                throw new InvalidOperationException("Save the Gameplay scene before M1 setup; unsaved work was left untouched.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                throw new InvalidOperationException("Run M0 foundation setup first. Scene missing: " + scenePath);

            var previousActive = SceneManager.GetActiveScene();
            if (openedHere)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var roots = scene.GetRootGameObjects();
                var cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).ToArray();
                if (cameras.Length != 1 || !cameras[0].CompareTag("MainCamera"))
                    throw new InvalidOperationException("Expected one existing Main Camera; no cameras were replaced.");
                var track = roots.SingleOrDefault(r => r.name == "Track Reference (Temporary)");
                if (track == null)
                    throw new InvalidOperationException("M0 reference track was not found; no replacement was created.");
                var players = roots.SelectMany(r => r.GetComponentsInChildren<DrillMotor>(true)).ToArray();
                if (players.Length > 1)
                    throw new InvalidOperationException("Multiple drill motors found; resolve them before setup.");
                var changed = false;
                var motor = players.SingleOrDefault();
                if (motor == null)
                {
                    var prefab = EnsureDrillPrefab();
                    var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    Undo.RegisterCreatedObjectUndo(player, "Create M1 drill");
                    motor = player.GetComponent<DrillMotor>();
                    changed = true;
                }
                var input = motor.GetComponent<DrillPointerInput>();
                if (input == null)
                    throw new InvalidOperationException("Existing drill has no pointer input. Existing objects were not rebuilt.");

                if (!roots.Any(r => r.name == "M1 Track Guides"))
                {
                    // Upgrade only the known M0 slab. Preserve other custom track layouts.
                    if (track.transform.localScale == new Vector3(6, 0.5f, 50) &&
                        track.transform.position == new Vector3(0, -0.25f, 15))
                    {
                        Undo.RecordObject(track.transform, "Extend M1 reference track");
                        track.transform.localScale = new Vector3(6, 0.5f, TrackLength);
                        track.transform.position = new Vector3(0, -0.25f, TrackLength / 2 - 10);
                    }
                    else if (track.transform.localScale.z < TrackLength)
                        throw new InvalidOperationException("Custom track dimensions detected. Extend its length before running setup; not overwritten.");
                    CreateTrackGuides(motor.Settings);
                    changed = true;
                }

                var follower = cameras[0].GetComponent<RunnerCamera>();
                if (follower == null)
                {
                    follower = Undo.AddComponent<RunnerCamera>(cameras[0].gameObject);
                    SetReference(follower, "target", motor);
                    Undo.RecordObject(cameras[0], "Frame M1 camera");
                    cameras[0].fieldOfView = 60;
                    Undo.RecordObject(follower.transform, "Frame M1 camera");
                    follower.Snap();
                    changed = true;
                }
                else if (follower.Target != motor)
                    throw new InvalidOperationException("Existing camera target differs from the drill; preserved for review.");

                var huds = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PrototypeHud>(true)).ToArray();
                if (huds.Length > 1)
                    throw new InvalidOperationException("Multiple prototype HUDs found; none were deleted.");
                var hud = huds.SingleOrDefault();
                if (hud == null)
                {
                    hud = CreateHud(input);
                    changed = true;
                }
                else if (hud.Input != input)
                    throw new InvalidOperationException("Existing HUD input reference differs; preserved for review.");
                var raycaster = hud.GetComponent<GraphicRaycaster>();
                if (input.UiRaycaster == null)
                {
                    SetReference(input, "uiRaycaster", raycaster);
                    changed = true;
                }
                else if (input.UiRaycaster != raycaster)
                    throw new InvalidOperationException("Existing input UI raycaster differs; preserved for review.");

                var eventSystems = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).ToArray();
                if (eventSystems.Length == 0)
                {
                    CreateEventSystem();
                    changed = true;
                }
                else if (eventSystems.Length != 1 || eventSystems[0].GetComponent<InputSystemUIInputModule>() == null)
                    throw new InvalidOperationException("Expected one Input System UI EventSystem; existing systems were not replaced.");

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                        throw new IOException("Could not save M1 scene: " + scenePath);
                }
                Debug.Log("DrillRunner M1 scene ready: " + scenePath + (changed ? " (saved)." : " (already configured; unchanged)."));
            }
            finally
            {
                if (previousActive.IsValid() && previousActive.isLoaded)
                    SceneManager.SetActiveScene(previousActive);
                if (openedHere && SceneManager.sceneCount > 1)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject EnsureDrillPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
                return existing;
            if (File.Exists(PrefabPath) || File.Exists(PrefabPath + ".meta"))
                throw new InvalidOperationException("Existing drill prefab cannot be loaded; restore it rather than replacing its GUID.");
            var body = Material("DrillBody", new Color(1f, 0.61f, 0.08f));
            var tread = Material("DrillTreads", new Color(0.055f, 0.075f, 0.09f));
            var steel = Material("DrillSteel", new Color(0.52f, 0.64f, 0.7f), 0.7f);
            var glass = Material("DrillWindow", new Color(0.08f, 0.38f, 0.55f));
            var root = new GameObject("Drill Prototype");
            try
            {
                var motor = root.AddComponent<DrillMotor>();
                var input = root.AddComponent<DrillPointerInput>();
                SetReference(input, "motor", motor);
                SetReference(input, "actionsSource", AssetDatabase.LoadAssetAtPath<InputActionAsset>(FoundationSetup.InputPath));
                var footprint = root.AddComponent<BoxCollider>();
                footprint.center = new Vector3(0, 0.65f, 0.35f);
                footprint.size = new Vector3(1.7f, 1.3f, 3f);
                var visuals = new GameObject("Visuals").transform;
                visuals.SetParent(root.transform, false);
                Part("Body", PrimitiveType.Cube, visuals, new Vector3(0, 0.65f, -0.1f), new Vector3(1.25f, 0.7f, 1.65f), body);
                Part("Left Tread", PrimitiveType.Cube, visuals, new Vector3(-0.58f, 0.28f, -0.1f), new Vector3(0.35f, 0.5f, 1.9f), tread);
                Part("Right Tread", PrimitiveType.Cube, visuals, new Vector3(0.58f, 0.28f, -0.1f), new Vector3(0.35f, 0.5f, 1.9f), tread);
                Part("Cabin", PrimitiveType.Cube, visuals, new Vector3(0, 1.15f, -0.35f), new Vector3(0.8f, 0.4f, 0.7f), body);
                Part("Front Window", PrimitiveType.Cube, visuals, new Vector3(0, 1.17f, 0.015f), new Vector3(0.66f, 0.23f, 0.035f), glass);
                var bit = new GameObject("Forward Drill Bit").transform;
                bit.SetParent(visuals, false);
                for (var i = 0; i < 4; i++)
                {
                    var diameter = 0.95f - i * 0.24f;
                    var ring = Part("Drill Step " + i, PrimitiveType.Cylinder, bit,
                        new Vector3(0, 0.65f, 0.85f + i * 0.26f), new Vector3(diameter, 0.15f, diameter), steel);
                    ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
                }
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }

        private static Material Material(string name, Color color, float metallic = 0)
        {
            var path = FoundationSetup.Root + "/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            if (File.Exists(path) || File.Exists(path + ".meta"))
                throw new InvalidOperationException("Restore existing material before setup: " + path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP/Lit is missing.");
            var material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void CreateTrackGuides(DrillMovementSettings settings)
        {
            var root = new GameObject("M1 Track Guides");
            Undo.RegisterCreatedObjectUndo(root, "Create M1 track guides");
            var edgeMaterial = Material("TrackEdges", new Color(0.95f, 0.61f, 0.08f));
            foreach (var side in new[] { -1, 1 })
            {
                var edge = Part(side < 0 ? "Left Boundary" : "Right Boundary", PrimitiveType.Cube, root.transform,
                    new Vector3(settings.TrackCenterX + side * (settings.TrackWidth / 2 + 0.075f), 0.05f, TrackLength / 2 - 10),
                    new Vector3(0.15f, 0.1f, TrackLength), edgeMaterial);
                edge.isStatic = true;
            }
            // One small static mesh supplies distance cues; no runtime track generation.
            var meshPath = FoundationSetup.Root + "/Models/PrototypeDistanceMarks.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                if (File.Exists(meshPath) || File.Exists(meshPath + ".meta"))
                    throw new InvalidOperationException("Restore the existing distance-mark mesh before setup.");
                var count = Mathf.FloorToInt(TrackLength / 12);
                var vertices = new Vector3[count * 4];
                var triangles = new int[count * 6];
                for (var i = 0; i < count; i++)
                {
                    var z = i * 12f;
                    var v = i * 4;
                    vertices[v] = new Vector3(-0.035f, 0.015f, z);
                    vertices[v + 1] = new Vector3(-0.035f, 0.015f, z + 2);
                    vertices[v + 2] = new Vector3(0.035f, 0.015f, z + 2);
                    vertices[v + 3] = new Vector3(0.035f, 0.015f, z);
                    var t = i * 6;
                    triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                    triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
                }
                mesh = new Mesh { name = "Prototype Distance Marks", vertices = vertices, triangles = triangles };
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, meshPath);
                AssetDatabase.SaveAssetIfDirty(mesh);
            }
            var markings = new GameObject("Distance Marks", typeof(MeshFilter), typeof(MeshRenderer));
            markings.transform.SetParent(root.transform, false);
            markings.GetComponent<MeshFilter>().sharedMesh = mesh;
            markings.GetComponent<MeshRenderer>().sharedMaterial = Material("TrackDistanceMarks", new Color(0.48f, 0.54f, 0.58f));
            markings.isStatic = true;
        }

        private static PrototypeHud CreateHud(DrillPointerInput input)
        {
            var root = new GameObject("M1 Prototype HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root, "Create M1 HUD");
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540, 960);
            scaler.matchWidthOrHeight = 0.5f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var status = Label("State", root.transform, font, 28, "READY");
            Rect(status.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-70, -60), new Vector2(290, 40));
            var hint = Label("Hint", root.transform, font, 19, "Hold anywhere on the track to charge");
            Rect(hint.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -155), new Vector2(500, 35));
            var bar = Image("Charge Background", root.transform, new Color(0.04f, 0.07f, 0.1f, 0.95f));
            Rect(bar.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -115), new Vector2(440, 22));
            var fill = Image("Charge Fill", bar.transform, new Color(0.2f, 0.85f, 0.85f));
            Rect(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6, -6));
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0;
            var buttonImage = Image("Restart", root.transform, new Color(0.12f, 0.22f, 0.3f));
            buttonImage.raycastTarget = true;
            Rect(buttonImage.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -56), new Vector2(130, 46));
            var button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var buttonText = Label("Label", button.transform, font, 19, "RESTART");
            Rect(buttonText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var hud = root.AddComponent<PrototypeHud>();
            SetReference(hud, "input", input);
            SetReference(hud, "chargeFill", fill);
            SetReference(hud, "stateText", status);
            SetReference(hud, "hintText", hint);
            SetReference(hud, "restartButton", button);
            return hud;
        }

        private static Text Label(string name, Transform parent, Font font, int size, string content)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        private static Image Image(string name, Transform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Rect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private static void CreateEventSystem()
        {
            var root = new GameObject("M1 EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(root, "Create M1 UI input");
            var module = root.AddComponent<InputSystemUIInputModule>();
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(TemplateInputPath);
            if (asset == null)
                throw new InvalidOperationException("Template UI actions missing: " + TemplateInputPath);
            var references = AssetDatabase.LoadAllAssetsAtPath(TemplateInputPath).OfType<InputActionReference>().ToArray();
            InputActionReference Reference(string name) => references.First(r => r.action != null && r.action.id == asset.FindAction("UI/" + name, true).id);
            module.actionsAsset = asset;
            module.point = Reference("Point");
            module.leftClick = Reference("Click");
            module.rightClick = Reference("RightClick");
            module.middleClick = Reference("MiddleClick");
            module.scrollWheel = Reference("ScrollWheel");
            module.move = Reference("Navigate");
            module.submit = Reference("Submit");
            module.cancel = Reference("Cancel");
            module.trackedDevicePosition = Reference("TrackedDevicePosition");
            module.trackedDeviceOrientation = Reference("TrackedDeviceOrientation");
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            if (value == null)
                throw new InvalidOperationException("Missing dependency for " + target.name + "." + propertyName);
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }
    }
}

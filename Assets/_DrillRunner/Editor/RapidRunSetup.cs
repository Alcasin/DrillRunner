using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrillRunner.Editor
{
    public static class RapidRunSetup
    {
        public const string Path = FoundationSetup.Root + "/ScriptableObjects/RapidRunBalance.asset";
        private static readonly float[][] Coast = {
            new[]{1.8f,1.2f,0.9f,0.7f,0.56f,0.45f}, new[]{0.44f,0.4f,0.36f,0.32f,0.29f,0.26f},
            new[]{0.25f,0.23f,0.21f,0.195f,0.18f,0.165f}, new[]{0.16f,0.155f,0.15f,0.145f,0.14f,0.135f},
            new[]{0.13f,0.125f,0.12f,0.115f,0.11f,0.105f}, new[]{0.1f,0.097f,0.094f,0.091f,0.088f,0.085f},
            new[]{0.084f,0.081f,0.078f,0.075f,0.072f,0.069f}, new[]{0.068f,0.065f,0.062f,0.059f,0.056f,0.053f} };
        [MenuItem("Tools/DrillRunner/Setup M8-R Rapid Runs")]
        public static void Setup() => SetupScene(FoundationSetup.ScenePath);
        public static void SetupScene(string path)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before rapid-run setup.");
            var scene = SceneManager.GetSceneByPath(path); var opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) throw new InvalidOperationException("Save the scene before setup; custom work is preserved.");
            var previous = SceneManager.GetActiveScene(); if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var flow = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WorkshopController>(true)).Single();
                FullEvolutionSetup.Validate(flow.Configuration);
                var balance = AssetDatabase.LoadAssetAtPath<RapidRunBalance>(Path);
                if (balance == null)
                {
                    PresentationAssets.RequireNew(Path); balance = ScriptableObject.CreateInstance<RapidRunBalance>();
                    // Fixed form operating costs, chosen against preserved base/MAX capacities, never current upgrade levels.
                    var moving = new[] {6f,9,14,20,27,32,43,57};
                    balance.Forms = moving.Select((rate,i) => new FormConsumption { FormId = ProgressionConfig.FormIds[i], Moving = rate,
                        Blocked = rate + 1, DrillingBase = rate + 0.2f, MaterialScale = 0.1f, BoostDecayByDriveLevel = (float[])Coast[i].Clone() }).ToArray();
                    // Approximately 400 Coins per short Pocket attempt supports the unchanged 2,480-Coin first tier.
                    balance.Rewards = new RewardSettings { DistancePerPayout = 5, CoinsPerPayout = 9, ObstacleCoinMultiplier = 25 };
                    balance.ChargeSeconds = 1.1f; balance.Validate(); AssetDatabase.CreateAsset(balance, Path); AssetDatabase.SaveAssetIfDirty(balance);
                }
                if (balance.Revision < 3)
                {
                    for (var i = 0; i < 8; i++) balance.ForForm(ProgressionConfig.FormIds[i]).BoostDecayByDriveLevel = (float[])Coast[i].Clone();
                    balance.Revision = 3;
                    foreach (var guard in flow.Run.Track.Encounters.Patterns.Where(p => p.name.StartsWith("OreGuard", StringComparison.Ordinal)))
                    { guard.MinimumOrdinal = 2; guard.EarlyWeight = 1; EditorUtility.SetDirty(guard); AssetDatabase.SaveAssetIfDirty(guard); }
                    EditorUtility.SetDirty(balance); AssetDatabase.SaveAssetIfDirty(balance);
                }
                UltraShortRunSetup.Apply(flow.Configuration, balance);
                balance.Validate(); var data = new SerializedObject(flow); var reference = data.FindProperty("rapidRunBalance");
                if (reference.objectReferenceValue != null && reference.objectReferenceValue != balance) throw new InvalidOperationException("Custom rapid-run balance reference preserved.");
                var library = flow.Run.Track.Encounters;
                if (reference.objectReferenceValue == balance && library.OpeningCopperBySelection == 2)
                { Debug.Log("M8-R already configured; scene, tuning and assets unchanged."); return; }
                if (reference.objectReferenceValue != balance)
                {
                    reference.objectReferenceValue = balance; data.ApplyModifiedProperties(); EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save rapid-run integration.");
                }
                if (library.OpeningCopperBySelection != 2)
                { library.OpeningCopperBySelection = 2; EditorUtility.SetDirty(library); AssetDatabase.SaveAssetIfDirty(library); }
                GameplayVarietySetup.Validate(library);
                Debug.Log("M8-R configured: fixed per-form consumption, short charge, early Copper, existing reward sources and unchanged progression/save data.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened && SceneManager.sceneCount > 1) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}

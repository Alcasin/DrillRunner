using UnityEditor;
using UnityEngine;

namespace DrillRunner.Editor
{
    [CustomEditor(typeof(WorkshopController))]
    public sealed class WorkshopInspector : UnityEditor.Editor
    {
        private int temporaryCopper = 24;
        private int temporaryStage = 1;
        private long temporaryCoins = 500000, temporaryIron = 500, temporaryQuartz = 500, temporaryAlloy = 500, temporaryShards = 500;
        private bool maximum;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); var flow = (WorkshopController)target;
            EditorGUILayout.HelpBox("Editor gameplay entry preserves direct M0-M3 QA. Open WORKSHOP from Ready or results. Active-run QA Reset discards unbanked earnings. Player builds start in Workshop.", MessageType.Info);
            if (!Application.isPlaying || flow.Service == null) return;
            EditorGUILayout.SelectableLabel(flow.SavePath, EditorStyles.wordWrappedLabel, GUILayout.Height(40));
            var progress = flow.Service.Snapshot;
            EditorGUILayout.LabelField("Permanent Coins / Copper", progress.Coins + " / " + progress.Copper);
            EditorGUILayout.LabelField("Current form", flow.Service.ActiveForm.DisplayName);
            EditorGUILayout.LabelField("Power / Energy / Drive levels", progress.Level(UpgradeCategory.DrillPower) + " / " + progress.Level(UpgradeCategory.EnergyCapacity) + " / " + progress.Level(UpgradeCategory.DrivePower));
            EditorGUILayout.HelpBox(flow.Message ?? "", MessageType.Info);
            if (GUILayout.Button("Open Workshop (Ready / Completed only)")) flow.OpenWorkshop();
            if (GUILayout.Button("QA: Fresh Pocket - 0 upgrades (no disk writes)"))
                flow.UseTemporaryStage(1, 0, 0, 0, 0, 0, 0, false);
            temporaryCopper = Mathf.Max(0, EditorGUILayout.IntField("Temporary Copper", temporaryCopper));
            if (GUILayout.Button("QA: Temporary funded profile (no disk writes)") &&
                EditorUtility.DisplayDialog("Temporary development profile", "Use 10,000 temporary Coins and " + temporaryCopper + " Copper for this Play session? Purchases, evolution, and run rewards stay in memory. Your local save remains unchanged. Leave Play Mode to restore it.", "Use temporary profile", "Cancel")) flow.UseTemporaryFunding(temporaryCopper);
            EditorGUILayout.Space(); EditorGUILayout.LabelField("M8 development-only stage shortcut", EditorStyles.boldLabel);
            temporaryStage = EditorGUILayout.IntSlider("Temporary EVO", temporaryStage, 1, 8);
            temporaryCoins = System.Math.Max(0, EditorGUILayout.LongField("Temporary Coins", temporaryCoins));
            temporaryIron = System.Math.Max(0, EditorGUILayout.LongField("Temporary Iron", temporaryIron));
            temporaryQuartz = System.Math.Max(0, EditorGUILayout.LongField("Temporary Quartz", temporaryQuartz));
            temporaryAlloy = System.Math.Max(0, EditorGUILayout.LongField("Temporary Alloy Core", temporaryAlloy));
            temporaryShards = System.Math.Max(0, EditorGUILayout.LongField("Temporary Energy Shard", temporaryShards));
            maximum = EditorGUILayout.Toggle("MAX current-form upgrades", maximum);
            if (GUILayout.Button("QA: Select temporary stage (no disk writes)") && EditorUtility.DisplayDialog("Development-only evolution shortcut",
                "Replace this session with temporary EVO " + temporaryStage + "? All purchases, resources and banked rewards stay in memory. Leave Play Mode to return to your real save.", "Use temporary stage", "Cancel"))
                flow.UseTemporaryStage(temporaryStage, temporaryCoins, temporaryCopper, temporaryIron, temporaryQuartz, temporaryAlloy, temporaryShards, maximum);
            var stats = flow.Service.Attributes;
            EditorGUILayout.LabelField("Power / Energy / Cruise / speed cap", stats.Power + " / " + stats.Energy + " / " + stats.Cruise + " / " + stats.SpeedCeiling);
            EditorGUILayout.LabelField("Playable regions unlocked", flow.Service.ActiveForm.EligibleRegionCount.ToString());
            using (new EditorGUI.DisabledScope(flow.Service.Writable || !flow.IsOpen))
                if (GUILayout.Button("Recover previous valid save revision") &&
                    EditorUtility.DisplayDialog("Recover local progress", "Restore the previous valid backup? The rejected primary will be archived unchanged. This may lose only the latest transaction. Unsupported newer saves must be opened with a compatible game; recovery deliberately restores the older schema.", "Archive and recover", "Cancel")) flow.RecoverBackup();
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace DrillRunner.Editor
{
    [CustomEditor(typeof(WorkshopController))]
    public sealed class WorkshopInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); var flow = (WorkshopController)target;
            EditorGUILayout.HelpBox("Editor gameplay entry preserves direct M0-M3 QA. Open WORKSHOP from Ready or results. Active-run QA Reset discards unbanked earnings. Player builds start in Workshop.", MessageType.Info);
            if (!Application.isPlaying || flow.Service == null) return;
            EditorGUILayout.SelectableLabel(flow.SavePath, EditorStyles.wordWrappedLabel, GUILayout.Height(40));
            var progress = flow.Service.Snapshot;
            EditorGUILayout.LabelField("Permanent Coins / Copper", progress.Coins + " / " + progress.Copper);
            EditorGUILayout.LabelField("Power / Energy / Drive levels", progress.PowerLevel + " / " + progress.EnergyLevel + " / " + progress.DriveLevel);
            EditorGUILayout.HelpBox(flow.Message ?? "", MessageType.Info);
            if (GUILayout.Button("Open Workshop (Ready / Completed only)")) flow.OpenWorkshop();
            if (GUILayout.Button("QA: Temporary funded profile (no disk writes)") &&
                EditorUtility.DisplayDialog("Temporary development profile", "Use 10,000 temporary Coins for this Play session? Purchases and run rewards will stay in memory. Your local save will remain unchanged. Leave Play Mode to restore it.", "Use temporary profile", "Cancel")) flow.UseTemporaryFunding();
            using (new EditorGUI.DisabledScope(flow.Service.Writable || !flow.IsOpen))
                if (GUILayout.Button("Recover previous valid save revision") &&
                    EditorUtility.DisplayDialog("Recover local progress", "Restore the previous valid backup? The rejected primary will be archived unchanged. This may lose only the latest transaction. Unsupported newer saves must be opened with a compatible game; recovery deliberately restores the older schema.", "Archive and recover", "Cancel")) flow.RecoverBackup();
        }
    }
}

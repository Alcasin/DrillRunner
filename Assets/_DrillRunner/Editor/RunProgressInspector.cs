using UnityEditor;
using UnityEngine;

namespace DrillRunner.Editor
{
    [CustomEditor(typeof(RunProgress))]
    public sealed class RunProgressInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var run = (RunProgress)target;
            if (Application.isPlaying && run.Rewards != null)
                EditorGUILayout.LabelField("Active run seed", run.Rewards.Seed.ToString());
            if (Application.isPlaying && run.Track.EncounterPlan != null)
            {
                var plan = run.Track.EncounterPlan; var active = run.Track.ActiveEncounter;
                EditorGUILayout.LabelField("Encounter", active != null ? active.Category + " / " + active.name : "Opening / transition");
                EditorGUILayout.LabelField("Validated patterns / generated picks", plan.ValidPatterns + " / " + plan.Ordinal);
                EditorGUILayout.LabelField("Rejected / fallback", plan.RejectedPatterns + " / " + plan.FallbackCount);
                if (plan.Rejection.Length > 0) EditorGUILayout.HelpBox(plan.Rejection, MessageType.Warning);
                EditorGUILayout.HelpBox("M7: assigning a different legacy catalog explicitly uses that catalog's original selection. Pair it with a matching encounter library to enable encounters.", MessageType.Info);
            }
            EditorGUILayout.HelpBox("M3 QA: seed changes apply on debug Restart, which discards active-run earnings. Fixed seed replays the layout; normal completed runs bank rewards through M4.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("QA: Replay configured seed")) Restart(true);
                if (GUILayout.Button("QA: New random seed")) Restart(false);
            }
        }
        private void Restart(bool fixedSeed)
        {
            var run = (RunProgress)target; run.UseFixedSeed = fixedSeed;
            var input = run.Motor.GetComponent<DrillPointerInput>();
            if (input != null) input.Restart(); else run.Motor.ResetRun();
        }
    }
}

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
            EditorGUILayout.HelpBox("M3 QA: seed changes apply on Restart. Fixed seed replays the same layout; random attempts use an independent seed source. No rewards are saved.", MessageType.Info);
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

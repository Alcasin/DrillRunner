using UnityEditor;
using UnityEngine;

namespace DrillRunner.Editor
{
    [CustomEditor(typeof(DrillMotor))]
    public sealed class DrillMotorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("M2 QA only. In Play Mode these controls reset the run and obstacles. Changes revert on leaving Play Mode. No upgrades or progression are applied.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("QA: Power 5 / Energy 100 - Restart")) Configure(5, 100);
                if (GUILayout.Button("QA: Power 10 / Energy 100 - Restart")) Configure(10, 100);
                if (GUILayout.Button("QA: Power 5 / Energy 8 - Restart")) Configure(5, 8);
            }
        }

        private void Configure(float power, float energy)
        {
            serializedObject.Update();
            var settings = serializedObject.FindProperty("drilling");
            settings.FindPropertyRelative("DrillPower").floatValue = power;
            settings.FindPropertyRelative("EnergyCapacity").floatValue = energy;
            serializedObject.ApplyModifiedProperties();
            var motor = (DrillMotor)target;
            var input = motor.GetComponent<DrillPointerInput>();
            if (input != null) input.Restart(); else motor.ResetRun();
        }
    }
}

using System;
using UnityEngine;

namespace DrillRunner
{
    [Serializable] public sealed class FormConsumption
    {
        public string FormId;
        public float Moving, Blocked, DrillingBase, MaterialScale;
        public float[] BoostDecayByDriveLevel = {1.8f,1.2f,0.9f,0.7f,0.56f,0.45f};
        public float DrillingRate(float materialRate) => DrillingBase + materialRate * MaterialScale;
        public void Validate()
        {
            if (Array.IndexOf(ProgressionConfig.FormIds, FormId) < 0) throw new ArgumentException("Unknown consumption-profile form.");
            DrillMovementSettings.Positive(Moving, "Moving consumption"); DrillMovementSettings.Positive(Blocked, "Blocked consumption");
            DrillRules.NonNegative(DrillingBase, "Drilling base consumption"); DrillRules.NonNegative(MaterialScale, "Material rate scale");
            if (Moving > 1000 || Blocked > 1000 || Blocked < Moving || DrillingRate(8) <= 0 || DrillingRate(18) > 1000)
                throw new ArgumentException("Consumption must be bounded and positive in every state; form-specific drilling efficiency is independent of travel cost.");
            if (BoostDecayByDriveLevel == null || BoostDecayByDriveLevel.Length != 6) throw new ArgumentException("Every Drive level needs explicit launch-coast tuning.");
            for (var i = 0; i < 6; i++)
                if (!DrillMovementSettings.Finite(BoostDecayByDriveLevel[i]) || BoostDecayByDriveLevel[i] < 0.04f || BoostDecayByDriveLevel[i] > 1.8f ||
                    (i > 0 && BoostDecayByDriveLevel[i] >= BoostDecayByDriveLevel[i - 1])) throw new ArgumentException("Drive must meaningfully improve bounded launch carry.");
        }
    }
    [CreateAssetMenu(menuName = "DrillRunner/Rapid Run Balance")]
    public sealed class RapidRunBalance : ScriptableObject
    {
        public FormConsumption[] Forms = Array.Empty<FormConsumption>();
        public RewardSettings Rewards = new RewardSettings();
        public float ChargeSeconds = 1.1f;
        [HideInInspector] public int Revision;
        public FormConsumption ForForm(string id)
        { foreach (var form in Forms) if (form != null && form.FormId == id) return form; throw new ArgumentException("Missing form consumption profile."); }
        public void Validate()
        {
            if (Forms == null || Forms.Length != 8 || Rewards == null) throw new ArgumentException("Rapid runs need all eight fixed form profiles and reward tuning.");
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var form in Forms) { if (form == null || !ids.Add(form.FormId)) throw new ArgumentException("Missing or duplicate consumption profile."); form.Validate(); }
            Rewards.Validate(); DrillMovementSettings.Positive(ChargeSeconds, "Charge duration");
            if (ChargeSeconds < 0.5f || ChargeSeconds > 1.5f) throw new ArgumentException("Charge must remain short and controllable.");
            for (var i = 1; i < 8; i++)
                if (ForForm(ProgressionConfig.FormIds[i]).BoostDecayByDriveLevel[0] > ForForm(ProgressionConfig.FormIds[i-1]).BoostDecayByDriveLevel[5])
                    throw new ArgumentException("Evolution must preserve the previous form's purchased launch-coast benefit.");
        }
    }
}

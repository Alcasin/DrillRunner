using System;
using UnityEngine;

namespace DrillRunner
{
    public enum FeedbackCue { Charge, Launch, Drill, Blocked, Break, CopperBreak, CopperReward, Upgrade, Evolution, Finish }
    public enum WorkshopFeedback { Opened, StartedRun, Purchased, EvolutionStarted, EvolutionCompleted }

    [CreateAssetMenu(menuName = "DrillRunner/Presentation Theme")]
    public sealed class PresentationTheme : ScriptableObject
    {
        public AudioClip[] Sounds = new AudioClip[10];
        public Material ParticleMaterial;
        public Color Wood = new Color(0.76f, 0.49f, 0.23f), Brick = new Color(0.78f, 0.32f, 0.21f);
        public Color Stone = new Color(0.57f, 0.65f, 0.7f), Copper = new Color(1, 0.59f, 0.18f);
        public Color Ink = new Color(0.08f, 0.17f, 0.23f), Teal = new Color(0.04f, 0.52f, 0.55f);
        public Color MaterialColor(string id) => id == "quartz_ore" ? new Color(0.4f,0.94f,0.94f) : id == "alloy_core" ? new Color(1,0.7f,0.25f) : id == "energy_shard" ? new Color(0.35f,0.7f,1) : id == "crystal_composite" ? new Color(0.64f,0.4f,0.8f) : id == "energy_alloy" ? new Color(0.2f,0.4f,0.56f) : id == "wood" ? Wood : id == "brick" ? Brick : id == "copper_ore" ? Copper : id == "iron_ore" ? new Color(0.65f,0.77f,0.86f) : id == "plating" ? new Color(0.33f,0.5f,0.59f) : id == "reinforced" ? new Color(0.24f,0.32f,0.38f) : Stone;
        public void Validate()
        {
            if (Sounds == null || Sounds.Length != 10 || ParticleMaterial == null) throw new ArgumentException("Incomplete presentation theme.");
            foreach (var clip in Sounds) if (clip == null) throw new ArgumentException("Missing original feedback sound.");
        }
    }
}

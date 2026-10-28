using UnityEngine;

namespace DrillRunner
{
    public sealed class PresentationAudio : MonoBehaviour
    {
        [SerializeField] private PresentationTheme theme;
        [SerializeField] private AudioSource loop;
        [SerializeField] private AudioSource[] voices;
        private int cursor;
        private float targetVolume;
        public int LoopCue { get; private set; } = -1;
        public int LoopStarts { get; private set; }
        public int VoiceCount => voices.Length;
        public int[] CueCounts { get; } = new int[10];
        public void SetLoop(int cue, float pitch = 1)
        {
            if (LoopCue != cue)
            {
                loop.Stop(); LoopCue = cue;
                if (cue >= 0) { loop.clip = theme.Sounds[cue]; loop.loop = true; loop.volume = 0; loop.Play(); LoopStarts++; }
            }
            targetVolume = cue >= 0 ? 0.16f : 0;
            loop.pitch = Mathf.Clamp(pitch, 0.65f, 1.6f);
        }
        public void Play(FeedbackCue cue)
        {
            var source = voices[cursor]; cursor = (cursor + 1) % voices.Length; source.Stop(); source.clip = theme.Sounds[(int)cue];
            source.pitch = 1 + ((cursor % 3) - 1) * 0.025f; source.volume = cue == FeedbackCue.CopperReward || cue == FeedbackCue.Evolution ? 0.32f : 0.25f;
            source.Play(); CueCounts[(int)cue]++;
        }
        private void Update() { if (loop != null) loop.volume = Mathf.MoveTowards(loop.volume, targetVolume, Time.unscaledDeltaTime * 2); }
        public void StopAll()
        {
            LoopCue = -1; targetVolume = 0; if (loop != null) loop.Stop();
            if (voices != null) foreach (var voice in voices) if (voice != null) voice.Stop();
        }
        private void OnDisable() => StopAll();
        private void OnApplicationFocus(bool focus) { if (!focus) StopAll(); }
        private void OnApplicationPause(bool pause) { if (pause) StopAll(); }
    }
}

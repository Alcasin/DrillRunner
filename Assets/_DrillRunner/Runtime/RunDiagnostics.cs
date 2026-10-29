#if UNITY_EDITOR
namespace DrillRunner
{
    /// <summary>Editor-only accounting of intervals actually consumed by the motor; never controls termination.</summary>
    public sealed class RunDiagnostics
    {
        public double MovingSeconds, DrillingSeconds, BlockedSeconds;
        public double MovingEnergy, DrillingEnergy, BlockedEnergy;
        public double Seconds => MovingSeconds + DrillingSeconds + BlockedSeconds;
        public double Consumed => MovingEnergy + DrillingEnergy + BlockedEnergy;
        public double FirstContactSeconds = -1;
        public int Contacts, Destroyed;
        public bool EndedByEnergy;
        internal void Record(RunState state, double seconds, float rate)
        {
            if (state == RunState.Drilling) { DrillingSeconds += seconds; DrillingEnergy += seconds * rate; }
            else if (state == RunState.Blocked) { BlockedSeconds += seconds; BlockedEnergy += seconds * rate; }
            else { MovingSeconds += seconds; MovingEnergy += seconds * rate; }
        }
    }
}
#endif

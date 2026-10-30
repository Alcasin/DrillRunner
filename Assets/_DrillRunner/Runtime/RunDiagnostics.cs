#if UNITY_EDITOR
namespace DrillRunner
{
    /// <summary>Editor-only accounting of intervals actually consumed by the motor; never controls termination.</summary>
    public sealed class RunDiagnostics
    {
        public sealed class DrillingContact
        {
            public readonly string MaterialId;
            public readonly ObstacleRewardKind Kind;
            public readonly float MaximumHp;
            public double Seconds, Energy, TotalEnergyAtCompletion;
            public bool Completed;
            internal DrillingContact(ObstacleDurability target){MaterialId=target.MaterialId;Kind=target.RewardKind;MaximumHp=target.Maximum;}
        }
        private readonly System.Collections.Generic.Dictionary<ObstacleDurability,DrillingContact> drillingContacts = new System.Collections.Generic.Dictionary<ObstacleDurability,DrillingContact>();
        public System.Collections.Generic.IEnumerable<DrillingContact> DrillingContacts => drillingContacts.Values;
        public double MovingSeconds, DrillingSeconds, BlockedSeconds;
        public double MovingEnergy, DrillingEnergy, BlockedEnergy;
        public double Seconds => MovingSeconds + DrillingSeconds + BlockedSeconds;
        public double Consumed => MovingEnergy + DrillingEnergy + BlockedEnergy;
        public double FirstContactSeconds = -1;
        public int Contacts, Destroyed;
        public bool EndedByEnergy;
        internal void RecordDrilling(ObstacleDurability target,double seconds,float rate)
        {
            Record(RunState.Drilling,seconds,rate);
            if(!drillingContacts.TryGetValue(target,out var contact)){contact=new DrillingContact(target);drillingContacts.Add(target,contact);}
            contact.Seconds+=seconds;contact.Energy+=seconds*rate;contact.Completed=target.IsDestroyed;
            if(contact.Completed)contact.TotalEnergyAtCompletion=Consumed;
        }
        internal void Record(RunState state, double seconds, float rate)
        {
            if (state == RunState.Drilling) { DrillingSeconds += seconds; DrillingEnergy += seconds * rate; }
            else if (state == RunState.Blocked) { BlockedSeconds += seconds; BlockedEnergy += seconds * rate; }
            else { MovingSeconds += seconds; MovingEnergy += seconds * rate; }
        }
    }
}
#endif

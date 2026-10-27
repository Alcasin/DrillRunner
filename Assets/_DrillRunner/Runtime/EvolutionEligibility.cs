namespace DrillRunner
{
    /// <summary>One authoritative, configuration-driven result for the UI and transaction.</summary>
    public readonly struct EvolutionEligibility
    {
        public readonly bool PowerComplete, EnergyComplete, DriveComplete, CopperAvailable, HasNext, Allowed, Ready;
        public readonly long Copper, RequiredCopper;
        public readonly string Description;
        public EvolutionEligibility(string error) { this = default; Description = error; }
        public EvolutionEligibility(int power, int maxPower, int energy, int maxEnergy, int drive, int maxDrive,
            long copper, long requiredCopper, string nextName, bool allowed)
        {
            PowerComplete = power == maxPower; EnergyComplete = energy == maxEnergy; DriveComplete = drive == maxDrive;
            Copper = copper; RequiredCopper = requiredCopper; CopperAvailable = copper >= requiredCopper; HasNext = nextName != null; Allowed = allowed;
            Ready = HasNext && PowerComplete && EnergyComplete && DriveComplete && CopperAvailable && allowed;
            Description = !HasNext ? "NO FURTHER EVOLUTION\nContinue upgrading your current form." :
                "NEXT: " + nextName + "   |   Copper " + copper + " / " + requiredCopper + "\n" +
                "Power " + power + "/" + maxPower + "   Energy " + energy + "/" + maxEnergy + "   Drive " + drive + "/" + maxDrive + "\n" +
                (Ready ? "READY: spend " + requiredCopper + " Copper" : !allowed ? "Evolution unavailable during a run, transition, or save error" : "Complete every upgrade and collect the required Copper");
        }
    }
}

namespace DrillRunner
{
    /// <summary>One authoritative, configuration-driven result for the UI and transaction.</summary>
    public readonly struct EvolutionEligibility
    {
        public readonly bool PowerComplete, EnergyComplete, DriveComplete, CopperAvailable, ResourcesAvailable, HasNext, Allowed, Ready;
        public readonly long Copper, RequiredCopper;
        public readonly string Description;
        public EvolutionEligibility(string error) { this = default; Description = error; }
        public EvolutionEligibility(int power, int maxPower, int energy, int maxEnergy, int drive, int maxDrive,
            PlayerProgress profile, ResourceCost[] costs, string nextName, bool allowed)
        {
            PowerComplete = power == maxPower; EnergyComplete = energy == maxEnergy; DriveComplete = drive == maxDrive;
            Copper = profile.Copper; RequiredCopper = 0; ResourcesAvailable = true; var resources = ""; var spending = "";
            foreach (var cost in costs)
            {
                if (cost.Resource == EvolutionResource.Copper) RequiredCopper = cost.Amount;
                ResourcesAvailable &= profile.Resource(cost.Resource) >= cost.Amount;
                resources += (resources.Length == 0 ? "" : " · ") + ResourceCost.Name(cost.Resource) + " " + profile.Resource(cost.Resource) + " / " + cost.Amount;
                spending += (spending.Length == 0 ? "" : " + ") + cost.Amount + " " + ResourceCost.Name(cost.Resource);
            }
            CopperAvailable = Copper >= RequiredCopper; HasNext = nextName != null; Allowed = allowed;
            Ready = HasNext && PowerComplete && EnergyComplete && DriveComplete && ResourcesAvailable && allowed;
            Description = !HasNext ? (PowerComplete && EnergyComplete && DriveComplete ? "FINAL FORM · EVO 8\nQUANTUM DRILL COMPLETE · ALL UPGRADES MAX" : "FINAL FORM · EVO 8\nContinue upgrading your Quantum Drill.") :
                "NEXT: " + nextName + (costs.Length > 1 ? "\n" : "   |   ") + resources + "\n" +
                "Power " + power + "/" + maxPower + "   Energy " + energy + "/" + maxEnergy + "   Drive " + drive + "/" + maxDrive + "\n" +
                (Ready ? "READY: spend " + spending : !allowed ? "Evolution unavailable right now" : "MAX all upgrades and collect required resources");
        }
    }
}

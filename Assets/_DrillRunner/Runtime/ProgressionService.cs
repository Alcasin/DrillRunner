using System;

namespace DrillRunner
{
    /// <summary>Only permanent currency/upgrade transaction owner. Publish in memory only after durable success.</summary>
    public sealed class ProgressionService
    {
        private readonly IProgressStore store;
        private readonly ProgressionConfig config;
        private PlayerProgress current;
        private bool writing;
        private long activeAttempt;
        public PlayerProgress Snapshot => current.Copy();
        public bool Writable { get; private set; }
        public string Status { get; private set; }
        public EffectiveAttributes Attributes => config.Calculate(current);
        public ProgressionConfig ActiveForm => config.ForForm(current.FormId);

        public ProgressionService(IProgressStore store, ProgressionConfig config)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.config = config ?? throw new ArgumentNullException(nameof(config)); config.Validate();
            var loaded = store.Load(); current = loaded.Progress.Copy(); Writable = loaded.Writable; Status = loaded.Message;
            try { current.Validate(config); current.FormSchema = 1; current.EvolutionSchema = 1; current.AdvancedForms = current.AdvancedForms ?? Array.Empty<FormUpgradeRecord>(); } // Additive migration; no disk rewrite on load.
            catch (ArgumentException exception) { current = new PlayerProgress(); Writable = false; Status = exception.Message; }
        }

        private bool Commit(PlayerProgress next, out string error)
        {
            error = null;
            if (!Writable || writing) { error = Writable ? "A transaction is in progress." : Status; return false; }
            try
            {
                config.Validate(); next.Revision = checked(current.Revision + 1); next.Validate(config); writing = true;
                if (!store.TrySave(next.Copy(), out error)) { Status = error; return false; }
                current = next; Status = "Progress saved."; return true;
            }
            catch (Exception exception) when (exception is OverflowException || exception is ArgumentException)
            { error = exception.Message; return false; }
            finally { writing = false; }
        }

        public bool TryPurchase(UpgradeCategory category, out string error)
        {
            error = null;
            try { config.Validate(); current.Validate(config); }
            catch (ArgumentException exception) { error = exception.Message; return false; }
            if (!Enum.IsDefined(typeof(UpgradeCategory), category)) { error = "Unknown upgrade category."; return false; }
            var level = current.Level(category); var table = ActiveForm.Table(category);
            if (level >= table.MaximumLevel) { error = "MAX level reached."; return false; }
            var price = table.Costs[level];
            if (price <= 0 || current.Coins < price) { error = "Not enough Coins."; return false; }
            var next = current.Copy(); next.Coins -= price; next.Increase(category);
            return Commit(next, out error);
        }

        public EvolutionEligibility Evolution(bool workshopAllowed)
        {
            try { config.Validate(); current.Validate(config); }
            catch (ArgumentException exception) { return new EvolutionEligibility("Evolution unavailable: " + exception.Message); }
            var form = ActiveForm; var next = form.NextForm;
            var power = current.Level(UpgradeCategory.DrillPower); var energy = current.Level(UpgradeCategory.EnergyCapacity); var drive = current.Level(UpgradeCategory.DrivePower);
            return new EvolutionEligibility(power, form.Power.MaximumLevel, energy, form.Energy.MaximumLevel, drive, form.Drive.MaximumLevel,
                current, next != null ? form.RequiredResources : Array.Empty<ResourceCost>(), next != null ? next.DisplayName : null, Writable && workshopAllowed);
        }

        public bool TryEvolve(bool workshopAllowed, out string error)
        {
            var eligibility = Evolution(workshopAllowed); error = eligibility.Description;
            if (!eligibility.Ready) return false;
            var next = current.Copy(); foreach (var cost in ActiveForm.RequiredResources) next.Spend(cost);
            next.EnterForm(ActiveForm.NextForm.FormId);
            return Commit(next, out error);
        }

        public bool TryBeginAttempt(out long token, out string error)
        {
            token = 0; error = null;
            if (current.IssuedAttempt == long.MaxValue) { error = "Attempt counter exhausted."; return false; }
            var next = current.Copy(); next.IssuedAttempt++;
            if (!Commit(next, out error)) return false;
            activeAttempt = token = next.IssuedAttempt; return true;
        }

        public bool TryBank(long token, long coins, long copper, out string error)
        {
            error = null;
            if (token <= 0 || coins < 0 || copper < 0) { error = "Invalid completed-run rewards."; return false; }
            if (token <= current.BankedAttempt) return true; // Durable monotonic receipt, including duplicate/late callbacks.
            if (token != activeAttempt || token > current.IssuedAttempt) { error = "This attempt does not belong to the current session."; return false; }
            try
            {
                var next = current.Copy(); next.Coins = checked(next.Coins + coins); next.Copper = checked(next.Copper + copper);
                next.BankedAttempt = token; return Commit(next, out error);
            }
            catch (OverflowException) { error = "Currency limit reached; rewards were not changed."; return false; }
        }
    }
}

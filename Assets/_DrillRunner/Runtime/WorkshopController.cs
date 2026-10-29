using System;
using UnityEngine;

namespace DrillRunner
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class WorkshopController : MonoBehaviour
    {
        [SerializeField] private ProgressionConfig configuration;
        [SerializeField] private RunProgress run;
        [SerializeField] private DrillPointerInput input;
        [SerializeField] private WorkshopView view;
        [SerializeField] private DrillVisuals visuals;
        [SerializeField] private bool editorGameplayEntry = true;
        [SerializeField] private RapidRunBalance rapidRunBalance;
        private RapidRunBalance sessionBalance;
        public RapidRunBalance Balance => sessionBalance;
        // Configuration injection for isolated historical-balance regression fixtures; normal play uses the authored asset.
        internal static Func<RapidRunBalance, RapidRunBalance> BalanceFactory;
        private int lastUiStartFrame = -1;
        private IProgressStore store;
        private ProgressionConfig sessionConfig;
        private readonly System.Collections.Generic.List<ProgressionConfig> sessionForms = new System.Collections.Generic.List<ProgressionConfig>();
        private RunRewards attempt, completed;
        private long token;
        private bool qaAttempt;
        private Func<bool> resetGuard;
        private float evolutionRemaining;
        internal static Func<IProgressStore> StoreFactory;
        public ProgressionService Service { get; private set; }
        public ProgressionConfig Configuration => sessionConfig != null ? sessionConfig : configuration;
        public RunProgress Run => run;
        public WorkshopView View => view;
        public bool IsOpen { get; private set; }
        public bool PendingBank { get; private set; }
        public bool IsBanked { get; private set; }
        public bool IsSandbox { get; private set; }
        public bool IsEvolving => evolutionRemaining > 0;
        public event Action<WorkshopFeedback> Feedback;
        public DrillVisuals Visuals => visuals;
        public EvolutionEligibility Evolution => Service.Evolution(CanPurchase);
        public string Message { get; private set; }
        public string SavePath => store is FileProgressStore disk ? disk.Path : "Isolated temporary profile";
        public bool CanPurchase => IsOpen && Service != null && Service.Writable && !PendingBank && !IsEvolving &&
            (input.Motor.Motion.State == RunState.Ready || input.Motor.Motion.State == RunState.RunEnded);

        private void Awake()
        {
            configuration.Validate(); ProgressionConfig previous = null;
            for (var source = configuration; source != null; source = source.NextForm)
            {
                var copy = Instantiate(source); sessionForms.Add(copy);
                if (previous == null) sessionConfig = copy; else previous.NextForm = copy; previous = copy;
            }
            sessionConfig.Validate();
            var balance = BalanceFactory != null ? BalanceFactory(rapidRunBalance) : rapidRunBalance;
            if (balance != null) { balance.Validate(); sessionBalance = Instantiate(balance); }
            store = StoreFactory != null ? StoreFactory() : new FileProgressStore(Application.persistentDataPath);
            Service = new ProgressionService(store, sessionConfig); Message = Service.Status;
            resetGuard = () => !PendingBank && !IsEvolving;
            input.Motor.ResetGuard = resetGuard;
            if (visuals != null) visuals.Show(Service.ActiveForm);
            ApplyRunConfiguration(); input.Restart();
        }
        private void OnEnable()
        {
            run.AttemptBegan += OnAttempt; run.Completed += OnCompleted;
            if (Service != null && view != null) { view.EvolutionPulse(1); view.Refresh(); }
        }
        private void OnDisable()
        {
            run.AttemptBegan -= OnAttempt; run.Completed -= OnCompleted;
            if (IsEvolving) FinishEvolution(false); // UI may already be destroyed during scene unload. The transaction is committed.
        }
        private void Update()
        {
            if (!IsEvolving) return;
            evolutionRemaining = Mathf.Max(0, evolutionRemaining - Time.unscaledDeltaTime);
            view.EvolutionPulse(1 - evolutionRemaining / 0.9f);
            if (!IsEvolving) FinishEvolution();
        }
        private void FinishEvolution(bool refresh = true)
        {
            evolutionRemaining = 0;
            Message = Service.ActiveForm.DisplayName.ToUpperInvariant() + " UNLOCKED\n" + (Service.ActiveForm.Stage == 8 ? "FINAL FORM · Your Quantum Drill is ready!" : "New stats and upgrades are ready!");
            if (refresh && view != null) { view.EvolutionPulse(1); view.Refresh(); }
            if (refresh) Feedback?.Invoke(WorkshopFeedback.EvolutionCompleted);
        }
        public bool Evolve()
        {
            if (!Service.TryEvolve(CanPurchase, out var error))
            { if (!IsEvolving) { Message = error; view.Refresh(); } return false; }
            evolutionRemaining = 0.9f;
            if (visuals != null) visuals.Show(Service.ActiveForm);
            Message = "EVOLVING · Assembling " + Service.ActiveForm.DisplayName + "..."; view.Refresh(); Feedback?.Invoke(WorkshopFeedback.EvolutionStarted); return true;
        }
        private void Start()
        {
            if (attempt != run.Rewards && run.Rewards != null) OnAttempt(run.Rewards);
            if (!Application.isEditor || !editorGameplayEntry || !Service.Writable) OpenWorkshop();
        }
        private void OnDestroy()
        {
            if (input != null && input.Motor != null && input.Motor.ResetGuard == resetGuard) input.Motor.ResetGuard = null;
            foreach (var form in sessionForms) if (form != null) Destroy(form);
            if (sessionBalance != null) Destroy(sessionBalance);
        }
        private void OnAttempt(RunRewards ledger)
        {
            if (Service == null || ledger == attempt) return;
            attempt = ledger; completed = null; IsBanked = false; PendingBank = false;
            if (!Service.TryBeginAttempt(out token, out var error))
            {
                ledger.Complete(); input.Motor.Motion.EndRun(); input.enabled = false; Message = error;
            }
            view.Refresh();
        }
        private void OnCompleted(RunRewards ledger)
        {
            if (ledger != attempt || !ledger.IsCompleted || IsBanked) return;
            completed = ledger; PendingBank = true; RetryBank();
        }
        public bool RetryBank()
        {
            if (!PendingBank) return IsBanked;
            if (qaAttempt)
            { PendingBank = false; IsBanked = true; Message = "QA attempt complete. Earnings were not saved."; }
            else if (Service.TryBank(token, completed.Coins, completed.Copper, out var error))
            { PendingBank = false; IsBanked = true; Message = IsSandbox ? "Temporary profile: rewards banked in memory." : "Run rewards saved."; }
            else Message = "Rewards not yet saved. Retry Save before another run. " + error;
            view.Refresh(); return IsBanked;
        }
        public bool OpenWorkshop()
        {
            var state = input.Motor.Motion.State;
            if (state != RunState.Ready && state != RunState.RunEnded) return false;
            input.enabled = false; input.Motor.Motion.CancelPointer(); IsOpen = true;
            if (Service.Writable && !PendingBank)
                Message = IsSandbox ? "Temporary profile. Changes stay in memory." : "Choose upgrades, then Start Run.";
            view.SetOpen(true); view.Refresh(); Feedback?.Invoke(WorkshopFeedback.Opened); return true;
        }
        public bool StartRun()
        {
            if (IsEvolving) return false;
            if (PendingBank && !RetryBank()) return false;
            if (!Service.Writable) { Message = Service.Status; view.Refresh(); return false; }
            var state = input.Motor.Motion.State;
            if (state != RunState.Ready && state != RunState.RunEnded) return false;
            qaAttempt = false; ApplyRunConfiguration();
            if (visuals != null) visuals.Show(Service.ActiveForm);
            run.UseFixedSeed = false; // Normal Workshop/retry flow varies attempts; fixed QA replay remains available.
            input.Restart();
            if (token == 0 || run.Rewards.IsCompleted) { view.Refresh(); return false; }
            IsOpen = false; view.SetOpen(false); input.enabled = true;
            Message = "New run ready. Hold to charge and release to launch."; view.Refresh(); Feedback?.Invoke(WorkshopFeedback.StartedRun); return true;
        }
        private void ApplyRunConfiguration()
        {
            input.Motor.ApplyAttributes(Service.Attributes);
            if (sessionBalance == null) return;
            input.Motor.ApplyConsumption(sessionBalance.ForForm(Service.ActiveForm.FormId), sessionBalance.ChargeSeconds,
                sessionBalance.ForForm(Service.ActiveForm.FormId).BoostDecayByDriveLevel[Service.Snapshot.Level(UpgradeCategory.DrivePower)]);
            run.ConfigureRewards(sessionBalance.Rewards);
        }
        public bool StartRunFromUi()
        {
            if (lastUiStartFrame == Time.frameCount) return false;
            if (!StartRun()) return false;
            lastUiStartFrame = Time.frameCount; return true;
        }
        public bool Purchase(UpgradeCategory category)
        {
            if (!CanPurchase) return false;
            var success = Service.TryPurchase(category, out var error);
            Message = success ? "Upgrade saved. Applies on Start Run." : error; view.Refresh();
            if (success) Feedback?.Invoke(WorkshopFeedback.Purchased); return success;
        }
#if UNITY_EDITOR
        public void StartQaRun(float power, float energy)
        {
            if (PendingBank || IsEvolving) return;
            // Explicit development reset discards an active attempt; neither overrides nor its earnings are persisted.
            qaAttempt = true; input.Motor.Drilling.DrillPower = power; input.Motor.Drilling.EnergyCapacity = energy;
            input.Restart(); IsOpen = false; view.SetOpen(false); input.enabled = true;
            Message = "QA override active. This attempt cannot bank permanent rewards.";
        }
        public void UseTemporaryFunding() => UseTemporaryFunding(configuration.EvolutionCopper * 2);
        public void UseTemporaryFunding(long copper)
        {
            var state = input.Motor.Motion.State;
            if (PendingBank || IsEvolving || copper < 0 || (state != RunState.Ready && state != RunState.RunEnded)) return;
            var copy = Service.Snapshot; copy.Coins = 10000; copy.Copper = copper;
            store = new MemoryProgressStore(copy); Service = new ProgressionService(store, sessionConfig);
            IsSandbox = true; attempt = null; qaAttempt = false;
            input.Restart(); OpenWorkshop(); Message = "DEVELOPMENT SANDBOX: 10,000 Coins + " + copper + " Copper. Nothing is written to your local save."; view.Refresh();
        }
        public void UseTemporaryStage(int stage, long coins, long copper, long iron, long quartz, long alloyCore, long energyShard, bool maxCurrent)
        {
            var state = input.Motor.Motion.State;
            if (PendingBank || IsEvolving || (state != RunState.Ready && state != RunState.RunEnded) || stage < 1 || stage > 8 ||
                coins < 0 || copper < 0 || iron < 0 || quartz < 0 || alloyCore < 0 || energyShard < 0) return;
            var copy = Service.Snapshot; copy.FormId = ProgressionConfig.FormIds[stage - 1]; copy.FormSchema = copy.EvolutionSchema = 1;
            copy.Coins = coins; copy.Copper = copper; copy.Iron = iron; copy.Quartz = quartz; copy.AlloyCore = alloyCore; copy.EnergyShard = energyShard;
            copy.PowerLevel = stage > 1 || maxCurrent ? sessionConfig.Power.MaximumLevel : 0;
            copy.EnergyLevel = stage > 1 || maxCurrent ? sessionConfig.Energy.MaximumLevel : 0;
            copy.DriveLevel = stage > 1 || maxCurrent ? sessionConfig.Drive.MaximumLevel : 0;
            var workshop = sessionConfig.NextForm;
            copy.WorkshopPowerLevel = stage > 2 || (stage == 2 && maxCurrent) ? workshop.Power.MaximumLevel : 0;
            copy.WorkshopEnergyLevel = stage > 2 || (stage == 2 && maxCurrent) ? workshop.Energy.MaximumLevel : 0;
            copy.WorkshopDriveLevel = stage > 2 || (stage == 2 && maxCurrent) ? workshop.Drive.MaximumLevel : 0;
            copy.AdvancedForms = new FormUpgradeRecord[Math.Max(0, stage - 2)];
            for (var i = 0; i < copy.AdvancedForms.Length; i++)
            {
                var form = sessionConfig.ForForm(ProgressionConfig.FormIds[i + 2]); var max = i + 3 < stage || maxCurrent;
                copy.AdvancedForms[i] = new FormUpgradeRecord { FormId = form.FormId, Power = max ? form.Power.MaximumLevel : 0,
                    Energy = max ? form.Energy.MaximumLevel : 0, Drive = max ? form.Drive.MaximumLevel : 0 };
            }
            copy.Validate(sessionConfig); store = new MemoryProgressStore(copy); Service = new ProgressionService(store, sessionConfig);
            IsSandbox = true; attempt = null; qaAttempt = false; ApplyRunConfiguration(); visuals.Show(Service.ActiveForm);
            input.Restart(); OpenWorkshop(); Message = "DEVELOPMENT SANDBOX · EVO " + stage + "\nStage shortcut and all balances are temporary; no disk writes."; view.Refresh();
        }
        public bool RecoverBackup()
        {
            if (!(store is FileProgressStore disk) || !IsOpen || IsEvolving) return false;
            if (!disk.RecoverBackup(sessionConfig, out var error)) { Message = error; view.Refresh(); return false; }
            Service = new ProgressionService(disk, sessionConfig); attempt = null;
            if (visuals != null) visuals.Show(Service.ActiveForm);
            input.Restart(); Message = "Previous revision recovered; rejected primary archived."; view.Refresh(); return true;
        }
#endif
    }
}

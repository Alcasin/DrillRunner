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
        private IProgressStore store;
        private ProgressionConfig sessionConfig;
        private ProgressionConfig nextSessionConfig;
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
            sessionConfig = Instantiate(configuration);
            if (sessionConfig.NextForm != null) { nextSessionConfig = Instantiate(sessionConfig.NextForm); sessionConfig.NextForm = nextSessionConfig; }
            sessionConfig.Validate();
            store = StoreFactory != null ? StoreFactory() : new FileProgressStore(Application.persistentDataPath);
            Service = new ProgressionService(store, sessionConfig); Message = Service.Status;
            resetGuard = () => !PendingBank && !IsEvolving;
            input.Motor.ResetGuard = resetGuard;
            if (visuals != null) visuals.Show(Service.ActiveForm);
            input.Motor.ApplyAttributes(Service.Attributes); input.Restart();
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
            Message = "WORKSHOP DRILL UNLOCKED\nNew stats and upgrades are ready!";
            if (refresh && view != null) { view.EvolutionPulse(1); view.Refresh(); }
            if (refresh) Feedback?.Invoke(WorkshopFeedback.EvolutionCompleted);
        }
        public bool Evolve()
        {
            if (!Service.TryEvolve(CanPurchase, out var error))
            { if (!IsEvolving) { Message = error; view.Refresh(); } return false; }
            evolutionRemaining = 0.9f;
            if (visuals != null) visuals.Show(Service.ActiveForm);
            Message = "EVOLVING · Assembling Workshop Drill..."; view.Refresh(); Feedback?.Invoke(WorkshopFeedback.EvolutionStarted); return true;
        }
        private void Start()
        {
            if (attempt != run.Rewards && run.Rewards != null) OnAttempt(run.Rewards);
            if (!Application.isEditor || !editorGameplayEntry || !Service.Writable) OpenWorkshop();
        }
        private void OnDestroy()
        {
            if (input != null && input.Motor != null && input.Motor.ResetGuard == resetGuard) input.Motor.ResetGuard = null;
            if (sessionConfig != null) Destroy(sessionConfig);
            if (nextSessionConfig != null) Destroy(nextSessionConfig);
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
            qaAttempt = false; input.Motor.ApplyAttributes(Service.Attributes);
            if (visuals != null) visuals.Show(Service.ActiveForm);
            run.UseFixedSeed = false; // Normal Workshop/retry flow varies attempts; fixed QA replay remains available.
            input.Restart();
            if (token == 0 || run.Rewards.IsCompleted) { view.Refresh(); return false; }
            IsOpen = false; view.SetOpen(false); input.enabled = true;
            Message = "New run ready. Hold to charge and release to launch."; view.Refresh(); Feedback?.Invoke(WorkshopFeedback.StartedRun); return true;
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

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
        [SerializeField] private bool editorGameplayEntry = true;
        private IProgressStore store;
        private ProgressionConfig sessionConfig;
        private RunRewards attempt, completed;
        private long token;
        private bool qaAttempt;
        private Func<bool> resetGuard;
        internal static Func<IProgressStore> StoreFactory;
        public ProgressionService Service { get; private set; }
        public ProgressionConfig Configuration => sessionConfig != null ? sessionConfig : configuration;
        public RunProgress Run => run;
        public WorkshopView View => view;
        public bool IsOpen { get; private set; }
        public bool PendingBank { get; private set; }
        public bool IsBanked { get; private set; }
        public bool IsSandbox { get; private set; }
        public string Message { get; private set; }
        public string SavePath => store is FileProgressStore disk ? disk.Path : "Isolated temporary profile";
        public bool CanPurchase => IsOpen && Service != null && Service.Writable && !PendingBank;

        private void Awake()
        {
            sessionConfig = Instantiate(configuration); sessionConfig.Validate();
            store = StoreFactory != null ? StoreFactory() : new FileProgressStore(Application.persistentDataPath);
            Service = new ProgressionService(store, sessionConfig); Message = Service.Status;
            resetGuard = () => !PendingBank;
            input.Motor.ResetGuard = resetGuard;
            input.Motor.ApplyAttributes(Service.Attributes); input.Restart();
        }
        private void OnEnable() { run.AttemptBegan += OnAttempt; run.Completed += OnCompleted; }
        private void OnDisable() { run.AttemptBegan -= OnAttempt; run.Completed -= OnCompleted; }
        private void Start()
        {
            if (attempt != run.Rewards && run.Rewards != null) OnAttempt(run.Rewards);
            if (!Application.isEditor || !editorGameplayEntry || !Service.Writable) OpenWorkshop();
        }
        private void OnDestroy()
        {
            if (input != null && input.Motor != null && input.Motor.ResetGuard == resetGuard) input.Motor.ResetGuard = null;
            if (sessionConfig != null) Destroy(sessionConfig);
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
            view.SetOpen(true); view.Refresh(); return true;
        }
        public bool StartRun()
        {
            if (PendingBank && !RetryBank()) return false;
            if (!Service.Writable) { Message = Service.Status; view.Refresh(); return false; }
            var state = input.Motor.Motion.State;
            if (state != RunState.Ready && state != RunState.RunEnded) return false;
            qaAttempt = false; input.Motor.ApplyAttributes(Service.Attributes);
            run.UseFixedSeed = false; // Normal Workshop/retry flow varies attempts; fixed QA replay remains available.
            input.Restart();
            if (token == 0 || run.Rewards.IsCompleted) { view.Refresh(); return false; }
            IsOpen = false; view.SetOpen(false); input.enabled = true;
            Message = "New run ready. Hold to charge and release to launch."; view.Refresh(); return true;
        }
        public bool Purchase(UpgradeCategory category)
        {
            if (!CanPurchase) return false;
            var success = Service.TryPurchase(category, out var error);
            Message = success ? "Upgrade saved. Applies on Start Run." : error; view.Refresh(); return success;
        }
#if UNITY_EDITOR
        public void StartQaRun(float power, float energy)
        {
            if (PendingBank) return;
            // Explicit development reset discards an active attempt; neither overrides nor its earnings are persisted.
            qaAttempt = true; input.Motor.Drilling.DrillPower = power; input.Motor.Drilling.EnergyCapacity = energy;
            input.Restart(); IsOpen = false; view.SetOpen(false); input.enabled = true;
            Message = "QA override active. This attempt cannot bank permanent rewards.";
        }
        public void UseTemporaryFunding()
        {
            var state = input.Motor.Motion.State;
            if (PendingBank || (state != RunState.Ready && state != RunState.RunEnded)) return;
            var copy = Service.Snapshot; copy.Coins = 10000;
            store = new MemoryProgressStore(copy); Service = new ProgressionService(store, sessionConfig);
            IsSandbox = true; attempt = null; qaAttempt = false;
            input.Restart(); OpenWorkshop(); Message = "DEVELOPMENT SANDBOX: 10,000 Coins. No changes are written to your local save."; view.Refresh();
        }
        public bool RecoverBackup()
        {
            if (!(store is FileProgressStore disk) || !IsOpen) return false;
            if (!disk.RecoverBackup(sessionConfig, out var error)) { Message = error; view.Refresh(); return false; }
            Service = new ProgressionService(disk, sessionConfig); attempt = null;
            input.Restart(); Message = "Previous revision recovered; rejected primary archived."; view.Refresh(); return true;
        }
#endif
    }
}

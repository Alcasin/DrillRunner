using System;
using UnityEngine;
using UnityEngine.UI;

namespace DrillRunner
{
    public sealed class WorkshopView : MonoBehaviour
    {
        [SerializeField] private WorkshopController controller;
        [SerializeField] private GameObject panel, gameplay;
        [SerializeField] private CanvasGroup[] gameplayGroups;
        [SerializeField] private Text balance, status, banking;
        [SerializeField] private Text[] rows, prices;
        [SerializeField] private Button[] purchase;
        [SerializeField] private Button startRun, openWorkshop, retrySave;
        private string previousMessage;
        private bool previousCompleted;
        public GameObject Panel => panel;
        public Text Balance => balance;
        public Text Status => status;
        public Text Banking => banking;
        public Text[] Rows => rows;
        public Button[] PurchaseButtons => purchase;
        public Button StartButton => startRun;
        public Button OpenButton => openWorkshop;
        public Button SaveButton => retrySave;
        public CanvasGroup[] GameplayGroups => gameplayGroups;

        private void OnEnable()
        {
            purchase[0].onClick.AddListener(BuyPower); purchase[1].onClick.AddListener(BuyEnergy); purchase[2].onClick.AddListener(BuyDrive);
            startRun.onClick.AddListener(StartAttempt); openWorkshop.onClick.AddListener(Open); retrySave.onClick.AddListener(Save);
        }
        private void OnDisable()
        {
            purchase[0].onClick.RemoveListener(BuyPower); purchase[1].onClick.RemoveListener(BuyEnergy); purchase[2].onClick.RemoveListener(BuyDrive);
            startRun.onClick.RemoveListener(StartAttempt); openWorkshop.onClick.RemoveListener(Open); retrySave.onClick.RemoveListener(Save);
        }
        private void BuyPower() => controller.Purchase(UpgradeCategory.DrillPower);
        private void BuyEnergy() => controller.Purchase(UpgradeCategory.EnergyCapacity);
        private void BuyDrive() => controller.Purchase(UpgradeCategory.DrivePower);
        private void StartAttempt() => controller.StartRun();
        private void Open() => controller.OpenWorkshop();
        private void Save() => controller.RetryBank();
        public void SetOpen(bool value)
        {
            panel.SetActive(value);
            foreach (var group in gameplayGroups)
            { group.alpha = value ? 0 : 1; group.interactable = group.blocksRaycasts = !value; }
            if (value) panel.transform.SetAsLastSibling();
        }
        private void Update()
        {
            if (controller.Service == null) return;
            var ledger = controller.Run.Rewards; var done = ledger != null && ledger.IsCompleted;
            var state = controller.Run.Motor.Motion.State;
            openWorkshop.gameObject.SetActive(state == RunState.Ready || state == RunState.RunEnded);
            if (previousMessage != controller.Message || previousCompleted != done)
            { previousMessage = controller.Message; previousCompleted = done; Refresh(); }
        }
        public void Refresh()
        {
            if (controller.Service == null) return;
            var profile = controller.Service.Snapshot; var config = controller.Configuration;
            balance.text = "COINS  " + profile.Coins + "       COPPER  " + profile.Copper;
            status.text = (controller.IsSandbox ? "DEVELOPMENT SANDBOX\n" : "") + controller.Message;
            banking.text = controller.Run.Rewards != null && controller.Run.Rewards.IsCompleted ? controller.Message : "";
            retrySave.gameObject.SetActive(controller.PendingBank);
            // Save remains reachable from the Workshop and results without allowing navigation to discard a pending bank.
            retrySave.transform.SetParent(controller.IsOpen ? panel.transform : gameplay.transform, false);
            ((RectTransform)retrySave.transform).anchoredPosition = new Vector2(0, controller.IsOpen ? -846 : -530);
            startRun.interactable = controller.Service.Writable && !controller.PendingBank;
            for (var i = 0; i < 3; i++)
            {
                var category = (UpgradeCategory)i; var table = config.Table(category); var level = profile.Level(category); var max = level == table.MaximumLevel;
                var title = i == 0 ? "DRILL POWER" : i == 1 ? "ENERGY CAPACITY" : "DRIVE POWER";
                var unit = i == 0 ? "Power " : i == 1 ? "Max Energy " : "Cruise ";
                rows[i].text = title + "    " + level + " / " + table.MaximumLevel + "\n" + unit + table.Values[level].ToString("0.#") +
                    (max ? "  |  MAX" : "  →  " + table.Values[level + 1].ToString("0.#")) +
                    (i == 2 ? "\nRecovery " + config.Recovery[level].ToString("0.#") + (max ? "" : " → " + config.Recovery[level + 1].ToString("0.#")) : "");
                prices[i].text = max ? "MAX" : "UPGRADE  ·  " + table.Costs[level] + " COINS";
                purchase[i].interactable = controller.CanPurchase && !max && profile.Coins >= table.Costs[level];
            }
        }
    }
}

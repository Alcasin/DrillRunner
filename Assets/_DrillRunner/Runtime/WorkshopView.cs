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
        [SerializeField] private Button evolve;
        [SerializeField] private Text requirements, formName;
        [SerializeField] private RawImage formPreview;
        [SerializeField] private Text coinBalance, copperBalance;
        [SerializeField] private Text evolutionResources;
        public Text ResourceSummary => evolutionResources;
        [SerializeField] private Image evolutionGlow;
        [SerializeField] private float workshopSaveTop = 846;
        private string previousMessage;
        private bool previousCompleted;
        private readonly int[] lastPurchaseFrames = {-1, -1, -1};
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
        public Button EvolveButton => evolve;
        public Text Requirements => requirements;
        public Text FormName => formName;
        public RawImage FormPreview => formPreview;

        private void OnEnable()
        {
            purchase[0].onClick.AddListener(BuyPower); purchase[1].onClick.AddListener(BuyEnergy); purchase[2].onClick.AddListener(BuyDrive);
            startRun.onClick.AddListener(StartAttempt); openWorkshop.onClick.AddListener(Open); retrySave.onClick.AddListener(Save);
            if (evolve != null) evolve.onClick.AddListener(Evolve);
        }
        private void OnDisable()
        {
            purchase[0].onClick.RemoveListener(BuyPower); purchase[1].onClick.RemoveListener(BuyEnergy); purchase[2].onClick.RemoveListener(BuyDrive);
            startRun.onClick.RemoveListener(StartAttempt); openWorkshop.onClick.RemoveListener(Open); retrySave.onClick.RemoveListener(Save);
            if (evolve != null) evolve.onClick.RemoveListener(Evolve);
        }
        private void BuyPower() => Buy(0);
        private void BuyEnergy() => Buy(1);
        private void BuyDrive() => Buy(2);
        private void Buy(int index)
        {
            // Match Start Run's UI callback guard. Separate cards and later-frame purchases remain independent.
            if (lastPurchaseFrames[index] == Time.frameCount) return;
            lastPurchaseFrames[index] = Time.frameCount;
            controller.Purchase((UpgradeCategory)index);
        }
        private void StartAttempt() => controller.StartRunFromUi();
        private void Open() => controller.OpenWorkshop();
        private void Save() => controller.RetryBank();
        private void Evolve() => controller.Evolve();
        public void EvolutionPulse(float progress)
        {
            if (formPreview != null)
            {
                var pulse = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
                formPreview.rectTransform.localScale = Vector3.one * (1 + 0.1f * pulse);
                formPreview.color = Color.Lerp(Color.white, new Color(1, 0.86f, 0.5f), pulse * 0.35f);
                if (evolutionGlow != null) evolutionGlow.color = new Color(1, 0.68f, 0.2f, pulse * 0.35f);
            }
        }
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
            var profile = controller.Service.Snapshot; var config = controller.Service.ActiveForm;
            balance.text = "COINS  " + profile.Coins + "       COPPER  " + profile.Copper;
            if (coinBalance != null) coinBalance.text = profile.Coins.ToString();
            if (copperBalance != null)
            {
                var region=controller.Regions!=null?controller.SelectedRegion.Id:RegionIds.Yard;
                copperBalance.fontSize=region==RegionIds.Advanced?22:28;
                copperBalance.text=region==RegionIds.Quarry?profile.Iron.ToString():region==RegionIds.Advanced?profile.Quartz+" / "+profile.AlloyCore:region==RegionIds.Energy?profile.EnergyShard.ToString():profile.Copper.ToString();
                var caption=copperBalance.transform.parent.Find("Caption");if(caption!=null)caption.GetComponent<Text>().text=region==RegionIds.Quarry?"IRON":region==RegionIds.Advanced?"QUARTZ / ALLOY CORE":region==RegionIds.Energy?"ENERGY SHARD":"COPPER";
            }
            status.text = (controller.IsSandbox ? "DEVELOPMENT SANDBOX\n" : "") + controller.Message;
            banking.text = controller.Run.Rewards != null && controller.Run.Rewards.IsCompleted ? controller.Message : "";
            retrySave.gameObject.SetActive(controller.PendingBank);
            // Save remains reachable from the Workshop and results without allowing navigation to discard a pending bank.
            retrySave.transform.SetParent(controller.IsOpen ? panel.transform : gameplay.transform, false);
            ((RectTransform)retrySave.transform).anchoredPosition = new Vector2(0, controller.IsOpen ? -workshopSaveTop : -530);
            startRun.gameObject.SetActive(!controller.PendingBank);
            startRun.interactable = controller.Service.Writable && !controller.PendingBank && !controller.IsEvolving;
            if (formName != null) formName.text = config.DisplayName.ToUpperInvariant() + "  /  EVO " + config.Stage;
            if (evolutionResources != null)
            {
                var resources = "";
                foreach (EvolutionResource resource in Enum.GetValues(typeof(EvolutionResource)))
                {
                    if (resource == EvolutionResource.Copper) continue;
                    var relevant = config.NextForm != null && Array.Exists(config.RequiredResources, c => c.Resource == resource);
                    if (relevant) resources += (resources.Length == 0 ? "" : "    ·    ") + ResourceCost.Name(resource) + " " + profile.Resource(resource);
                }
                evolutionResources.text = resources;
                if (formPreview != null)
                {
                    float height=controller.Regions!=null?(resources.Length>0?48:60):resources.Length>0?82:116;
                    float width=controller.Regions!=null&&config.PreviewImage!=null?height*config.PreviewImage.width/config.PreviewImage.height:formPreview.rectTransform.sizeDelta.x;
                    formPreview.rectTransform.sizeDelta=new Vector2(width,height);
                }
            }
            if (formPreview != null) formPreview.texture = config.PreviewImage;
            var eligibility = controller.Evolution; // One authoritative evaluation per refresh, shared by text and button.
            if (requirements != null) { requirements.fontSize = config.NextForm != null && config.RequiredResources.Length > 1 ? 14 : 16; requirements.text = EvolutionText(eligibility, config, profile, controller.Regions); }
            if (evolve != null) { evolve.gameObject.SetActive(eligibility.HasNext); evolve.interactable = eligibility.Ready; }
            for (var i = 0; i < 3; i++)
            {
                var category = (UpgradeCategory)i; var table = config.Table(category); var level = profile.Level(category); var max = level == table.MaximumLevel;
                var title = i == 0 ? "DRILL POWER" : i == 1 ? "ENERGY CAPACITY" : "DRIVE POWER";
                var unit = i == 0 ? "Power " : i == 1 ? "Max Energy " : "Cruise ";
                var value = i == 2 && config.Stage >= 3 ? "Launch " + config.MinimumBoost[level].ToString("0.##") +
                    (max ? "  |  MAX" : " → " + config.MinimumBoost[level + 1].ToString("0.##")) + "  ·  Cruise " + config.Drive.Values[level].ToString("0.#") :
                    unit + table.Values[level].ToString("0.#") + (max ? "  |  MAX" : "  →  " + table.Values[level + 1].ToString("0.#"));
                rows[i].text = title + "    " + level + " / " + table.MaximumLevel + "\n" + value +
                    (i == 2 ? "\nRecovery " + config.Recovery[level].ToString("0.#") + (max ? "" : " → " + config.Recovery[level + 1].ToString("0.#")) : "");
                if (i == 2 && controller.Balance != null)
                {
                    var coast = controller.Balance.ForForm(config.FormId).BoostDecayByDriveLevel;
                    var starterCoast = controller.Balance.ForForm(ProgressionConfig.PocketForm).BoostDecayByDriveLevel[0];
                    var carry = "Coast ×" + (starterCoast / coast[level]).ToString("0.##") + (max ? "" : " → " + (starterCoast / coast[level + 1]).ToString("0.##"));
                    rows[i].text = title + "    " + level + " / " + table.MaximumLevel + "\n" +
                        (config.Stage >= 3 ? "Launch " + config.MinimumBoost[level].ToString("0.##") + (max ? "" : " → " + config.MinimumBoost[level + 1].ToString("0.##")) :
                            "Cruise " + table.Values[level].ToString("0.#") + (max ? "" : " → " + table.Values[level + 1].ToString("0.#"))) + "  ·  " + carry +
                        "\nRecovery " + config.Recovery[level].ToString("0.#") + (max ? "  |  MAX" : " → " + config.Recovery[level + 1].ToString("0.#"));
                }
                prices[i].text = max ? "MAX" : "UPGRADE  ·  " + table.Costs[level] + " COINS";
                purchase[i].interactable = controller.CanPurchase && !max && profile.Coins >= table.Costs[level];
            }
        }
        internal static string EvolutionText(EvolutionEligibility eligibility, ProgressionConfig config, PlayerProgress profile, RegionCatalog regions)
        {
            if (regions == null || !eligibility.HasNext || eligibility.Ready || !eligibility.Allowed || eligibility.ResourcesAvailable ||
                !eligibility.PowerComplete || !eligibility.EnergyComplete || !eligibility.DriveComplete) return eligibility.Description;
            // Reuse the authored source/unlock catalog; guide one missing ingredient without adding another text row.
            foreach (var region in regions.Regions)
            {
                if (!region.CanPlay(config.Stage)) continue;
                foreach (var cost in config.RequiredResources)
                {
                    long missing = cost.Amount - profile.Resource(cost.Resource);
                    if (missing <= 0) continue;
                    foreach (var resource in region.Resources)
                        if (resource == cost.Resource)
                            return eligibility.Description.Substring(0, eligibility.Description.LastIndexOf('\n') + 1) +
                                "COLLECT " + missing + " " + ResourceCost.Name(resource) + " → " + region.DisplayName;
                }
            }
            return eligibility.Description;
        }
    }
}

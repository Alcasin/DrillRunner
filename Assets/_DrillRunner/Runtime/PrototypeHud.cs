using UnityEngine;
using UnityEngine.UI;

namespace DrillRunner
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private DrillPointerInput input;
        [SerializeField] private Image chargeFill;
        [SerializeField] private Text stateText;
        [SerializeField] private Text hintText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Image energyFill;
        [SerializeField] private Image durabilityFill;
        [SerializeField] private Text materialText;
        [SerializeField] private RunProgress runProgress;
        [SerializeField] private Text rewardText;
        [SerializeField] private Text summaryText;
        [SerializeField] private WorkshopController workshop;
        [SerializeField] private Text attributesText;
        [SerializeField] private bool premiumSummary;
        private int shownEnergy = -1;
        private float shownPower = -1, shownCapacity = -1;
        private int displayedDistance = -1;
        private long displayedCoins = -1, displayedCopper = -1;
        private RunRewards displayedRun;
        private RunState previousState = (RunState)(-1);
        private DrillObstacle previousTarget;
        private int lastRestartFrame = -1;
        private float nextAttributeRefresh;

        public DrillPointerInput Input => input;
        public Button RestartButton => restartButton;
        public Image ChargeFill => chargeFill;
        public Text StateText => stateText;
        public Image EnergyFill => energyFill;
        public Image DurabilityFill => durabilityFill;
        public Text RewardText => rewardText;
        public Text SummaryText => summaryText;

        private void OnEnable() => restartButton.onClick.AddListener(RestartClicked);
        private void OnDisable() => restartButton.onClick.RemoveListener(RestartClicked);
        private void RestartClicked()
        {
            if (lastRestartFrame == Time.frameCount) return;
            lastRestartFrame = Time.frameCount;
            if (workshop != null && input.Motor.Motion.State == RunState.RunEnded) workshop.StartRunFromUi();
            else Restart();
        }
        public void Restart() => input.Restart();

        private void Update()
        {
            var motion = input.Motor.Motion;
            UpdateRewards();
            if (attributesText != null && input.Motor.Energy != null)
            {
                var energy = Mathf.CeilToInt(input.Motor.Energy.Remaining);
                var attributesChanged = shownPower != input.Motor.DrillPower || shownCapacity != input.Motor.Energy.Capacity;
                var refreshDue = workshop == null || workshop.Balance == null || Time.unscaledTime >= nextAttributeRefresh || energy == 0 || motion.State == RunState.Ready;
                if (attributesChanged || (energy != shownEnergy && refreshDue))
                {
                    nextAttributeRefresh = Time.unscaledTime + 0.1f; // Continuous Energy fill; bounded numeric-text rebuilds in rapid runs.
                    shownEnergy = energy; shownPower = input.Motor.DrillPower; shownCapacity = input.Motor.Energy.Capacity;
                    attributesText.text = "POWER " + shownPower.ToString("0.#") + "     ENERGY " + energy + " / " + shownCapacity.ToString("0.#");
                }
            }
            chargeFill.fillAmount = motion.ChargeNormalized;
            var target = input.Motor.ContactTarget;
            if (energyFill != null)
                energyFill.fillAmount = input.Motor.Energy != null ? input.Motor.Energy.Fraction : 0;
            if (durabilityFill != null)
                durabilityFill.fillAmount = target != null && target.Durability != null ? 1 - target.Durability.Fraction : 0;
            if (previousState == motion.State && previousTarget == target)
                return;
            previousState = motion.State;
            restartButton.GetComponentInChildren<Text>().text = motion.State == RunState.RunEnded ? "RETRY" : "RESET";
            previousTarget = target;
            if (materialText != null)
                materialText.text = target != null && target.Durability != null ? target.Durability.DisplayName +
                    "  |  Hardness " + target.Durability.Hardness.ToString("0.#") + "  |  Power " + input.Motor.DrillPower.ToString("0.#") : "DRILL PROGRESS";
            switch (motion.State)
            {
                case RunState.Ready:
                    stateText.text = "READY";
                    hintText.text = "Hold anywhere on the track to charge";
                    chargeFill.color = new Color(0.2f, 0.85f, 0.85f);
                    break;
                case RunState.Charging:
                    stateText.text = "CHARGING";
                    hintText.text = "Release to launch";
                    chargeFill.color = new Color(1f, 0.72f, 0.15f);
                    break;
                case RunState.Moving:
                    stateText.text = "MOVING";
                    hintText.text = "Press and drag horizontally to steer";
                    chargeFill.color = new Color(0.3f, 0.9f, 0.5f);
                    break;
                case RunState.Drilling:
                    stateText.text = "DRILLING";
                    hintText.text = "Breaking material - steering locked";
                    break;
                case RunState.Blocked:
                    stateText.text = "BLOCKED";
                    hintText.text = input.Motor.ContactIsTooHard ? "Requires more power - drag around it" : "Align the drill tip or steer around";
                    break;
                case RunState.RunEnded:
                    stateText.text = "ENERGY EMPTY";
                    hintText.text = workshop != null ? "Run complete · Retry or Workshop" : "Run ended - press RESTART";
                    break;
            }
        }

        private void UpdateRewards()
        {
            var run = runProgress != null ? runProgress.Rewards : null;
            if (run == null || rewardText == null || summaryText == null) return;
            var distance = (int)System.Math.Min(int.MaxValue, System.Math.Floor(run.Distance));
            if (displayedRun != run || displayedDistance != distance || displayedCoins != run.Coins || displayedCopper != run.Copper)
            {
                displayedRun = run; displayedDistance = distance; displayedCoins = run.Coins; displayedCopper = run.Copper;
                rewardText.text = distance + " m    COINS " + run.Coins + "    COPPER " + run.Copper;
                summaryText.text = premiumSummary ? "<size=18>RUN COMPLETE</size>\n<size=40>" + distance + " m</size>\n<size=22>" + run.Coins + " Coins  ·  " + run.Copper + " Copper</size>" :
                    "RUN COMPLETE\n" + distance + " m  |  " + run.Coins + " Coins  |  " + run.Copper + " Copper";
            }
            summaryText.gameObject.SetActive(run.IsCompleted);
        }
    }
}

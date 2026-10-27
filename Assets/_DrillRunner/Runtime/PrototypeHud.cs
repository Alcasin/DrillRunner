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
        private int displayedDistance = -1;
        private long displayedCoins = -1, displayedCopper = -1;
        private RunRewards displayedRun;
        private RunState previousState = (RunState)(-1);
        private DrillObstacle previousTarget;

        public DrillPointerInput Input => input;
        public Button RestartButton => restartButton;
        public Image ChargeFill => chargeFill;
        public Text StateText => stateText;
        public Image EnergyFill => energyFill;
        public Image DurabilityFill => durabilityFill;
        public Text RewardText => rewardText;
        public Text SummaryText => summaryText;

        private void OnEnable() => restartButton.onClick.AddListener(Restart);
        private void OnDisable() => restartButton.onClick.RemoveListener(Restart);
        public void Restart() => input.Restart();

        private void Update()
        {
            var motion = input.Motor.Motion;
            UpdateRewards();
            chargeFill.fillAmount = motion.ChargeNormalized;
            var target = input.Motor.ContactTarget;
            if (energyFill != null)
                energyFill.fillAmount = input.Motor.Energy != null ? input.Motor.Energy.Fraction : 0;
            if (durabilityFill != null)
                durabilityFill.fillAmount = target != null && target.Durability != null ? 1 - target.Durability.Fraction : 0;
            if (previousState == motion.State && previousTarget == target)
                return;
            previousState = motion.State;
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
                    hintText.text = "Run ended - press RESTART";
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
                summaryText.text = "RUN COMPLETE\n" + distance + " m  |  " + run.Coins + " Coins  |  " + run.Copper + " Copper";
            }
            summaryText.gameObject.SetActive(run.IsCompleted);
        }
    }
}

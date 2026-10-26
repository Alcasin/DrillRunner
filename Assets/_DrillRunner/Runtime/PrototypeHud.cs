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
        private RunState previousState = (RunState)(-1);

        public DrillPointerInput Input => input;
        public Button RestartButton => restartButton;
        public Image ChargeFill => chargeFill;
        public Text StateText => stateText;

        private void OnEnable() => restartButton.onClick.AddListener(Restart);
        private void OnDisable() => restartButton.onClick.RemoveListener(Restart);
        public void Restart() => input.Restart();

        private void Update()
        {
            var motion = input.Motor.Motion;
            chargeFill.fillAmount = motion.ChargeNormalized;
            if (previousState == motion.State)
                return;
            previousState = motion.State;
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
            }
        }
    }
}

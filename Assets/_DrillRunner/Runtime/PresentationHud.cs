using UnityEngine;
using UnityEngine.UI;

namespace DrillRunner
{
    public sealed class PresentationHud : MonoBehaviour
    {
        [SerializeField] private PrototypeHud hud;
        [SerializeField] private WorkshopController flow;
        [SerializeField] private GameObject resultsCard;
        [SerializeField] private Text toast, chargeLabel;
        [SerializeField] private CanvasGroup chargeGroup, rewardGroup;
        [SerializeField] private Image statePlate;
        [SerializeField] private GameObject resourcePlate;
        private float toastRemaining;
        private bool ended;
        private int shownCharge = -1;
        private RunState chargeState = (RunState)(-1);
        private bool copperToast;
        private Vector2 toastPosition;
        private bool hasToastPosition;
        public GameObject ResultsCard => resultsCard;
        public string Toast => toast.text;
        public void Reward(long amount, bool copper)
        {
            if (!hasToastPosition) { toastPosition = toast.rectTransform.anchoredPosition; hasToastPosition = true; }
            copperToast = copper; toast.text = "+" + amount + (copper ? " COPPER" : " COIN");
            toast.color = copper ? new Color(0.51f, 0.21f, 0.04f) : new Color(0.03f, 0.45f, 0.45f); toastRemaining = copper ? 1.6f : 1.1f;
        }
        public void ClearToast() { toastRemaining = 0; if (toast != null) { toast.text = ""; toast.rectTransform.localScale = Vector3.one; } }
        private void LateUpdate()
        {
            var motor = hud.Input.Motor; if (motor.Motion == null) return;
            var state = motor.Motion.State; var nextEnded = state == RunState.RunEnded;
            resultsCard.SetActive(nextEnded && !flow.IsOpen);
            chargeGroup.alpha = !flow.IsOpen && (state == RunState.Ready || state == RunState.Charging) ? 1 : 0;
            chargeLabel.gameObject.SetActive(state == RunState.Ready || state == RunState.Charging);
            resourcePlate.SetActive(!nextEnded);
            rewardGroup.alpha = flow.IsOpen || nextEnded ? 0 : 1;
            var chargePercent = Mathf.RoundToInt(motor.Motion.ChargeNormalized * 100);
            if (chargeState != state || shownCharge != chargePercent)
            {
                chargeState = state; shownCharge = chargePercent;
                chargeLabel.text = state == RunState.Charging ? chargePercent == 100 ? "MAX CHARGE · RELEASE!" : "HOLD · " + chargePercent + "%" : "HOLD TO CHARGE";
            }
            statePlate.color = state == RunState.Blocked ? new Color(1, 0.86f, 0.72f) : new Color(0.96f, 0.98f, 0.97f, 0.96f);
            hud.StateText.color = state == RunState.Blocked ? new Color(0.65f, 0.22f, 0.1f) : new Color(0.07f, 0.19f, 0.24f);
            hud.ChargeFill.color = Color.Lerp(new Color(0.9f, 0.57f, 0.12f), new Color(1, 0.78f, 0.25f), motor.Motion.ChargeNormalized);
            if (ended != nextEnded)
            {
                ended = nextEnded; var rect = hud.RestartButton.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = ended ? new Vector2(124, -521) : new Vector2(183, -47);
                rect.sizeDelta = ended ? new Vector2(236, 52) : new Vector2(126, 38);
                var label = hud.RestartButton.GetComponentInChildren<Text>(); label.text = ended ? "RETRY" : "RESET";
                label.rectTransform.sizeDelta = rect.sizeDelta;
            }
            toastRemaining = Mathf.Max(0, toastRemaining - Time.unscaledDeltaTime);
            var color = toast.color; color.a = flow.IsOpen ? 0 : Mathf.Clamp01(toastRemaining * 3); toast.color = color;
            if (toastRemaining > 0)
            {
                var elapsed = (copperToast ? 1.6f : 1.1f) - toastRemaining;
                toast.rectTransform.anchoredPosition = toastPosition + Vector2.up * Mathf.Min(18, elapsed * 16);
                toast.rectTransform.localScale = Vector3.one * (1 + (copperToast ? 0.13f : 0.05f) * Mathf.Clamp01(1 - elapsed * 4));
            }
        }
    }
}

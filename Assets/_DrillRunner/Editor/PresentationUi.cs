using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DrillRunner.Editor
{
    public static class PresentationUi
    {
        private static readonly Color Ink = new Color(0.065f, 0.17f, 0.23f);
        private static readonly Color Paper = new Color(0.91f, 0.95f, 0.94f);
        private static readonly Color Teal = new Color(0.025f, 0.47f, 0.5f);
        public static PresentationHud Build(PrototypeHud hud, WorkshopController flow)
        {
            var view = flow.View; var panel = view.Panel.GetComponent<RectTransform>(); var sprite = PresentationAssets.Sprite("M6Rounded");
            panel.GetComponent<Image>().color = Paper;
            foreach (var text in panel.GetComponentsInChildren<Text>(true)) text.color = Ink;
            var title = panel.Find("Title").GetComponent<Text>(); title.fontStyle = FontStyle.Bold; title.fontSize = 32; Top(title.rectTransform, 24, 492, 44);
            panel.Find("Permanent Balances").GetComponent<Text>().color = Color.clear;
            var coins = Resource(panel, "M6 Coins", -124, "COINS", new Color(0.77f, 0.45f, 0.08f), sprite);
            var copper = Resource(panel, "M6 Copper", 124, "COPPER", new Color(0.68f, 0.31f, 0.13f), sprite);
            var display = panel.Find("Pocket Drill Presentation").GetComponent<RectTransform>(); Top(display, 149, 492, 146); Card(display.GetComponent<Image>(), sprite, Color.white);
            Top(view.FormName.rectTransform, 7, 480, 28); view.FormName.fontSize = 19; view.FormName.fontStyle = FontStyle.Bold;
            Top(view.FormPreview.rectTransform, 32, 280, 116);
            var glow = Rect(display, "M6 Evolution Glow", 30, 310, 116).gameObject.AddComponent<Image>(); Card(glow, sprite, Color.clear); glow.transform.SetSiblingIndex(1);
            for (var i = 0; i < 3; i++)
            {
                var card = view.Rows[i].transform.parent.GetComponent<RectTransform>(); Top(card, 311 + i * 126, 492, 120); Card(card.GetComponent<Image>(), sprite, Color.white);
                var stripe = Rect(card, "M6 Card Accent", 12, 4, 96).gameObject.AddComponent<Image>(); stripe.rectTransform.anchoredPosition = new Vector2(-238, -12); stripe.color = i == 0 ? new Color(0.94f, 0.62f, 0.17f) : Teal; stripe.raycastTarget = false;
                Top(view.Rows[i].rectTransform, 4, 476, 66); view.Rows[i].fontSize = 18; view.Rows[i].color = Ink;
                Top(view.PurchaseButtons[i].GetComponent<RectTransform>(), 75, 450, 38); Button(view.PurchaseButtons[i], sprite, Teal);
                var label = view.PurchaseButtons[i].GetComponentInChildren<Text>(); Top(label.rectTransform, 0, 438, 38); label.fontSize = 18;
            }
            var evolution = panel.Find("M5 Evolution").GetComponent<RectTransform>(); Top(evolution, 704, 492, 112); Card(evolution.GetComponent<Image>(), sprite, new Color(0.83f, 0.9f, 0.88f));
            Top(view.Requirements.rectTransform, 2, 480, 67); view.Requirements.fontSize = 16; view.Requirements.color = Ink;
            Top(view.EvolveButton.GetComponent<RectTransform>(), 72, 450, 36); Button(view.EvolveButton, sprite, new Color(0.81f, 0.43f, 0.08f));
            Top(view.EvolveButton.GetComponentInChildren<Text>().rectTransform, 0, 438, 36);
            Top(view.StartButton.GetComponent<RectTransform>(), 829, 492, 56); Button(view.StartButton, sprite, Teal);
            Top(view.StartButton.GetComponentInChildren<Text>().rectTransform, 0, 480, 56); view.StartButton.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
            Top(view.Status.rectTransform, 898, 492, 55); view.Status.fontSize = 15; view.Status.color = new Color(0.25f, 0.35f, 0.38f);
            Button(view.SaveButton, sprite, new Color(0.75f, 0.32f, 0.08f)); view.SaveButton.GetComponent<RectTransform>().sizeDelta = new Vector2(492, 56);
            view.SaveButton.GetComponentInChildren<Text>().rectTransform.sizeDelta = new Vector2(480, 56);
            var viewData = new SerializedObject(view); Ref(viewData, "coinBalance", coins); Ref(viewData, "copperBalance", copper); Ref(viewData, "evolutionGlow", glow);
            viewData.FindProperty("workshopSaveTop").floatValue = 829; viewData.ApplyModifiedProperties();

            var statePlate = Rect(hud.transform, "M6 State Plate", 16, 492, 70).gameObject.AddComponent<Image>(); Card(statePlate, sprite, Color.white); statePlate.transform.SetAsFirstSibling();
            Top(hud.StateText.rectTransform, 28, 316, 36); hud.StateText.rectTransform.anchoredPosition = new Vector2(-78, -28);
            hud.StateText.fontSize = 27; hud.StateText.fontStyle = FontStyle.Bold;
            Button(hud.RestartButton, sprite, Teal); Top(hud.RestartButton.GetComponent<RectTransform>(), 28, 126, 38); hud.RestartButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(183, -28);
            var restartLabel = hud.RestartButton.GetComponentInChildren<Text>(); restartLabel.text = "RESET"; restartLabel.fontSize = 17; restartLabel.rectTransform.sizeDelta = new Vector2(126, 38);
            Top(restartLabel.rectTransform, 0, 126, 38);
            hud.RestartButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f); hud.RestartButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(183, -47);
            var hint = hud.transform.Find("Hint").GetComponent<Text>(); Top(hint.rectTransform, 111, 492, 42); hint.fontSize = 17; hint.color = Ink;
            var charge = hud.ChargeFill.transform.parent.GetComponent<RectTransform>(); Top(charge, 94, 492, 12); Card(charge.GetComponent<Image>(), sprite, new Color(0.72f, 0.81f, 0.8f));
            hud.ChargeFill.sprite = sprite; hud.ChargeFill.rectTransform.sizeDelta = new Vector2(-2, -2);
            var chargeGroup = charge.GetComponent<CanvasGroup>(); if (chargeGroup == null) chargeGroup = charge.gameObject.AddComponent<CanvasGroup>();
            var chargeLabel = Label(hud.transform, "M6 Charge Label", 77, 13, 18); chargeLabel.color = Ink;
            var rewardGroup = hud.RewardText.GetComponent<CanvasGroup>(); Top(hud.RewardText.rectTransform, 160, 492, 36); hud.RewardText.fontSize = 20; hud.RewardText.color = Ink;
            var resourcePlate = Rect(hud.transform, "M6 Resource Plate", 156, 492, 44).gameObject.AddComponent<Image>(); Card(resourcePlate, sprite, new Color(0.96f, 0.98f, 0.97f, 0.95f)); resourcePlate.transform.SetSiblingIndex(hud.RewardText.transform.GetSiblingIndex());

            var feedback = hud.transform.Find("M2 Feedback");
            var bottom = Rect(feedback, "M6 Instrument Plate", 0, 540, 174).gameObject.AddComponent<Image>(); Bottom(bottom.rectTransform, 0, 540, 174); Card(bottom, sprite, new Color(0.96f, 0.98f, 0.97f, 0.95f)); bottom.transform.SetAsFirstSibling();
            Bottom(hud.EnergyFill.transform.parent.GetComponent<RectTransform>(), 40, 492, 13); Card(hud.EnergyFill.transform.parent.GetComponent<Image>(), sprite, new Color(0.74f, 0.82f, 0.8f));
            hud.EnergyFill.sprite = sprite; hud.EnergyFill.color = Teal;
            var energyLabel = feedback.Find("Energy Label").GetComponent<Text>(); Bottom(energyLabel.rectTransform, 15, 492, 20); energyLabel.fontSize = 12; energyLabel.color = Ink;
            var attributes = hud.transform.Find("M4 Effective Attributes").GetComponent<Text>(); Bottom(attributes.rectTransform, 65, 492, 24); attributes.fontSize = 18; attributes.color = Ink;
            var material = feedback.Find("Material Label").GetComponent<Text>(); Bottom(material.rectTransform, 132, 492, 28); material.fontSize = 17; material.color = Ink;
            Bottom(hud.DurabilityFill.transform.parent.GetComponent<RectTransform>(), 113, 492, 10); Card(hud.DurabilityFill.transform.parent.GetComponent<Image>(), sprite, new Color(0.74f, 0.82f, 0.8f));
            hud.DurabilityFill.sprite = sprite; hud.DurabilityFill.color = new Color(0.9f, 0.55f, 0.11f);
            var result = Rect(hud.transform, "M6 Results Card", 215, 492, 265).gameObject.AddComponent<Image>(); Card(result, sprite, new Color(0.97f, 0.99f, 0.98f, 0.98f));
            Top(hud.SummaryText.rectTransform, 238, 468, 145); hud.SummaryText.color = Ink; hud.SummaryText.transform.SetAsLastSibling();
            Top(view.Banking.rectTransform, 393, 464, 68); view.Banking.color = new Color(0.1f, 0.4f, 0.39f); view.Banking.fontSize = 16; view.Banking.transform.SetAsLastSibling();
            Top(view.OpenButton.GetComponent<RectTransform>(), 495, 236, 52); view.OpenButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-124, -495);
            Button(view.OpenButton, sprite, Teal); Top(view.OpenButton.GetComponentInChildren<Text>().rectTransform, 0, 224, 52); view.OpenButton.GetComponentInChildren<Text>().fontSize = 19;
            var toast = Label(hud.transform, "M6 Reward Toast", 205, 22, 36); toast.fontStyle = FontStyle.Bold; toast.text = "";
            var presentation = flow.gameObject.AddComponent<PresentationHud>(); var data = new SerializedObject(presentation);
            Ref(data, "hud", hud); Ref(data, "flow", flow); Ref(data, "resultsCard", result.gameObject); Ref(data, "toast", toast); Ref(data, "chargeLabel", chargeLabel);
            Ref(data, "chargeGroup", chargeGroup); Ref(data, "rewardGroup", rewardGroup); Ref(data, "statePlate", statePlate); data.ApplyModifiedProperties();
            Ref(data, "resourcePlate", resourcePlate.gameObject); data.ApplyModifiedProperties();
            var hudData = new SerializedObject(hud); hudData.FindProperty("premiumSummary").boolValue = true; hudData.ApplyModifiedProperties();
            var groupObjects = new[] { statePlate.gameObject, chargeLabel.gameObject, resourcePlate.gameObject, result.gameObject, toast.gameObject };
            var visibility = new SerializedObject(view); var array = visibility.FindProperty("gameplayGroups");
            var start = array.arraySize; array.arraySize += groupObjects.Length;
            for (var i = 0; i < groupObjects.Length; i++) array.GetArrayElementAtIndex(start + i).objectReferenceValue = groupObjects[i].AddComponent<CanvasGroup>();
            visibility.ApplyModifiedProperties(); result.gameObject.SetActive(false); panel.SetAsLastSibling(); return presentation;
        }
        private static Text Resource(Transform parent, string name, float x, string label, Color color, Sprite sprite)
        {
            var pill = Rect(parent, name, 78, 238, 58); pill.anchoredPosition = new Vector2(x, -78); Card(pill.gameObject.AddComponent<Image>(), sprite, Color.white);
            var caption = Label(pill, "Caption", 4, 12, 18); caption.text = label; caption.color = color; caption.fontStyle = FontStyle.Bold;
            var value = Label(pill, "Value", 22, 26, 33); value.rectTransform.sizeDelta = new Vector2(224, 33); value.fontStyle = FontStyle.Bold; value.color = Ink; return value;
        }
        public static void ReviseGameplay(PrototypeHud hud)
        {
            var hint = hud.transform.Find("Hint").GetComponent<Text>(); Top(hint.rectTransform, 132, 500, 26); hint.fontSize = 19;
            var charge = hud.ChargeFill.transform.parent.GetComponent<RectTransform>(); Top(charge, 112, 492, 16);
            var label = hud.transform.Find("M6 Charge Label").GetComponent<Text>(); Top(label.rectTransform, 86, 492, 24); label.fontSize = 18; label.fontStyle = FontStyle.Bold;
            Top(hud.RewardText.rectTransform, 166, 500, 40); hud.RewardText.fontSize = 22;
            Top(hud.transform.Find("M6 Resource Plate").GetComponent<RectTransform>(), 164, 510, 46);
            var feedback = hud.transform.Find("M2 Feedback");
            Bottom(hud.EnergyFill.transform.parent.GetComponent<RectTransform>(), 41, 492, 19);
            label = feedback.Find("Energy Label").GetComponent<Text>(); Bottom(label.rectTransform, 12, 492, 24); label.fontSize = 16; label.fontStyle = FontStyle.Bold;
            label = hud.transform.Find("M4 Effective Attributes").GetComponent<Text>(); Bottom(label.rectTransform, 68, 510, 30); label.fontSize = 22; label.fontStyle = FontStyle.Bold;
            label = feedback.Find("Material Label").GetComponent<Text>(); Bottom(label.rectTransform, 132, 510, 30); label.fontSize = 18;
            Bottom(hud.DurabilityFill.transform.parent.GetComponent<RectTransform>(), 111, 492, 14);
            label = hud.transform.Find("M6 Reward Toast").GetComponent<Text>(); Top(label.rectTransform, 225, 500, 42); label.fontSize = 28;
        }
        private static void Button(Button button, Sprite sprite, Color color)
        {
            Card(button.GetComponent<Image>(), sprite, Color.white); var colors = button.colors;
            colors.normalColor = color; colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f); colors.pressedColor = color * 0.78f;
            colors.disabledColor = new Color(0.69f, 0.77f, 0.76f); colors.fadeDuration = 0.08f; button.colors = colors;
            button.GetComponentInChildren<Text>().color = Color.white;
        }
        private static void Card(Image image, Sprite sprite, Color color) { image.sprite = sprite; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = image.GetComponent<Button>() != null; }
        private static RectTransform Rect(Transform parent, string name, float top, float width, float height)
        { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); Top(rect, top, width, height); return rect; }
        private static Text Label(Transform parent, string name, float top, int size, float height)
        {
            var text = Rect(parent, name, top, 480, height).gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size; text.alignment = TextAnchor.MiddleCenter; text.color = Ink; text.raycastTarget = false; return text;
        }
        private static void Top(RectTransform rect, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1); rect.anchoredPosition = new Vector2(0, -y); rect.sizeDelta = new Vector2(width, height); }
        private static void Bottom(RectTransform rect, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0); rect.pivot = new Vector2(0.5f, 0); rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(width, height); }
        private static void Ref(SerializedObject data, string name, Object value) => data.FindProperty(name).objectReferenceValue = value;
    }
}

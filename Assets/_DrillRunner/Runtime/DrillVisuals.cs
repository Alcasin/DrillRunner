using System;
using UnityEngine;

namespace DrillRunner
{
    /// <summary>Visual children only. The motor, footprint, contact tip, input, and camera never change.</summary>
    [DisallowMultipleComponent]
    public sealed class DrillVisuals : MonoBehaviour
    {
        [SerializeField] private GameObject pocketVisual;
        private GameObject workshopVisual;
        private GameObject workshopSource;
        public string CurrentForm { get; private set; }
        public GameObject ActiveVisual => CurrentForm == ProgressionConfig.WorkshopForm ? workshopVisual : pocketVisual;
        public void Show(ProgressionConfig form)
        {
            if (form == null || pocketVisual == null) throw new ArgumentException("Missing form visual configuration.");
            var evolved = form.FormId == ProgressionConfig.WorkshopForm;
            if (evolved && (workshopVisual == null || workshopSource != form.VisualPrefab))
            {
                if (form.VisualPrefab == null) throw new ArgumentException("Workshop Drill needs an authored visual prefab.");
                if (workshopVisual != null) { workshopVisual.SetActive(false); Destroy(workshopVisual); }
                workshopVisual = Instantiate(form.VisualPrefab, transform, false); workshopSource = form.VisualPrefab;
                workshopVisual.name = "Workshop Drill Visual";
            }
            pocketVisual.SetActive(!evolved); if (workshopVisual != null) workshopVisual.SetActive(evolved);
            CurrentForm = form.FormId;
        }
    }
}

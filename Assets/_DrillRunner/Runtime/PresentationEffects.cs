using UnityEngine;

namespace DrillRunner
{
    public sealed class PresentationEffects : MonoBehaviour
    {
        [SerializeField] private PresentationTheme theme;
        [SerializeField] private ParticleSystem contact;
        [SerializeField] private ParticleSystem[] bursts;
        private int cursor;
        private string currentMaterial;
        public bool IsDrilling { get; private set; }
        public int BurstCount { get; private set; }
        public int SystemCount => bursts.Length + 1;
        public int CopperResponses { get; private set; }
        public int IronResponses { get; private set; }
        public int AdvancedResponses { get; private set; }
        public int ParticleCount
        {
            get { var count = contact.particleCount; foreach (var burst in bursts) count += burst.particleCount; return count; }
        }
        public void SetDrilling(bool active, string materialId, Vector3 point)
        {
            contact.transform.position = point + new Vector3(0, 0.05f, -0.1f);
            if (currentMaterial != materialId)
            {
                currentMaterial = materialId; var main = contact.main; main.startColor = Color.Lerp(theme.MaterialColor(materialId), Color.white, 0.2f);
                main.startSize3D = true; main.startSizeX = materialId == "wood" ? 0.07f : 0.12f;
                main.startSizeY = materialId == "wood" ? 0.045f : 0.1f; main.startSizeZ = materialId == "wood" ? 0.22f : 0.12f;
                var shape = contact.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 55; shape.radius = 0.08f;
                contact.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0.8f, -1));
            }
            if (active == IsDrilling) return;
            IsDrilling = active;
            if (active) contact.Play(); else contact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public void Burst(string materialId, Bounds bounds, bool confirmedCopper = false)
        {
            var burst = bursts[cursor]; cursor = (cursor + 1) % bursts.Length; burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            burst.transform.position = bounds.center;
            var main = burst.main; main.startColor = theme.MaterialColor(materialId);
            main.startSize3D = true;
            main.startSizeX = materialId == "wood" ? 0.15f : 0.27f; main.startSizeY = materialId == "wood" ? 0.09f : materialId == "copper_ore" ? 0.34f : 0.23f;
            main.startSizeZ = materialId == "wood" ? 0.48f : 0.25f;
            main.startLifetime = materialId == "stone" ? 0.9f : 0.75f;
            var count = materialId == "wood" ? 18 : 22;
            for (var i = 0; i < count; i++)
            {
                var angle = i * 2.39996f; var radial = 0.25f + (i % 4) * 0.19f;
                var offset = new Vector3(Mathf.Cos(angle) * radial, ((i % 5) / 4f - 0.5f) * 1.5f, Mathf.Sin(angle) * radial);
                var emit = new ParticleSystem.EmitParams
                {
                    position = bounds.center + Vector3.Scale(offset, bounds.extents),
                    velocity = new Vector3(Mathf.Cos(angle) * 1.7f, 1.2f + (i % 3) * 0.6f, -1.2f + Mathf.Sin(angle)),
                    startColor = Color.Lerp(theme.MaterialColor(materialId), Color.white, (i % 3) * 0.12f)
                };
                // Reward glints use the same bounded burst, only after the ledger confirms Copper.
                if (confirmedCopper && i >= 16)
                { emit.velocity = new Vector3(Mathf.Cos(angle) * 0.8f, 2.8f, -0.8f); emit.startColor = materialId=="copper_ore"?new Color(1,0.84f,0.4f):Color.Lerp(theme.MaterialColor(materialId),Color.white,0.4f); emit.startSize3D = new Vector3(0.09f, 0.42f, 0.09f); }
                burst.Emit(emit, 1);
            }
            if (confirmedCopper) {if(materialId=="iron_ore")IronResponses++;else if(materialId=="copper_ore")CopperResponses++;else AdvancedResponses++;}
            BurstCount++;
        }
        public void StopAll()
        {
            IsDrilling = false;
            if (contact != null) contact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (bursts != null) foreach (var burst in bursts) if (burst != null) burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        private void OnDisable() => StopAll();
    }
}

using UnityEngine;

namespace DrillRunner
{
    [DisallowMultipleComponent]
    public sealed class ObstacleAppearance : MonoBehaviour
    {
        [SerializeField] private GameObject details;
        [SerializeField] private GameObject[] cracks;
        private DrillObstacle obstacle;
        private MeshRenderer body;
        private MaterialPropertyBlock block;
        private Color baseColor;
        public int DamageStage { get; private set; }
        private void OnEnable()
        {
            obstacle = GetComponent<DrillObstacle>(); body = GetComponent<MeshRenderer>();
            block = new MaterialPropertyBlock(); baseColor = body.sharedMaterial.GetColor("_BaseColor");
            obstacle.Broken += Broken; obstacle.Restored += Restored; Restored();
        }
        private void OnDisable()
        { if (obstacle != null) { obstacle.Broken -= Broken; obstacle.Restored -= Restored; } }
        private void Broken(DrillObstacle source) { if (details != null) details.SetActive(false); }
        private void Restored()
        {
            DamageStage = -1; if (details != null) details.SetActive(true); SetDamage(1);
            if (obstacle.Durability != null && obstacle.Durability.IsDestroyed) Broken(obstacle);
        }
        public void SetDamage(float healthFraction)
        {
            var stage = Mathf.Clamp(Mathf.FloorToInt((1 - healthFraction) * 4), 0, 3);
            if (stage == DamageStage) return;
            DamageStage = stage;
            for (var i = 0; i < cracks.Length; i++) if (cracks[i] != null) cracks[i].SetActive(i < stage);
            block.SetColor("_BaseColor", Color.Lerp(baseColor, new Color(0.92f, 0.86f, 0.7f), stage * 0.1f)); body.SetPropertyBlock(block);
        }
    }
}

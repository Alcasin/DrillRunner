using System;
using UnityEngine;

namespace DrillRunner
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(MeshRenderer))]
    public sealed class DrillObstacle : MonoBehaviour
    {
        [SerializeField] private MaterialDefinition definition;
        private BoxCollider volume;
        private MeshRenderer intact;
        public MaterialDefinition Definition => definition;
        public ObstacleDurability Durability { get; private set; }
        public bool IsSolid => gameObject.activeInHierarchy && enabled && Volume.enabled && (Durability == null || !Durability.IsDestroyed);
        private BoxCollider Volume => volume != null ? volume : volume = GetComponent<BoxCollider>();
        public Bounds WorldBounds => DrillContacts.WorldBounds(Volume);
        public event Action<DrillObstacle> Broken;

        private void Awake() => Restore();

        public void Restore()
        {
            if (intact == null)
                intact = GetComponent<MeshRenderer>();
            if (Durability != null)
                Durability.Destroyed -= OnBroken;
            try
            {
                Durability = new ObstacleDurability(definition);
                Durability.Destroyed += OnBroken;
            }
            catch (ArgumentException exception)
            {
                Durability = null; // Invalid material stays physically solid and cannot be damaged.
                Debug.LogWarning("Drill obstacle cannot be drilled: " + name + ". " + exception.Message, this);
            }
            Volume.enabled = true;
            intact.enabled = true;
        }

        private void OnBroken()
        {
            Volume.enabled = false;
            intact.enabled = false;
            Broken?.Invoke(this);
        }
    }
}

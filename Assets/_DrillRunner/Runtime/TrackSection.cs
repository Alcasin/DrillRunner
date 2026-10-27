using System;
using UnityEngine;

namespace DrillRunner
{
    [DisallowMultipleComponent]
    public sealed class TrackSection : MonoBehaviour
    {
        [SerializeField] private string sectionId;
        [SerializeField] private float width = 6;
        [SerializeField] private float length = 40;
        [SerializeField] private Transform entrance;
        [SerializeField] private Transform exit;
        [SerializeField] private BoxCollider surface;
        [SerializeField] private DrillObstacle[] obstacles = Array.Empty<DrillObstacle>();
        public string SectionId => sectionId;
        public float Width => width;
        public float Length => length;
        public float StartZ => transform.position.z;
        public float EndZ => StartZ + length;
        public DrillObstacle[] Obstacles => obstacles;

        public TrackPlacement[] ReadPlacements()
        {
            if (string.IsNullOrWhiteSpace(sectionId) || entrance == null || exit == null || surface == null ||
                !gameObject.activeSelf || !surface.gameObject.activeSelf || !surface.enabled || surface.isTrigger ||
                surface.GetComponent<MeshRenderer>() == null || !surface.GetComponent<MeshRenderer>().enabled ||
                surface.GetComponent<MeshRenderer>().sharedMaterial == null ||
                entrance.localPosition != Vector3.zero || exit.localPosition != Vector3.forward * length ||
                transform.localScale != Vector3.one || Quaternion.Angle(transform.rotation, Quaternion.identity) > 0.01f)
                throw new ArgumentException("Section needs an ID, aligned entrance/exit, unscaled root, and surface.");
            var floor = DrillContacts.WorldBounds(surface);
            if (Mathf.Abs(floor.size.x - width) > 0.001f || Mathf.Abs(floor.size.z - length) > 0.001f ||
                Mathf.Abs(floor.max.y) > 0.001f || Mathf.Abs(floor.min.z - StartZ) > 0.001f || Mathf.Abs(floor.center.x) > 0.001f)
                throw new ArgumentException("Section floor does not match its entrance, exit, width or track height.");
            var result = new TrackPlacement[obstacles.Length];
            for (var i = 0; i < obstacles.Length; i++)
            {
                if (obstacles[i] == null || obstacles[i].Definition == null || !obstacles[i].gameObject.activeSelf ||
                    !obstacles[i].enabled || !obstacles[i].GetComponent<BoxCollider>().enabled || obstacles[i].GetComponent<BoxCollider>().isTrigger)
                    throw new ArgumentException("Missing or disabled section material/obstacle.");
                obstacles[i].Definition.Validate();
                var bounds = obstacles[i].WorldBounds;
                bounds.center -= Vector3.forward * StartZ;
                result[i] = new TrackPlacement(bounds, obstacles[i].Definition.Hardness, obstacles[i].Definition.RewardKind);
            }
            return result;
        }

        public void RestoreContent()
        {
            foreach (var obstacle in obstacles) if (obstacle != null) obstacle.Restore();
        }

        public bool Contains(DrillObstacle target)
        {
            foreach (var obstacle in obstacles) if (obstacle == target) return true;
            return false;
        }
    }
}

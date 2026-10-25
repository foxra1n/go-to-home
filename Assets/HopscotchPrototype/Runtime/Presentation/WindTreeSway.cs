using System.Collections.Generic;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class WindTreeSway : MonoBehaviour
    {
        private static readonly HashSet<WindTreeSway> active = new HashSet<WindTreeSway>();
        private Vector3 restPosition;
        private Quaternion restRotation;
        private void Awake() { restPosition = transform.localPosition; restRotation = transform.localRotation; }
        private void OnEnable() => active.Add(this);
        private void OnDisable() { active.Remove(this); Restore(); }
        private void Restore() { transform.localPosition = restPosition; transform.localRotation = restRotation; }

        public static void Apply(Vector3 origin, float amount, float elapsed)
        {
            foreach (var tree in active)
            {
                float distance = Mathf.Abs(tree.transform.position.z - origin.z);
                float localAmount = amount * (1f - Mathf.InverseLerp(22f, 38f, distance));
                if (localAmount <= 0f) { tree.Restore(); continue; }
                float sway = localAmount * (.72f + .28f * Mathf.Sin(elapsed * 9f + tree.restPosition.z * .3f));
                tree.transform.localRotation = tree.restRotation * Quaternion.Euler(0, 0, -12f * sway);
                tree.transform.localPosition = tree.restPosition + Vector3.right * (.38f * sway);
            }
        }
    }
}

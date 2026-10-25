using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class EndlessGroundFollower : MonoBehaviour
    {
        private Transform target;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target != null)
            {
                Vector3 position = transform.position;
                position.z = target.position.z + 25f;
                transform.position = position;
            }
        }
    }
}

using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class QuarterViewCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 cameraOffset = new Vector3(6.8f, 9.2f, -10.8f);
        [SerializeField] private Vector3 lookAhead = new Vector3(0f, 0.8f, 5.2f);
        [SerializeField, Min(0f)] private float followSharpness = 10f;

        private Transform target;
        private Camera viewCamera;

        private void UpdatePortraitViewport()
        {
            float screenAspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            float width = screenAspect > 1f ? (9f / 16f) / screenAspect : 1f;
            viewCamera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            Camera cameraComponent = GetComponent<Camera>();
            viewCamera = cameraComponent;
            UpdatePortraitViewport();
            cameraComponent.fieldOfView = 46f;
            cameraComponent.nearClipPlane = 0.1f;
            cameraComponent.farClipPlane = 250f;
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;
            cameraComponent.backgroundColor = new Color(0.72f, 0.88f, 0.91f);
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (viewCamera != null) UpdatePortraitViewport();
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + cameraOffset;
            float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
            transform.rotation = Quaternion.LookRotation(target.position + lookAhead - transform.position, Vector3.up);
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.position + cameraOffset;
            transform.rotation = Quaternion.LookRotation(target.position + lookAhead - transform.position, Vector3.up);
        }
    }
}

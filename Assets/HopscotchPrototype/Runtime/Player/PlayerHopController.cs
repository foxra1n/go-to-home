using System;
using System.Collections;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class PlayerHopController : MonoBehaviour
    {
        private Transform body;
        private Transform leftFoot;
        private Transform rightFoot;
        private Vector3 startingPosition;
        private bool landOnLeftFoot = true;
        private Coroutine activeHop;
        private PlayerMotionFeedback feedback;

        public void Initialize()
        {
            startingPosition = Vector3.up * 0.22f;
            transform.position = startingPosition;

            var visual = gameObject.AddComponent<VoxelStudentVisual>();
            visual.Build(out body, out leftFoot, out rightFoot);
            feedback = gameObject.AddComponent<PlayerMotionFeedback>();
            feedback.Configure(body, visual);
            SetDoubleLandingPose();
        }

        public void ResetPlayer()
        {
            feedback.ResetFeedback();
            SetReadyPulse(0f);
            if (activeHop != null)
            {
                StopCoroutine(activeHop);
                activeHop = null;
            }

            transform.position = startingPosition;
            transform.rotation = Quaternion.identity;
            body.localRotation = Quaternion.identity;
            landOnLeftFoot = true;
            SetDoubleLandingPose();
        }

        public void HopTo(Vector3 target, LandingType landingType, float duration, Action onComplete)
        {
            if (activeHop != null)
            {
                StopCoroutine(activeHop);
            }

            activeHop = StartCoroutine(HopRoutine(target, landingType, duration, onComplete));
        }

        public void ShowFailurePose()
        {
            feedback.Fall();
            if (activeHop != null)
            {
                StopCoroutine(activeHop);
            }

            activeHop = StartCoroutine(FallForwardRoutine());
        }

        public void JumpToSurface(Vector3 target, float duration, Action onComplete)
        {
            if (activeHop != null)
            {
                StopCoroutine(activeHop);
            }

            activeHop = StartCoroutine(SurfaceJumpRoutine(target, duration, onComplete));
        }

        public void BeginBalanceWalk()
        {
            SetDoubleLandingPose();
            body.localRotation = Quaternion.identity;
        }

        public void BeginRoadWalk()
        {
            transform.rotation = Quaternion.identity;
            SetDoubleLandingPose();
            body.localRotation = Quaternion.identity;
        }

        public void SetReadyPulse(float amount)
        {
            float bend = Mathf.Clamp01(amount);
            body.localPosition = new Vector3(0f, 1.05f - bend * 0.18f, 0f);
            body.localRotation = Quaternion.Euler(bend * 8f, 0f, 0f);
        }

        public void MoveForward(float distance)
        {
            transform.position += Vector3.forward * distance;
        }

        public void SetBalanceWalkPose(float leanNormalized, float gaitPhase, float failureAngle)
        {
            feedback.Walk(gaitPhase, leanNormalized);
            transform.rotation = Quaternion.Euler(0f, 0f, -leanNormalized * failureAngle);
            float stride = Mathf.Sin(gaitPhase);
            float leftLift = Mathf.Max(0f, stride) * 0.16f;
            float rightLift = Mathf.Max(0f, -stride) * 0.16f;
            leftFoot.localPosition = new Vector3(-0.28f, 0.16f + leftLift, stride * 0.16f);
            rightFoot.localPosition = new Vector3(0.28f, 0.16f + rightLift, -stride * 0.16f);
        }

        public void SetRoadWalkPose(float gaitPhase)
        {
            feedback.Walk(gaitPhase);
            transform.rotation = Quaternion.identity;
            float stride = Mathf.Sin(gaitPhase);
            float leftLift = Mathf.Max(0f, stride) * 0.13f;
            float rightLift = Mathf.Max(0f, -stride) * 0.13f;
            leftFoot.localPosition = new Vector3(-0.28f, 0.16f + leftLift, stride * 0.14f);
            rightFoot.localPosition = new Vector3(0.28f, 0.16f + rightLift, -stride * 0.14f);
        }

        public void ShowSideFailure(float leanDirection)
        {
            feedback.Fall();
            if (activeHop != null)
            {
                StopCoroutine(activeHop);
            }

            activeHop = StartCoroutine(SideFallRoutine(Mathf.Sign(leanDirection)));
        }

        private IEnumerator FallForwardRoutine()
        {
            const float duration = 0.42f;
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + Vector3.forward * 0.42f;
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.Euler(80f, 0f, 0f);
            float elapsed = 0f;

            body.localRotation = Quaternion.identity;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float accelerated = t * t;
                transform.position = Vector3.Lerp(startPosition, endPosition, accelerated);
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, accelerated);
                yield return null;
            }

            transform.position = endPosition;
            transform.rotation = endRotation;
            activeHop = null;
        }

        private IEnumerator SideFallRoutine(float direction)
        {
            const float duration = 0.38f;
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + Vector3.right * direction * 0.58f;
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.Euler(0f, 0f, -direction * 92f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float accelerated = t * t;
                transform.position = Vector3.Lerp(startPosition, endPosition, accelerated);
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, accelerated);
                yield return null;
            }

            transform.position = endPosition;
            transform.rotation = endRotation;
            activeHop = null;
        }

        private IEnumerator SurfaceJumpRoutine(Vector3 target, float duration, Action onComplete)
        {
            feedback.Jump();
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;

            SetDoubleLandingPose();
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                Vector3 position = Vector3.Lerp(startPosition, target, eased);
                position.y += Mathf.Sin(t * Mathf.PI) * 0.82f;
                transform.position = position;
                transform.rotation = Quaternion.Slerp(startRotation, Quaternion.identity, eased);
                yield return null;
            }

            transform.position = target;
            transform.rotation = Quaternion.identity;
            SetDoubleLandingPose();
            activeHop = null;
            onComplete?.Invoke();
            if (activeHop == null) feedback.Land(false);
        }

        private IEnumerator HopRoutine(Vector3 target, LandingType landingType, float duration, Action onComplete)
        {
            feedback.Jump();
            Vector3 start = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                Vector3 position = Vector3.Lerp(start, target, eased);
                position.y += Mathf.Sin(t * Mathf.PI) * 1.15f;
                transform.position = position;
                transform.rotation = Quaternion.Slerp(startRotation, Quaternion.identity, eased);
                yield return null;
            }

            transform.position = target;
            transform.rotation = Quaternion.identity;
            ApplyLandingPose(landingType);
            activeHop = null;
            onComplete?.Invoke();
            if (activeHop == null) feedback.Land(landingType == LandingType.Single);
        }

        private void ApplyLandingPose(LandingType landingType)
        {
            if (landingType == LandingType.Double)
            {
                SetDoubleLandingPose();
                return;
            }

            float balanceDirection = landOnLeftFoot ? -1f : 1f;
            Transform plantedFoot = landOnLeftFoot ? leftFoot : rightFoot;
            Transform raisedFoot = landOnLeftFoot ? rightFoot : leftFoot;
            plantedFoot.localPosition = new Vector3(0f, 0.16f, 0.05f);
            raisedFoot.localPosition = new Vector3(-balanceDirection * 0.3f, 0.58f, -0.04f);
            body.localRotation = Quaternion.Euler(0f, 0f, balanceDirection * 6f);
            landOnLeftFoot = !landOnLeftFoot;
        }

        private void SetDoubleLandingPose()
        {
            leftFoot.localPosition = new Vector3(-0.34f, 0.16f, 0.05f);
            rightFoot.localPosition = new Vector3(0.34f, 0.16f, 0.05f);
            body.localRotation = Quaternion.identity;
        }

    }
}

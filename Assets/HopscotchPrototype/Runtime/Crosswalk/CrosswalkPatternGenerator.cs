using System.Collections.Generic;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class CrosswalkPatternGenerator : MonoBehaviour
    {
        [SerializeField, Min(6)] private int visibleStripeCount = 14;
        [SerializeField, Min(0.5f)] private float stripeLength = 2.4f;
        [SerializeField, Min(0.2f)] private float gapLength = 1.6f;
        [SerializeField, Min(2f)] private float stripeWidth = 5.8f;
        [SerializeField, Min(0)] private int retainedBehindPlayer = 2;
        [SerializeField, Range(0f, 1f)] private float landingProgressFromBackEdge = 0.4f;
        [SerializeField, Range(0f, 0.2f)] private float entryProgressFromBackEdge = 0.1f;
        [SerializeField, Range(0f, 0.2f)] private float landingEdgeTolerance = 0.12f;

        private readonly List<Transform> activeStripes = new List<Transform>();
        private readonly Stack<Transform> stripePool = new Stack<Transform>();
        private Material stripeMaterial;
        private int cursor;
        private float nextCenterZ;
        private bool finite;
        public bool IsLastStripe => finite && cursor == activeStripes.Count - 1;
        public float EndZ { get; private set; }

        public void BeginSection(float playerZ, int count)
        {
            RecycleAll();
            finite = true;
            cursor = 0;
            nextCenterZ = playerZ + stripeLength * (0.5f - entryProgressFromBackEdge);
            for (int i = 0; i < Mathf.Max(2, count); i++) AppendStripe();
            EndZ = nextCenterZ - stripeLength - gapLength + stripeLength * 0.5f;
        }

        public int ActiveStripeCount => activeStripes.Count;
        public float StripeLength => stripeLength;
        public float GapLength => gapLength;
        public float JumpDistance => gapLength + stripeLength * landingProgressFromBackEdge;
        public float CurrentCenterZ => activeStripes[cursor].position.z;
        public float CurrentStartZ => CurrentCenterZ - stripeLength * 0.5f;
        public float CurrentEndZ => CurrentCenterZ + stripeLength * 0.5f;

        public Vector3 JumpTargetFrom(Vector3 currentPosition)
        {
            return new Vector3(0f, 0.22f, currentPosition.z + JumpDistance);
        }

        public bool IsOnNextStripe(float positionZ)
        {
            return TryResolveNextLanding(positionZ, out _);
        }

        public bool TryResolveNextLanding(float positionZ, out float resolvedZ)
        {
            resolvedZ = positionZ;
            if (cursor + 1 >= activeStripes.Count) return false;
            // Never let landing assistance cover a significant part of the dark gap.
            float tolerance = Mathf.Min(landingEdgeTolerance, gapLength * 0.1f);
            return CrosswalkTimingState.TryResolveLanding(positionZ,
                activeStripes[cursor + 1].position.z, stripeLength, tolerance, out resolvedZ);
        }

        public bool TryResolveCurrentLanding(float positionZ, out float resolvedZ)
        {
            float tolerance = Mathf.Min(landingEdgeTolerance, gapLength * .1f);
            return CrosswalkTimingState.TryResolveLanding(positionZ,
                CurrentCenterZ, stripeLength, tolerance, out resolvedZ);
        }

        public void Initialize()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            stripeMaterial = new Material(shader)
            {
                name = "CrosswalkWhite",
                color = new Color(0.96f, 0.97f, 0.94f)
            };
        }

        public void BeginRun(float playerZ)
        {
            finite = false;
            RecycleAll();
            cursor = 0;
            nextCenterZ = playerZ + stripeLength * (0.5f - entryProgressFromBackEdge);
            for (int i = 0; i < visibleStripeCount; i++)
            {
                AppendStripe();
            }
        }

        public void Advance()
        {
            cursor++;
            if (finite) return;
            AppendStripe();
            if (cursor <= retainedBehindPlayer)
            {
                return;
            }

            Transform oldest = activeStripes[0];
            activeStripes.RemoveAt(0);
            cursor--;
            oldest.gameObject.SetActive(false);
            stripePool.Push(oldest);
        }

        private void AppendStripe()
        {
            Transform stripe = AcquireStripe();
            stripe.position = new Vector3(0f, 0f, nextCenterZ);
            stripe.gameObject.SetActive(true);
            activeStripes.Add(stripe);
            nextCenterZ += stripeLength + gapLength;
        }

        private Transform AcquireStripe()
        {
            if (stripePool.Count > 0)
            {
                return stripePool.Pop();
            }

            GameObject stripeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripeObject.name = "CrosswalkWhiteStripe";
            stripeObject.transform.SetParent(transform, false);
            stripeObject.transform.localScale = new Vector3(stripeWidth, 0.12f, stripeLength);
            stripeObject.GetComponent<Renderer>().sharedMaterial = stripeMaterial;
            return stripeObject.transform;
        }

        private void RecycleAll()
        {
            foreach (Transform stripe in activeStripes)
            {
                stripe.gameObject.SetActive(false);
                stripePool.Push(stripe);
            }

            activeStripes.Clear();
        }
    }
}

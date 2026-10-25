namespace Homeward.Hopscotch
{
    public sealed class CrosswalkTimingState
    {
        public static bool TryResolveLanding(float playerZ, float stripeCenterZ,
            float stripeLength, float edgeTolerance, out float resolvedZ)
        {
            float start = stripeCenterZ - stripeLength * 0.5f;
            float end = stripeCenterZ + stripeLength * 0.5f;
            float margin = System.Math.Max(0f, System.Math.Min(edgeTolerance, stripeLength * 0.1f));
            resolvedZ = playerZ;
            if (playerZ < start - margin || playerZ > end + margin) return false;

            // A shoe overlapping an edge is supported; put its center safely on the stripe.
            if (playerZ < start) resolvedZ = start + margin;
            else if (playerZ > end) resolvedZ = end - margin;
            return true;
        }

        public float StripeEndZ { get; private set; }

        public void Begin(float stripeCenterZ, float stripeLength)
        {
            StripeEndZ = stripeCenterZ + stripeLength * 0.5f;
        }

        public bool CanLaunch(float playerZ, float launchWindowDistance)
        {
            float distanceToEdge = StripeEndZ - playerZ;
            return distanceToEdge >= 0f && distanceToEdge <= launchWindowDistance;
        }

        public bool HasSteppedIntoGap(float playerZ)
        {
            return playerZ > StripeEndZ;
        }
    }
}

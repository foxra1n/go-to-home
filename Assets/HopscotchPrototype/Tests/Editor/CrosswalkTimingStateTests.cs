using NUnit.Framework;

namespace Homeward.Hopscotch.Tests
{
    public sealed class CrosswalkTimingStateTests
    {
        [TestCase(2.70f, true, 2.92f)]
        [TestCase(2.67f, false, 2.67f)]
        [TestCase(3.76f, true, 3.76f)]
        [TestCase(5.30f, true, 5.08f)]
        [TestCase(5.33f, false, 5.33f)]
        [TestCase(2.00f, false, 2.00f)]
        public void LandingGraceOnlyAssistsStripeEdges(float landingZ, bool accepted, float expectedZ)
        {
            Assert.That(CrosswalkTimingState.TryResolveLanding(landingZ, 4f, 2.4f, 0.12f,
                out float resolvedZ), Is.EqualTo(accepted));
            Assert.That(resolvedZ, Is.EqualTo(expectedZ).Within(0.001f));
        }

        [Test]
        public void RepeatedImmediateJumpStillFallsShortOfNextStripe()
        {
            const float landingZ = 3.76f;
            const float jumpDistance = 2.56f;
            Assert.That(CrosswalkTimingState.TryResolveLanding(landingZ + jumpDistance,
                8f, 2.4f, 0.12f, out _), Is.False);
        }

        [Test]
        public void LaunchIsRejectedBeforeTheEdgeWindow()
        {
            CrosswalkTimingState state = new CrosswalkTimingState();
            state.Begin(0f, 1.4f);

            Assert.That(state.CanLaunch(0.1f, 0.5f), Is.False);
        }

        [Test]
        public void LaunchIsAcceptedNearTheWhiteStripeEdge()
        {
            CrosswalkTimingState state = new CrosswalkTimingState();
            state.Begin(0f, 1.4f);

            Assert.That(state.CanLaunch(0.3f, 0.5f), Is.True);
        }

        [Test]
        public void PassingTheStripeEdgeIsAMiss()
        {
            CrosswalkTimingState state = new CrosswalkTimingState();
            state.Begin(0f, 1.4f);

            Assert.That(state.HasSteppedIntoGap(0.71f), Is.True);
        }

        [Test]
        public void EdgeLaunchKeepsTheLandingDistanceNearTwoMeters()
        {
            CrosswalkTimingState state = new CrosswalkTimingState();
            const float stripeLength = 2.4f;
            const float gapLength = 1.6f;
            state.Begin(0f, stripeLength);

            float launchZ = state.StripeEndZ;
            float nextStripeCenterZ = stripeLength + gapLength;
            float nextStripeBackEdgeZ = nextStripeCenterZ - stripeLength * 0.5f;
            float landingZ = nextStripeBackEdgeZ + stripeLength * 0.4f;

            Assert.That(state.CanLaunch(launchZ, 0.12f), Is.True);
            Assert.That(landingZ - launchZ, Is.EqualTo(2.56f).Within(0.001f));
        }
    }
}

using NUnit.Framework;

namespace Homeward.Hopscotch.Tests
{
    public sealed class CurbBalanceStateTests
    {
        [Test]
        public void BeginsMovingToTheRight()
        {
            CurbBalanceState state = new CurbBalanceState();
            state.Begin();
            state.Advance(0.25f, 1f);

            Assert.That(state.LeanDirection, Is.EqualTo(1));
            Assert.That(state.LeanNormalized, Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void TapReversesDirectionWithoutResettingLean()
        {
            CurbBalanceState state = new CurbBalanceState();
            state.Begin();
            state.Advance(0.6f, 1f);
            float leanAtTap = state.LeanNormalized;

            bool accepted = state.TryReverseDirection(1f, 0.13f);

            Assert.That(accepted, Is.True);
            Assert.That(state.LeanNormalized, Is.EqualTo(leanAtTap));
            Assert.That(state.LeanDirection, Is.EqualTo(-1));
            state.Advance(0.2f, 1f);
            Assert.That(state.LeanNormalized, Is.LessThan(leanAtTap));
        }

        [Test]
        public void ReachingEitherLimitFails()
        {
            CurbBalanceState state = new CurbBalanceState();
            state.Begin();

            Assert.That(state.Advance(1.01f, 1f), Is.True);
            Assert.That(state.LeanNormalized, Is.EqualTo(1f));
        }

        [Test]
        public void RapidTapsAreIgnoredAndKeepTheLockoutActive()
        {
            CurbBalanceState state = new CurbBalanceState();
            state.Begin();

            Assert.That(state.TryReverseDirection(0f, 0.13f), Is.True);
            Assert.That(state.LeanDirection, Is.EqualTo(-1));
            Assert.That(state.TryReverseDirection(0.05f, 0.13f), Is.False);
            Assert.That(state.TryReverseDirection(0.14f, 0.13f), Is.False);
            Assert.That(state.LeanDirection, Is.EqualTo(-1));
            Assert.That(state.TryReverseDirection(0.28f, 0.13f), Is.True);
            Assert.That(state.LeanDirection, Is.EqualTo(1));
        }
    }
}

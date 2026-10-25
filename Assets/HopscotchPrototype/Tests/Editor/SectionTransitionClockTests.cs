using NUnit.Framework;

namespace Homeward.Hopscotch.Tests
{
    public sealed class SectionTransitionClockTests
    {
        [Test]
        public void StartsPreparationAfterSectionDuration()
        {
            SectionTransitionClock clock = new SectionTransitionClock(10f, 1.25f);

            Assert.That(clock.Advance(9.99f), Is.EqualTo(SectionTransitionSignal.None));
            Assert.That(clock.Advance(0.01f), Is.EqualTo(SectionTransitionSignal.PreparationStarted));
            Assert.That(clock.IsPreparing, Is.True);
        }

        [Test]
        public void HoldsNextSectionForFullPreparationDuration()
        {
            SectionTransitionClock clock = new SectionTransitionClock(10f, 1.25f);
            clock.Advance(10f);

            Assert.That(clock.Advance(1.24f), Is.EqualTo(SectionTransitionSignal.None));
            Assert.That(clock.IsPreparing, Is.True);
            Assert.That(clock.Advance(0.01f), Is.EqualTo(SectionTransitionSignal.NextSectionReady));
            Assert.That(clock.IsPreparing, Is.False);
        }

        [Test]
        public void ResetReturnsToSectionTiming()
        {
            SectionTransitionClock clock = new SectionTransitionClock(10f, 1.25f);
            clock.Advance(10f);
            clock.Reset();

            Assert.That(clock.IsPreparing, Is.False);
            Assert.That(clock.Advance(1.25f), Is.EqualTo(SectionTransitionSignal.None));
        }
    }
}

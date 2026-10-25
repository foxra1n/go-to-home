using NUnit.Framework;

namespace Homeward.Hopscotch.Tests
{
    public sealed class HopscotchPatternRandomizerTests
    {
        [TestCase(true, LandingType.Single, LandingType.Double)]
        [TestCase(false, LandingType.Double, LandingType.Single)]
        public void SameRandomChoiceNeverAppearsThreeTimes(
            bool chooseSingle,
            LandingType repeatedType,
            LandingType forcedType)
        {
            HopscotchPatternRandomizer randomizer = new HopscotchPatternRandomizer();

            Assert.That(randomizer.Next(chooseSingle), Is.EqualTo(repeatedType));
            Assert.That(randomizer.Next(chooseSingle), Is.EqualTo(repeatedType));
            Assert.That(randomizer.Next(chooseSingle), Is.EqualTo(forcedType));
        }

        [Test]
        public void ResetClearsTheConsecutiveHistory()
        {
            HopscotchPatternRandomizer randomizer = new HopscotchPatternRandomizer();
            randomizer.Next(true);
            randomizer.Next(true);

            randomizer.Reset();

            Assert.That(randomizer.Next(true), Is.EqualTo(LandingType.Single));
        }

        [Test]
        public void LongSequenceNeverContainsThreeMatchingPatterns()
        {
            HopscotchPatternRandomizer randomizer = new HopscotchPatternRandomizer();
            LandingType first = randomizer.Next(true);
            LandingType second = randomizer.Next(true);

            for (int i = 0; i < 100; i++)
            {
                LandingType third = randomizer.Next(i % 5 != 0);
                Assert.That(first == second && second == third, Is.False);
                first = second;
                second = third;
            }
        }
    }
}

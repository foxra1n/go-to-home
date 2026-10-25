using System.Collections.Generic;
using NUnit.Framework;

namespace Homeward.Hopscotch.Tests
{
    public sealed class GameModeShuffleBagTests
    {
        [Test]
        public void EveryThreeModesContainEachModeExactlyOnce()
        {
            GameModeShuffleBag bag = new GameModeShuffleBag(12345);

            for (int set = 0; set < 100; set++)
            {
                HashSet<PrototypeGameMode> modes = new HashSet<PrototypeGameMode>
                {
                    bag.Next(),
                    bag.Next(),
                    bag.Next()
                };

                Assert.That(modes.Count, Is.EqualTo(3));
            }
        }

        [Test]
        public void ModesNeverRepeatAcrossSetBoundaries()
        {
            GameModeShuffleBag bag = new GameModeShuffleBag(9876);
            PrototypeGameMode previous = bag.Next();

            for (int i = 0; i < 300; i++)
            {
                PrototypeGameMode current = bag.Next();
                Assert.That(current, Is.Not.EqualTo(previous));
                previous = current;
            }
        }

        [Test]
        public void ResetStartsANewCompleteSet()
        {
            GameModeShuffleBag bag = new GameModeShuffleBag(42);
            bag.Next();
            bag.Reset();

            HashSet<PrototypeGameMode> modes = new HashSet<PrototypeGameMode>
            {
                bag.Next(),
                bag.Next(),
                bag.Next()
            };

            Assert.That(modes.Count, Is.EqualTo(3));
        }
    }
}

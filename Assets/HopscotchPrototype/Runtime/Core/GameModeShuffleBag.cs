using System;

namespace Homeward.Hopscotch
{
    public enum PrototypeGameMode
    {
        Hopscotch,
        CurbBalance,
        Crosswalk
    }

    public sealed class GameModeShuffleBag
    {
        private readonly Random random;
        private readonly PrototypeGameMode[] bag = new PrototypeGameMode[3];
        private int cursor = 3;
        private bool hasPrevious;
        private PrototypeGameMode previous;

        public GameModeShuffleBag(int? seed = null)
        {
            random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public void Reset()
        {
            cursor = bag.Length;
            hasPrevious = false;
        }

        public PrototypeGameMode Next()
        {
            if (cursor >= bag.Length)
            {
                Refill();
            }

            PrototypeGameMode result = bag[cursor++];
            previous = result;
            hasPrevious = true;
            return result;
        }

        private void Refill()
        {
            bag[0] = PrototypeGameMode.Hopscotch;
            bag[1] = PrototypeGameMode.CurbBalance;
            bag[2] = PrototypeGameMode.Crosswalk;

            for (int i = bag.Length - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (bag[i], bag[swapIndex]) = (bag[swapIndex], bag[i]);
            }

            if (hasPrevious && bag[0] == previous)
            {
                int swapIndex = bag[1] != previous ? 1 : 2;
                (bag[0], bag[swapIndex]) = (bag[swapIndex], bag[0]);
            }

            cursor = 0;
        }
    }
}

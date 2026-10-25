namespace Homeward.Hopscotch
{
    public sealed class HopscotchPatternRandomizer
    {
        private bool hasPrevious;
        private LandingType previousType;
        private int consecutiveCount;

        public void Reset()
        {
            hasPrevious = false;
            consecutiveCount = 0;
        }

        public LandingType Next(bool randomChoiceIsSingle)
        {
            LandingType candidate = randomChoiceIsSingle
                ? LandingType.Single
                : LandingType.Double;

            if (hasPrevious && candidate == previousType && consecutiveCount >= 2)
            {
                candidate = candidate == LandingType.Single
                    ? LandingType.Double
                    : LandingType.Single;
            }

            if (hasPrevious && candidate == previousType)
            {
                consecutiveCount++;
            }
            else
            {
                previousType = candidate;
                consecutiveCount = 1;
                hasPrevious = true;
            }

            return candidate;
        }
    }
}

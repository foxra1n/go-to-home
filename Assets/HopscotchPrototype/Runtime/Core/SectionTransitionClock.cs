using System;

namespace Homeward.Hopscotch
{
    public enum SectionTransitionSignal
    {
        None,
        PreparationStarted,
        NextSectionReady
    }

    public sealed class SectionTransitionClock
    {
        private readonly float sectionDuration;
        private readonly float preparationDuration;
        private float elapsed;

        public SectionTransitionClock(float sectionDuration, float preparationDuration)
        {
            if (sectionDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sectionDuration));
            }

            if (preparationDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(preparationDuration));
            }

            this.sectionDuration = sectionDuration;
            this.preparationDuration = preparationDuration;
        }

        public bool IsPreparing { get; private set; }

        public void Reset()
        {
            elapsed = 0f;
            IsPreparing = false;
        }

        public SectionTransitionSignal Advance(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            elapsed += deltaTime;
            if (!IsPreparing && elapsed >= sectionDuration)
            {
                elapsed = 0f;
                IsPreparing = true;
                return SectionTransitionSignal.PreparationStarted;
            }

            if (IsPreparing && elapsed >= preparationDuration)
            {
                elapsed = 0f;
                IsPreparing = false;
                return SectionTransitionSignal.NextSectionReady;
            }

            return SectionTransitionSignal.None;
        }
    }
}

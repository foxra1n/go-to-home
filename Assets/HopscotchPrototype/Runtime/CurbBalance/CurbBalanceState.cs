using System;

namespace Homeward.Hopscotch
{
    public sealed class CurbBalanceState
    {
        private float nextAllowedTapTime;
        private float leanVelocity;
        private const float WindAcceleration = 4f;
        private const float MaximumLeanVelocity = 1.75f;

        public float LeanNormalized { get; private set; }
        public int LeanDirection { get; private set; }

        public void Begin(float currentTime = 0f)
        {
            LeanNormalized = 0f;
            LeanDirection = 1;
            leanVelocity = 1f;
            nextAllowedTapTime = currentTime;
        }

        public bool TryReverseDirection(float tapTime, float lockoutSeconds)
        {
            if (tapTime < nextAllowedTapTime)
            {
                nextAllowedTapTime = tapTime + lockoutSeconds;
                return false;
            }

            LeanDirection *= -1;
            // A tap always gives an immediate, full-speed counter movement.
            leanVelocity = LeanDirection;
            nextAllowedTapTime = tapTime + lockoutSeconds;
            return true;
        }

        public bool Advance(float deltaTime, float normalizedLeanSpeed, float windFraction = 0f)
        {
            if (deltaTime <= 0f) return Math.Abs(LeanNormalized) >= 1f;
            float boundedWind = Math.Max(-.75f, Math.Min(.75f, windFraction));
            float displacement;
            if (Math.Abs(boundedWind) < .0001f)
            {
                // Without wind, preserve the original constant-speed tap toggle.
                leanVelocity = LeanDirection;
                displacement = leanVelocity * deltaTime;
            }
            else
            {
                // Wind changes velocity over time, rather than granting a permanent
                // slow, safe return after one tap. It can turn the character back.
                float acceleration = boundedWind * WindAcceleration;
                float limit = Math.Sign(acceleration) * MaximumLeanVelocity;
                float acceleratingTime = Math.Max(0f, Math.Min(deltaTime,
                    (limit - leanVelocity) / acceleration));
                displacement = leanVelocity * acceleratingTime
                    + .5f * acceleration * acceleratingTime * acceleratingTime
                    + limit * (deltaTime - acceleratingTime);
                leanVelocity += acceleration * acceleratingTime;
                if (Math.Abs(leanVelocity) > .00001f)
                    LeanDirection = Math.Sign(leanVelocity);
            }
            LeanNormalized += displacement * normalizedLeanSpeed;
            LeanNormalized = Math.Max(-1f, Math.Min(1f, LeanNormalized));
            return Math.Abs(LeanNormalized) >= 1f;
        }
    }
}

using UnityEngine;

namespace Homeward.Hopscotch
{
    public readonly struct DifficultySnapshot
    {
        public DifficultySnapshot(
            float beatsPerMinute,
            float timingTolerance,
            float jumpDuration,
            float curbForwardSpeed,
            float curbLeanDegreesPerSecond,
            float curbTapLockoutSeconds,
            float crosswalkForwardSpeed,
            float crosswalkLaunchWindowDistance,
            float normalized,
            float speedMultiplier = 1f)
        {
            BeatsPerMinute = beatsPerMinute;
            TimingTolerance = timingTolerance;
            JumpDuration = jumpDuration;
            CurbForwardSpeed = curbForwardSpeed;
            CurbLeanDegreesPerSecond = curbLeanDegreesPerSecond;
            CurbTapLockoutSeconds = curbTapLockoutSeconds;
            CrosswalkForwardSpeed = crosswalkForwardSpeed;
            CrosswalkLaunchWindowDistance = crosswalkLaunchWindowDistance;
            Normalized = normalized;
            ObstacleIntensity = normalized;
            SpeedMultiplier = speedMultiplier;
        }

        public float BeatsPerMinute { get; }
        public float TimingTolerance { get; }
        public float JumpDuration { get; }
        public float CurbForwardSpeed { get; }
        public float CurbLeanDegreesPerSecond { get; }
        public float CurbTapLockoutSeconds { get; }
        public float CrosswalkForwardSpeed { get; }
        public float CrosswalkLaunchWindowDistance { get; }
        public float Normalized { get; }
        public float ObstacleIntensity { get; }
        public float SpeedMultiplier { get; }
    }

    public sealed class DifficultyController : MonoBehaviour
    {
        [Header("Progression")]
        [SerializeField, Min(1)] private int scoreAtMaximumDifficulty = 40;
        [SerializeField] private AnimationCurve progressionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Rhythm")]
        [SerializeField, Min(40f)] private float startingBeatsPerMinute = 88f;
        [SerializeField, Min(0.03f)] private float startingTimingTolerance = 0.30f;

        [Header("Motion")]
        [SerializeField, Min(0.05f)] private float startingJumpDuration = 0.38f;

        [Header("Curb Balance")]
        [SerializeField, Min(0.5f)] private float startingCurbForwardSpeed = 2.15f;
        [SerializeField, Min(5f)] private float startingCurbLeanDegreesPerSecond = 31f;
        [SerializeField, Min(0.02f)] private float startingCurbTapLockoutSeconds = 0.13f;

        [Header("Crosswalk")]
        [SerializeField, Min(0.5f)] private float startingCrosswalkForwardSpeed = 2.15f;
        [SerializeField, Min(0.02f)] private float crosswalkLaunchDistanceFromEdge = 0.12f;

        public DifficultySnapshot Current { get; private set; }
        [Header("Temporary Playtest Override")]
        [SerializeField] private bool testMaximumDifficulty;
        public bool TestMaximumDifficulty
        {
            get => testMaximumDifficulty;
            set { testMaximumDifficulty = value; ApplyScore(currentScore); }
        }
        public int SetIndex { get; private set; }
        private int currentScore;
        private int runSeed;

        public const float MaximumSpeed = 1.4f;
        public static float SpeedForSet(int setIndex) => 1f + .1f * Mathf.Clamp(setIndex - 3, 0, 4);
        public static int ObstacleCountForSet(int setIndex) => Mathf.Clamp(setIndex, 0, 3);

        public void ResetProgression()
        {
            runSeed = Random.Range(0, int.MaxValue);
            currentScore = 0;
            ApplySet(0);
        }

        public bool HasObstacle(int setIndex, PrototypeGameMode mode)
        {
            if (testMaximumDifficulty) return true;
            // Reproducible within a run, including the section generated ahead of time.
            var order = new[] { PrototypeGameMode.Hopscotch, PrototypeGameMode.CurbBalance, PrototypeGameMode.Crosswalk };
            var random = new System.Random(unchecked(runSeed * 397 ^ setIndex));
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            for (int i = 0; i < ObstacleCountForSet(setIndex); i++)
                if (order[i] == mode) return true;
            return false;
        }

        public void ApplySet(int setIndex)
        {
            SetIndex = Mathf.Max(0, setIndex);
            ApplyScore(currentScore);
        }

        public void ApplyScore(int score)
        {
            currentScore = score;
            float speed = testMaximumDifficulty ? MaximumSpeed : SpeedForSet(SetIndex);
            float linearProgress = Mathf.Clamp01(score / (float)Mathf.Max(1, scoreAtMaximumDifficulty));
            float shapedProgress = Mathf.Clamp01(progressionCurve.Evaluate(linearProgress));

            Current = new DifficultySnapshot(
                startingBeatsPerMinute * speed,
                startingTimingTolerance,
                startingJumpDuration / speed,
                startingCurbForwardSpeed * speed,
                startingCurbLeanDegreesPerSecond * speed * Mathf.Lerp(1f, 1.2f, Mathf.InverseLerp(1f, 1.3f, speed)),
                startingCurbTapLockoutSeconds / speed,
                startingCrosswalkForwardSpeed * speed,
                crosswalkLaunchDistanceFromEdge,
                shapedProgress,
                speed);
        }
    }
}

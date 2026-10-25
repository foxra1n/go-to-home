using System;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public enum CurbBalancePhase
    {
        Entering,
        Balancing,
        Exiting,
        Resting,
        Failed
    }

    public sealed class CurbBalanceModeController : MonoBehaviour, ISectionGameMode
    {
        [SerializeField, Min(15f)] private float failureAngle = 36f;
        [SerializeField, Min(0.5f)] private float scoreDistance = 2.2f;
        [Header("Section Transition")]
        [SerializeField, Min(6f)] private float sectionLength = 18f;
        [SerializeField, Min(0.1f)] private float curbElevation = 0.42f;
        [SerializeField, Min(0.2f)] private float transitionDuration = 0.52f;
        [SerializeField, Min(0.2f)] private float entryGap = 1.25f;
        [SerializeField, Min(0.2f)] private float exitGap = 1.35f;
        [SerializeField, Min(0f)] private float restBetweenPrototypeSections = 0.45f;
        [SerializeField] private bool loopPrototypeSections = true;
        [Header("PC Wind Experiment")]
        [SerializeField] private bool enableWind = true;
        public bool ObstaclesEnabled { get; set; } = true;
        private bool WindEnabled => enableWind && ObstaclesEnabled;
        [SerializeField, Range(0f, 2f)] private float windForceMultiplier = CurbWindProfile.DefaultForceMultiplier;
        private float balanceElapsed;
        private CurbWindVisual windVisual;
        public float CurrentWindFraction => WindEnabled && Phase == CurbBalancePhase.Balancing
            ? CurbWindProfile.Strength(balanceElapsed) * windForceMultiplier : 0f;

        private readonly CurbBalanceState balance = new CurbBalanceState();
        private CurbPathGenerator path;
        private PlayerHopController player;
        private DifficultyController difficulty;
        private float distanceUntilScore;
        private float gaitPhase;
        private float sectionEndZ;
        private Coroutine sectionLoop;
        private bool sectionRunning;
        private bool finite;
        private float preparedStartZ;
        public float EndZ => sectionEndZ + exitGap;

        public void PrepareSection(float startZ, float duration)
        {
            EndRun();
            finite = true;
            preparedStartZ = startZ + entryGap;
            float length = Mathf.Max(2f, difficulty.Current.CurbForwardSpeed * Mathf.Max(1f, duration - 2f * transitionDuration / difficulty.Current.SpeedMultiplier) + 1.25f);
            sectionEndZ = preparedStartZ + length;
            path.BeginSection(preparedStartZ, length, curbElevation);
        }

        public void StartSection()
        {
            sectionRunning = true;
            Phase = CurbBalancePhase.Entering;
            player.JumpToSurface(new Vector3(0f, 0.22f + curbElevation, preparedStartZ + 0.7f), transitionDuration / difficulty.Current.SpeedMultiplier, BeginBalancing);
        }

        public event Action StepCompleted;
        public event Action<string> Failed;
        public event Action SectionCompleted;

        public float LeanNormalized => balance.LeanNormalized;
        public int LeanDirection => balance.LeanDirection;
        public CurbBalancePhase Phase { get; private set; }

        public void Configure(
            CurbPathGenerator pathGenerator,
            PlayerHopController playerController,
            DifficultyController difficultyController)
        {
            path = pathGenerator;
            player = playerController;
            difficulty = difficultyController;
            if (windVisual == null)
            {
                var cue = new GameObject("CurbWindCue");
                cue.transform.SetParent(transform, false);
                windVisual = cue.AddComponent<CurbWindVisual>();
                windVisual.Initialize();
            }
        }

        public void BeginRun()
        {
            finite = false;
            sectionRunning = true;
            if (sectionLoop != null)
            {
                StopCoroutine(sectionLoop);
                sectionLoop = null;
            }

            player.ResetPlayer();
            BeginSection();
        }

        public void SubmitTap()
        {
            if (Phase == CurbBalancePhase.Balancing)
            {
                balance.TryReverseDirection(Time.unscaledTime, difficulty.Current.CurbTapLockoutSeconds);
            }
        }

        public void Tick(float deltaTime)
        {
            if (Phase != CurbBalancePhase.Balancing)
            {
                return;
            }

            DifficultySnapshot settings = difficulty.Current;
            float forwardDistance = settings.CurbForwardSpeed * deltaTime;
            float scaledDelta = deltaTime * settings.SpeedMultiplier;
            float normalizedLeanSpeed = settings.CurbLeanDegreesPerSecond / settings.SpeedMultiplier / failureAngle;
            float wind = WindEnabled ? CurbWindProfile.Strength(balanceElapsed + scaledDelta * .5f) * windForceMultiplier : 0f;
            balanceElapsed += scaledDelta;
            bool failed = balance.Advance(scaledDelta, normalizedLeanSpeed, wind);

            player.MoveForward(forwardDistance);
            if (WindEnabled) windVisual.Show(player.transform.position, balanceElapsed);
            else windVisual.gameObject.SetActive(false);
            gaitPhase += scaledDelta * (4.8f + settings.CurbForwardSpeed / settings.SpeedMultiplier * 0.6f);
            player.SetBalanceWalkPose(balance.LeanNormalized, gaitPhase, failureAngle);

            distanceUntilScore -= forwardDistance;
            while (distanceUntilScore <= 0f)
            {
                distanceUntilScore += scoreDistance;
                StepCompleted?.Invoke();
            }

            if (player.transform.position.z >= sectionEndZ - 0.55f)
            {
                BeginExitJump();
                return;
            }

            if (!failed)
            {
                return;
            }

            Phase = CurbBalancePhase.Failed;
            windVisual.gameObject.SetActive(false);
            player.ShowSideFailure(balance.LeanNormalized);
            Failed?.Invoke("Balance limit reached");
        }

        public void EndRun()
        {
            balanceElapsed = 0f;
            if (windVisual != null) windVisual.gameObject.SetActive(false);
            sectionRunning = false;
            if (sectionLoop != null)
            {
                StopCoroutine(sectionLoop);
                sectionLoop = null;
            }

            Phase = CurbBalancePhase.Resting;
        }

        private void BeginSection()
        {
            float sectionStartZ = player.transform.position.z + entryGap;
            sectionEndZ = sectionStartZ + sectionLength;
            path.BeginSection(sectionStartZ, sectionLength, curbElevation);
            Phase = CurbBalancePhase.Entering;

            Vector3 target = new Vector3(0f, 0.22f + curbElevation, sectionStartZ + 0.7f);
            player.JumpToSurface(target, transitionDuration / difficulty.Current.SpeedMultiplier, BeginBalancing);
        }

        private void BeginBalancing()
        {
            if (!sectionRunning) return;
            player.BeginBalanceWalk();
            balance.Begin(Time.unscaledTime);
            balanceElapsed = 0f;
            distanceUntilScore = scoreDistance;
            gaitPhase = 0f;
            Phase = CurbBalancePhase.Balancing;
        }

        private void BeginExitJump()
        {
            windVisual.gameObject.SetActive(false);
            Phase = CurbBalancePhase.Exiting;
            Vector3 target = new Vector3(0f, 0.22f, sectionEndZ + exitGap);
            player.JumpToSurface(target, transitionDuration / difficulty.Current.SpeedMultiplier, OnExitLanded);
        }

        private void OnExitLanded()
        {
            if (!sectionRunning) return;
            Phase = CurbBalancePhase.Resting;
            SectionCompleted?.Invoke();
            if (!finite && loopPrototypeSections)
            {
                sectionLoop = StartCoroutine(RestartSectionAfterDelay());
            }
        }

        private System.Collections.IEnumerator RestartSectionAfterDelay()
        {
            yield return new WaitForSeconds(restBetweenPrototypeSections);
            sectionLoop = null;
            BeginSection();
        }
    }
}

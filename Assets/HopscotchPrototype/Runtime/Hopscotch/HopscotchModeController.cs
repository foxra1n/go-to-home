using System;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class HopscotchModeController : MonoBehaviour, ISectionGameMode
    {
        [SerializeField, Min(0f)] private float lateInputGraceSeconds = 0.15f;
        private readonly HopscotchTapJudge judge = new HopscotchTapJudge();
        private HopscotchPatternGenerator generator;
        private PlayerHopController player;
        private DifficultyController difficulty;
        private RhythmClock rhythm;
        private RhythmWindow activeWindow;
        private bool acceptingInput;
        private bool sectionRunning;
        private bool finite;
        private bool entryCued;
        private bool firstPattern;
        private HopscotchBallObstacle ball;
        private bool ballPattern;
        private bool hopStarted;
        private bool hopLanded;
        private bool inputValidated;
        public const float BallReactionSeconds = .85f;
        private double ballTapDeadline;
        public const float BallEscapeSeconds = .65f;
        private bool ballEncounterStarted;
        private Vector3 ballTilePosition;
        public bool ObstaclesEnabled { get; set; } = true;
        public bool IsEntryPreparing => !ballPattern && entryCued && firstPattern && rhythm.DspTime < activeWindow.FirstTapDspTime - difficulty.Current.TimingTolerance;

        public void BeginEntryCue(float minimumFirstTapDelay = 0f)
        {
            if (entryCued) return;
            entryCued = true;
            firstPattern = true;
            rhythm.BeginRun(difficulty.Current.BeatsPerMinute, minimumFirstTapDelay);
            PrepareCurrentPattern();
            acceptingInput = false;
        }

        public void TickEntryCue()
        {
            if (!entryCued || !firstPattern) return;
            double now = rhythm.DspTime;
            generator.Current.ShowTiming(now, activeWindow, rhythm.BeatDuration);
            float pulse = Mathf.Max(
                Mathf.Clamp01(1f - (float)System.Math.Abs(now - rhythm.EntryFirstBeatDspTime) / 0.22f),
                Mathf.Clamp01(1f - (float)System.Math.Abs(now - rhythm.EntryFirstBeatDspTime - rhythm.BeatDuration) / 0.22f));
            player.SetReadyPulse(pulse);
        }
        public event Action SectionCompleted;
        public float EndZ => generator.EndZ;

        public void PrepareSection(float startZ, float duration)
        {
            EndRun();
            finite = true;
            float beat = 60f / difficulty.Current.BeatsPerMinute;
            int beatsPerStep = 1 + Mathf.CeilToInt((difficulty.Current.JumpDuration + 0.1f / difficulty.Current.SpeedMultiplier) / beat);
            int count = Mathf.Max(1, Mathf.RoundToInt(duration / (beat * beatsPerStep)));
            generator.BeginSection(startZ, count);
        }

        public void StartSection()
        {
            sectionRunning = true;
            BeginEntryCue();
            acceptingInput = true;
        }

        public event Action StepCompleted;
        public event Action<string> Failed;

        public void Configure(
            HopscotchPatternGenerator patternGenerator,
            PlayerHopController playerController,
            DifficultyController difficultyController,
            RhythmClock rhythmClock)
        {
            generator = patternGenerator;
            player = playerController;
            difficulty = difficultyController;
            rhythm = rhythmClock;
            var obstacle = new GameObject("HopscotchBallObstacle");
            obstacle.transform.SetParent(transform, false);
            ball = obstacle.AddComponent<HopscotchBallObstacle>();
            ball.Initialize();
        }

        public void BeginRun()
        {
            finite = false;
            sectionRunning = true;
            acceptingInput = false;
            player.ResetPlayer();
            generator.BeginRun();
            entryCued = false;
            StartSection();
        }

        public void SubmitTap()
        {
            if (!acceptingInput || IsEntryPreparing)
            {
                return;
            }

            double now = rhythm.DspTime;
            if (ballPattern)
            {
                if (now >= ballTapDeadline) { FailBallReaction(); return; }
                // A ball is a reaction jump, not another rhythm/counting challenge.
                acceptingInput = false;
                inputValidated = true;
                StartHop();
                return;
            }
            ResolveDecision(judge.RegisterTap(now));
        }

        public void Tick(float deltaTime)
        {
            if (sectionRunning && ball.gameObject.activeSelf) ball.Show(rhythm.DspTime);
            if (ballPattern)
            {
                if (sectionRunning && acceptingInput && rhythm.DspTime >= ballTapDeadline) FailBallReaction();
                return;
            }
            if (acceptingInput) TickEntryCue();
            if (!acceptingInput)
            {
                return;
            }

            HopscotchPattern pattern = generator.Current;
            pattern.ShowTiming(rhythm.DspTime, activeWindow, rhythm.BeatDuration);
            if (IsEntryPreparing) return;
            ResolveDecision(judge.Tick(rhythm.DspTime));
        }

        public void EndRun()
        {
            if (ball != null) ball.Hide();
            ballPattern = false;
            ballEncounterStarted = false;
            hopStarted = hopLanded = inputValidated = false;
            if (sectionRunning || entryCued)
            {
                player.SetReadyPulse(0f);
                if (!generator.IsComplete) generator.Current.SetCurrent(false);
            }
            entryCued = false;
            firstPattern = false;
            sectionRunning = false;
            acceptingInput = false;
            rhythm.Stop();
        }

        private void PrepareCurrentPattern()
        {
            HopscotchPattern pattern = generator.Current;
            pattern.SetCurrent(true);
            DifficultySnapshot settings = difficulty.Current;
            hopStarted = hopLanded = inputValidated = false;
            // A single predictable encounter per section; first three steps stay familiar.
            bool enteringBallTile = ObstaclesEnabled && pattern.SequenceIndex == 3;
            bool leavingBallTile = ObstaclesEnabled && ballEncounterStarted && pattern.SequenceIndex == 4;
            ballPattern = enteringBallTile || leavingBallTile;
            if (enteringBallTile)
            {
                rhythm.Stop(); // Keep music, but don't cue a misleading beat for this jump.
                ballEncounterStarted = true;
                ballTilePosition = pattern.LandingPosition;
                ballTapDeadline = rhythm.DspTime + BallReactionSeconds;
                ball.Schedule(ballTilePosition, ballTapDeadline + settings.JumpDuration + BallEscapeSeconds, settings.SpeedMultiplier);
                ball.Show(rhythm.DspTime);
            }
            else if (leavingBallTile)
            {
                rhythm.Stop();
                ballTapDeadline = rhythm.DspTime + BallEscapeSeconds;
                ball.Schedule(ballTilePosition, ballTapDeadline, settings.SpeedMultiplier);
                ball.Show(rhythm.DspTime);
            }
            else if (ObstaclesEnabled && pattern.SequenceIndex >= 1 && pattern.SequenceIndex < 3 &&
                generator.TryGetPattern(3, out HopscotchPattern upcoming))
                ball.Preview(upcoming.LandingPosition);
            else if (!ballEncounterStarted) ball.Hide();
            if (!ballPattern)
            {
                // Keep a readable lead after landing, even at the maximum music tempo.
                activeWindow = rhythm.ScheduleNextWindow(settings.BeatsPerMinute, pattern.ExpectedTapCount, .30f);
                judge.Begin(pattern.ExpectedTapCount, activeWindow, settings.TimingTolerance, lateInputGraceSeconds);
            }
            acceptingInput = true;
        }

        private void ResolveDecision(TapDecision decision)
        {
            if (decision == TapDecision.Pending)
            {
                return;
            }

            acceptingInput = false;
            if (decision == TapDecision.Failure)
            {
                sectionRunning = false;
                ball.Hide();
                player.ShowFailurePose();
                Failed?.Invoke(FailureReason());
                return;
            }

            inputValidated = true;
            if (hopStarted)
            {
                if (hopLanded) CompleteHop();
                return;
            }
            StartHop();
        }

        private void StartHop()
        {
            hopStarted = true;
            HopscotchPattern completedPattern = generator.Current;
            player.SetReadyPulse(0f);
            completedPattern.SetCurrent(false);
            player.HopTo(
                completedPattern.LandingPosition,
                completedPattern.LandingType,
                difficulty.Current.JumpDuration,
                OnHopFinished);
        }

        private void OnHopFinished()
        {
            if (!sectionRunning) return;
            hopLanded = true;
            if (!inputValidated) return;
            CompleteHop();
        }

        private void CompleteHop()
        {
            firstPattern = false;
            generator.Advance();
            StepCompleted?.Invoke();
            if (!sectionRunning) return;
            if (finite && generator.IsComplete)
            {
                EndRun();
                SectionCompleted?.Invoke();
                return;
            }
            PrepareCurrentPattern();
        }

        private void FailBallReaction()
        {
            EndRun();
            player.ShowFailurePose();
            Failed?.Invoke("Jump before the ball arrives");
        }

        private string FailureReason()
        {
            if (generator.Current.ExpectedTapCount == 1)
            {
                return "One tile needs exactly one tap";
            }

            return "Two tiles need two quick taps";
        }
    }
}

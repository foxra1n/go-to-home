using System;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public enum CrosswalkPhase
    {
        Walking,
        Jumping,
        Failed,
        WaitingForGreen,
        Entering,
        NearMiss
    }

    public sealed class CrosswalkModeController : MonoBehaviour, ISectionGameMode
    {
        private readonly CrosswalkTimingState timing = new CrosswalkTimingState();
        private CrosswalkPatternGenerator generator;
        private PlayerHopController player;
        private DifficultyController difficulty;
        private float gaitPhase;
        private bool sectionRunning;
        private bool finite;
        private CrosswalkSignal signal;
        private float signalElapsed;
        private float crossingEntryZ;
        private float nearMissElapsed;
        private float nearMissZ;
        public bool ObstaclesEnabled { get; set; } = true;
        public bool SignalEnabled => ObstaclesEnabled && difficulty.Current.SpeedMultiplier >= 1.2f;
        public float EntryWaitZ => SurfaceStartZ - .8f;
        public event Action SectionCompleted;
        public float EndZ => generator.EndZ;
        public float SurfaceStartZ => generator.CurrentStartZ;
        public float ForwardSpeed => difficulty.Current.CrosswalkForwardSpeed;

        public void PrepareSection(float startZ, float duration)
        {
            EndRun();
            finite = true;
            float walkDistance = generator.StripeLength + generator.GapLength - generator.JumpDistance;
            float cycle = walkDistance / difficulty.Current.CrosswalkForwardSpeed + difficulty.Current.JumpDuration;
            int count = Mathf.Max(2, Mathf.RoundToInt((duration + difficulty.Current.JumpDuration) / cycle));
            generator.BeginSection(startZ, count);
            crossingEntryZ = startZ;
            signal.Prepare(SurfaceStartZ);
            if (!SignalEnabled) signal.gameObject.SetActive(false);
        }

        public void StartSection()
        {
            sectionRunning = true;
            player.BeginRoadWalk();
            signalElapsed = 0f;
            if (SignalEnabled)
            {
                signal.ShowWait(0f);
                Phase = CrosswalkPhase.WaitingForGreen;
            }
            else
            {
                signal.gameObject.SetActive(false);
                Phase = CrosswalkPhase.Entering;
            }
        }

        private void BeginCrossing()
        {
            timing.Begin(generator.CurrentCenterZ, generator.StripeLength);
            gaitPhase = 0f;
            Phase = CrosswalkPhase.Walking;
        }

        public event Action StepCompleted;
        public event Action<string> Failed;

        public CrosswalkPhase Phase { get; private set; }

        public void Configure(
            CrosswalkPatternGenerator patternGenerator,
            PlayerHopController playerController,
            DifficultyController difficultyController)
        {
            generator = patternGenerator;
            player = playerController;
            difficulty = difficultyController;
            var signalObject = new GameObject("CrosswalkSignal");
            signalObject.transform.SetParent(transform, false);
            signal = signalObject.AddComponent<CrosswalkSignal>();
            signal.Initialize();
        }

        public void BeginRun()
        {
            finite = false;
            sectionRunning = true;
            player.ResetPlayer();
            player.BeginRoadWalk();
            generator.BeginRun(player.transform.position.z);
            timing.Begin(generator.CurrentCenterZ, generator.StripeLength);
            gaitPhase = 0f;
            Phase = CrosswalkPhase.Walking;
        }

        public void SubmitTap()
        {
            if (!sectionRunning) return;
            if (Phase == CrosswalkPhase.WaitingForGreen)
            {
                Phase = CrosswalkPhase.NearMiss;
                nearMissElapsed = 0f;
                nearMissZ = Mathf.Max(player.transform.position.z + .8f, SurfaceStartZ + CrosswalkSignal.RoadVehicleInset);
                signal.ShowNearMiss(0f, nearMissZ, SignalEnabled);
                player.JumpToSurface(new Vector3(0f, .22f, nearMissZ), .25f / difficulty.Current.SpeedMultiplier, null);
                return;
            }
            if (Phase == CrosswalkPhase.Entering)
            {
                Phase = CrosswalkPhase.Jumping;
                player.JumpToSurface(generator.JumpTargetFrom(player.transform.position),
                    difficulty.Current.JumpDuration, OnEntryJumpLanded);
                return;
            }
            if (Phase != CrosswalkPhase.Walking)
            {
                return;
            }

            if (finite && generator.IsLastStripe) return;
            StartJumpImmediately();
        }

        public void Tick(float deltaTime)
        {
            if (!sectionRunning) return;
            if (Phase == CrosswalkPhase.NearMiss)
            {
                nearMissElapsed += deltaTime * difficulty.Current.SpeedMultiplier;
                signal.ShowNearMiss(nearMissElapsed, nearMissZ, SignalEnabled);
                if (nearMissElapsed >= .65f) Fail("Jumped into traffic before the crossing");
                return;
            }
            if (Phase == CrosswalkPhase.WaitingForGreen)
            {
                signalElapsed += deltaTime * difficulty.Current.SpeedMultiplier;
                signal.ShowWait(signalElapsed);
                if (signalElapsed >= CrosswalkSignal.RedDuration) Phase = CrosswalkPhase.Entering;
                return;
            }
            if (Phase == CrosswalkPhase.Entering)
            {
                float step = Mathf.Min(Mathf.Max(0f, crossingEntryZ - player.transform.position.z), ForwardSpeed * deltaTime);
                player.MoveForward(step);
                gaitPhase += deltaTime * 6f * difficulty.Current.SpeedMultiplier;
                player.SetRoadWalkPose(gaitPhase);
                if (player.transform.position.z >= crossingEntryZ) BeginCrossing();
                return;
            }
            if (Phase != CrosswalkPhase.Walking)
            {
                return;
            }

            float distance = difficulty.Current.CrosswalkForwardSpeed * deltaTime;
            if (finite && generator.IsLastStripe && player.transform.position.z + distance >= EndZ)
            {
                player.MoveForward(Mathf.Max(0f, EndZ - player.transform.position.z));
                EndRun();
                SectionCompleted?.Invoke();
                return;
            }
            player.MoveForward(distance);
            gaitPhase += deltaTime * difficulty.Current.SpeedMultiplier * (4.6f + difficulty.Current.CrosswalkForwardSpeed / difficulty.Current.SpeedMultiplier * 0.7f);
            player.SetRoadWalkPose(gaitPhase);

            if (timing.HasSteppedIntoGap(player.transform.position.z))
            {
                Fail("Stepped on the dark road");
            }
        }

        public void EndRun()
        {
            sectionRunning = false;
            if (signal != null) signal.gameObject.SetActive(false);
            Phase = CrosswalkPhase.Failed;
        }

        private void StartJumpImmediately()
        {
            Phase = CrosswalkPhase.Jumping;
            player.JumpToSurface(
                generator.JumpTargetFrom(player.transform.position),
                difficulty.Current.JumpDuration,
                OnLanded);
        }

        private void OnLanded()
        {
            if (!sectionRunning) return;
            if (!generator.TryResolveNextLanding(player.transform.position.z, out float resolvedZ))
            {
                Fail("Landed on the dark road");
                return;
            }

            Vector3 landingPosition = player.transform.position;
            landingPosition.z = resolvedZ;
            player.transform.position = landingPosition;
            generator.Advance();
            timing.Begin(generator.CurrentCenterZ, generator.StripeLength);
            StepCompleted?.Invoke();
            player.BeginRoadWalk();
            Phase = CrosswalkPhase.Walking;
        }

        private void OnEntryJumpLanded()
        {
            if (!sectionRunning) return;
            if (!generator.TryResolveCurrentLanding(player.transform.position.z, out float resolvedZ))
            {
                Fail("Landed outside the first white stripe");
                return;
            }
            player.transform.position = new Vector3(0f, .22f, resolvedZ);
            player.BeginRoadWalk();
            BeginCrossing();
        }

        private void Fail(string reason)
        {
            if (Phase == CrosswalkPhase.Failed)
            {
                return;
            }

            Phase = CrosswalkPhase.Failed;
            player.ShowFailurePose();
            Failed?.Invoke(reason);
        }
    }
}

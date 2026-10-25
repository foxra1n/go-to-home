using System;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class GameModeSequenceController : MonoBehaviour, IGameMode
    {
        [SerializeField, Min(1f)] private float sectionDuration = 10f;
        [SerializeField, Range(1f, 1.5f)] private float transitionPreparationSeconds = 1.25f;
        private readonly GameModeShuffleBag shuffleBag = new GameModeShuffleBag();
        private ISectionGameMode[] modes;
        private GameObject[] worlds;
        private PlayerHopController player;
        private ISectionGameMode activeMode;
        private PrototypeGameMode pendingMode;
        private float nextStartZ;
        private Vector3 transitionStart;
        private float transitionElapsed;
        private bool running;
        private DifficultyController difficulty;
        private RhythmClock rhythm;
        public int CompletedSections { get; private set; }
        public int SetNumber => CompletedSections / 3 + 1;

        public event Action StepCompleted;
        public event Action<string> Failed;
        public event Action RunReset;
        public event Action<PrototypeGameMode, float, float, float> SectionPrepared;
        private float initialEntryZ;
        public bool IsEnteringInitialSection { get; private set; }
        public PrototypeGameMode CurrentMode { get; private set; }
        public float SectionDuration => sectionDuration;
        public float TransitionPreparationSeconds => transitionPreparationSeconds;
        public bool IsPreparingNextSection { get; private set; }

        public void Configure(HopscotchModeController hopscotch, CurbBalanceModeController curb,
            CrosswalkModeController crosswalk, GameObject hopscotchWorld, GameObject curbWorld,
            GameObject crosswalkWorld, PlayerHopController playerController,
            DifficultyController difficultyController = null, RhythmClock rhythmClock = null)
        {
            difficulty = difficultyController;
            rhythm = rhythmClock;
            player = playerController;
            modes = new ISectionGameMode[] { hopscotch, curb, crosswalk };
            worlds = new[] { hopscotchWorld, curbWorld, crosswalkWorld };
            foreach (ISectionGameMode mode in modes)
            {
                mode.StepCompleted += ForwardStep;
                mode.Failed += ForwardFailure;
                mode.SectionCompleted += CompleteSection;
            }
        }

        public void BeginRun()
        {
            foreach (ISectionGameMode mode in modes) mode.EndRun();
            foreach (GameObject world in worlds) world.SetActive(false);
            player.ResetPlayer();
            RunReset?.Invoke();
            shuffleBag.Reset();
            CompletedSections = 0;
            if (difficulty != null) difficulty.ResetProgression();
            if (rhythm != null) rhythm.SetPlaybackSpeed(difficulty != null ? difficulty.Current.SpeedMultiplier : 1f);
            running = true;
            IsPreparingNextSection = false;
            IsEnteringInitialSection = false;
            CurrentMode = shuffleBag.Next();
            activeMode = Mode(CurrentMode);
            IsEnteringInitialSection = CurrentMode == PrototypeGameMode.Crosswalk;
            initialEntryZ = player.transform.position.z + (IsEnteringInitialSection ? 3f : 0f);
            Prepare(CurrentMode, initialEntryZ, 0);
            if (IsEnteringInitialSection) initialEntryZ = ((CrosswalkModeController)activeMode).EntryWaitZ;
            PrepareNext();
            if (IsEnteringInitialSection) player.BeginRoadWalk();
            else activeMode.StartSection();
        }

        public void SubmitTap()
        {
            if (running && !IsPreparingNextSection && !IsEnteringInitialSection) activeMode.SubmitTap();
        }

        public void Tick(float deltaTime)
        {
            if (!running) return;
            if (IsEnteringInitialSection)
            {
                // Only the very first crosswalk has a sidewalk approach. No taps are queued.
                float speed = ((CrosswalkModeController)activeMode).ForwardSpeed;
                float distance = Mathf.Min(Mathf.Max(0f, initialEntryZ - player.transform.position.z), speed * deltaTime);
                player.MoveForward(distance);
                player.SetRoadWalkPose(player.transform.position.z * 4f);
                if (player.transform.position.z >= initialEntryZ)
                {
                    IsEnteringInitialSection = false;
                    activeMode.StartSection();
                }
                return;
            }
            if (!IsPreparingNextSection)
            {
                activeMode.Tick(deltaTime);
                return;
            }

            transitionElapsed += deltaTime;
            float t = Mathf.Clamp01(transitionElapsed / transitionPreparationSeconds);
            float entryZ = pendingMode == PrototypeGameMode.Crosswalk
                ? ((CrosswalkModeController)Mode(pendingMode)).EntryWaitZ : nextStartZ;
            player.transform.position = Vector3.Lerp(transitionStart, new Vector3(0f, 0.22f, entryZ), t);
            player.SetRoadWalkPose(transitionElapsed * 7f);
            if (pendingMode == PrototypeGameMode.Hopscotch)
                ((HopscotchModeController)Mode(pendingMode)).TickEntryCue();
            if (t < 1f) return;

            World(CurrentMode).SetActive(false);
            CurrentMode = pendingMode;
            activeMode = Mode(CurrentMode);
            IsPreparingNextSection = false;
            PrepareNext();
            activeMode.StartSection();
        }

        private ISectionGameMode Mode(PrototypeGameMode mode) => modes[Index(mode)];
        private GameObject World(PrototypeGameMode mode) => worlds[Index(mode)];

        private static int Index(PrototypeGameMode mode)
        {
            switch (mode)
            {
                case PrototypeGameMode.Hopscotch: return 0;
                case PrototypeGameMode.CurbBalance: return 1;
                default: return 2;
            }
        }

        private void Prepare(PrototypeGameMode mode, float startZ, int sectionIndex)
        {
            World(mode).SetActive(true);
            int activeSet = difficulty != null ? difficulty.SetIndex : 0;
            if (difficulty != null)
            {
                int set = sectionIndex / 3;
                bool obstacle = difficulty.HasObstacle(set, mode);
                if (Mode(mode) is HopscotchModeController hop) hop.ObstaclesEnabled = obstacle;
                if (Mode(mode) is CurbBalanceModeController curb) curb.ObstaclesEnabled = obstacle;
                if (Mode(mode) is CrosswalkModeController cross) cross.ObstaclesEnabled = obstacle;
                difficulty.ApplySet(set);
            }
            try { Mode(mode).PrepareSection(startZ, sectionDuration); }
            finally { if (difficulty != null) difficulty.ApplySet(activeSet); }
            float surfaceStart = mode == PrototypeGameMode.Crosswalk
                ? ((CrosswalkModeController)Mode(mode)).SurfaceStartZ : startZ;
            SectionPrepared?.Invoke(mode, startZ, Mode(mode).EndZ, surfaceStart);
        }

        private void PrepareNext()
        {
            pendingMode = shuffleBag.Next();
            nextStartZ = activeMode.EndZ + 3f;
            Prepare(pendingMode, nextStartZ, CompletedSections + 1);
        }

        private void CompleteSection()
        {
            if (!running || IsPreparingNextSection) return;
            activeMode.EndRun();
            CompletedSections++;
            if (difficulty != null)
            {
                difficulty.ApplySet(CompletedSections / 3);
                if (rhythm != null) rhythm.SetPlaybackSpeed(difficulty.Current.SpeedMultiplier);
            }
            IsEnteringInitialSection = false;
            IsPreparingNextSection = true;
            transitionElapsed = 0f;
            transitionStart = player.transform.position;
            player.BeginRoadWalk();
            if (pendingMode == PrototypeGameMode.Hopscotch)
                ((HopscotchModeController)Mode(pendingMode)).BeginEntryCue(transitionPreparationSeconds + .1f);
        }

        private void ForwardStep()
        {
            if (running && !IsPreparingNextSection) StepCompleted?.Invoke();
        }

        private void ForwardFailure(string reason)
        {
            if (!running || IsPreparingNextSection) return;
            running = false;
            activeMode.EndRun();
            Failed?.Invoke(reason);
        }

        private void OnDestroy()
        {
            if (modes == null) return;
            foreach (ISectionGameMode mode in modes)
            {
                mode.StepCompleted -= ForwardStep;
                mode.Failed -= ForwardFailure;
                mode.SectionCompleted -= CompleteSection;
            }
        }
    }
}

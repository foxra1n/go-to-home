using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Homeward.Hopscotch.Tests
{
    public sealed class BallAndSignalTests
    {
        [Test]
        public void SetProgressionKeepsGeometryAndDoesNotAdvanceWithScore()
        {
            var root = new GameObject("DifficultyProgressionTest");
            try
            {
                var difficulty = root.AddComponent<DifficultyController>();
                difficulty.ResetProgression();
                var baseline = difficulty.Current;
                difficulty.ApplyScore(10000);
                Assert.That(difficulty.Current.SpeedMultiplier, Is.EqualTo(1f));
                difficulty.TestMaximumDifficulty = true;
                difficulty.ResetProgression();
                Assert.That(difficulty.Current.SpeedMultiplier, Is.EqualTo(1.4f));
                foreach (PrototypeGameMode mode in System.Enum.GetValues(typeof(PrototypeGameMode)))
                    Assert.That(difficulty.HasObstacle(0, mode), Is.True);
                difficulty.TestMaximumDifficulty = false;
                for (int set = 0; set < 30; set++)
                {
                    difficulty.ApplySet(set);
                    int obstacles = 0;
                    foreach (PrototypeGameMode mode in System.Enum.GetValues(typeof(PrototypeGameMode)))
                    {
                        bool enabled = difficulty.HasObstacle(set, mode);
                        Assert.That(difficulty.HasObstacle(set, mode), Is.EqualTo(enabled));
                        if (enabled) obstacles++;
                    }
                    Assert.That(obstacles, Is.EqualTo(Mathf.Min(set, 3)));
                    var settings = difficulty.Current;
                    float speed = set <= 3 ? 1f : set == 4 ? 1.1f : set == 5 ? 1.2f : set == 6 ? 1.3f : 1.4f;
                    Assert.That(settings.SpeedMultiplier, Is.EqualTo(speed).Within(.0001f));
                    Assert.That(settings.BeatsPerMinute, Is.EqualTo(baseline.BeatsPerMinute * speed).Within(.001f));
                    Assert.That(settings.TimingTolerance, Is.EqualTo(baseline.TimingTolerance));
                    Assert.That(settings.CrosswalkForwardSpeed * settings.JumpDuration,
                        Is.EqualTo(baseline.CrosswalkForwardSpeed * baseline.JumpDuration).Within(.001f));
                    Assert.That(settings.CurbTapLockoutSeconds * speed, Is.EqualTo(baseline.CurbTapLockoutSeconds).Within(.001f));
                    if (set >= 6)
                        Assert.That(settings.CurbLeanDegreesPerSecond / baseline.CurbLeanDegreesPerSecond, Is.EqualTo(set == 6 ? 1.56f : 1.68f).Within(.001f));
                }
                difficulty.ResetProgression();
                Assert.That(difficulty.SetIndex, Is.Zero);
                Assert.That(difficulty.Current.SpeedMultiplier, Is.EqualTo(1f));
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator MusicAndClicksSpeedUpWithoutRestartingSong()
        {
            var root = new GameObject("TempoTest");
            try
            {
                var clock = root.AddComponent<RhythmClock>(); clock.Initialize(); clock.StartMusic(88f);
                var source = root.GetComponents<AudioSource>()[0];
                var clip = source.clip;
                double originalBeat = clock.MusicBeatSeconds;
                yield return new WaitForSecondsRealtime(.25f);
                int before = source.timeSamples;
                clock.SetPlaybackSpeed(1.4f);
                Assert.That(source.clip, Is.SameAs(clip));
                Assert.That(source.timeSamples, Is.GreaterThanOrEqualTo(before));
                Assert.That(source.pitch, Is.EqualTo(1.4f).Within(.001f));
                double cueStart = clock.DspTime;
                clock.BeginRun(123.2f, 1.35f);
                var window = clock.ScheduleNextWindow(123.2f, 2);
                Assert.That(window.FirstTapDspTime, Is.GreaterThanOrEqualTo(cueStart + 1.35d - .001d));
                Assert.That(window.EndDspTime - window.StartDspTime, Is.EqualTo(originalBeat / 1.4f).Within(.00001d));
                Assert.That(window.FirstTapDspTime - clock.EntryFirstBeatDspTime, Is.EqualTo(originalBeat / 1.4f * 2d).Within(.00001d));
                Assert.That(root.GetComponents<AudioSource>()[4].pitch, Is.EqualTo(1.4f).Within(.001f));
                yield return new WaitForSecondsRealtime(2.5f);
                double afterLanding = clock.DspTime;
                var next = clock.ScheduleNextWindow(123.2f, 2, .30f);
                Assert.That(next.FirstTapDspTime, Is.GreaterThanOrEqualTo(afterLanding + .30d - .001d));
                clock.SetPlaybackSpeed(1f);
                Assert.That(clock.MusicBeatSeconds, Is.EqualTo(originalBeat).Within(.00001d));
                Assert.That(source.pitch, Is.EqualTo(1f));
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator DisabledObstaclesHideBallWindAndSignal()
        {
            var root = new GameObject("DisabledObstaclesTest");
            try
            {
                var playerObject = new GameObject("Player"); playerObject.transform.SetParent(root.transform);
                var player = playerObject.AddComponent<PlayerHopController>(); player.Initialize();
                var difficulty = root.AddComponent<DifficultyController>(); difficulty.ResetProgression();
                var clock = root.AddComponent<RhythmClock>(); clock.Initialize();
                var generator = root.AddComponent<HopscotchPatternGenerator>(); generator.Initialize();
                var hop = root.AddComponent<HopscotchModeController>(); hop.Configure(generator, player, difficulty, clock);
                hop.ObstaclesEnabled = false; hop.PrepareSection(0, 10); hop.StartSection();
                for (int i = 1; i <= 3; i++)
                {
                    generator.Advance(); hop.SendMessage("PrepareCurrentPattern");
                    Assert.That(root.GetComponentInChildren<HopscotchBallObstacle>(true).gameObject.activeSelf, Is.False);
                }
                hop.EndRun();
                var path = root.AddComponent<CurbPathGenerator>(); path.Initialize();
                var curb = root.AddComponent<CurbBalanceModeController>(); curb.Configure(path, player, difficulty);
                curb.ObstaclesEnabled = false; curb.PrepareSection(0, 10); curb.StartSection();
                yield return new WaitForSeconds(.6f);
                curb.Tick(3.8f);
                Assert.That(curb.CurrentWindFraction, Is.Zero);
                Assert.That(root.GetComponentInChildren<CurbWindVisual>(true).gameObject.activeSelf, Is.False);
                curb.EndRun(); player.ResetPlayer();
                var stripes = root.AddComponent<CrosswalkPatternGenerator>(); stripes.Initialize();
                var cross = root.AddComponent<CrosswalkModeController>(); cross.Configure(stripes, player, difficulty);
                cross.ObstaclesEnabled = false; cross.PrepareSection(0, 10); cross.StartSection();
                Assert.That(cross.Phase, Is.EqualTo(CrosswalkPhase.Entering));
                Assert.That(root.GetComponentInChildren<CrosswalkSignal>(true).gameObject.activeSelf, Is.False);
                cross.EndRun();
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator BallIsPreviewedTwoPatternsEarlyAndReusedUntilEncounter()
        {
            var root = new GameObject("BallPreviewTest");
            try
            {
                var playerObject = new GameObject("TestPlayer"); playerObject.transform.SetParent(root.transform);
                var player = playerObject.AddComponent<PlayerHopController>(); player.Initialize();
                var difficulty = root.AddComponent<DifficultyController>(); difficulty.ApplyScore(0);
                var clock = root.AddComponent<RhythmClock>(); clock.Initialize();
                var generator = root.AddComponent<HopscotchPatternGenerator>(); generator.Initialize();
                var mode = root.AddComponent<HopscotchModeController>(); mode.Configure(generator, player, difficulty, clock);
                mode.PrepareSection(0, 10); mode.StartSection();
                var ball = root.GetComponentInChildren<HopscotchBallObstacle>(true);
                Assert.That(ball.gameObject.activeSelf, Is.False);
                Assert.That(generator.TryGetPattern(3, out var target), Is.True);
                var visual = ball.transform.Find("RollingSchoolBall");
                for (int index = 1; index <= 2; index++)
                {
                    generator.Advance(); mode.SendMessage("PrepareCurrentPattern");
                    mode.Tick(0f);
                    Assert.That(ball.gameObject.activeSelf, Is.True);
                    Assert.That(visual.gameObject.activeSelf, Is.True);
                    Assert.That(visual.position.z, Is.EqualTo(target.LandingPosition.z));
                    Assert.That(ball.Blocks(clock.DspTime), Is.False);
                }
                Vector3 previewPosition = visual.position;
                generator.Advance(); mode.SendMessage("PrepareCurrentPattern");
                Assert.That(Vector3.Distance(visual.position, previewPosition), Is.LessThan(.08f));
                Assert.That(root.GetComponentsInChildren<HopscotchBallObstacle>(true).Length, Is.EqualTo(1));
                mode.EndRun(); Assert.That(ball.gameObject.activeSelf, Is.False);
                mode.PrepareSection(50f, 1f); mode.StartSection();
                Assert.That(ball.gameObject.activeSelf, Is.False);
                mode.EndRun();
            }
            finally { Object.Destroy(root); }
            yield return null;
        }

        [Test]
        public void PreviewApproachesSmoothlyOutsideUnchangedCollisionZone()
        {
            var root = new GameObject("BallApproachTest");
            try
            {
                var ball = root.AddComponent<HopscotchBallObstacle>(); ball.Initialize();
                ball.Preview(new Vector3(0, .22f, 10));
                var visual = root.transform.Find("RollingSchoolBall");
                float initialX = visual.position.x;
                double now = AudioSettings.dspTime;
                ball.Schedule(new Vector3(0, .22f, 10), now + 2d);
                ball.Show(now + .9d);
                Assert.That(visual.position.x, Is.LessThan(initialX));
                Assert.That(visual.position.x, Is.GreaterThan(HopscotchBallObstacle.LandingHalfWidth));
                ball.Show(now + 1.9d);
                Assert.That(visual.position.x, Is.EqualTo(1.2f).Within(.001f));
                ball.Show(now + 2d);
                Assert.That(visual.position.x, Is.EqualTo(0f).Within(.001f));
                Assert.That(ball.Blocks(now + 2d), Is.True);
                ball.Hide(); ball.Preview(new Vector3(0, .22f, 50));
                Assert.That(visual.position.z, Is.EqualTo(50f));
                Assert.That(visual.position.x, Is.EqualTo(initialX));
            }
            finally { Object.Destroy(root); }
        }

        [TestCase(1)]
        [TestCase(2)]
        public void OrdinaryPatternsKeepExistingRhythmTolerance(int taps)
        {
            var window = new RhythmWindow(10d, 60d / 88d, taps);
            foreach (double offset in new[] { -.2d, .25d })
            {
                var judge = new HopscotchTapJudge();
                judge.Begin(taps, window, .3f, .15f);
                for (int i = 0; i < taps; i++)
                    Assert.That(judge.RegisterTap(window.TargetDspTime(i) + offset), Is.EqualTo(TapDecision.Pending));
                Assert.That(judge.Tick(12d), Is.EqualTo(TapDecision.Success));
            }
        }

        [UnityTest] public IEnumerator ImmediateBallTapSucceeds() => BallAttempt(0d, false, false);
        [UnityTest] public IEnumerator ReactionTapBeforeDeadlineSucceeds() => BallAttempt(.65d, false, false);
        [UnityTest] public IEnumerator MaximumSpeedKeepsBallReactionGrace() => BallAttempt(.75d, false, false, true);
        [UnityTest] public IEnumerator TapAfterBallDeadlineFails() => BallAttempt(1d, false, true);
        [UnityTest] public IEnumerator NoBallTapFails() => BallAttempt(double.PositiveInfinity, false, true);
        [UnityTest] public IEnumerator ExtraTapsDuringBallJumpAreIgnored() => BallAttempt(0d, true, false);

        private static IEnumerator BallAttempt(double offset, bool extraTap, bool expectFailure, bool maximumSpeed = false)
        {
            var root = new GameObject("BallIntegrationTest");
            try
            {
                var playerObject = new GameObject("TestPlayer"); playerObject.transform.SetParent(root.transform);
                var player = playerObject.AddComponent<PlayerHopController>(); player.Initialize();
                var difficulty = root.AddComponent<DifficultyController>(); difficulty.ApplyScore(0);
                difficulty.TestMaximumDifficulty = maximumSpeed;
                var clock = root.AddComponent<RhythmClock>(); clock.Initialize();
                var generator = root.AddComponent<HopscotchPatternGenerator>(); generator.Initialize();
                var mode = root.AddComponent<HopscotchModeController>(); mode.Configure(generator, player, difficulty, clock);
                mode.PrepareSection(0, 10);
                for (int i = 0; i < 3; i++) generator.Advance();
                player.transform.position = generator.Current.LandingPosition - Vector3.forward * generator.Spacing;
                bool failed = false; int score = 0;
                mode.Failed += _ => { failed = true; mode.EndRun(); };
                mode.StepCompleted += () => { score++; mode.EndRun(); };
                mode.StartSection();
                double start = clock.DspTime;
                int sent = 0; bool sentExtra = false;
                double deadline = clock.DspTime + 5d;
                while (!failed && score == 0 && clock.DspTime < deadline)
                {
                    if (sent == 0 && clock.DspTime >= start + offset)
                    { mode.SubmitTap(); sent++; }
                    else if (extraTap && sent == 1 && !sentExtra)
                    { mode.SubmitTap(); sentExtra = true; }
                    mode.Tick(Time.deltaTime);
                    yield return null;
                }
                Assert.That(failed, Is.EqualTo(expectFailure));
                Assert.That(score, Is.EqualTo(expectFailure ? 0 : 1));
                mode.EndRun();
            }
            finally { Object.Destroy(root); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator RedWaitReleasesOnGreenAndAllowsSidewalkJump()
        {
            var root = new GameObject("SignalIntegrationTest");
            try
            {
                var playerObject = new GameObject("TestPlayer"); playerObject.transform.SetParent(root.transform);
                var player = playerObject.AddComponent<PlayerHopController>(); player.Initialize();
                var difficulty = root.AddComponent<DifficultyController>(); difficulty.ApplyScore(0);
                var generator = root.AddComponent<CrosswalkPatternGenerator>(); generator.Initialize();
                var mode = root.AddComponent<CrosswalkModeController>(); mode.Configure(generator, player, difficulty);
                foreach (int set in new[] { 0, 4, 5, 6 })
                {
                    difficulty.ApplySet(set);
                    mode.PrepareSection(10f, 10f); mode.StartSection();
                    Assert.That(mode.SignalEnabled, Is.EqualTo(set >= 5));
                    Assert.That(mode.Phase, Is.EqualTo(set >= 5 ? CrosswalkPhase.WaitingForGreen : CrosswalkPhase.Entering));
                    mode.EndRun();
                }
                difficulty.ApplySet(5);
                mode.PrepareSection(10f, 10f);
                player.transform.position = new Vector3(0, .22f, mode.EntryWaitZ);
                var signal = root.GetComponentInChildren<CrosswalkSignal>(true);
                float waitZ = player.transform.position.z;
                Assert.That(waitZ, Is.LessThan(mode.SurfaceStartZ));
                mode.StartSection();
                for (int i = 0; i < 20; i++) mode.Tick(.1f / 1.2f);
                Assert.That(mode.Phase, Is.EqualTo(CrosswalkPhase.WaitingForGreen));
                Assert.That(player.transform.position.z, Is.EqualTo(waitZ));
                Assert.That(signal.IsGreen, Is.False);
                mode.Tick(.3f / 1.2f);
                Assert.That(signal.IsGreen, Is.True);
                Assert.That(signal.transform.Find("PassingCar").gameObject.activeSelf, Is.False);
                mode.SubmitTap();
                Assert.That(mode.Phase, Is.EqualTo(CrosswalkPhase.Jumping));
                mode.SubmitTap();
                yield return new WaitForSeconds(.5f);
                Assert.That(mode.Phase, Is.EqualTo(CrosswalkPhase.Walking));
                Assert.That(player.transform.position.z, Is.EqualTo(waitZ + generator.JumpDistance).Within(.001f));
                yield return null;
                Assert.That(mode.Phase, Is.EqualTo(CrosswalkPhase.Walking));
                mode.EndRun(); Assert.That(signal.gameObject.activeSelf, Is.False);
                mode.PrepareSection(100f, 10f); mode.StartSection();
                Assert.That(signal.IsGreen, Is.False);
                Assert.That(root.GetComponentsInChildren<CrosswalkSignal>(true).Length, Is.EqualTo(1));
                mode.EndRun();
            }
            finally { Object.Destroy(root); }
            yield return null;
        }

        [UnityTest] public IEnumerator BallTileNeedsAnImmediateExitJump() => BallExitAttempt(true);
        [UnityTest] public IEnumerator StayingOnBallTileFails() => BallExitAttempt(false);

        private static IEnumerator BallExitAttempt(bool jumpAgain)
        {
            var root = new GameObject("BallExitTest");
            try
            {
                var child = new GameObject("Player"); child.transform.SetParent(root.transform);
                var player = child.AddComponent<PlayerHopController>(); player.Initialize();
                var difficulty = root.AddComponent<DifficultyController>(); difficulty.TestMaximumDifficulty = true;
                var clock = root.AddComponent<RhythmClock>(); clock.Initialize();
                var generator = root.AddComponent<HopscotchPatternGenerator>(); generator.Initialize();
                var mode = root.AddComponent<HopscotchModeController>(); mode.Configure(generator, player, difficulty, clock);
                mode.PrepareSection(0, 10);
                for (int i = 0; i < 3; i++) generator.Advance();
                player.transform.position = generator.Current.LandingPosition - Vector3.forward * generator.Spacing;
                int score = 0; bool failed = false; bool escaped = false;
                mode.StepCompleted += () => score++;
                mode.Failed += _ => failed = true;
                mode.StartSection(); mode.SubmitTap();
                var ball = root.GetComponentInChildren<HopscotchBallObstacle>(true);
                var visual = ball.transform.Find("RollingSchoolBall");
                float ballZ = visual.position.z;
                double timeout = clock.DspTime + 4d;
                while (!failed && score < 2 && clock.DspTime < timeout)
                {
                    if (score == 1 && jumpAgain && !escaped)
                    {
                        Assert.That(ball.gameObject.activeSelf, Is.True, "Ball must remain after the first landing");
                        Assert.That(visual.position.z, Is.EqualTo(ballZ));
                        mode.SubmitTap(); escaped = true;
                    }
                    mode.Tick(Time.deltaTime);
                    yield return null;
                }
                Assert.That(failed, Is.EqualTo(!jumpAgain));
                Assert.That(score, Is.EqualTo(jumpAgain ? 2 : 1));
                if (jumpAgain)
                {
                    Assert.That(ball.gameObject.activeSelf, Is.True, "Ball must keep rolling after escape");
                    ball.Show(clock.DspTime + 1f);
                    Assert.That(visual.position.x, Is.LessThan(-6.5f));
                    ball.Show(clock.DspTime + 10f);
                    Assert.That(ball.gameObject.activeSelf, Is.False, "Recycle only after rolling out of view");
                }
                mode.EndRun();
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator RedLightJumpShowsNearMissThenFailsOnce()
        {
            var root = new GameObject("NearMissTest");
            try
            {
                var child = new GameObject("Player"); child.transform.SetParent(root.transform);
                var player = child.AddComponent<PlayerHopController>(); player.Initialize();
                var difficulty = root.AddComponent<DifficultyController>(); difficulty.TestMaximumDifficulty = true;
                var generator = root.AddComponent<CrosswalkPatternGenerator>(); generator.Initialize();
                var mode = root.AddComponent<CrosswalkModeController>(); mode.Configure(generator, player, difficulty);
                mode.PrepareSection(10f, 10f);
                player.transform.position = new Vector3(0, .22f, mode.EntryWaitZ);
                int failures = 0; int score = 0;
                mode.Failed += _ => failures++; mode.StepCompleted += () => score++;
                mode.StartSection(); mode.SubmitTap();
                Assert.That(mode.Phase, Is.EqualTo(CrosswalkPhase.NearMiss));
                var car = root.GetComponentInChildren<CrosswalkSignal>(true).transform.Find("PassingCar");
                Assert.That(car.gameObject.activeSelf, Is.True);
                foreach (var renderer in car.GetComponentsInChildren<Renderer>())
                    Assert.That(renderer.bounds.min.z, Is.GreaterThan(mode.SurfaceStartZ), "The entire car must stay on the road");
                float initialX = car.position.x;
                mode.Tick(.1f); mode.SubmitTap();
                Assert.That(car.position.x, Is.GreaterThan(initialX));
                Assert.That(failures, Is.Zero);
                yield return new WaitForSeconds(.3f);
                mode.Tick(.5f);
                Assert.That(mode.Phase, Is.EqualTo(CrosswalkPhase.Failed));
                Assert.That(failures, Is.EqualTo(1));
                Assert.That(score, Is.Zero);
                Assert.That(car.position.x, Is.LessThan(-1.5f));
                foreach (var renderer in car.GetComponentsInChildren<Renderer>())
                    Assert.That(renderer.bounds.min.z, Is.GreaterThan(mode.SurfaceStartZ));
                mode.Tick(1f); mode.SubmitTap();
                Assert.That(failures, Is.EqualTo(1));
                mode.EndRun();
                Assert.That(car.gameObject.activeInHierarchy, Is.False);
            }
            finally { Object.Destroy(root); }
        }
    }
}

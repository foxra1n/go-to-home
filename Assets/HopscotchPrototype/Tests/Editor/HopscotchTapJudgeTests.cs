using NUnit.Framework;

namespace Homeward.Hopscotch.Tests
{
    public sealed class HopscotchTapJudgeTests
    {
        [Test]
        public void LateGraceKeepsDoubleWindowOpenPastOriginalEnd()
        {
            var judge = new HopscotchTapJudge();
            var window = new RhythmWindow(10d, 60d / 88d, 2);
            judge.Begin(2, window, 0.30f, 0.15f);
            Assert.That(judge.RegisterTap(window.FirstTapDspTime + 0.40d), Is.EqualTo(TapDecision.Pending));
            Assert.That(judge.Tick(window.EndDspTime), Is.EqualTo(TapDecision.Pending));
            Assert.That(judge.RegisterTap(window.SecondTapDspTime + 0.40d), Is.EqualTo(TapDecision.Pending));
            Assert.That(judge.Tick(window.SecondTapDspTime + 0.46d), Is.EqualTo(TapDecision.Success));
        }

        [Test]
        public void LateGraceStillRejectsMissingLateEarlyAndExtraTaps()
        {
            var judge = new HopscotchTapJudge();
            var window = new RhythmWindow(10d, 60d / 88d, 1);
            judge.Begin(1, window, 0.30f, 0.15f);
            Assert.That(judge.Tick(window.FirstTapDspTime + 0.40d), Is.EqualTo(TapDecision.Pending));
            Assert.That(judge.Tick(window.FirstTapDspTime + 0.46d), Is.EqualTo(TapDecision.Failure));
            judge.Begin(1, window, 0.30f, 0.15f);
            Assert.That(judge.RegisterTap(window.FirstTapDspTime + 0.46d), Is.EqualTo(TapDecision.Failure));
            judge.Begin(1, window, 0.30f, 0.15f);
            Assert.That(judge.RegisterTap(window.FirstTapDspTime - 0.31d), Is.EqualTo(TapDecision.Failure));
            judge.Begin(1, window, 0.30f, 0.15f);
            Assert.That(judge.RegisterTap(window.FirstTapDspTime + 0.40d), Is.EqualTo(TapDecision.Pending));
            Assert.That(judge.RegisterTap(window.FirstTapDspTime + 0.41d), Is.EqualTo(TapDecision.Failure));
        }

        [Test]
        public void BeginnerToleranceAcceptsEarlyTapBeforeWindowStart()
        {
            HopscotchTapJudge judge = new HopscotchTapJudge();
            RhythmWindow window = new RhythmWindow(10d, 60d / 88d, 1);
            judge.Begin(1, window, 0.30f);
            Assert.That(judge.RegisterTap(window.FirstTapDspTime - 0.25d), Is.EqualTo(TapDecision.Pending));
            Assert.That(judge.Tick(window.EndDspTime), Is.EqualTo(TapDecision.Success));
        }

        [Test]
        public void BeginnerToleranceAcceptsLateDoubleAndRejectsExtraTap()
        {
            HopscotchTapJudge judge = new HopscotchTapJudge();
            RhythmWindow window = new RhythmWindow(10d, 60d / 88d, 2);
            judge.Begin(2, window, 0.30f);
            Assert.That(judge.RegisterTap(window.FirstTapDspTime + 0.25d), Is.EqualTo(TapDecision.Pending));
            Assert.That(judge.RegisterTap(window.SecondTapDspTime + 0.25d), Is.EqualTo(TapDecision.Pending));
            Assert.That(judge.RegisterTap(window.SecondTapDspTime + 0.26d), Is.EqualTo(TapDecision.Failure));
        }

        [Test]
        public void SingleTap_OnBeatSucceedsWhenWindowCloses()
        {
            HopscotchTapJudge judge = new HopscotchTapJudge();
            RhythmWindow window = new RhythmWindow(10d, 1d, 1);
            judge.Begin(1, window, 0.15f);

            Assert.That(judge.RegisterTap(window.FirstTapDspTime), Is.EqualTo(TapDecision.Pending));
            Assert.That(judge.Tick(window.EndDspTime), Is.EqualTo(TapDecision.Success));
        }

        [Test]
        public void SingleTile_DoubleTapFails()
        {
            HopscotchTapJudge judge = new HopscotchTapJudge();
            RhythmWindow window = new RhythmWindow(10d, 1d, 1);
            judge.Begin(1, window, 0.15f);

            judge.RegisterTap(window.FirstTapDspTime);
            Assert.That(judge.RegisterTap(window.FirstTapDspTime + 0.05d), Is.EqualTo(TapDecision.Failure));
        }

        [Test]
        public void DoubleTile_TwoQuickTapsSucceed()
        {
            HopscotchTapJudge judge = new HopscotchTapJudge();
            RhythmWindow window = new RhythmWindow(10d, 1d, 2);
            judge.Begin(2, window, 0.15f);

            judge.RegisterTap(window.FirstTapDspTime);
            Assert.That(judge.RegisterTap(window.SecondTapDspTime), Is.EqualTo(TapDecision.Pending));
            Assert.That(judge.Tick(window.EndDspTime), Is.EqualTo(TapDecision.Success));
        }

        [Test]
        public void DoubleTile_MissingSecondTapFails()
        {
            HopscotchTapJudge judge = new HopscotchTapJudge();
            RhythmWindow window = new RhythmWindow(10d, 1d, 2);
            judge.Begin(2, window, 0.15f);

            judge.RegisterTap(window.FirstTapDspTime);
            Assert.That(judge.Tick(window.SecondTapDspTime + 0.16d), Is.EqualTo(TapDecision.Failure));
        }

        [Test]
        public void NoInputByFirstBeatToleranceFails()
        {
            HopscotchTapJudge judge = new HopscotchTapJudge();
            RhythmWindow window = new RhythmWindow(10d, 1d, 1);
            judge.Begin(1, window, 0.15f);

            Assert.That(judge.Tick(window.FirstTapDspTime + 0.16d), Is.EqualTo(TapDecision.Failure));
        }

        [Test]
        public void OffBeatTapFails()
        {
            HopscotchTapJudge judge = new HopscotchTapJudge();
            RhythmWindow window = new RhythmWindow(10d, 1d, 1);
            judge.Begin(1, window, 0.1f);

            Assert.That(judge.RegisterTap(window.FirstTapDspTime + 0.2d), Is.EqualTo(TapDecision.Failure));
        }

        [Test]
        public void TapBeforeTheRhythmWindowFails()
        {
            HopscotchTapJudge judge = new HopscotchTapJudge();
            RhythmWindow window = new RhythmWindow(10d, 1d, 1);
            judge.Begin(1, window, 0.1f);

            Assert.That(judge.RegisterTap(window.StartDspTime - 0.01d), Is.EqualTo(TapDecision.Failure));
        }
    }
}

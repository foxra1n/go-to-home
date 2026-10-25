namespace Homeward.Hopscotch
{
    public enum TapJudgePhase
    {
        WaitingForWindow,
        Judging,
        Finished
    }

    public enum TapDecision
    {
        Pending,
        Success,
        Failure
    }

    public sealed class HopscotchTapJudge
    {
        private int expectedTapCount;
        private int receivedTapCount;
        private float timingTolerance;
        private float lateGrace;
        private RhythmWindow window;

        public TapJudgePhase Phase { get; private set; } = TapJudgePhase.Finished;
        public int ReceivedTapCount => receivedTapCount;

        public void Begin(int expectedTaps, RhythmWindow rhythmWindow, float tolerance, float lateGraceSeconds = 0f)
        {
            expectedTapCount = expectedTaps;
            receivedTapCount = 0;
            timingTolerance = tolerance;
            lateGrace = System.Math.Max(0f, lateGraceSeconds);
            window = rhythmWindow;
            Phase = TapJudgePhase.WaitingForWindow;
        }

        public TapDecision RegisterTap(double dspTime)
        {
            if (Phase == TapJudgePhase.Finished)
            {
                return TapDecision.Pending;
            }

            if (dspTime < window.FirstTapDspTime - timingTolerance)
            {
                return Finish(TapDecision.Failure);
            }

            Phase = TapJudgePhase.Judging;
            if (receivedTapCount >= expectedTapCount)
            {
                return Finish(TapDecision.Failure);
            }

            double targetDspTime = window.TargetDspTime(receivedTapCount);
            if (dspTime < targetDspTime - timingTolerance ||
                dspTime > targetDspTime + timingTolerance + lateGrace)
            {
                return Finish(TapDecision.Failure);
            }

            receivedTapCount++;
            return TapDecision.Pending;
        }

        public TapDecision Tick(double dspTime)
        {
            if (Phase == TapJudgePhase.Finished)
            {
                return TapDecision.Pending;
            }

            if (dspTime < window.StartDspTime)
            {
                return TapDecision.Pending;
            }

            Phase = TapJudgePhase.Judging;
            if (receivedTapCount < expectedTapCount &&
                dspTime > window.TargetDspTime(receivedTapCount) + timingTolerance + lateGrace)
            {
                return Finish(TapDecision.Failure);
            }

            // Keep judging through the last tap's grace period, including extra taps.
            double closeTime = System.Math.Max(window.EndDspTime,
                window.TargetDspTime(expectedTapCount - 1) + timingTolerance + lateGrace);
            if (dspTime >= closeTime)
            {
                return Finish(receivedTapCount == expectedTapCount
                    ? TapDecision.Success
                    : TapDecision.Failure);
            }

            return TapDecision.Pending;
        }

        private TapDecision Finish(TapDecision decision)
        {
            Phase = TapJudgePhase.Finished;
            return decision;
        }
    }
}

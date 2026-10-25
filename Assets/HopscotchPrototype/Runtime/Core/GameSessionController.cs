using System.Collections;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public enum GamePhase
    {
        WaitingToStart,
        CountingDown,
        Playing,
        GameOver
    }

    public sealed class GameSessionController : MonoBehaviour
    {
        private TapInputReader input;
        private IGameMode gameMode;
        private ScoreController score;
        private DifficultyController difficulty;
        private PrototypeHud hud;
        private RhythmClock rhythm;

        public GamePhase Phase { get; private set; }

        public void Configure(
            TapInputReader inputReader,
            IGameMode activeGameMode,
            ScoreController scoreController,
            DifficultyController difficultyController,
            PrototypeHud prototypeHud,
            RhythmClock rhythmClock = null)
        {
            input = inputReader;
            gameMode = activeGameMode;
            score = scoreController;
            difficulty = difficultyController;
            hud = prototypeHud;
            rhythm = rhythmClock;

            input.Tapped += OnTap;
            gameMode.StepCompleted += OnStepCompleted;
            gameMode.Failed += OnFailed;
            score.ScoreChanged += hud.SetScore;

            Phase = GamePhase.WaitingToStart;
            score.ResetScore();
            difficulty.ApplyScore(score.Score);
            hud.ShowWaitingToStart();
        }

        private void Update()
        {
            if (Phase == GamePhase.Playing)
            {
                gameMode.Tick(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (input != null)
            {
                input.Tapped -= OnTap;
            }

            if (gameMode != null)
            {
                gameMode.StepCompleted -= OnStepCompleted;
                gameMode.Failed -= OnFailed;
            }

            if (score != null)
            {
                score.ScoreChanged -= hud.SetScore;
            }
        }

        private void BeginRun()
        {
            Phase = GamePhase.Playing;
            score.ResetScore();
            difficulty.ApplyScore(score.Score);
            hud.ShowPlaying();
            if (rhythm != null) rhythm.StartMusic(difficulty.Current.BeatsPerMinute / difficulty.Current.SpeedMultiplier);
            gameMode.BeginRun();
        }

        public void StartCountdown()
        {
            if (Phase != GamePhase.WaitingToStart && Phase != GamePhase.GameOver)
            {
                return;
            }

            Phase = GamePhase.CountingDown;
            score.ResetScore();
            difficulty.ResetProgression();
            if (rhythm != null) rhythm.SetPlaybackSpeed(1f);
            difficulty.ApplyScore(score.Score);
            StartCoroutine(CountdownThenBeginRun());
        }

        private IEnumerator CountdownThenBeginRun()
        {
            for (int count = 3; count >= 1; count--)
            {
                hud.ShowCountdown(count);
                if (rhythm != null) rhythm.PlayCountdown(count);
                yield return new WaitForSecondsRealtime(1f);
            }

            BeginRun();
        }

        private void OnTap()
        {
            if (Phase == GamePhase.WaitingToStart || Phase == GamePhase.GameOver)
            {
                StartCountdown();
                return;
            }

            if (Phase == GamePhase.Playing)
            {
                gameMode.SubmitTap();
            }
        }

        private void OnStepCompleted()
        {
            score.AddPoint();
            difficulty.ApplyScore(score.Score);
        }

        private void OnFailed(string reason)
        {
            if (Phase != GamePhase.Playing)
            {
                return;
            }

            Phase = GamePhase.GameOver;
            if (rhythm != null) rhythm.StopMusic();
            score.SaveBestScore();
            hud.ShowGameOver(score.Score, reason, score.BestScore);
        }
    }
}

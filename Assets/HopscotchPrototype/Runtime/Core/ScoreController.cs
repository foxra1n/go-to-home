using System;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class ScoreController : MonoBehaviour
    {
        public event Action<int> ScoreChanged;

        public int Score { get; private set; }
        private LocalBestScore best;
        public int BestScore => best.Value;
        private void Awake() => best = new LocalBestScore();
        public void SaveBestScore() => best.Save();
        private void OnApplicationPause(bool paused) { if (paused) best.Save(); }
        private void OnApplicationQuit() => best.Save();

        public void ResetScore()
        {
            Score = 0;
            ScoreChanged?.Invoke(Score);
        }

        public void AddPoint()
        {
            Score++;
            best.Record(Score);
            ScoreChanged?.Invoke(Score);
        }
    }
}

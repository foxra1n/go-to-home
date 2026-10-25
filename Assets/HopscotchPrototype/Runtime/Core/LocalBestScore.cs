using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class LocalBestScore
    {
        public const string DefaultKey = "Homeward.BestScore.v1";
        private readonly string key;
        private bool dirty;
        public int Value { get; private set; }

        public LocalBestScore(string storageKey = DefaultKey)
        {
            key = storageKey;
            Value = Mathf.Max(0, PlayerPrefs.GetInt(key, 0));
        }

        public void Record(int score)
        {
            if (score <= Value) return;
            Value = score;
            PlayerPrefs.SetInt(key, Value);
            dirty = true;
        }

        public void Save()
        {
            if (!dirty) return;
            PlayerPrefs.Save();
            dirty = false;
        }
    }
}

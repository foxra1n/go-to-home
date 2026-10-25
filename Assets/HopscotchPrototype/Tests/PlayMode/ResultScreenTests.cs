using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Homeward.Hopscotch.Tests
{
    public sealed class ResultScreenTests
    {
        [UnityTest]
        public IEnumerator ScoreBadgeFitsViewportAndFollowsHudState()
        {
            var root = new GameObject("ScoreBadgeTest");
            var camera = Camera.main;
            Rect original = camera.rect;
            try
            {
                var hud = root.AddComponent<PrototypeHud>(); hud.Initialize();
                var badge = (RectTransform)root.transform.Find("ScoreBadge");
                var text = badge.GetComponentInChildren<UnityEngine.UI.Text>();
                Assert.That(badge.GetComponentsInChildren<UnityEngine.UI.Image>(), Is.Empty);
                Assert.That(text.GetComponent<UnityEngine.UI.Shadow>(), Is.Not.Null);
                Assert.That(text.GetComponent<UnityEngine.UI.Outline>(), Is.Not.Null);
                Assert.That(text.color, Is.EqualTo(Color.white));
                Assert.That(text.resizeTextMaxSize, Is.EqualTo(132));
                hud.ShowPlaying();
                foreach (float width in new[]{1f, .56f, .16f})
                {
                    camera.rect = new Rect((1-width)*.5f, 0, width, 1);
                    foreach (int score in new[]{0, 9, 100, 999999, int.MaxValue})
                    {
                        hud.SetScore(score);
                        hud.SendMessage("LateUpdate"); Canvas.ForceUpdateCanvases();
                        Assert.That(text.text, Is.EqualTo(score.ToString()));
                        var corners = new Vector3[4]; badge.GetWorldCorners(corners);
                        Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(camera.pixelRect.xMin));
                        Assert.That(corners[2].x, Is.LessThanOrEqualTo(camera.pixelRect.xMax));
                        Assert.That(corners[2].y, Is.LessThanOrEqualTo(Screen.safeArea.yMax));
                        Assert.That(badge.rect.height, Is.GreaterThan(0));
                    }
                }
                Assert.That(text.color.r, Is.GreaterThan(.9f));
                foreach (var graphic in badge.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                    Assert.That(graphic.raycastTarget, Is.False);
                hud.ShowGameOver(1, "test", 2); Assert.That(badge.gameObject.activeSelf, Is.False);
                hud.ShowCountdown(3); Assert.That(badge.gameObject.activeSelf, Is.True);
                yield return null;
            }
            finally { camera.rect = original; Object.Destroy(root); }
        }

        [Test]
        public void BestScorePersistsAndNeverDecreases()
        {
            string key = "Homeward.Test.Best." + System.Guid.NewGuid();
            try
            {
                var store = new LocalBestScore(key);
                Assert.That(store.Value, Is.Zero);
                store.Record(12); store.Save();
                var reloaded = new LocalBestScore(key);
                Assert.That(reloaded.Value, Is.EqualTo(12));
                reloaded.Record(3); reloaded.Record(-1); reloaded.Save();
                Assert.That(new LocalBestScore(key).Value, Is.EqualTo(12));
                reloaded.Record(19); reloaded.Save();
                Assert.That(new LocalBestScore(key).Value, Is.EqualTo(19));
            }
            finally { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
        }

        [UnityTest]
        public IEnumerator ResultFitsViewportAndHidesDuringCountdown()
        {
            var root = new GameObject("ResultHudTest");
            var camera = Camera.main;
            Rect original = camera.rect;
            try
            {
                var hud = root.AddComponent<PrototypeHud>(); hud.Initialize();
                hud.ShowGameOver(12,"test",34);
                foreach(float width in new[]{1f,.56f,.16f})
                {
                    camera.rect = new Rect((1-width)*.5f,0,width,1);
                    hud.SendMessage("LateUpdate"); Canvas.ForceUpdateCanvases();
                    foreach(var text in root.GetComponentsInChildren<UnityEngine.UI.Text>())
                    {
                        var corners = new Vector3[4]; text.rectTransform.GetWorldCorners(corners);
                        Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(camera.pixelRect.xMin-.1f));
                        Assert.That(corners[2].x, Is.LessThanOrEqualTo(camera.pixelRect.xMax+.1f));
                        Assert.That(text.rectTransform.rect.height, Is.GreaterThan(0));
                    }
                }
                Assert.That(root.GetComponentsInChildren<UnityEngine.UI.Text>().Single(t=>t.name=="ResultScores").text,
                    Is.EqualTo("SCORE  12\nBEST  34"));
                hud.ShowCountdown(3);
                Assert.That(root.transform.Find("GameOverPanel").gameObject.activeSelf, Is.False);
                yield return null;
            }
            finally { camera.rect=original; Object.Destroy(root); }
        }

        private sealed class FakeMode : IGameMode
        {
            public event System.Action StepCompleted { add {} remove {} }
            public event System.Action<string> Failed;
            public int Starts;
            public void BeginRun() { Starts++; }
            public void SubmitTap() {}
            public void Tick(float dt) {}
            public void Fail() => Failed?.Invoke("test");
        }

        [UnityTest]
        public IEnumerator OneTapAfterFailRestartsThroughOneCountdown()
        {
            var root = new GameObject("RetrySessionTest");
            try
            {
                var hudObject = new GameObject("RetryHud"); hudObject.transform.SetParent(root.transform);
                var hud = hudObject.AddComponent<PrototypeHud>(); hud.Initialize();
                var score = root.AddComponent<ScoreController>();
                var difficulty = root.AddComponent<DifficultyController>();
                var input = root.AddComponent<TapInputReader>(); input.enabled=false;
                var mode = new FakeMode();
                var session = root.AddComponent<GameSessionController>();
                session.Configure(input,mode,score,difficulty,hud);
                session.StartCountdown(); yield return new WaitForSecondsRealtime(3.1f);
                Assert.That(mode.Starts,Is.EqualTo(1));
                mode.Fail(); Assert.That(session.Phase,Is.EqualTo(GamePhase.GameOver));
                session.SendMessage("OnTap");
                Assert.That(session.Phase,Is.EqualTo(GamePhase.CountingDown));
                session.SendMessage("OnTap"); // Repeated taps cannot schedule extra restarts.
                yield return new WaitForSecondsRealtime(3.1f);
                Assert.That(mode.Starts,Is.EqualTo(2));
                Assert.That(session.Phase,Is.EqualTo(GamePhase.Playing));
                Assert.That(score.Score,Is.Zero);
            }
            finally { Object.Destroy(root); }
        }
    }
}

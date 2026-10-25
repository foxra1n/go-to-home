using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Homeward.Hopscotch.Tests
{
    public sealed class PrototypeBootstrapPlayModeTests
    {
        [UnityTest]
        public IEnumerator StartPromptFitsNarrowCameraViewports()
        {
            yield return null;
            var hud = Object.FindFirstObjectByType<PrototypeHud>();
            var camera = Camera.main;
            Rect originalRect = camera.rect;
            var text = hud.GetComponentsInChildren<UnityEngine.UI.Text>(true)
                .Single(item => item.name == "StartCountdown");
            hud.ShowWaitingToStart();
            try
            {
                foreach (float width in new[] { 1f, 0.56f, 0.16f })
                {
                    camera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
                    hud.SendMessage("LateUpdate");
                    Canvas.ForceUpdateCanvases();
                    Vector3[] corners = new Vector3[4];
                    text.rectTransform.GetWorldCorners(corners);
                    Assert.That(corners[0].x, Is.GreaterThanOrEqualTo(camera.pixelRect.xMin));
                    Assert.That(corners[2].x, Is.LessThanOrEqualTo(camera.pixelRect.xMax));
                    var generator = new TextGenerator();
                    generator.Populate(text.text, text.GetGenerationSettings(text.rectTransform.rect.size));
                    Assert.That(generator.lineCount, Is.EqualTo(1));
                    Assert.That(generator.characterCountVisible, Is.EqualTo(text.text.Length));
                }
            }
            finally { camera.rect = originalRect; hud.SendMessage("LateUpdate"); }
        }

        [UnityTest]
        public IEnumerator EntryCuesReserveTwoBeatsBeforeFirstTap()
        {
            GameObject testObject = new GameObject("EntryCueClockTest");
            RhythmClock clock = testObject.AddComponent<RhythmClock>();
            clock.Initialize();
            clock.StartMusic(88f);
            clock.BeginRun(88f);
            RhythmWindow window = clock.ScheduleNextWindow(88f, 1);
            Assert.That(window.FirstTapDspTime, Is.EqualTo(clock.EntryTapDspTime).Within(0.00001d));
            Assert.That(window.FirstTapDspTime - clock.EntryFirstBeatDspTime,
                Is.EqualTo(clock.BeatDuration * 2d).Within(0.00001d));
            Assert.That(window.FirstTapDspTime, Is.GreaterThan(clock.DspTime + 1.2d));
            // Two preparation clicks and two pattern clicks need independent scheduled sources.
            Assert.That(testObject.GetComponents<AudioSource>().Length, Is.EqualTo(6));
            Object.Destroy(testObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FiniteSectionsAreAheadAndTransitionsPreserveForwardProgress()
        {
            yield return null;
            GameSessionController session = Object.FindFirstObjectByType<GameSessionController>();
            session.enabled = false;
            GameModeSequenceController sequence = Object.FindFirstObjectByType<GameModeSequenceController>();
            PlayerHopController player = Object.FindFirstObjectByType<PlayerHopController>();
            HopscotchModeController hop = Object.FindFirstObjectByType<HopscotchModeController>();
            CurbBalanceModeController curb = Object.FindFirstObjectByType<CurbBalanceModeController>();
            CrosswalkModeController cross = Object.FindFirstObjectByType<CrosswalkModeController>();
            var difficulty = Object.FindFirstObjectByType<DifficultyController>();
            difficulty.TestMaximumDifficulty = false;
            var rhythm = Object.FindFirstObjectByType<RhythmClock>();
            sequence.BeginRun();
            float previousZ = 0f;
            int obstaclesInSet = 0;
            var modesInSet = new System.Collections.Generic.HashSet<PrototypeGameMode>();
            float[] expectedSpeeds = { 1f, 1f, 1f, 1f, 1.1f, 1.2f, 1.3f, 1.4f, 1.4f };
            for (int i = 0; i < 27; i++)
            {
                // Let the automatic curb-entry jump finish before simulating section completion.
                yield return new WaitForSeconds(0.65f);
                ISectionGameMode current = sequence.CurrentMode == PrototypeGameMode.Hopscotch ? (ISectionGameMode)hop
                    : sequence.CurrentMode == PrototypeGameMode.CurbBalance ? (ISectionGameMode)curb : cross;
                Assert.That(sequence.CompletedSections, Is.EqualTo(i));
                Assert.That(difficulty.Current.SpeedMultiplier, Is.EqualTo(expectedSpeeds[i / 3]).Within(.0001f), "Lookahead must not alter the active speed");
                Assert.That(rhythm.PlaybackSpeed, Is.EqualTo(expectedSpeeds[i / 3]).Within(.0001f));
                modesInSet.Add(sequence.CurrentMode);
                bool obstacle = current == hop ? hop.ObstaclesEnabled : current == curb ? curb.ObstaclesEnabled : cross.ObstaclesEnabled;
                if (obstacle) obstaclesInSet++;
                if (i % 3 == 2)
                {
                    Assert.That(modesInSet.Count, Is.EqualTo(3));
                    Assert.That(obstaclesInSet, Is.EqualTo(Mathf.Min(i / 3, 3)));
                    modesInSet.Clear(); obstaclesInSet = 0;
                }
                Assert.That(current.EndZ, Is.GreaterThan(previousZ));
                SectionScenery scenery = Object.FindFirstObjectByType<SectionScenery>();
                SceneryTheme? theme = scenery.ThemeAt(current.EndZ - .01f);
                Assert.That(theme, Is.Not.Null);
                if (sequence.CurrentMode == PrototypeGameMode.Crosswalk) Assert.That(theme, Is.EqualTo(SceneryTheme.Road));
                else Assert.That(theme, Is.Not.EqualTo(SceneryTheme.Road));
                player.transform.position = new Vector3(0f, 0.22f, current.EndZ);
                PrototypeGameMode oldMode = sequence.CurrentMode;
                sequence.SendMessage("CompleteSection");
                sequence.SubmitTap();
                sequence.Tick(1f);
                Assert.That(sequence.IsPreparingNextSection, Is.True);
                Assert.That(sequence.CurrentMode, Is.EqualTo(oldMode));
                sequence.Tick(0.25f);
                Assert.That(sequence.IsPreparingNextSection, Is.False);
                Assert.That(sequence.CurrentMode, Is.Not.EqualTo(oldMode));
                Assert.That(player.transform.position.z, Is.GreaterThan(previousZ));
                previousZ = player.transform.position.z;
            }
            hop.EndRun(); curb.EndRun(); cross.EndRun();
            player.ResetPlayer();
            sequence.BeginRun();
            Assert.That(sequence.SetNumber, Is.EqualTo(1));
            Assert.That(difficulty.Current.SpeedMultiplier, Is.EqualTo(1f));
            Assert.That(rhythm.PlaybackSpeed, Is.EqualTo(1f));
            hop.EndRun(); curb.EndRun(); cross.EndRun(); player.ResetPlayer();
            difficulty.TestMaximumDifficulty = true;
            sequence.BeginRun();
            Assert.That(difficulty.Current.SpeedMultiplier, Is.EqualTo(1.4f));
            Assert.That(rhythm.PlaybackSpeed, Is.EqualTo(1.4f));
            Assert.That(difficulty.HasObstacle(0, sequence.CurrentMode), Is.True);
            hop.EndRun(); curb.EndRun(); cross.EndRun(); player.ResetPlayer();
        }

        [UnityTest]
        public IEnumerator FiniteGeneratorsStopAppendingAndCanBeReusedAhead()
        {
            yield return null;
            Object.FindFirstObjectByType<GameSessionController>().enabled = false;
            HopscotchPatternGenerator hop = Object.FindFirstObjectByType<HopscotchPatternGenerator>(FindObjectsInactive.Include);
            CrosswalkPatternGenerator cross = Object.FindFirstObjectByType<CrosswalkPatternGenerator>(FindObjectsInactive.Include);
            hop.gameObject.SetActive(true);
            cross.gameObject.SetActive(true);
            hop.BeginSection(50f, 7);
            Assert.That(hop.Current.LandingType, Is.EqualTo(LandingType.Single));
            float hopEnd = hop.EndZ;
            for (int i = 0; i < 7; i++) hop.Advance();
            Assert.That(hop.IsComplete, Is.True);
            Assert.That(hop.GetComponentsInChildren<HopscotchPattern>().Length, Is.EqualTo(7));
            hop.BeginSection(hopEnd + 3f, 7);
            Assert.That(hop.Current.LandingType, Is.EqualTo(LandingType.Single));
            Assert.That(hop.Current.LandingPosition.z, Is.GreaterThan(hopEnd));
            cross.BeginSection(100f, 7);
            for (int i = 0; i < 6; i++) cross.Advance();
            Assert.That(cross.IsLastStripe, Is.True);
            Assert.That(cross.ActiveStripeCount, Is.EqualTo(7));
            Assert.That(cross.CurrentEndZ, Is.EqualTo(cross.EndZ).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator BootstrapCreatesAPlayablePrototype()
        {
            yield return null;

            Assert.That(Object.FindFirstObjectByType<HopscotchPrototypeBootstrap>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PlayerHopController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<PrototypeHud>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<RhythmClock>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<CurbBalanceModeController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<CrosswalkModeController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<GameModeSequenceController>(), Is.Not.Null);

            GameSessionController session = Object.FindFirstObjectByType<GameSessionController>();
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.WaitingToStart));

            PrototypeHud hud = Object.FindFirstObjectByType<PrototypeHud>();
            UnityEngine.UI.Text[] gameplayText = hud.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            Assert.That(gameplayText.Length, Is.EqualTo(5));
            Assert.That(gameplayText.Single(text => text.name == "Score").text, Is.EqualTo("0"));
            UnityEngine.UI.Text countdownText = gameplayText.Single(text => text.name == "StartCountdown");
            Assert.That(countdownText.text.Replace('\u00a0', ' '), Is.EqualTo("TAP TO START"));
            Assert.That(countdownText.gameObject.activeInHierarchy, Is.True);
            UnityEngine.UI.Text failText = gameplayText.Single(text => text.name == "Fail");
            Assert.That(failText.text, Is.EqualTo("FAIL"));
            Assert.That(failText.gameObject.activeInHierarchy, Is.False);

            session.StartCountdown();
            Assert.That(session.Phase, Is.EqualTo(GamePhase.CountingDown));
            Assert.That(countdownText.text, Is.EqualTo("3"));

            yield return new WaitForSecondsRealtime(1.05f);
            Assert.That(countdownText.text, Is.EqualTo("2"));

            yield return new WaitForSecondsRealtime(2.05f);
            Assert.That(session.Phase, Is.EqualTo(GamePhase.Playing));
            Assert.That(countdownText.gameObject.activeInHierarchy, Is.False);

            PlayerHopController player = Object.FindFirstObjectByType<PlayerHopController>();
            // Initial crosswalk now includes a safe red-light wait on the sidewalk.
            float timeoutAt = Time.realtimeSinceStartup + 8f;
            while (session.Phase != GamePhase.GameOver && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(session.Phase, Is.EqualTo(GamePhase.GameOver));
            yield return new WaitForSeconds(0.5f);

            bool fellForward = Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.x, 80f)) < 1f;
            bool fellSideways = Mathf.Abs(Mathf.Abs(Mathf.DeltaAngle(0f, player.transform.eulerAngles.z)) - 92f) < 1f;
            Assert.That(fellForward || fellSideways, Is.True);
            Assert.That(failText.gameObject.activeInHierarchy, Is.True);

            player.ResetPlayer();
            hud.ShowPlaying();
        }

        [UnityTest]
        public IEnumerator HopscotchFailureStillFallsForward()
        {
            GameObject testObject = new GameObject("ForwardFallTestPlayer");
            PlayerHopController player = testObject.AddComponent<PlayerHopController>();
            player.Initialize();
            float startingZ = player.transform.position.z;

            player.ShowFailurePose();
            yield return new WaitForSeconds(0.5f);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.x, 80f)), Is.LessThan(1f));
            Assert.That(player.transform.position.z, Is.GreaterThan(startingZ + 0.35f));
            Object.Destroy(testObject);
        }

        [UnityTest]
        public IEnumerator SurfaceTransitionsJumpUpAndDownWithDoubleLanding()
        {
            GameObject testObject = new GameObject("SurfaceTransitionTestPlayer");
            PlayerHopController player = testObject.AddComponent<PlayerHopController>();
            player.Initialize();
            Vector3 groundPosition = player.transform.position;
            Vector3 curbPosition = groundPosition + new Vector3(0f, 0.42f, 1.2f);

            player.JumpToSurface(curbPosition, 0.3f, null);
            yield return new WaitForSeconds(0.4f);
            Assert.That(Vector3.Distance(player.transform.position, curbPosition), Is.LessThan(0.01f));

            Vector3 landingPosition = new Vector3(0f, groundPosition.y, 2.4f);
            player.JumpToSurface(landingPosition, 0.3f, null);
            yield return new WaitForSeconds(0.4f);
            Assert.That(Vector3.Distance(player.transform.position, landingPosition), Is.LessThan(0.01f));

            Object.Destroy(testObject);
        }

        [UnityTest]
        public IEnumerator CurbFailureStillFallsSideways()
        {
            GameObject testObject = new GameObject("SideFallTestPlayer");
            PlayerHopController player = testObject.AddComponent<PlayerHopController>();
            player.Initialize();
            float startingX = player.transform.position.x;

            player.ShowSideFailure(1f);
            yield return new WaitForSeconds(0.5f);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.z, -92f)), Is.LessThan(1f));
            Assert.That(player.transform.position.x, Is.GreaterThan(startingX + 0.5f));
            Object.Destroy(testObject);
        }

        [UnityTest]
        public IEnumerator CrosswalkIgnoresAirTapsButRapidGroundTapLandsInTheGap()
        {
            yield return null;

            CrosswalkModeController crosswalk = Object.FindFirstObjectByType<CrosswalkModeController>();
            CrosswalkPatternGenerator generator = Object.FindFirstObjectByType<CrosswalkPatternGenerator>(FindObjectsInactive.Include);
            PlayerHopController player = Object.FindFirstObjectByType<PlayerHopController>();
            GameSessionController session = Object.FindFirstObjectByType<GameSessionController>();
            Assert.That(crosswalk, Is.Not.Null);
            Assert.That(generator, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(session, Is.Not.Null);
            session.enabled = false;
            generator.gameObject.SetActive(true);
            crosswalk.BeginRun();
            int completedSteps = 0;
            crosswalk.StepCompleted += () => completedSteps++;
            player.transform.position = new Vector3(
                player.transform.position.x,
                player.transform.position.y,
                generator.CurrentEndZ);

            crosswalk.SubmitTap();
            Assert.That(crosswalk.Phase, Is.EqualTo(CrosswalkPhase.Jumping));

            crosswalk.SubmitTap();
            crosswalk.SubmitTap();
            crosswalk.SubmitTap();
            Assert.That(completedSteps, Is.Zero);
            Assert.That(crosswalk.Phase, Is.EqualTo(CrosswalkPhase.Jumping));

            yield return new WaitForSeconds(0.42f);

            Assert.That(completedSteps, Is.EqualTo(1));
            Assert.That(crosswalk.Phase, Is.EqualTo(CrosswalkPhase.Walking));

            crosswalk.SubmitTap();
            Assert.That(crosswalk.Phase, Is.EqualTo(CrosswalkPhase.Jumping));

            yield return new WaitForSeconds(0.42f);

            Assert.That(completedSteps, Is.EqualTo(1));
            Assert.That(crosswalk.Phase, Is.EqualTo(CrosswalkPhase.Failed));
        }
    }
}

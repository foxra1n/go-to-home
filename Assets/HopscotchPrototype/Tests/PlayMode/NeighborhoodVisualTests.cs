using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Homeward.Hopscotch.Tests
{
    public sealed class NeighborhoodVisualTests
    {
        [UnityTest]
        public IEnumerator FirstCrosswalkApproachesFromSidewalkWithoutQueuingTaps()
        {
            yield return null;
            var session = Object.FindFirstObjectByType<GameSessionController>();
            bool wasEnabled = session.enabled;
            session.enabled = false;
            try
            {
            var sequence = Object.FindFirstObjectByType<GameModeSequenceController>();
            Object.FindFirstObjectByType<DifficultyController>().TestMaximumDifficulty = false;
            var player = Object.FindFirstObjectByType<PlayerHopController>();
            var cross = Object.FindFirstObjectByType<CrosswalkModeController>();
            var generator = Object.FindFirstObjectByType<CrosswalkPatternGenerator>(FindObjectsInactive.Include);
            bool found = false;
            for (int i = 0; i < 100; i++)
            {
                sequence.BeginRun();
                if (sequence.CurrentMode == PrototypeGameMode.Crosswalk) { found = true; break; }
            }
            Assert.That(found, Is.True, "Shuffle did not produce a crosswalk start");
            Assert.That(sequence.IsEnteringInitialSection, Is.True);
            Assert.That(player.transform.position.z, Is.Zero);
            Assert.That(generator.CurrentStartZ, Is.GreaterThan(2.5f));
            var apron = Object.FindFirstObjectByType<SectionScenery>().transform.Find("InitialCrosswalkSidewalk");
            Assert.That(apron.gameObject.activeSelf, Is.True);
            Assert.That(apron.Find("SidewalkApron").GetComponent<Renderer>().bounds.min.z, Is.LessThan(0));
            sequence.SubmitTap(); sequence.SubmitTap();
            sequence.Tick(.5f);
            Assert.That(player.transform.position.z, Is.LessThan(generator.CurrentStartZ));
            Assert.That(sequence.IsEnteringInitialSection, Is.True);
            sequence.Tick(1f);
            Assert.That(sequence.IsEnteringInitialSection, Is.False);
            Assert.That(cross.ObstaclesEnabled, Is.False, "First set has no signal obstacle");
            Assert.That(cross.Phase, Is.EqualTo(CrosswalkPhase.Entering));
            Assert.That(player.transform.position.z, Is.EqualTo(cross.EntryWaitZ).Within(.001f));
            float takeoffZ = player.transform.position.z;
            sequence.SubmitTap(); sequence.Tick(.01f);
            Assert.That(cross.Phase, Is.EqualTo(CrosswalkPhase.Jumping));
            sequence.SubmitTap();
            yield return new WaitForSeconds(.5f);
            Assert.That(cross.Phase, Is.EqualTo(CrosswalkPhase.Walking));
            Assert.That(player.transform.position.z, Is.EqualTo(takeoffZ + generator.JumpDistance).Within(.001f));
            sequence.Tick(.15f);
            Assert.That(cross.Phase, Is.EqualTo(CrosswalkPhase.Walking));
            Assert.That(player.transform.position.y, Is.EqualTo(.22f).Within(.001f));
            }
            finally { session.enabled = wasEnabled; }
        }

        [UnityTest]
        public IEnumerator CrosswalkEntryStartsAtRearWithoutChangingJumpLanding()
        {
            var root = new GameObject("CrosswalkEntryTest");
            try
            {
                var generator = root.AddComponent<CrosswalkPatternGenerator>();
                generator.Initialize();
                foreach (float start in new[] { 0f, 50f, 300f })
                {
                    generator.BeginSection(start, 7);
                    float back = generator.CurrentCenterZ - generator.StripeLength * .5f;
                    Assert.That(start - back, Is.EqualTo(generator.StripeLength * .1f).Within(.001f));
                    Assert.That(generator.CurrentEndZ - start, Is.EqualTo(generator.StripeLength * .9f).Within(.001f));
                    float target = generator.JumpTargetFrom(new Vector3(0, .22f, generator.CurrentEndZ)).z;
                    Assert.That(generator.TryResolveNextLanding(target, out float landed), Is.True);
                    generator.Advance();
                    float nextBack = generator.CurrentCenterZ - generator.StripeLength * .5f;
                    Assert.That(landed - nextBack, Is.EqualTo(generator.StripeLength * .4f).Within(.001f));
                }
                generator.BeginRun(0);
                Assert.That(generator.CurrentEndZ, Is.EqualTo(generator.StripeLength * .9f).Within(.001f));
                yield return null;
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator SectionThemesMatchRoadAndRecycleWithoutGameplaySideEffects()
        {
            var root = new GameObject("SectionSceneryTest");
            var state = Random.state;
            try
            {
                float expected = Random.value;
                Random.state = state;
                var scenery = root.AddComponent<SectionScenery>();
                scenery.Configure(null);
                Assert.That(Random.value, Is.EqualTo(expected));
                yield return null;
                Assert.That(root.GetComponentsInChildren<Collider>(true).Length, Is.Zero);
                int renderers = root.GetComponentsInChildren<MeshRenderer>(true).Length;
                scenery.ResetSections();
                int neighborhoods = 0;
                for (int i = 0; i < 90; i++)
                {
                    var mode = i % 3 == 1 ? PrototypeGameMode.Crosswalk : PrototypeGameMode.Hopscotch;
                    scenery.PrepareSection(mode, i * 30, i * 30 + 27);
                    SceneryTheme theme = mode == PrototypeGameMode.Crosswalk ? SceneryTheme.Road
                        : neighborhoods++ % 2 == 0 ? SceneryTheme.Residential : SceneryTheme.School;
                    Assert.That(scenery.ThemeAt(i * 30 + 15), Is.EqualTo(theme));
                    Assert.That(scenery.ActiveSectionCount, Is.LessThanOrEqualTo(3));
                    Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(renderers));
                    if (i > 0) Assert.That(scenery.ThemeAt(i * 30 - .01f), Is.Not.Null);
                }
                scenery.ResetSections();
                Assert.That(scenery.ActiveSectionCount, Is.Zero);
                scenery.PrepareSection(PrototypeGameMode.CurbBalance, 0, 27);
                Assert.That(scenery.ThemeAt(0), Is.EqualTo(SceneryTheme.Residential));
            }
            finally { Random.state = state; Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator SceneryRecyclesWithoutCollidersOrRandomSideEffects()
        {
            var root = new GameObject("SceneryTest");
            var target = new GameObject("SceneryTestTarget");
            var state = Random.state;
            try
            {
                float expected = Random.value;
                Random.state = state;
                var scenery = root.AddComponent<NeighborhoodScenery>();
                scenery.Configure(target.transform);
                Assert.That(Random.value, Is.EqualTo(expected));
                yield return null;
                Assert.That(root.GetComponentsInChildren<Collider>(true).Length, Is.Zero);
                int count = root.GetComponentsInChildren<MeshRenderer>().Length;
                Assert.That(count, Is.LessThanOrEqualTo(NeighborhoodScenery.ChunkCount * 10));
                foreach (float z in new[] { 19f, 230f, 4000f, 0f })
                {
                    target.transform.position = Vector3.forward * z;
                    scenery.SendMessage("LateUpdate");
                    Assert.That(root.transform.childCount, Is.EqualTo(NeighborhoodScenery.ChunkCount));
                    Assert.That(root.GetComponentsInChildren<MeshRenderer>().Length, Is.EqualTo(count));
                    var positions = root.transform.Cast<Transform>().Select(t => t.position.z).OrderBy(v => v).ToArray();
                    Assert.That(positions.First(), Is.LessThanOrEqualTo(z));
                    Assert.That(positions.Last(), Is.GreaterThan(z + 4 * NeighborhoodScenery.ChunkLength));
                    for (int i = 1; i < positions.Length; i++)
                        Assert.That(positions[i] - positions[i - 1], Is.EqualTo(NeighborhoodScenery.ChunkLength));
                }
            }
            finally { Random.state = state; Object.Destroy(root); Object.Destroy(target); }
        }

        [UnityTest]
        public IEnumerator StudentAccessoriesFollowTorsoAndFeetRemainSeparate()
        {
            var root = new GameObject("StudentVisualTest");
            try
            {
                var player = root.AddComponent<PlayerHopController>();
                player.Initialize();
                yield return null;
                Transform body = root.transform.Find("Body");
                Assert.That(body.Find("Backpack"), Is.Not.Null);
                Assert.That(body.Find("HairCap"), Is.Not.Null);
                Assert.That(root.transform.Find("LeftFoot/Sock"), Is.Not.Null);
                Assert.That(root.transform.Find("RightFoot/Sock"), Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<Collider>().Length, Is.Zero);
                player.SetReadyPulse(1f);
                Assert.That(body.localPosition.y, Is.EqualTo(.87f).Within(.001f));
                player.ResetPlayer();
                Assert.That(body.localPosition.y, Is.EqualTo(1.05f).Within(.001f));
            }
            finally { Object.Destroy(root); }
        }
    }
}

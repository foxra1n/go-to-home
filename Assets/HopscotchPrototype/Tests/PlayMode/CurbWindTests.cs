using NUnit.Framework;
using UnityEngine;

namespace Homeward.Hopscotch.Tests
{
    public sealed class CurbWindTests
    {
        [Test]
        public void SoftWindHasSmoothEdgesAndLimitedHighFrequencyEnergy()
        {
            AudioClip clip = CurbWindSound.Create();
            try
            {
                var samples = new float[clip.samples]; clip.GetData(samples, 0);
                double energy = 0, differenceEnergy = 0;
                for (int i = 1; i < samples.Length; i++)
                {
                    Assert.That(Mathf.Abs(samples[i]), Is.LessThan(1f));
                    energy += samples[i] * samples[i];
                    float difference = samples[i] - samples[i - 1];
                    differenceEnergy += difference * difference;
                }
                Assert.That(samples[0], Is.Zero);
                Assert.That(samples[samples.Length - 1], Is.Zero);
                Assert.That(Rms(clip), Is.EqualTo(.095f).Within(.001f));
                Assert.That(differenceEnergy / energy, Is.LessThan(.03));
            }
            finally { Object.Destroy(clip); }
        }

        [Test]
        public void WindPresentationFadesAndTreeReturnsToRest()
        {
            var tree = new GameObject("SwayTest");
            tree.transform.position = new Vector3(-5f, 2f, 4f);
            Vector3 rest = tree.transform.position;
            try
            {
                tree.AddComponent<WindTreeSway>();
                Assert.That(CurbWindVisual.PresentationEnvelope(2f), Is.Zero);
                Assert.That(CurbWindVisual.PresentationEnvelope(2.5f), Is.GreaterThan(0));
                WindTreeSway.Apply(Vector3.zero, 1f, 4f);
                Assert.That(tree.transform.position.x, Is.GreaterThan(rest.x));
                Assert.That(Quaternion.Angle(tree.transform.rotation, Quaternion.identity), Is.GreaterThan(1f));
                WindTreeSway.Apply(Vector3.zero, 0f, 6f);
                Assert.That(tree.transform.position, Is.EqualTo(rest));
                Assert.That(tree.transform.rotation, Is.EqualTo(Quaternion.identity));
                Assert.That(CurbWindVisual.PresentationEnvelope(5.4f), Is.Zero);
            }
            finally { Object.Destroy(tree); }
        }

        [Test]
        public void WindAudioIsBelowMusicAndStopsWhenCueHides()
        {
            var root = new GameObject("WindAudioTest");
            AudioClip music = PrototypeMusic.Create(88f);
            try
            {
                var cue = root.AddComponent<CurbWindVisual>(); cue.Initialize();
                cue.Show(Vector3.zero, 4f);
                var source = root.GetComponent<AudioSource>();
                Assert.That(source.volume, Is.EqualTo(CurbWindSound.MaximumVolume).Within(.001f));
                Assert.That(Rms(source.clip) * source.volume, Is.LessThan(Rms(music) * .6f));
                Assert.That(Rms(source.clip), Is.GreaterThan(.05f));
                cue.Show(Vector3.zero, 6f);
                Assert.That(source.isPlaying, Is.False);
                Assert.That(source.volume, Is.Zero);
                Assert.That(Object.FindObjectsByType<WindTreeSway>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length, Is.GreaterThan(0), "Scene trees must remain separate from combined static meshes.");
            }
            finally { Object.Destroy(root); Object.Destroy(music); }
        }

        private static float Rms(AudioClip clip)
        {
            var data = new float[clip.samples * clip.channels]; clip.GetData(data, 0);
            double sum = 0; foreach (float sample in data) sum += sample * sample;
            return Mathf.Sqrt((float)(sum / data.Length));
        }

        [Test]
        public void WarningPrecedesForceAndGustEnds()
        {
            Assert.That(CurbWindProfile.IsVisible(1.99f), Is.False);
            Assert.That(CurbWindProfile.IsVisible(2f), Is.True);
            Assert.That(CurbWindProfile.Strength(2.99f), Is.Zero);
            Assert.That(CurbWindProfile.Strength(3f), Is.Zero);
            Assert.That(CurbWindProfile.Strength(3.2f), Is.InRange(.01f, .34f));
            Assert.That(CurbWindProfile.Strength(4f), Is.EqualTo(.35f).Within(.001f));
            Assert.That(CurbWindProfile.Strength(5.4f), Is.Zero);
            Assert.That(CurbWindProfile.IsVisible(5.4f), Is.False);
        }

        [Test]
        public void WindDoesNotOverrideReversalOrReset()
        {
            var state = new CurbBalanceState(); state.Begin();
            state.Advance(.3f, 1f, .35f);
            float before = state.LeanNormalized;
            Assert.That(state.TryReverseDirection(.3f, .13f), Is.True);
            state.Advance(.1f, 1f, 100f);
            Assert.That(state.LeanNormalized, Is.LessThan(before));
            Assert.That(state.TryReverseDirection(.31f, .13f), Is.False);
            state.Begin();
            Assert.That(state.LeanNormalized, Is.Zero);
            Assert.That(state.LeanDirection, Is.EqualTo(1));
        }

        [TestCase(.7f)]
        [TestCase(-.7f)]
        public void TapCountersImmediatelyButWindPushesBackWithoutFurtherInput(float wind)
        {
            var state = new CurbBalanceState(); state.Begin();
            if (wind > 0f) state.TryReverseDirection(0f, .13f);
            state.Advance(.05f, 1f, wind);
            Assert.That(state.LeanNormalized * Mathf.Sign(wind), Is.LessThan(-.04f));
            for (int i = 0; i < 90; i++) state.Advance(1f / 60f, 1f, wind);
            Assert.That(state.LeanNormalized * Mathf.Sign(wind), Is.EqualTo(1f));
            Assert.That(state.LeanDirection, Is.EqualTo((int)Mathf.Sign(wind)));
        }

        [Test]
        public void CalmRestoresOriginalSpeedAndRestartClearsWindMomentum()
        {
            var state = new CurbBalanceState(); state.Begin();
            state.Advance(.1f, 1f, .7f);
            float before = state.LeanNormalized;
            state.Advance(.1f, 1f);
            Assert.That(state.LeanNormalized - before, Is.EqualTo(.1f).Within(.0001f));
            state.Begin(); state.Advance(.1f, 1f);
            Assert.That(state.LeanNormalized, Is.EqualTo(.1f).Within(.0001f));
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void OneTapCannotCoastThroughActualGust(int fps)
        {
            var state = new CurbBalanceState(); state.Begin();
            state.TryReverseDirection(0f, .13f);
            bool failed = false;
            for (int i = 0; i < 2.4f * fps; i++)
                failed |= state.Advance(1f / fps, 31f / 36f,
                    CurbWindProfile.Strength(3f + (i + .5f) / fps) * CurbWindProfile.DefaultForceMultiplier);
            Assert.That(failed, Is.True, "One counter tap must not cover the whole gust.");
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void VisibleLeanResponsiveTapsCanSurviveGust(int fps)
        {
            var state = new CurbBalanceState(); state.Begin();
            float step = 1f / fps;
            for (int i = 0; i < 9 * fps; i++)
            {
                float time = i * step;
                if (state.LeanNormalized * state.LeanDirection > .5f)
                    state.TryReverseDirection(time, .13f);
                Assert.That(state.Advance(step, 31f / 36f,
                    CurbWindProfile.Strength(time + step * .5f) * CurbWindProfile.DefaultForceMultiplier), Is.False);
            }
        }

        [Test]
        public void CueIsReusedAndHiddenOutsideGust()
        {
            var root = new GameObject("WindVisualTest");
            try
            {
                var cue = root.AddComponent<CurbWindVisual>(); cue.Initialize();
                int count = root.transform.childCount;
                cue.Show(Vector3.zero, 2.1f); Assert.That(root.activeSelf, Is.True);
                cue.Show(Vector3.one, 4f); Assert.That(root.transform.childCount, Is.EqualTo(count));
                foreach (var collider in root.GetComponentsInChildren<Collider>()) Assert.That(collider.enabled, Is.False);
                cue.Show(Vector3.one, 6f); Assert.That(root.activeSelf, Is.False);
                cue.Show(Vector3.zero, 0); Assert.That(root.activeSelf, Is.False);
            }
            finally { Object.Destroy(root); }
        }
    }
}

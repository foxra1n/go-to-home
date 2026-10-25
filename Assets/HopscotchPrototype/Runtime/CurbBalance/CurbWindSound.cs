using UnityEngine;

namespace Homeward.Hopscotch
{
    public static class CurbWindSound
    {
        public const float MaximumVolume = .15f;
        public static AudioClip Create()
        {
            const int rate = 22050;
            var samples = new float[rate * 4];
            var random = new System.Random(713);
            float lowPass = 0f, softened = 0f, rumble = 0f;
            double squares = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                // Two low-pass stages soften the hiss; remove bass rumble afterwards.
                lowPass += .12f * (noise - lowPass);
                softened += .12f * (lowPass - softened);
                rumble += .017f * (softened - rumble);
                float t = i / (float)rate;
                float breathing = .82f + .12f * Mathf.Sin(t * 2.1f) + .06f * Mathf.Sin(t * 3.7f + .8f);
                float edge = Mathf.Min(1f, i / (rate * .25f), (samples.Length - 1 - i) / (rate * .25f));
                float envelope = Mathf.SmoothStep(0f, 1f, edge);
                samples[i] = (softened - rumble) * breathing * envelope;
                squares += samples[i] * samples[i];
            }
            float gain = .095f / Mathf.Sqrt((float)(squares / samples.Length));
            for (int i = 0; i < samples.Length; i++) samples[i] *= gain;
            var clip = AudioClip.Create("SoftCurbWind", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

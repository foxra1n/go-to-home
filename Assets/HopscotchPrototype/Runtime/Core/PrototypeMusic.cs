using System;
using UnityEngine;

namespace Homeward.Hopscotch
{
    // Original, asset-free four-bar loop for the rhythm prototype.
    public static class PrototypeMusic
    {
        public const int SampleRate = 44100;
        public static int SamplesPerBeat(float bpm) => Mathf.RoundToInt(SampleRate * 60f / Mathf.Max(1f, bpm));

        public static AudioClip Create(float bpm)
        {
            int beatSamples = SamplesPerBeat(bpm);
            float[] samples = new float[beatSamples * 16];
            System.Random noise = new System.Random(88);
            float[] roots = { 130.81f, 110f, 87.31f, 98f }; // C, A, F, G
            for (int beat = 0; beat < 16; beat++)
            {
                int start = beat * beatSamples;
                float root = roots[beat / 4];
                AddTone(samples, start, root, 0.32f, 0.19f);
                if (beat % 2 == 0) AddDrum(samples, start, false, noise);
                else AddDrum(samples, start, true, noise);
                AddHat(samples, start, noise);
                AddHat(samples, start + beatSamples / 2, noise);
                float melody = root * (beat % 4 == 2 ? 3f : 4f);
                AddTone(samples, start + beatSamples / 2, melody, 0.20f, 0.07f);
            }
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Clamp(samples[i], -0.9f, 0.9f);
            AudioClip clip = AudioClip.Create("Homeward_Original_88BPM", samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void AddTone(float[] data, int start, float frequency, float duration, float gain)
        {
            int count = (int)(duration * SampleRate);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Min(1f, t / 0.006f) * Mathf.Pow(1f - i / (float)count, 2f);
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.2f * Mathf.Sin(4f * Mathf.PI * frequency * t);
                data[(start + i) % data.Length] += wave * envelope * gain;
            }
        }

        private static void AddDrum(float[] data, int start, bool snare, System.Random noise)
        {
            int count = (int)(0.16f * SampleRate);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Pow(1f - i / (float)count, 3f);
                float wave = snare ? (float)(noise.NextDouble() * 2d - 1d) * 0.17f
                    : Mathf.Sin(2f * Mathf.PI * (48f * t + 42f * 0.025f * (1f - Mathf.Exp(-t / 0.025f)))) * 0.40f;
                data[(start + i) % data.Length] += wave * envelope * Mathf.Min(1f, t / 0.002f);
            }
        }

        private static void AddHat(float[] data, int start, System.Random noise)
        {
            int count = (int)(0.035f * SampleRate);
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float value = (float)(noise.NextDouble() * 2d - 1d);
                float envelope = Mathf.Pow(1f - i / (float)count, 3f);
                data[(start + i) % data.Length] += (value - previous) * envelope * 0.035f;
                previous = value;
            }
        }
    }
}

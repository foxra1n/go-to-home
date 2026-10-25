using UnityEngine;

namespace Homeward.Hopscotch
{
    public readonly struct RhythmWindow
    {
        public RhythmWindow(double startDspTime, double duration, int tapCount)
        {
            StartDspTime = startDspTime;
            EndDspTime = startDspTime + duration;
            FirstTapDspTime = startDspTime + duration * 0.22d;
            SecondTapDspTime = tapCount > 1
                ? startDspTime + duration * 0.55d
                : double.PositiveInfinity;
        }

        public double StartDspTime { get; }
        public double EndDspTime { get; }
        public double FirstTapDspTime { get; }
        public double SecondTapDspTime { get; }

        public double TargetDspTime(int zeroBasedTapIndex)
        {
            return zeroBasedTapIndex == 0 ? FirstTapDspTime : SecondTapDspTime;
        }
    }

    public sealed class RhythmClock : MonoBehaviour
    {
        [Header("Prototype Beat")]
        [SerializeField, Range(0f, 1f)] private float clickVolume = 0.65f;

        private readonly AudioSource[] clickSources = new AudioSource[4];
        private AudioClip clickClip;
        private AudioSource countdownSource;
        private AudioSource musicSource;
        private AudioClip musicClip;
        private double musicStartDspTime;
        private double musicBeatSeconds;
        private float musicBpm;
        private bool musicRunning;
        public float PlaybackSpeed { get; private set; } = 1f;
        public double MusicBeatSeconds => musicBeatSeconds;

        public void SetPlaybackSpeed(float speed)
        {
            speed = Mathf.Clamp(speed, 1f, DifficultyController.MaximumSpeed);
            if (Mathf.Approximately(speed, PlaybackSpeed)) return;
            double now = DspTime;
            if (musicRunning)
            {
                // Preserve the song's current beat phase instead of restarting the loop.
                double beatsElapsed = (now - musicStartDspTime) / musicBeatSeconds;
                musicBeatSeconds *= PlaybackSpeed / speed;
                musicStartDspTime = now - beatsElapsed * musicBeatSeconds;
                musicSource.pitch = speed;
            }
            PlaybackSpeed = speed;
            StopScheduledClicks();
        }

        public void StartMusic(float bpm)
        {
            StopMusic();
            if (musicClip == null || !Mathf.Approximately(musicBpm, bpm))
            {
                if (musicClip != null) Destroy(musicClip);
                musicClip = PrototypeMusic.Create(bpm);
            }
            musicBpm = bpm;
            musicBeatSeconds = PrototypeMusic.SamplesPerBeat(bpm) / (double)PrototypeMusic.SampleRate;
            PlaybackSpeed = 1f;
            musicSource.pitch = 1f;
            musicStartDspTime = DspTime + 0.1d;
            musicSource.clip = musicClip;
            musicSource.PlayScheduled(musicStartDspTime);
            musicRunning = true;
        }

        public void StopMusic()
        {
            if (musicSource != null) musicSource.Stop();
            musicRunning = false;
            StopScheduledClicks();
        }

        public void PlayCountdown(int count)
        {
            countdownSource.pitch = count == 1 ? 1.25f : 1f;
            countdownSource.PlayOneShot(clickClip);
        }
        private double nextWindowStartDspTime;
        private double lastBeatDuration;
        private int nextClickSource;

        public double DspTime => AudioSettings.dspTime;
        public double EntryFirstBeatDspTime { get; private set; }
        public double EntryTapDspTime { get; private set; }
        public double BeatDuration => lastBeatDuration;

        public void Initialize()
        {
            clickClip = CreateClickClip();
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.loop = true;
            musicSource.volume = 0.6f;
            countdownSource = gameObject.AddComponent<AudioSource>();
            countdownSource.playOnAwake = false;
            countdownSource.spatialBlend = 0f;
            countdownSource.volume = clickVolume;
            for (int i = 0; i < clickSources.Length; i++)
            {
                clickSources[i] = gameObject.AddComponent<AudioSource>();
                clickSources[i].playOnAwake = false;
                clickSources[i].spatialBlend = 0f;
                clickSources[i].volume = clickVolume;
            }
        }

        public void BeginRun(float beatsPerMinute, float minimumFirstTapDelay = 0f)
        {
            StopScheduledClicks();
            lastBeatDuration = musicRunning ? musicBeatSeconds : SecondsPerBeat(beatsPerMinute);
            EntryFirstBeatDspTime = DspTime + System.Math.Max(0.1d, minimumFirstTapDelay - 2d * lastBeatDuration);
            if (musicRunning)
            {
                double beat = System.Math.Ceiling((EntryFirstBeatDspTime - musicStartDspTime) / musicBeatSeconds);
                EntryFirstBeatDspTime = musicStartDspTime + beat * musicBeatSeconds;
            }
            EntryTapDspTime = EntryFirstBeatDspTime + 2d * lastBeatDuration;
            ScheduleClick(EntryFirstBeatDspTime, 0.65f);
            ScheduleClick(EntryFirstBeatDspTime + lastBeatDuration, 0.75f);
            nextWindowStartDspTime = EntryTapDspTime - lastBeatDuration * 0.22d;
        }

        public void Stop()
        {
            StopScheduledClicks();
        }

        public RhythmWindow ScheduleNextWindow(float beatsPerMinute, int tapCount, float minimumFirstTapLead = 0f)
        {
            double now = DspTime;
            double beatDuration = musicRunning ? musicBeatSeconds : SecondsPerBeat(beatsPerMinute);
            double minimumLead = System.Math.Max(0.08d, minimumFirstTapLead - beatDuration * .22d);

            while (nextWindowStartDspTime < now + minimumLead)
            {
                nextWindowStartDspTime += lastBeatDuration;
            }

            RhythmWindow window = new RhythmWindow(nextWindowStartDspTime, beatDuration, tapCount);
            ScheduleClick(window.FirstTapDspTime, 1f);
            if (tapCount > 1)
            {
                ScheduleClick(window.SecondTapDspTime, 0.78f);
            }

            nextWindowStartDspTime = window.EndDspTime;
            lastBeatDuration = beatDuration;
            return window;
        }

        public static float PulseAt(double dspTime, RhythmWindow window, int tapCount)
        {
            float first = PulseForTarget(dspTime, window.FirstTapDspTime);
            float second = tapCount > 1 ? PulseForTarget(dspTime, window.SecondTapDspTime) : 0f;
            return Mathf.Max(first, second);
        }

        private void ScheduleClick(double dspTime, float pitch)
        {
            AudioSource source = clickSources[nextClickSource];
            nextClickSource = (nextClickSource + 1) % clickSources.Length;
            source.Stop();
            source.clip = clickClip;
            source.pitch = pitch * PlaybackSpeed;
            source.PlayScheduled(dspTime);
        }

        private void StopScheduledClicks()
        {
            foreach (AudioSource source in clickSources)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }

            nextClickSource = 0;
        }

        private static double SecondsPerBeat(float beatsPerMinute)
        {
            return 60d / Mathf.Max(1f, beatsPerMinute);
        }

        private static float PulseForTarget(double dspTime, double targetDspTime)
        {
            const double pulseLength = 0.16d;
            double distance = System.Math.Abs(dspTime - targetDspTime);
            return Mathf.Clamp01(1f - (float)(distance / pulseLength));
        }

        private static AudioClip CreateClickClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.10f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = 1f - i / (float)sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 920f * time) * envelope * envelope;
            }

            AudioClip clip = AudioClip.Create("PrototypeBeatClick", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            StopMusic();
            if (musicClip != null) Destroy(musicClip);
            if (clickClip != null) Destroy(clickClip);
        }
    }
}

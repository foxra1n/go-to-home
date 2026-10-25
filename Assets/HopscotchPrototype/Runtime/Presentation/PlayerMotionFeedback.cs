using UnityEngine;

namespace Homeward.Hopscotch
{
    // No input, movement, time scale, scoring or collision state is changed here.
    public sealed class PlayerMotionFeedback : MonoBehaviour
    {
        private Transform torso;
        private VoxelStudentVisual visual;
        private AudioSource source;
        private AudioClip jump, single, dual, fail;
        private float landingPulse;

        public void Configure(Transform body, VoxelStudentVisual student)
        {
            torso = body; visual = student;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = .22f;
            source.priority = 160; // Rhythm cues remain more important than decorative sounds.
            jump = Tone("SoftJump", .10f, 330, 570, .32f);
            single = Tone("SingleFootLanding", .11f, 170, 65, .52f);
            dual = Tone("TwoFootLanding", .14f, 130, 48, .62f);
            fail = Tone("FallImpact", .19f, 95, 35, .6f);
        }

        public void ResetFeedback()
        {
            landingPulse = 0;
            torso.localScale = Vector3.one;
            visual.SetArms(0);
            source.Stop();
        }

        public void Jump()
        {
            landingPulse = 0;
            torso.localScale = Vector3.one;
            visual.SetArms(42);
            source.PlayOneShot(jump);
        }

        public void Land(bool oneFoot)
        {
            landingPulse = 1;
            visual.SetArms(oneFoot ? 38 : 12);
            source.PlayOneShot(oneFoot ? single : dual);
        }

        public void Walk(float gait, float? balance = null)
        {
            visual.SetArms(balance.HasValue ? 65 + Mathf.Abs(balance.Value) * 15 : 8,
                Mathf.Sin(gait) * (balance.HasValue ? 12 : 24));
        }

        public void Fall()
        {
            ResetFeedback();
            visual.SetArms(75, 25);
            source.PlayOneShot(fail);
        }

        private void LateUpdate()
        {
            if (torso == null) return;
            // A short visual knee bend; callbacks and the next input never wait for this.
            float amount = Mathf.Sin(landingPulse * Mathf.PI);
            torso.localScale = new Vector3(1 + amount * .06f, 1 - amount * .16f, 1 + amount * .04f);
            landingPulse = Mathf.Max(0, landingPulse - Time.deltaTime / .18f);
        }

        private static AudioClip Tone(string label, float seconds, float from, float to, float gain)
        {
            const int rate = 22050;
            var data = new float[Mathf.CeilToInt(rate * seconds)];
            float phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)(data.Length - 1);
                phase += 2 * Mathf.PI * Mathf.Lerp(from, to, t) / rate;
                float envelope = Mathf.Min(1, t * 30) * Mathf.Pow(1 - t, 2);
                data[i] = (Mathf.Sin(phase) + .2f * Mathf.Sin(phase * 2)) * envelope * gain;
            }
            var clip = AudioClip.Create(label, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            if (jump != null) Destroy(jump);
            if (single != null) Destroy(single);
            if (dual != null) Destroy(dual);
            if (fail != null) Destroy(fail);
        }
    }
}

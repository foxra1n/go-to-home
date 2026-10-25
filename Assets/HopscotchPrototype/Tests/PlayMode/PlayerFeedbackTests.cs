using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Homeward.Hopscotch.Tests
{
    public sealed class PlayerFeedbackTests
    {
        [UnityTest]
        public IEnumerator LandingFeedbackKeepsTargetAndCallbackAndDistinctFootPoses()
        {
            var root = new GameObject("LandingFeedbackTest");
            try
            {
                var player = root.AddComponent<PlayerHopController>(); player.Initialize();
                var target = new Vector3(0,.22f,2.35f);
                int calls = 0;
                player.HopTo(target, LandingType.Single, .1f, () => calls++);
                yield return new WaitForSeconds(.15f);
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(Vector3.Distance(root.transform.position,target), Is.LessThan(.001f));
                float left = root.transform.Find("LeftFoot").localPosition.y;
                float right = root.transform.Find("RightFoot").localPosition.y;
                Assert.That(Mathf.Abs(left-right), Is.GreaterThan(.3f));
                yield return new WaitForSeconds(.25f);
                Assert.That(root.transform.Find("Body").localScale, Is.EqualTo(Vector3.one));
                target.z += 2.35f;
                player.HopTo(target, LandingType.Double, .1f, () => calls++);
                yield return new WaitForSeconds(.15f);
                Assert.That(calls, Is.EqualTo(2));
                Assert.That(root.transform.Find("LeftFoot").localPosition.y,
                    Is.EqualTo(root.transform.Find("RightFoot").localPosition.y));
                player.ResetPlayer();
                Assert.That(root.transform.Find("Body").localScale, Is.EqualTo(Vector3.one));
                Assert.That(root.GetComponent<AudioSource>().isPlaying, Is.False);
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator BalanceArmsAndGeneratedAudioArePresentationOnly()
        {
            var root = new GameObject("BalanceFeedbackTest");
            try
            {
                var player = root.AddComponent<PlayerHopController>(); player.Initialize();
                Vector3 position = root.transform.position;
                player.SetBalanceWalkPose(.8f,1f,35f);
                Assert.That(root.transform.position, Is.EqualTo(position));
                Transform arm = root.transform.Find("Body/LeftArm");
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(0,arm.localEulerAngles.z)), Is.GreaterThan(60));
                var feedback = root.GetComponent<PlayerMotionFeedback>();
                foreach (string name in new[] { "jump", "single", "dual", "fail" })
                {
                    var clip = (AudioClip)typeof(PlayerMotionFeedback).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(feedback);
                    Assert.That(clip.length, Is.LessThan(.21f));
                    var samples = new float[clip.samples];
                    Assert.That(clip.GetData(samples,0), Is.True);
                    float peak=0;
                    foreach(float sample in samples) { Assert.That(float.IsNaN(sample), Is.False); peak=Mathf.Max(peak,Mathf.Abs(sample)); }
                    Assert.That(peak, Is.GreaterThan(.05f).And.LessThan(1f));
                    Assert.That(samples[0], Is.EqualTo(0));
                    Assert.That(samples[samples.Length-1], Is.EqualTo(0));
                }
                player.ResetPlayer();
                Assert.That(arm.localRotation, Is.EqualTo(Quaternion.identity));
                yield return null;
            }
            finally { Object.Destroy(root); }
        }
    }
}

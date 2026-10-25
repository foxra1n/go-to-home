using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class CurbWindVisual : MonoBehaviour
    {
        private readonly Transform[] leaves = new Transform[7];
        private Transform ribbon;
        private Material leafMaterial;
        private Material ribbonMaterial;
        private AudioSource windSource;
        private AudioClip windClip;

        public static float PresentationEnvelope(float elapsed)
        {
            if (!CurbWindProfile.IsVisible(elapsed)) return 0f;
            float fade = Mathf.Min(Mathf.Clamp01((elapsed - CurbWindProfile.WarningStart) / .3f),
                Mathf.Clamp01((CurbWindProfile.GustEnd - elapsed) / .4f));
            return fade * (.45f + .55f * CurbWindProfile.Strength(elapsed) / .35f);
        }

        public void Initialize()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            leafMaterial = new Material(shader) { color = new Color(.89f, .54f, .16f) };
            ribbonMaterial = new Material(shader) { color = new Color(.96f, .9f, .64f) };
            ribbon = Cube("WindRibbon", ribbonMaterial, new Vector3(.8f, .12f, .12f));
            Cube("WindMarkerPole", ribbonMaterial, new Vector3(.06f, 1.1f, .06f)).localPosition = new Vector3(-1.4f, .2f, 1.3f);
            for (int i = 0; i < leaves.Length; i++)
                leaves[i] = Cube("WindLeaf", leafMaterial, new Vector3(.19f, .035f, .1f));
            windClip = CurbWindSound.Create();
            windSource = gameObject.AddComponent<AudioSource>();
            windSource.playOnAwake = false;
            windSource.loop = true;
            windSource.spatialBlend = 0f;
            windSource.priority = 180;
            windSource.volume = 0f;
            windSource.clip = windClip;
            gameObject.SetActive(false);
        }

        private Transform Cube(string objectName, Material material, Vector3 scale)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = objectName;
            item.transform.SetParent(transform, false);
            item.transform.localScale = scale;
            var collider = item.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            var renderer = item.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return item.transform;
        }

        public void Show(Vector3 playerPosition, float elapsed)
        {
            bool visible = CurbWindProfile.IsVisible(elapsed);
            gameObject.SetActive(visible);
            if (!visible) return;
            transform.position = playerPosition;
            float envelope = PresentationEnvelope(elapsed);
            WindTreeSway.Apply(playerPosition, envelope, elapsed);
            windSource.volume = CurbWindSound.MaximumVolume * envelope;
            if (!windSource.isPlaying) windSource.Play();
            float strength = CurbWindProfile.Strength(elapsed);
            ribbon.localPosition = new Vector3(-1.02f, .72f, 1.3f);
            ribbon.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(elapsed * 18f) * (5f + strength * 20f));
            for (int i = 0; i < leaves.Length; i++)
            {
                float progress = Mathf.Repeat((elapsed - CurbWindProfile.WarningStart) * .6f + i / 7f, 1f);
                leaves[i].localPosition = new Vector3(Mathf.Lerp(-2.1f, 2.1f, progress),
                    .25f + (i % 3) * .2f + Mathf.Sin(progress * Mathf.PI) * .18f, .4f + i * .24f);
                leaves[i].localRotation = Quaternion.Euler(20, elapsed * 100f + i * 41f, 20);
            }
        }

        private void OnDestroy()
        {
            if (windClip != null) Destroy(windClip);
            if (leafMaterial != null) Destroy(leafMaterial);
            if (ribbonMaterial != null) Destroy(ribbonMaterial);
        }

        private void OnDisable()
        {
            WindTreeSway.Apply(Vector3.zero, 0f, 0f);
            if (windSource != null) { windSource.Stop(); windSource.volume = 0f; }
        }
    }
}

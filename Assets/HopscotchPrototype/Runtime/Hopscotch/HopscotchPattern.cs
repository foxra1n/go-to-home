using System.Collections.Generic;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public enum LandingType
    {
        Single,
        Double
    }

    public sealed class HopscotchPattern : MonoBehaviour
    {
        private readonly List<Renderer> tileRenderers = new List<Renderer>();
        private readonly List<Vector3> tileBaseScales = new List<Vector3>();
        private Material normalMaterial;
        private Material currentMaterial;
        private Material cueMaterial;
        private readonly List<LineRenderer> timingFrames = new List<LineRenderer>();
        private MaterialPropertyBlock tint;

        public void ShowTiming(double now, RhythmWindow window, double beatDuration)
        {
            for (int i = 0; i < tileRenderers.Count; i++)
            {
                double target = window.TargetDspTime(i);
                float remaining = (float)(target - now);
                float progress = Mathf.Clamp01(1f - remaining / (float)beatDuration);
                float flash = Mathf.Clamp01(1f - Mathf.Abs(remaining) / 0.14f);
                LineRenderer frame = timingFrames[i];
                frame.enabled = remaining >= -0.18f;
                Vector3 size = tileBaseScales[i];
                float expansion = Mathf.Lerp(1.65f, 1f, progress);
                float x = size.x * 0.5f * expansion;
                float z = size.z * 0.5f * expansion;
                frame.SetPosition(0, new Vector3(-x, 0f, -z));
                frame.SetPosition(1, new Vector3(-x, 0f, z));
                frame.SetPosition(2, new Vector3(x, 0f, z));
                frame.SetPosition(3, new Vector3(x, 0f, -z));
                Color cueColor = Color.Lerp(new Color(0.2f, 0.9f, 1f), Color.white, flash);
                frame.startColor = cueColor;
                frame.endColor = cueColor;
                Color tileColor = Color.Lerp(currentMaterial.color, Color.white, flash);
                tint.SetColor("_BaseColor", tileColor);
                tint.SetColor("_Color", tileColor);
                tileRenderers[i].SetPropertyBlock(tint);
                tileRenderers[i].transform.localScale = new Vector3(size.x, size.y * (1f + flash * 0.45f), size.z);
            }
        }

        public LandingType LandingType { get; private set; }
        public int SequenceIndex { get; private set; }
        public int ExpectedTapCount => LandingType == LandingType.Single ? 1 : 2;
        public Vector3 LandingPosition => transform.position + Vector3.up * 0.22f;

        public void Initialize(LandingType landingType, Material normal, Material current)
        {
            tint = new MaterialPropertyBlock();
            LandingType = landingType;
            normalMaterial = normal;
            currentMaterial = current;
            cueMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
            name = $"{landingType}Pattern";

            if (landingType == LandingType.Single)
            {
                CreateTile(Vector3.zero, new Vector3(1.25f, 0.22f, 1.45f));
            }
            else
            {
                CreateTile(new Vector3(-0.68f, 0f, 0f), new Vector3(1.15f, 0.22f, 1.45f));
                CreateTile(new Vector3(0.68f, 0f, 0f), new Vector3(1.15f, 0.22f, 1.45f));
            }
        }

        public void Activate(int sequenceIndex, Vector3 position)
        {
            SequenceIndex = sequenceIndex;
            transform.position = position;
            gameObject.SetActive(true);
            SetCurrent(false);
        }

        public void SetCurrent(bool isCurrent)
        {
            Material material = isCurrent ? currentMaterial : normalMaterial;
            foreach (Renderer tileRenderer in tileRenderers)
            {
                tileRenderer.sharedMaterial = material;
                tileRenderer.SetPropertyBlock(null);
            }

            if (!isCurrent)
            {
                SetRhythmPulse(0f);
                foreach (LineRenderer frame in timingFrames) frame.enabled = false;
            }
        }

        public void SetRhythmPulse(float amount)
        {
            float scale = 1f + Mathf.Clamp01(amount) * 0.45f;
            for (int i = 0; i < tileRenderers.Count; i++)
            {
                Vector3 baseScale = tileBaseScales[i];
                tileRenderers[i].transform.localScale = new Vector3(baseScale.x, baseScale.y * scale, baseScale.z);
            }
        }

        private void CreateTile(Vector3 localPosition, Vector3 localScale)
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "Tile";
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = localPosition;
            tile.transform.localScale = localScale;
            Renderer tileRenderer = tile.GetComponent<Renderer>();
            tileRenderer.sharedMaterial = normalMaterial;
            tileRenderers.Add(tileRenderer);
            tileBaseScales.Add(localScale);
            GameObject cue = new GameObject("TimingFrame");
            cue.transform.SetParent(transform, false);
            cue.transform.localPosition = localPosition + Vector3.up * 0.19f;
            LineRenderer line = cue.AddComponent<LineRenderer>();
            line.sharedMaterial = cueMaterial;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 4;
            line.widthMultiplier = 0.065f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            timingFrames.Add(line);
        }

        private void OnDestroy()
        {
            if (cueMaterial != null) Destroy(cueMaterial);
        }
    }
}

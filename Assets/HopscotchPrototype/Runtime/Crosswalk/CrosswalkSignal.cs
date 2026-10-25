using System.Collections.Generic;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class CrosswalkSignal : MonoBehaviour
    {
        public const float RedDuration = 2.25f;
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Renderer> redLights = new List<Renderer>();
        private readonly List<Renderer> greenLights = new List<Renderer>();
        private Material red, green, off;
        private Transform car;
        private float roadStartZ;
        public const float RoadVehicleInset = .8f;
        public bool IsGreen { get; private set; }

        public void Initialize()
        {
            Material pole = Make(new Color(.16f, .19f, .22f));
            off = Make(new Color(.12f, .15f, .15f));
            red = Make(new Color(1f, .08f, .04f), true);
            green = Make(new Color(.12f, 1f, .35f), true);
            foreach (float x in new[] { -3.5f, 3.5f })
            {
                Part(transform, "SignalPole", new Vector3(x, 1.4f, 0), new Vector3(.14f, 2.8f, .14f), pole);
                Part(transform, "SignalHousing", new Vector3(x, 2.9f, 0), new Vector3(.65f, 1.35f, .36f), pole);
                redLights.Add(Part(transform, "RedLight", new Vector3(x, 3.22f, -.21f), new Vector3(.43f, .43f, .1f), red));
                greenLights.Add(Part(transform, "GreenLight", new Vector3(x, 2.6f, -.21f), new Vector3(.43f, .43f, .1f), off));
            }
            car = new GameObject("PassingCar").transform;
            car.SetParent(transform, false);
            Material paint = Make(new Color(.9f, .65f, .13f));
            Material glass = Make(new Color(.23f, .4f, .5f));
            Part(car, "Body", new Vector3(0, .5f, 0), new Vector3(2.2f, .65f, 1.15f), paint);
            Part(car, "Cabin", new Vector3(-.1f, 1f, 0), new Vector3(1.1f, .5f, 1.04f), glass);
            foreach (float x in new[] { -.7f, .7f })
                foreach (float z in new[] { -.57f, .57f })
                    Part(car, "Wheel", new Vector3(x, .24f, z), new Vector3(.45f, .45f, .16f), pole);
            gameObject.SetActive(false);
        }

        public void Prepare(float surfaceStart)
        {
            roadStartZ = surfaceStart;
            SetSignsVisible(true);
            transform.position = new Vector3(0, 0, surfaceStart - .25f);
            gameObject.SetActive(true);
            SetGreen(false);
            car.gameObject.SetActive(false);
        }

        public void ShowWait(float elapsed)
        {
            SetGreen(elapsed >= RedDuration);
            car.gameObject.SetActive(elapsed < 1.8f);
            car.localPosition = new Vector3(Mathf.Lerp(-10f, 10f, Mathf.Clamp01(elapsed / 1.8f)), 0, 2.8f);
        }

        public void SetGreen(bool value)
        {
            IsGreen = value;
            foreach (var light in redLights) light.sharedMaterial = value ? off : red;
            foreach (var light in greenLights) light.sharedMaterial = value ? green : off;
            if (value) car.gameObject.SetActive(false);
        }

        public void ShowNearMiss(float elapsed, float stopZ, bool showSignals)
        {
            gameObject.SetActive(true);
            SetSignsVisible(showSignals);
            car.gameObject.SetActive(true);
            // Brake beside the student, leaving a visible gap instead of a collision.
            float t = Mathf.Clamp01(elapsed / .4f);
            float braking = 1f - (1f - t) * (1f - t);
            car.position = new Vector3(Mathf.Lerp(-5.5f, -1.65f, braking), 0f,
                Mathf.Max(stopZ, roadStartZ + RoadVehicleInset));
        }

        private void SetSignsVisible(bool visible)
        {
            foreach (Transform child in transform)
                if (child != car) child.gameObject.SetActive(visible);
        }

        private Material Make(Color color, bool glow = false)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = color };
            if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .8f); }
            materials.Add(material);
            return material;
        }

        private static Renderer Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            var renderer = part.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            return renderer;
        }
        private void OnDestroy() { foreach (var material in materials) Destroy(material); }
    }
}

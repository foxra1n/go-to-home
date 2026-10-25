using System.Collections.Generic;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class CurbPathGenerator : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float segmentLength = 3f;
        [SerializeField, Min(0.3f)] private float curbWidth = 0.82f;

        private readonly List<Transform> activeSegments = new List<Transform>();
        private readonly Stack<Transform> segmentPool = new Stack<Transform>();
        private Material curbMaterial;
        public int ActiveSegmentCount => activeSegments.Count;

        public void Initialize()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            curbMaterial = new Material(shader)
            {
                name = "CurbBalanceMaterial",
                color = new Color(0.78f, 0.74f, 0.66f)
            };
        }

        public void BeginSection(float startZ, float sectionLength, float elevation)
        {
            RecycleAll();
            int segmentCount = Mathf.Max(1, Mathf.CeilToInt(sectionLength / segmentLength));
            for (int i = 0; i < segmentCount; i++)
            {
                Transform segment = AcquireSegment();
                float length = Mathf.Min(segmentLength, sectionLength - i * segmentLength);
                segment.localScale = new Vector3(curbWidth, 0.28f, length);
                segment.position = new Vector3(0f, elevation, startZ + segmentLength * i + length * 0.5f);
                segment.gameObject.SetActive(true);
                activeSegments.Add(segment);
            }
        }

        private Transform AcquireSegment()
        {
            if (segmentPool.Count > 0)
            {
                return segmentPool.Pop();
            }

            GameObject segmentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segmentObject.name = "CurbSegment";
            segmentObject.transform.SetParent(transform, false);
            segmentObject.transform.localScale = new Vector3(curbWidth, 0.28f, segmentLength + 0.03f);
            segmentObject.GetComponent<Renderer>().sharedMaterial = curbMaterial;
            return segmentObject.transform;
        }

        private void RecycleAll()
        {
            foreach (Transform segment in activeSegments)
            {
                segment.gameObject.SetActive(false);
                segmentPool.Push(segment);
            }

            activeSegments.Clear();
        }
    }
}

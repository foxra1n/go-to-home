using System.Collections.Generic;
using UnityEngine;

namespace Homeward.Hopscotch
{
    public sealed class HopscotchPatternGenerator : MonoBehaviour
    {
        [SerializeField, Min(5)] private int visiblePatternCount = 13;
        [SerializeField, Min(0)] private int retainedBehindPlayer = 2;
        [SerializeField, Min(1f)] private float patternSpacing = 2.35f;

        private readonly List<HopscotchPattern> activePatterns = new List<HopscotchPattern>();
        private readonly Stack<HopscotchPattern> singlePool = new Stack<HopscotchPattern>();
        private readonly Stack<HopscotchPattern> doublePool = new Stack<HopscotchPattern>();
        private readonly HopscotchPatternRandomizer randomizer = new HopscotchPatternRandomizer();

        private Material singleMaterial;
        private Material doubleMaterial;
        private Material currentMaterial;
        private int cursor;
        private int nextSequenceIndex;
        private float nextZ;
        private bool finite;
        public bool IsComplete => cursor >= activePatterns.Count;
        public float Spacing => patternSpacing;
        public float EndZ { get; private set; }

        public void BeginSection(float startZ, int count)
        {
            RecycleAll();
            finite = true;
            cursor = 0;
            nextSequenceIndex = 0;
            nextZ = startZ + patternSpacing;
            randomizer.Reset();
            for (int i = 0; i < Mathf.Max(1, count); i++) AppendPattern();
            EndZ = nextZ - patternSpacing;
        }

        public HopscotchPattern Current => activePatterns[cursor];

        public bool TryGetPattern(int sequenceIndex, out HopscotchPattern pattern)
        {
            foreach (var candidate in activePatterns)
                if (candidate.SequenceIndex == sequenceIndex) { pattern = candidate; return true; }
            pattern = null;
            return false;
        }

        public void Initialize()
        {
            singleMaterial = CreateMaterial("SingleTile", new Color(0.22f, 0.66f, 0.78f));
            doubleMaterial = CreateMaterial("DoubleTile", new Color(0.96f, 0.48f, 0.34f));
            currentMaterial = CreateMaterial("CurrentTile", new Color(1f, 0.85f, 0.22f));
        }

        public void BeginRun()
        {
            finite = false;
            RecycleAll();
            cursor = 0;
            nextSequenceIndex = 0;
            nextZ = patternSpacing;
            randomizer.Reset();

            for (int i = 0; i < visiblePatternCount; i++)
            {
                AppendPattern();
            }

            Current.SetCurrent(true);
        }

        public void Advance()
        {
            Current.SetCurrent(false);
            cursor++;
            if (finite)
            {
                if (!IsComplete) Current.SetCurrent(true);
                return;
            }
            AppendPattern();

            if (cursor > retainedBehindPlayer)
            {
                HopscotchPattern oldest = activePatterns[0];
                activePatterns.RemoveAt(0);
                cursor--;
                Recycle(oldest);
            }

            Current.SetCurrent(true);
        }

        private void AppendPattern()
        {
            LandingType type = ChooseNextType();
            HopscotchPattern pattern = Acquire(type);
            pattern.Activate(nextSequenceIndex, new Vector3(0f, 0f, nextZ));
            activePatterns.Add(pattern);
            nextSequenceIndex++;
            nextZ += patternSpacing;
        }

        private LandingType ChooseNextType()
        {
            return randomizer.Next(nextSequenceIndex == 0 || Random.value < 0.5f);
        }

        private HopscotchPattern Acquire(LandingType type)
        {
            Stack<HopscotchPattern> pool = type == LandingType.Single ? singlePool : doublePool;
            if (pool.Count > 0)
            {
                return pool.Pop();
            }

            GameObject patternObject = new GameObject();
            patternObject.transform.SetParent(transform, false);
            HopscotchPattern pattern = patternObject.AddComponent<HopscotchPattern>();
            pattern.Initialize(type, type == LandingType.Single ? singleMaterial : doubleMaterial, currentMaterial);
            return pattern;
        }

        private void Recycle(HopscotchPattern pattern)
        {
            pattern.gameObject.SetActive(false);
            Stack<HopscotchPattern> pool = pattern.LandingType == LandingType.Single ? singlePool : doublePool;
            pool.Push(pattern);
        }

        private void RecycleAll()
        {
            foreach (HopscotchPattern pattern in activePatterns)
            {
                Recycle(pattern);
            }

            activePatterns.Clear();
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = materialName,
                color = color
            };
            return material;
        }
    }
}

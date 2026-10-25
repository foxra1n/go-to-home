using UnityEngine;

namespace Homeward.Hopscotch
{
    // One deterministic, telegraphed gust for the first PC experiment.
    public static class CurbWindProfile
    {
        // Gameplay only: keep the accepted sound and scenery envelope unchanged.
        public const float DefaultForceMultiplier = 2f;
        public const float WarningStart = 2f;
        public const float GustStart = 3f;
        public const float GustEnd = 5.4f;
        public static bool IsVisible(float elapsed) => elapsed >= WarningStart && elapsed < GustEnd;
        public static float Strength(float elapsed)
        {
            if (elapsed <= GustStart || elapsed >= GustEnd) return 0f;
            float envelope = Mathf.Min(Mathf.Clamp01((elapsed - GustStart) / .4f),
                Mathf.Clamp01((GustEnd - elapsed) / .4f));
            return .35f * Mathf.SmoothStep(0f, 1f, envelope);
        }
    }
}

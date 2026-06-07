using System;

namespace EmberCrpg.Domain.Forge
{
    public sealed class SingleFigureRefinementOptions
    {
        public SingleFigureRefinementOptions(int maxAttempts, byte alphaThreshold, int cropPadding, int minimumLargeComponentPixels, float dominantComponentRatio, bool allowBestEffortFallback = false, bool rejectFireArtifacts = false, int fireArtifactMinPixels = 480)
        {
            MaxAttempts = Math.Max(1, maxAttempts);
            AlphaThreshold = alphaThreshold;
            CropPadding = Math.Max(0, cropPadding);
            MinimumLargeComponentPixels = Math.Max(1, minimumLargeComponentPixels);
            DominantComponentRatio = Math.Max(0f, dominantComponentRatio);
            AllowBestEffortFallback = allowBestEffortFallback;
            RejectFireArtifacts = rejectFireArtifacts;
            FireArtifactMinPixels = Math.Max(1, fireArtifactMinPixels);
        }

        public int MaxAttempts { get; }
        public byte AlphaThreshold { get; }
        public int CropPadding { get; }
        public int MinimumLargeComponentPixels { get; }
        public float DominantComponentRatio { get; }
        public bool AllowBestEffortFallback { get; }
        public bool RejectFireArtifacts { get; }
        public int FireArtifactMinPixels { get; }
    }
}

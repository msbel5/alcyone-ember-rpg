using System;

namespace EmberCrpg.Domain.Forge
{
    public sealed class SingleFigureRefinementOptions
    {
        public SingleFigureRefinementOptions(int maxAttempts, byte alphaThreshold, int cropPadding, int minimumLargeComponentPixels, float dominantComponentRatio)
        {
            MaxAttempts = Math.Max(1, maxAttempts);
            AlphaThreshold = alphaThreshold;
            CropPadding = Math.Max(0, cropPadding);
            MinimumLargeComponentPixels = Math.Max(1, minimumLargeComponentPixels);
            DominantComponentRatio = Math.Max(0f, dominantComponentRatio);
        }

        public int MaxAttempts { get; }
        public byte AlphaThreshold { get; }
        public int CropPadding { get; }
        public int MinimumLargeComponentPixels { get; }
        public float DominantComponentRatio { get; }
    }
}

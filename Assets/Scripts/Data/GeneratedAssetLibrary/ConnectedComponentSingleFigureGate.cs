using System;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Data.GeneratedAssets
{
    public sealed class ConnectedComponentSingleFigureGate : ISingleFigureGate
    {
        private readonly byte _threshold;
        private readonly int _minimumLargeComponentPixels;
        private readonly float _dominantComponentRatio;
        private readonly float _upperBodyFraction;
        private readonly int _upperBodyMinimumLargePixels;

        public ConnectedComponentSingleFigureGate(byte threshold, int minimumLargeComponentPixels, float dominantComponentRatio, float upperBodyFraction, int upperBodyMinimumLargePixels)
        {
            _threshold = threshold;
            _minimumLargeComponentPixels = minimumLargeComponentPixels < 1 ? 1 : minimumLargeComponentPixels;
            _dominantComponentRatio = Math.Max(0f, dominantComponentRatio);
            _upperBodyFraction = upperBodyFraction <= 0f ? 0.42f : Math.Min(0.8f, upperBodyFraction);
            _upperBodyMinimumLargePixels = upperBodyMinimumLargePixels < 1 ? 1 : upperBodyMinimumLargePixels;
        }

        public SingleFigureGateResult Evaluate(MatteResult matte)
        {
            if (matte == null) throw new ArgumentNullException(nameof(matte));
            var analysis = GeneratedSpriteAlphaAnalyzer.Analyze(matte.Width, matte.Height, matte.SoftAlpha, _threshold, _minimumLargeComponentPixels);
            var dominantRatio = analysis.opaquePixelCount <= 0 ? 0f : (float)analysis.mainComponentPixels / analysis.opaquePixelCount;
            var upperBodyComponents = CountUpperBodyLargeComponents(matte);
            var isSingleFigure = analysis.largeComponentCount <= 1
                && upperBodyComponents <= 1
                && dominantRatio >= _dominantComponentRatio;
            return new SingleFigureGateResult(
                isSingleFigure,
                new PixelBounds(analysis.mainBounds.x, analysis.mainBounds.y, analysis.mainBounds.width, analysis.mainBounds.height),
                analysis.largeComponentCount,
                upperBodyComponents,
                analysis.touchesEdge,
                analysis.opaquePixelCount,
                analysis.mainComponentPixels,
                analysis.mainComponentMask);
        }

        private int CountUpperBodyLargeComponents(MatteResult matte)
        {
            var bandHeight = Math.Max(1, (int)Math.Round(matte.Height * _upperBodyFraction));
            var band = new byte[matte.Width * bandHeight];
            for (var y = 0; y < bandHeight; y++)
            {
                Buffer.BlockCopy(matte.SoftAlpha, y * matte.Width, band, y * matte.Width, matte.Width);
            }

            return GeneratedSpriteAlphaAnalyzer
                .Analyze(matte.Width, bandHeight, band, _threshold, _upperBodyMinimumLargePixels)
                .largeComponentCount;
        }
    }
}

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

        // A second connected component at or above this fraction of the main one reads as a second figure.
        // Tuned so a child-sized second NPC (~0.2-0.4 of the main) is rejected while a held prop or a stray flame
        // (well under 0.1) is kept. Relative, so it holds across sprite resolutions.
        private const float SecondaryFigureRejectRatio = 0.12f;

        // Two side-by-side figures that the matte MERGES into one blob (so component count cannot split them) make
        // the bounding box far wider than a lone standing figure. Reject anything wider than this. Kept above the
        // synthetic gate-test mattes' aspect so it only trips on real side-by-side pairs, not unit fixtures.
        private const float MaxFigureAspectRatio = 0.78f;
        private const float HeadBandFraction = 0.24f;
        private const float MinimumHeadRunWidthFraction = 0.08f;

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
            var secondaryRatio = analysis.mainComponentPixels <= 0 ? 0f : (float)analysis.secondComponentPixels / analysis.mainComponentPixels;
            var upperBodyComponents = CountUpperBodyLargeComponents(matte);
            var headRuns = CountTopBandRuns(matte, analysis.mainBounds, analysis.mainComponentMask);
            var isSingleFigure = analysis.largeComponentCount <= 1
                && upperBodyComponents <= 1
                && headRuns <= 1
                && dominantRatio >= _dominantComponentRatio
                && secondaryRatio < SecondaryFigureRejectRatio
                && (analysis.aspectRatio <= 0f || analysis.aspectRatio <= MaxFigureAspectRatio);
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

        private int CountTopBandRuns(MatteResult matte, PixelRect bounds, byte[] mainMask)
        {
            if (bounds.width <= 0 || bounds.height <= 0 || mainMask == null) return 0;
            var bandHeight = Math.Max(1, (int)Math.Round(bounds.height * HeadBandFraction));
            var yMin = Math.Max(0, bounds.y);
            var yMax = Math.Min(matte.Height, yMin + bandHeight);
            var xMin = Math.Max(0, bounds.x);
            var xMax = Math.Min(matte.Width, bounds.x + bounds.width);
            var minColumnPixels = Math.Max(1, bandHeight / 5);
            var minRunWidth = Math.Max(1, (int)Math.Round(bounds.width * MinimumHeadRunWidthFraction));
            var runs = 0;
            var runWidth = 0;

            for (var x = xMin; x < xMax; x++)
            {
                var columnPixels = 0;
                for (var y = yMin; y < yMax; y++)
                {
                    var index = (y * matte.Width) + x;
                    if (mainMask[index] == 0 || matte.SoftAlpha[index] < _threshold) continue;
                    columnPixels++;
                }

                if (columnPixels >= minColumnPixels)
                {
                    runWidth++;
                    continue;
                }

                if (runWidth >= minRunWidth) runs++;
                runWidth = 0;
            }

            if (runWidth >= minRunWidth) runs++;
            return runs;
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

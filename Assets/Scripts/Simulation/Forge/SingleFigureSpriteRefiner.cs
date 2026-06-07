using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Simulation.Forge
{
    public sealed class SingleFigureSpriteRefiner
    {
        private readonly IAssetForge _inner;
        private readonly IImageMatteService _matteService;
        private readonly ISingleFigureGate _gate;
        private readonly ISpriteImageCodec _codec;
        private readonly SingleFigureRefinementOptions _options;
        private readonly Func<AssetGenerationRequest, bool> _shouldRefine;
        private readonly Action<string> _log;

        public SingleFigureSpriteRefiner(IAssetForge inner, IImageMatteService matteService, ISingleFigureGate gate, ISpriteImageCodec codec, SingleFigureRefinementOptions options, Func<AssetGenerationRequest, bool> shouldRefine, Action<string> log)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _matteService = matteService ?? throw new ArgumentNullException(nameof(matteService));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _shouldRefine = shouldRefine ?? throw new ArgumentNullException(nameof(shouldRefine));
            _log = log ?? (_ => { });
        }

        public async Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
        {
            if (!_shouldRefine(request))
                return await _inner.GenerateAsync(request, cancellationToken).ConfigureAwait(false);

            var stopwatch = Stopwatch.StartNew();
            RefinedAttempt bestAttempt = null;

            for (var attempt = 0; attempt < _options.MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var attemptRequest = attempt == 0 ? request : Reseed(request, unchecked(request.Seed + (uint)attempt));
                var generated = await _inner.GenerateAsync(attemptRequest, cancellationToken).ConfigureAwait(false);
                if (!generated.Success || generated.IsPlaceholder)
                    return generated;

                RefinedAttempt refined;
                try
                {
                    refined = RefineAttempt(generated);
                }
                catch (Exception ex)
                {
                    _log("[SingleFigureGate] " + request.RequestId + " matte_refine_failed " + ex.GetType().Name + ": " + ex.Message);
                    return generated;
                }

                bestAttempt = ChooseBest(bestAttempt, refined);
                _log("[SingleFigureGate] " + request.RequestId + " attempt=" + (attempt + 1) + "/" + _options.MaxAttempts + " components=" + refined.Gate.ComponentCount + " upper=" + refined.Gate.UpperBodyComponentCount + " fire=" + (refined.HasFireArtifact ? 1 : 0) + " accepted=" + refined.Accepted);
                if (!refined.Accepted) continue;

                stopwatch.Stop();
                return new AssetGenerationResult(request.RequestId, refined.FinalPng, generated.MimeType, stopwatch.ElapsedMilliseconds, true, string.Empty);
            }

            if (bestAttempt != null && _options.AllowBestEffortFallback)
            {
                stopwatch.Stop();
                _log("[SingleFigureGate] " + request.RequestId + " fallback=best-of-" + _options.MaxAttempts + " components=" + bestAttempt.Gate.ComponentCount + " accepted=" + bestAttempt.Accepted);
                return new AssetGenerationResult(request.RequestId, bestAttempt.FinalPng, "image/png", stopwatch.ElapsedMilliseconds, true, string.Empty);
            }

            stopwatch.Stop();
            _log("[SingleFigureGate] " + request.RequestId + " rejected_all_attempts=" + _options.MaxAttempts);
            return AssetGenerationResult.Failed(request.RequestId, "single_figure_gate_rejected_all_attempts");
        }

        private RefinedAttempt RefineAttempt(AssetGenerationResult generated)
        {
            var frame = _codec.Decode(generated.ImageBytes);
            var matte = _matteService.Matte(frame.Rgba, frame.Width, frame.Height);
            var gate = _gate.Evaluate(matte);
            var hasFireArtifact = _options.RejectFireArtifacts && HasFireArtifact(frame.Rgba, frame.Width, frame.Height, matte.SoftAlpha, gate.MainComponentMask);
            ApplyAlpha(frame.Rgba, matte.SoftAlpha, gate.MainComponentMask);
            if (gate.Bounds.Width <= 0 || gate.Bounds.Height <= 0)
                return new RefinedAttempt(gate, generated.ImageBytes, false, hasFireArtifact);

            var crop = GeneratedSpriteCropUtility.CropRgba(
                frame.Width,
                frame.Height,
                frame.Rgba,
                new PixelRect(gate.Bounds.X, gate.Bounds.Y, gate.Bounds.Width, gate.Bounds.Height),
                _options.CropPadding);
            return new RefinedAttempt(gate, _codec.Encode(new SpriteImageFrame(crop.Width, crop.Height, crop.Rgba)), gate.IsSingleFigure && !gate.TouchesFrameEdge && !hasFireArtifact, hasFireArtifact);
        }

        private bool HasFireArtifact(byte[] rgba, int width, int height, byte[] alpha, byte[] mainMask)
        {
            var fireMask = new byte[width * height];
            for (var i = 0; i < fireMask.Length; i++)
            {
                if (alpha != null && i < alpha.Length && alpha[i] < _options.AlphaThreshold) continue;
                if (mainMask != null && i < mainMask.Length && mainMask[i] == 0) continue;

                var offset = i * 4;
                var r = rgba[offset + 0];
                var g = rgba[offset + 1];
                var b = rgba[offset + 2];
                if (r >= 220 && g >= 90 && b <= 90 && r >= g && g > b)
                    fireMask[i] = 255;
            }

            return GeneratedSpriteAlphaAnalyzer
                .Analyze(width, height, fireMask, 1, _options.FireArtifactMinPixels)
                .largeComponentCount > 0;
        }

        private static RefinedAttempt ChooseBest(RefinedAttempt current, RefinedAttempt candidate)
        {
            if (candidate == null) return current;
            if (current == null) return candidate;
            if (candidate.Accepted && !current.Accepted) return candidate;
            if (!candidate.Accepted && current.Accepted) return current;
            return candidate.Gate.MainComponentPixels > current.Gate.MainComponentPixels ? candidate : current;
        }

        private static void ApplyAlpha(byte[] rgba, byte[] alpha, byte[] mainComponentMask)
        {
            for (var i = 0; i < alpha.Length; i++)
            {
                var masked = mainComponentMask != null && i < mainComponentMask.Length && mainComponentMask[i] == 0
                    ? (byte)0
                    : alpha[i];
                rgba[(i * 4) + 3] = masked;
            }
        }

        private static AssetGenerationRequest Reseed(AssetGenerationRequest request, uint seed)
        {
            return new AssetGenerationRequest(
                request.RequestId,
                request.Subject,
                request.Style,
                request.Genre,
                request.MoodKeyword,
                request.PromptHash,
                request.Width,
                request.Height,
                seed,
                request.Prompt,
                request.NegativePrompt,
                request.TimeoutSeconds,
                request.ModelHint,
                request.Steps);
        }

        private sealed class RefinedAttempt
        {
            public RefinedAttempt(SingleFigureGateResult gate, byte[] finalPng, bool accepted, bool hasFireArtifact)
            {
                Gate = gate;
                FinalPng = finalPng;
                Accepted = accepted;
                HasFireArtifact = hasFireArtifact;
            }

            public SingleFigureGateResult Gate { get; }
            public byte[] FinalPng { get; }
            public bool Accepted { get; }
            public bool HasFireArtifact { get; }
        }
    }
}

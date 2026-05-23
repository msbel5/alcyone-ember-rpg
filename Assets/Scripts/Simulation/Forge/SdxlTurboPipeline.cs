using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
#if USE_ONNX_RUNTIME
using Microsoft.ML.OnnxRuntime;
#endif

namespace EmberCrpg.Simulation.Forge
{
    internal sealed class SdxlTurboPipeline : IDiffusionPipeline
    {
        private const int ClipTokenLength = 77;
        private const int ClipBosId = 49406;
        private const int ClipEosId = 49407;
        private const float SigmaMax = 14.6146f;
        private const float VaeScale = 0.13025f;

        private readonly OnnxModelBundle _models;
        private readonly OnnxSessionFactory _sessionFactory;

        public SdxlTurboPipeline(OnnxModelBundle models, OnnxSessionFactory sessionFactory)
        {
            _models = models ?? throw new ArgumentNullException(nameof(models));
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        }

        public bool ProbeAvailability(out string error)
        {
            error = string.Empty;
#if !USE_ONNX_RUNTIME
            error = "onnx_runtime_define_missing";
            return false;
#else
            if (_sessionFactory.ProviderPreference != OnnxExecutionProviderPreference.PreferCuda)
            {
                error = "sdxl_requires_cuda";
                return false;
            }

            try
            {
                ClipBpeTokenizer.LoadFromVocab(_models.TokenizerVocab);
                ProbeSession(_models.TextEncoder);
                ProbeSession(_models.TextEncoder2);
                ProbeSession(_models.Unet);
                ProbeSession(_models.VaeDecoder);
                return true;
            }
            catch (Exception ex)
            {
                error = OnnxSessionFactory.IsCudaProviderFailure(ex)
                    ? "sdxl_requires_cuda"
                    : "sdxl_init_failed:" + ex.GetType().Name;
                return false;
            }
#endif
        }

        public Task<byte[]> RunAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
        {
#if !USE_ONNX_RUNTIME
            return Task.FromException<byte[]>(new InvalidOperationException("onnx_runtime_define_missing"));
#else
            return Task.Run(() => Run(request, cancellationToken), cancellationToken);
#endif
        }

#if USE_ONNX_RUNTIME
        private byte[] Run(AssetGenerationRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int width = ClampDimension(request.Width);
            int height = ClampDimension(request.Height);
            int latentWidth = Math.Max(1, width / 8);
            int latentHeight = Math.Max(1, height / 8);
            int latentLength = 4 * latentWidth * latentHeight;

            var tokenizer = ClipBpeTokenizer.LoadFromVocab(_models.TokenizerVocab);
            var tokens = ToIntTokens(tokenizer.Tokenize(request.Prompt, ClipTokenLength, ClipBosId, ClipEosId));

            var hidden768 = EncodeText(_models.TextEncoder, tokens, "last_hidden_state");
            cancellationToken.ThrowIfCancellationRequested();

            var encoder2 = EncodeText2(tokens);
            cancellationToken.ThrowIfCancellationRequested();

            var conditioning = SdxlConditioning.Concat(hidden768, encoder2.HiddenStates1280, encoder2.Pooled1280);
            var latents = LatentNoiseSampler.SampleGaussian(request.Seed, latentLength, SigmaMax);
            var timeIds = new[] { (float)height, (float)width, 0f, 0f, (float)height, (float)width };

            var scaled = ScaleLatentsForEulerInput(latents, SigmaMax);
            var eps = RunUnet(scaled, latentHeight, latentWidth, 999f, conditioning, timeIds);
            for (int i = 0; i < latents.Length; i++)
                latents[i] = latents[i] - (SigmaMax * eps[i]);

            for (int i = 0; i < latents.Length; i++)
                latents[i] /= VaeScale;

            var decoded = DecodeLatents(latents, latentHeight, latentWidth);
            return OnnxPngEncoder.EncodeRgba(width, height, DecodedNchwToRgba(decoded, width, height));
        }

        private float[] EncodeText(string modelPath, int[] tokens, string outputName)
        {
            using (var session = _sessionFactory.CreateSession(modelPath))
            using (var outputs = session.Run(new[] { _sessionFactory.CreateTokenInput(session, tokens) }))
            {
                return OnnxSessionFactory.ReadFloatTensor(outputs, outputName);
            }
        }

        private SdxlEncoder2Output EncodeText2(int[] tokens)
        {
            using (var session = _sessionFactory.CreateSession(_models.TextEncoder2))
            using (var outputs = session.Run(new[] { _sessionFactory.CreateTokenInput(session, tokens) }))
            {
                var pooled = OnnxSessionFactory.ReadFloatTensor(outputs, "text_embeds");
                var hidden = OnnxSessionFactory.ReadFloatTensor(outputs, "last_hidden_state");
                return new SdxlEncoder2Output(hidden, pooled);
            }
        }

        private float[] RunUnet(
            float[] sample,
            int latentHeight,
            int latentWidth,
            float timestep,
            SdxlConditioning conditioning,
            float[] timeIds)
        {
            using (var session = _sessionFactory.CreateSession(_models.Unet))
            {
                var inputs = new List<NamedOnnxValue>
                {
                    _sessionFactory.CreateFloatInput(session, "sample", sample, new[] { 1, 4, latentHeight, latentWidth }),
                    _sessionFactory.CreateFloatInput(session, "timestep", new[] { timestep }, new[] { 1 }),
                    _sessionFactory.CreateFloatInput(session, "encoder_hidden_states", conditioning.HiddenStates2048, new[] { 1, ClipTokenLength, 2048 }),
                    _sessionFactory.CreateFloatInput(session, "text_embeds", conditioning.Pooled1280, new[] { 1, 1280 }),
                    _sessionFactory.CreateFloatInput(session, "time_ids", timeIds, new[] { 1, 6 }),
                };
                using (var outputs = session.Run(inputs))
                {
                    return OnnxSessionFactory.ReadFloatTensor(outputs, "out_sample");
                }
            }
        }

        private float[] DecodeLatents(float[] latents, int latentHeight, int latentWidth)
        {
            using (var session = _sessionFactory.CreateSession(_models.VaeDecoder))
            {
                var inputName = OnnxSessionFactory.ResolveInputName(session, "latent_sample");
                var input = _sessionFactory.CreateFloatInput(session, inputName, latents, new[] { 1, 4, latentHeight, latentWidth });
                using (var outputs = session.Run(new[] { input }))
                {
                    return OnnxSessionFactory.ReadFloatTensor(outputs, "sample");
                }
            }
        }

        private void ProbeSession(string modelPath)
        {
            using (_sessionFactory.CreateSession(modelPath))
            {
            }
        }

        private static float[] ScaleLatentsForEulerInput(float[] latents, float sigma)
        {
            float scale = 1.0f / MathF.Sqrt((sigma * sigma) + 1.0f);
            var scaled = new float[latents.Length];
            for (int i = 0; i < latents.Length; i++)
                scaled[i] = latents[i] * scale;
            return scaled;
        }

        private static int[] ToIntTokens(long[] tokens)
        {
            var result = new int[tokens.Length];
            for (int i = 0; i < tokens.Length; i++) result[i] = (int)tokens[i];
            return result;
        }

        private static byte[] DecodedNchwToRgba(float[] decoded, int width, int height)
        {
            int pixelCount = width * height;
            int channelStride = pixelCount;
            if (decoded == null || decoded.Length < channelStride * 3)
                throw new InvalidOperationException("VAE output size mismatch.");

            var rgba = new byte[pixelCount * 4];
            for (int i = 0; i < pixelCount; i++)
            {
                int p = i * 4;
                rgba[p + 0] = ToByte((decoded[i] + 1f) * 0.5f);
                rgba[p + 1] = ToByte((decoded[channelStride + i] + 1f) * 0.5f);
                rgba[p + 2] = ToByte((decoded[(channelStride * 2) + i] + 1f) * 0.5f);
                rgba[p + 3] = 255;
            }
            return rgba;
        }

        private static byte ToByte(float value)
        {
            if (value <= 0f) return 0;
            if (value >= 1f) return 255;
            return (byte)(value * 255f);
        }

        private static int ClampDimension(int value)
        {
            if (value < 64) return 64;
            if (value > 1024) return 1024;
            return value;
        }

        private sealed class SdxlEncoder2Output
        {
            public SdxlEncoder2Output(float[] hiddenStates1280, float[] pooled1280)
            {
                HiddenStates1280 = hiddenStates1280;
                Pooled1280 = pooled1280;
            }

            public float[] HiddenStates1280 { get; }
            public float[] Pooled1280 { get; }
        }

        private sealed class SdxlConditioning
        {
            private SdxlConditioning(float[] hiddenStates2048, float[] pooled1280)
            {
                HiddenStates2048 = hiddenStates2048;
                Pooled1280 = pooled1280;
            }

            public float[] HiddenStates2048 { get; }
            public float[] Pooled1280 { get; }

            public static SdxlConditioning Concat(float[] hidden768, float[] hidden1280, float[] pooled1280)
            {
                var merged = new float[ClipTokenLength * 2048];
                for (int t = 0; t < ClipTokenLength; t++)
                {
                    int base768 = t * 768;
                    int base1280 = t * 1280;
                    int baseMerged = t * 2048;
                    for (int i = 0; i < 768; i++) merged[baseMerged + i] = hidden768[base768 + i];
                    for (int i = 0; i < 1280; i++) merged[baseMerged + 768 + i] = hidden1280[base1280 + i];
                }
                return new SdxlConditioning(merged, pooled1280);
            }
        }
#endif
    }
}

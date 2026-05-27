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
    internal sealed class Sd15LcmPipeline : IDiffusionPipeline
    {
        private const int ClipTokenLength = 77;
        private const int ClipBosId = 49406;
        private const int ClipEosId = 49407;
        private const float SigmaMax = 1.0f;
        private const float VaeScale = 0.18215f;

        private readonly OnnxModelBundle _models;
        private readonly OnnxSessionFactory _sessionFactory;

        public Sd15LcmPipeline(OnnxModelBundle models, OnnxSessionFactory sessionFactory)
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
            try
            {
                ClipBpeTokenizer.LoadFromVocab(_models.TokenizerVocab);
                ProbeSession(_models.TextEncoder);
                ProbeSession(_models.Unet);
                ProbeSession(_models.VaeDecoder);
                return true;
            }
            catch (Exception ex)
            {
                error = "sd15_init_failed:" + ex.GetType().Name;
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
            var promptTokens = ToIntTokens(tokenizer.Tokenize(request.Prompt, ClipTokenLength, ClipBosId, ClipEosId));
            var negativeTokens = ToIntTokens(tokenizer.Tokenize(request.NegativePrompt, ClipTokenLength, ClipBosId, ClipEosId));

            var promptHidden = EncodeText(promptTokens);
            var negativeHidden = EncodeText(negativeTokens);
            var latents = LatentNoiseSampler.SampleGaussian(request.Seed, latentLength, SigmaMax);

            // 9 sigmas paired with 8 timesteps so the loop's sigmas[step+1] lookup stays in range.
            // (4-step path used 5 sigmas; 8-step path needs 9.) Evenly-spaced over the noise range.
            var sigmas = new[] { 1.0f, 0.875f, 0.75f, 0.625f, 0.5f, 0.375f, 0.25f, 0.125f, 0.0f };
            // 8 evenly-spaced LCM timesteps (was 4). LCM is calibrated for 1-8 steps; 8 produces
            // noticeably better composition + colour without the 16-step law of diminishing returns.
            // Generation time roughly doubles vs 4-step but stays under the 300s timeout.
            var timesteps = new[] { 999f, 874f, 749f, 624f, 499f, 374f, 249f, 124f };
            const float guidanceScale = 7.5f;

            using (var unet = _sessionFactory.CreateSession(_models.Unet))
            {
                for (int step = 0; step < timesteps.Length; step++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    float sigma = sigmas[step];
                    float sigmaNext = sigmas[step + 1];
                    float timestep = timesteps[step];
                    var scaled = ScaleLatentsForSchedulerInput(latents, sigma);

                    var epsUncond = RunUnet(unet, scaled, latentHeight, latentWidth, timestep, negativeHidden);
                    var epsCond = RunUnet(unet, scaled, latentHeight, latentWidth, timestep, promptHidden);

                    for (int i = 0; i < latents.Length; i++)
                    {
                        float eps = epsUncond[i] + guidanceScale * (epsCond[i] - epsUncond[i]);
                        float x0 = latents[i] - sigma * eps;
                        latents[i] = x0 + sigmaNext * eps;
                    }
                }
            }

            for (int i = 0; i < latents.Length; i++)
                latents[i] /= VaeScale;

            var decoded = DecodeLatents(latents, latentHeight, latentWidth);
            return OnnxPngEncoder.EncodeRgba(width, height, DecodedNchwToRgba(decoded, width, height));
        }

        private float[] EncodeText(int[] tokens)
        {
            using (var session = _sessionFactory.CreateSession(_models.TextEncoder))
            using (var outputs = session.Run(new[] { _sessionFactory.CreateTokenInput(session, tokens) }))
            {
                return OnnxSessionFactory.ReadFloatTensor(outputs, "last_hidden_state");
            }
        }

        private float[] RunUnet(
            InferenceSession session,
            float[] sample,
            int latentHeight,
            int latentWidth,
            float timestep,
            float[] hiddenStates768)
        {
            var inputs = new List<NamedOnnxValue>
            {
                _sessionFactory.CreateFloatInput(session, "sample", sample, new[] { 1, 4, latentHeight, latentWidth }),
                _sessionFactory.CreateFloatInput(session, "timestep", new[] { timestep }, new[] { 1 }),
                _sessionFactory.CreateFloatInput(session, "encoder_hidden_states", hiddenStates768, new[] { 1, ClipTokenLength, 768 }),
            };
            using (var outputs = session.Run(inputs))
            {
                return OnnxSessionFactory.ReadFloatTensor(outputs, "out_sample");
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

        private static float[] ScaleLatentsForSchedulerInput(float[] latents, float sigma)
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

#endif
        private static int ClampDimension(int value)
        {
            if (value < 64) return 64;
            if (value > 1024) return 1024;
            return (value / 8) * 8;
        }
    }
}

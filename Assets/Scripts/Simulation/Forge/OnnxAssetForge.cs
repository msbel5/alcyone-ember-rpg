using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
#if USE_ONNX_RUNTIME
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
#endif

namespace EmberCrpg.Simulation.Forge
{
    public enum OnnxDiffusionFlavor
    {
        SdxlTurbo = 0,
        Sd15Lcm = 1,
    }

    public enum OnnxExecutionProviderPreference
    {
        CpuOnly = 0,
        PreferCuda = 1,
    }

    public sealed class OnnxAssetForge : IAssetForge, IDisposable
    {
        public const string TextEncoderKey = "text_encoder";
        public const string TextEncoder2Key = "text_encoder_2";
        public const string UnetKey = "unet";
        public const string VaeDecoderKey = "vae_decoder";
        public const string TokenizerVocabKey = "tokenizer_vocab";
        public const string TokenizerMergesKey = "tokenizer_merges";

        private const int ClipTokenLength = 77;
        private const int ClipBosId = 49406;
        private const int ClipEosId = 49407;
        private const float SdxlSigmaMax = 14.6146f;
        private const float Sd15SigmaMax = 1.0f;
        private const float SdxlVaeScale = 0.13025f;
        private const float Sd15VaeScale = 0.18215f;

        private readonly string _textEncoderPath;
        private readonly string _textEncoder2Path;
        private readonly string _unetPath;
        private readonly string _vaeDecoderPath;
        private readonly string _tokenizerVocabPath;
        private readonly string _tokenizerMergesPath;
        private readonly string _tokenizerConfigPath;
        private readonly OnnxDiffusionFlavor _flavor;
        private readonly OnnxExecutionProviderPreference _providerPreference;
        private readonly object _initLock = new object();

        private bool _initialised;
        private bool _placeholderMode;
        private string _lastInitError;

#if USE_ONNX_RUNTIME
        private static readonly OrtMemoryInfo OrtMemory = OrtMemoryInfo.DefaultInstance;
        private InferenceSession _textEncoderSession;
        private InferenceSession _textEncoder2Session;
        private InferenceSession _unetSession;
        private InferenceSession _vaeDecoderSession;
        private Tokenizer _tokenizer;
#endif

        public OnnxAssetForge(string[] modelPaths, OnnxDiffusionFlavor flavor = OnnxDiffusionFlavor.SdxlTurbo)
            : this(modelPaths, flavor, OnnxExecutionProviderPreference.CpuOnly)
        {
        }

        public OnnxAssetForge(
            string[] modelPaths,
            OnnxDiffusionFlavor flavor,
            OnnxExecutionProviderPreference providerPreference)
        {
            if (modelPaths == null) throw new ArgumentNullException(nameof(modelPaths));
            if (modelPaths.Length < 4)
                throw new ArgumentException("Expected at least 4 model paths.", nameof(modelPaths));

            _flavor = flavor;
            _providerPreference = providerPreference;

            if (flavor == OnnxDiffusionFlavor.SdxlTurbo)
            {
                // New explicit layout: text_encoder, text_encoder_2, unet, vae_decoder,
                // tokenizer vocab, tokenizer merges, tokenizer config (optional).
                if (modelPaths.Length >= 6)
                {
                    _textEncoderPath = modelPaths[0] ?? string.Empty;
                    _textEncoder2Path = modelPaths[1] ?? string.Empty;
                    _unetPath = modelPaths[2] ?? string.Empty;
                    _vaeDecoderPath = modelPaths[3] ?? string.Empty;
                    _tokenizerVocabPath = modelPaths[4] ?? string.Empty;
                    _tokenizerMergesPath = modelPaths[5] ?? string.Empty;
                    _tokenizerConfigPath = modelPaths.Length > 6 ? (modelPaths[6] ?? string.Empty) : Path.Combine(Path.GetDirectoryName(_tokenizerVocabPath) ?? string.Empty, "tokenizer_config.json");
                }
                else
                {
                    // Backward-compatible layout used by older tests/bootstrap:
                    // text_encoder, unet, vae_decoder, tokenizer vocab.
                    _textEncoderPath = modelPaths[0] ?? string.Empty;
                    _textEncoder2Path = DeriveSiblingModel(_textEncoderPath, "text_encoder_2");
                    _unetPath = modelPaths[1] ?? string.Empty;
                    _vaeDecoderPath = modelPaths[2] ?? string.Empty;
                    _tokenizerVocabPath = modelPaths[3] ?? string.Empty;
                    _tokenizerMergesPath = DeriveTokenizerSibling(_tokenizerVocabPath, "merges.txt");
                    _tokenizerConfigPath = DeriveTokenizerSibling(_tokenizerVocabPath, "tokenizer_config.json");
                }
            }
            else
            {
                // SD 1.5 layout: text_encoder, unet, vae_decoder, tokenizer vocab,
                // tokenizer merges (optional), tokenizer config (optional).
                _textEncoderPath = modelPaths[0] ?? string.Empty;
                _textEncoder2Path = string.Empty;
                _unetPath = modelPaths[1] ?? string.Empty;
                _vaeDecoderPath = modelPaths[2] ?? string.Empty;
                _tokenizerVocabPath = modelPaths[3] ?? string.Empty;
                _tokenizerMergesPath = modelPaths.Length > 4
                    ? (modelPaths[4] ?? string.Empty)
                    : DeriveTokenizerSibling(_tokenizerVocabPath, "merges.txt");
                _tokenizerConfigPath = modelPaths.Length > 5
                    ? (modelPaths[5] ?? string.Empty)
                    : DeriveTokenizerSibling(_tokenizerVocabPath, "tokenizer_config.json");
            }
        }

        public OnnxDiffusionFlavor Flavor => _flavor;

        public bool PlaceholderMode
        {
            get
            {
                lock (_initLock)
                {
                    return _placeholderMode;
                }
            }
        }

        public string LastInitError
        {
            get
            {
                lock (_initLock)
                {
                    return _lastInitError;
                }
            }
        }

        public bool UsesCuda => _providerPreference == OnnxExecutionProviderPreference.PreferCuda;

        public bool IsAvailable()
        {
            if (!File.Exists(_textEncoderPath) || !File.Exists(_unetPath) || !File.Exists(_vaeDecoderPath))
                return false;

            if (!File.Exists(_tokenizerVocabPath) || !File.Exists(_tokenizerMergesPath))
                return false;

            if (!string.IsNullOrEmpty(_tokenizerConfigPath) && !File.Exists(_tokenizerConfigPath))
                return false;

            if (_flavor == OnnxDiffusionFlavor.SdxlTurbo && !File.Exists(_textEncoder2Path))
                return false;

            return true;
        }

        public bool TryWarmup(out string error)
        {
            EnsureInitialised();
            lock (_initLock)
            {
                error = _lastInitError ?? string.Empty;
                return !_placeholderMode;
            }
        }

        public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var stopwatch = Stopwatch.StartNew();
                EnsureInitialised();

                bool placeholder;
                string initError;
                lock (_initLock)
                {
                    placeholder = _placeholderMode;
                    initError = _lastInitError ?? string.Empty;
                }

                if (placeholder)
                {
                    var bytes = PlaceholderPng(request);
                    stopwatch.Stop();
                    return new AssetGenerationResult(
                        request.RequestId,
                        bytes,
                        "image/png",
                        stopwatch.ElapsedMilliseconds,
                        true,
                        string.IsNullOrEmpty(initError) ? "placeholder" : initError);
                }

                try
                {
                    var pngBytes = RunDiffusion(request, cancellationToken);
                    stopwatch.Stop();
                    return new AssetGenerationResult(request.RequestId, pngBytes, "image/png", stopwatch.ElapsedMilliseconds, true, string.Empty);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    var bytes = PlaceholderPng(request);
                    return new AssetGenerationResult(
                        request.RequestId,
                        bytes,
                        "image/png",
                        stopwatch.ElapsedMilliseconds,
                        true,
                        "fallback_" + ex.GetType().Name);
                }
            }, cancellationToken);
        }

        private void EnsureInitialised()
        {
            lock (_initLock)
            {
                if (_initialised) return;
                _initialised = true;

                if (!IsAvailable())
                {
                    _placeholderMode = true;
                    _lastInitError = "model_files_missing";
                    return;
                }

#if USE_ONNX_RUNTIME
                try
                {
                    _tokenizer = BpeTokenizer.Create(_tokenizerVocabPath, _tokenizerMergesPath);

                    using (var options = CreateSessionOptions())
                    {
                        _textEncoderSession = new InferenceSession(_textEncoderPath, options);
                        if (_flavor == OnnxDiffusionFlavor.SdxlTurbo)
                            _textEncoder2Session = new InferenceSession(_textEncoder2Path, options);
                        _unetSession = new InferenceSession(_unetPath, options);
                        _vaeDecoderSession = new InferenceSession(_vaeDecoderPath, options);
                    }
                }
                catch (Exception ex)
                {
                    _placeholderMode = true;
                    _lastInitError = ex.GetType().Name + ":" + ex.Message;
                    DisposeSessions();
                }
#else
                _placeholderMode = true;
                _lastInitError = "onnx_runtime_define_missing";
#endif
            }
        }

#if USE_ONNX_RUNTIME
        private SessionOptions CreateSessionOptions()
        {
            var options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            options.EnableCpuMemArena = true;
            options.EnableMemoryPattern = true;

            if (_providerPreference == OnnxExecutionProviderPreference.PreferCuda)
            {
                // Explicit CUDA path (RTX target). If this fails we surface the
                // initialization error so the caller can fall back to SD1.5 CPU.
                options.AppendExecutionProvider_CUDA(0);
            }

            return options;
        }
#endif

        private byte[] RunDiffusion(AssetGenerationRequest request, CancellationToken cancellationToken)
        {
#if USE_ONNX_RUNTIME
            cancellationToken.ThrowIfCancellationRequested();

            if (_textEncoderSession == null || _unetSession == null || _vaeDecoderSession == null || _tokenizer == null)
                return PlaceholderPng(request);

            int width = ClampDimension(request.Width);
            int height = ClampDimension(request.Height);
            int latentWidth = Math.Max(1, width / 8);
            int latentHeight = Math.Max(1, height / 8);
            int latentLength = 4 * latentWidth * latentHeight;

            var rng = new System.Random((int)request.Seed);
            var latents = new float[latentLength];
            float sigmaMax = _flavor == OnnxDiffusionFlavor.SdxlTurbo ? SdxlSigmaMax : Sd15SigmaMax;
            for (int i = 0; i < latents.Length; i++)
                latents[i] = NextGaussian(rng) * sigmaMax;

            int[] promptTokens = TokenizeClip(request.Prompt);
            int[] negativeTokens = TokenizeClip(request.NegativePrompt);

            if (_flavor == OnnxDiffusionFlavor.SdxlTurbo)
            {
                var promptCond = BuildSdxlConditioning(promptTokens);
                var negativeCond = BuildSdxlConditioning(negativeTokens);

                var sigmas = new[] { SdxlSigmaMax, 0f };
                var timesteps = new[] { 999f };
                var timeIds = new[] { (float)height, (float)width, 0f, 0f, (float)height, (float)width };
                const float guidanceScale = 1.25f;

                for (int step = 0; step < timesteps.Length; step++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    float sigma = sigmas[step];
                    float sigmaNext = sigmas[step + 1];
                    float timestep = timesteps[step];

                    var vUncond = RunSdxlUnet(latents, latentHeight, latentWidth, timestep, negativeCond.HiddenStates2048, negativeCond.Pooled1280, timeIds);
                    var vCond = RunSdxlUnet(latents, latentHeight, latentWidth, timestep, promptCond.HiddenStates2048, promptCond.Pooled1280, timeIds);

                    for (int i = 0; i < latents.Length; i++)
                    {
                        float v = vUncond[i] + guidanceScale * (vCond[i] - vUncond[i]);

                        float sigmaSqPlusOne = (sigma * sigma) + 1f;
                        float x0 = (latents[i] / sigmaSqPlusOne) - ((sigma / MathF.Sqrt(sigmaSqPlusOne)) * v);

                        if (sigmaNext <= 0f)
                        {
                            latents[i] = x0;
                        }
                        else
                        {
                            float derivative = (latents[i] - x0) / sigma;
                            latents[i] = latents[i] + (sigmaNext - sigma) * derivative;
                        }
                    }
                }

                float scale = 1f / SdxlVaeScale;
                for (int i = 0; i < latents.Length; i++) latents[i] *= scale;
            }
            else
            {
                var promptCond = BuildSd15Conditioning(promptTokens);
                var negativeCond = BuildSd15Conditioning(negativeTokens);

                var sigmas = new[] { 1.0f, 0.75f, 0.5f, 0.25f, 0.0f };
                var timesteps = new[] { 999f, 749f, 499f, 249f };
                const float guidanceScale = 7.5f;

                for (int step = 0; step < timesteps.Length; step++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    float sigma = sigmas[step];
                    float sigmaNext = sigmas[step + 1];
                    float timestep = timesteps[step];

                    var epsUncond = RunSd15Unet(latents, latentHeight, latentWidth, timestep, negativeCond.HiddenStates768);
                    var epsCond = RunSd15Unet(latents, latentHeight, latentWidth, timestep, promptCond.HiddenStates768);

                    for (int i = 0; i < latents.Length; i++)
                    {
                        float eps = epsUncond[i] + guidanceScale * (epsCond[i] - epsUncond[i]);
                        float x0 = latents[i] - sigma * eps;
                        latents[i] = x0 + sigmaNext * eps;
                    }
                }

                float scale = 1f / Sd15VaeScale;
                for (int i = 0; i < latents.Length; i++) latents[i] *= scale;
            }

            float[] decoded = DecodeLatents(latents, latentHeight, latentWidth);
            var rgba = ConvertDecodedRgbToRgba(decoded, width, height);
            return EncodePng(width, height, rgba, channels: 4);
#else
            return PlaceholderPng(request);
#endif
        }

#if USE_ONNX_RUNTIME
        private SdxlConditioning BuildSdxlConditioning(int[] tokens)
        {
            var text1 = EncodeTextWithEncoder1(tokens);
            var text2 = EncodeTextWithEncoder2(tokens);

            var merged = new float[ClipTokenLength * 2048];
            for (int t = 0; t < ClipTokenLength; t++)
            {
                int base768 = t * 768;
                int base1280 = t * 1280;
                int baseMerged = t * 2048;
                for (int i = 0; i < 768; i++) merged[baseMerged + i] = text1[base768 + i];
                for (int i = 0; i < 1280; i++) merged[baseMerged + 768 + i] = text2.HiddenStates1280[base1280 + i];
            }

            return new SdxlConditioning(merged, text2.Pooled1280);
        }

        private Sd15Conditioning BuildSd15Conditioning(int[] tokens)
        {
            return new Sd15Conditioning(EncodeTextWithEncoder1(tokens));
        }

        private float[] EncodeTextWithEncoder1(int[] tokenIds)
        {
            var inputTensor = new DenseTensor<int>(new[] { 1, ClipTokenLength });
            for (int i = 0; i < ClipTokenLength; i++) inputTensor[0, i] = tokenIds[i];

            var inputs = new List<NamedOnnxValue>();
            inputs.Add(NamedOnnxValue.CreateFromTensor("input_ids", inputTensor));
            using (var outputs = _textEncoderSession.Run(inputs))
            {
                return ReadFloatTensor(outputs, "last_hidden_state");
            }
        }

        private SdxlEncoder2Output EncodeTextWithEncoder2(int[] tokenIds)
        {
            if (_textEncoder2Session == null) throw new InvalidOperationException("text_encoder_2 session not initialized.");

            var inputTensor = new DenseTensor<long>(new[] { 1, ClipTokenLength });
            for (int i = 0; i < ClipTokenLength; i++) inputTensor[0, i] = tokenIds[i];

            var inputs = new List<NamedOnnxValue>();
            inputs.Add(NamedOnnxValue.CreateFromTensor("input_ids", inputTensor));
            using (var outputs = _textEncoder2Session.Run(inputs))
            {
                var pooled = ReadFloatTensor(outputs, "text_embeds");
                var hidden = ReadFloatTensor(outputs, "last_hidden_state");
                return new SdxlEncoder2Output(hidden, pooled);
            }
        }

        private float[] RunSdxlUnet(
            float[] latent,
            int latentHeight,
            int latentWidth,
            float timestep,
            float[] encoderHiddenStates2048,
            float[] pooledTextEmbeds,
            float[] timeIds)
        {
            var sampleTensor = CreateFloat16Tensor(latent, new[] { 1, 4, latentHeight, latentWidth });
            var timestepTensor = CreateFloat16Tensor(new[] { timestep }, new[] { 1 });
            var hiddenTensor = CreateFloat16Tensor(encoderHiddenStates2048, new[] { 1, ClipTokenLength, 2048 });
            var pooledTensor = CreateFloat16Tensor(pooledTextEmbeds, new[] { 1, 1280 });
            var timeIdTensor = CreateFloat16Tensor(timeIds, new[] { 1, 6 });

            var inputs = new List<NamedOnnxValue>();
            inputs.Add(NamedOnnxValue.CreateFromTensor("sample", sampleTensor));
            inputs.Add(NamedOnnxValue.CreateFromTensor("timestep", timestepTensor));
            inputs.Add(NamedOnnxValue.CreateFromTensor("encoder_hidden_states", hiddenTensor));
            inputs.Add(NamedOnnxValue.CreateFromTensor("text_embeds", pooledTensor));
            inputs.Add(NamedOnnxValue.CreateFromTensor("time_ids", timeIdTensor));

            using (var outputs = _unetSession.Run(inputs))
            {
                return ReadFloatTensor(outputs, "out_sample");
            }
        }

        private float[] RunSd15Unet(
            float[] latent,
            int latentHeight,
            int latentWidth,
            float timestep,
            float[] encoderHiddenStates768)
        {
            var sampleTensor = CreateFloat16Tensor(latent, new[] { 1, 4, latentHeight, latentWidth });
            var timestepTensor = CreateFloat16Tensor(new[] { timestep }, new[] { 1 });
            var hiddenTensor = CreateFloat16Tensor(encoderHiddenStates768, new[] { 1, ClipTokenLength, 768 });

            var inputs = new List<NamedOnnxValue>();
            inputs.Add(NamedOnnxValue.CreateFromTensor("sample", sampleTensor));
            inputs.Add(NamedOnnxValue.CreateFromTensor("timestep", timestepTensor));
            inputs.Add(NamedOnnxValue.CreateFromTensor("encoder_hidden_states", hiddenTensor));

            using (var outputs = _unetSession.Run(inputs))
            {
                return ReadFloatTensor(outputs, "out_sample");
            }
        }

        private float[] DecodeLatents(float[] latent, int latentHeight, int latentWidth)
        {
            var latentTensor = CreateFloat16Tensor(latent, new[] { 1, 4, latentHeight, latentWidth });
            string latentInputName = ResolvePrimaryInputName(_vaeDecoderSession, "latent_sample");

            var inputs = new List<NamedOnnxValue>();
            inputs.Add(NamedOnnxValue.CreateFromTensor(latentInputName, latentTensor));
            using (var outputs = _vaeDecoderSession.Run(inputs))
            {
                return ReadFloatTensor(outputs, "sample");
            }
        }

        private int[] TokenizeClip(string text)
        {
            if (_tokenizer == null) throw new InvalidOperationException("Tokenizer not initialized.");

            var ids = _tokenizer.EncodeToIds(text ?? string.Empty, considerPreTokenization: true, considerNormalization: true);
            var tokens = new int[ClipTokenLength];

            for (int i = 0; i < tokens.Length; i++) tokens[i] = ClipEosId;
            tokens[0] = ClipBosId;

            int copy = Math.Min(ClipTokenLength - 2, ids.Count);
            for (int i = 0; i < copy; i++) tokens[i + 1] = ids[i];

            int eosIndex = Math.Min(ClipTokenLength - 1, copy + 1);
            tokens[eosIndex] = ClipEosId;
            return tokens;
        }

        private static DenseTensor<Float16> CreateFloat16Tensor(float[] values, int[] dimensions)
        {
            var fp16 = new Float16[values.Length];
            for (int i = 0; i < values.Length; i++) fp16[i] = (Float16)values[i];
            return new DenseTensor<Float16>(fp16, dimensions);
        }

        private static string ResolvePrimaryInputName(InferenceSession session, string preferred)
        {
            foreach (var key in session.InputMetadata.Keys)
            {
                if (string.Equals(key, preferred, StringComparison.Ordinal))
                    return key;
            }

            foreach (var key in session.InputMetadata.Keys)
                return key;

            throw new InvalidOperationException("Session has no inputs.");
        }

        private static float[] ReadFloatTensor(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs, string preferredName)
        {
            DisposableNamedOnnxValue selected = null;
            foreach (var output in outputs)
            {
                if (selected == null) selected = output;
                if (string.Equals(output.Name, preferredName, StringComparison.Ordinal))
                {
                    selected = output;
                    break;
                }
            }

            if (selected == null)
                throw new InvalidOperationException("Inference produced no outputs.");

            try
            {
                var h = selected.AsTensor<Float16>();
                var arr = new float[h.Length];
                int i = 0;
                foreach (var value in h) arr[i++] = (float)value;
                return arr;
            }
            catch (InvalidCastException)
            {
                var f = selected.AsTensor<float>();
                var arr = new float[f.Length];
                int i = 0;
                foreach (var value in f) arr[i++] = value;
                return arr;
            }
        }

        private static float NextGaussian(System.Random random)
        {
            // Box-Muller, deterministic from request seed.
            var u1 = 1.0 - random.NextDouble();
            var u2 = 1.0 - random.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
        }
#endif

        internal static byte[] PlaceholderPng(AssetGenerationRequest request)
        {
            byte gray = (byte)((request != null ? (int)(request.Seed & 0xFFu) : 128) & 0xFF);
            return EncodePng(8, 8, BuildSolidGrayRgba(8, 8, gray), channels: 4);
        }

        internal static byte[] EncodePng(int width, int height, byte[] pixels, int channels)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (channels != 1 && channels != 3 && channels != 4)
                throw new ArgumentOutOfRangeException(nameof(channels), "PNG encoder supports 1, 3, or 4 channels.");

            int rowBytes = width * channels;
            int expected = rowBytes * height;
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (pixels.Length != expected)
                throw new ArgumentException("Pixel buffer length mismatch.", nameof(pixels));

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

                var ihdr = new byte[13];
                WriteBigEndianInt(ihdr, 0, width);
                WriteBigEndianInt(ihdr, 4, height);
                ihdr[8] = 8;
                ihdr[9] = channels == 1 ? (byte)0 : channels == 3 ? (byte)2 : (byte)6;
                ihdr[10] = 0;
                ihdr[11] = 0;
                ihdr[12] = 0;
                WriteChunk(bw, "IHDR", ihdr);

                var raw = new byte[(rowBytes + 1) * height];
                int src = 0;
                int dst = 0;
                for (int y = 0; y < height; y++)
                {
                    raw[dst++] = 0;
                    Buffer.BlockCopy(pixels, src, raw, dst, rowBytes);
                    src += rowBytes;
                    dst += rowBytes;
                }

                var zlib = ZlibStore(raw);
                WriteChunk(bw, "IDAT", zlib);
                WriteChunk(bw, "IEND", new byte[0]);

                bw.Flush();
                return ms.ToArray();
            }
        }

        private static byte[] BuildSolidGrayRgba(int width, int height, byte gray)
        {
            var pixels = new byte[width * height * 4];
            for (int i = 0; i < width * height; i++)
            {
                int p = i * 4;
                pixels[p + 0] = gray;
                pixels[p + 1] = gray;
                pixels[p + 2] = gray;
                pixels[p + 3] = 255;
            }
            return pixels;
        }

#if USE_ONNX_RUNTIME
        private static byte[] ConvertDecodedRgbToRgba(float[] decoded, int width, int height)
        {
            int pixelCount = width * height;
            int channelStride = pixelCount;
            if (decoded == null || decoded.Length < channelStride * 3)
                throw new InvalidOperationException("VAE output size mismatch.");

            var rgba = new byte[pixelCount * 4];
            for (int i = 0; i < pixelCount; i++)
            {
                float r = Clamp01((decoded[i] + 1f) * 0.5f);
                float g = Clamp01((decoded[channelStride + i] + 1f) * 0.5f);
                float b = Clamp01((decoded[(channelStride * 2) + i] + 1f) * 0.5f);

                int p = i * 4;
                rgba[p + 0] = (byte)(r * 255f);
                rgba[p + 1] = (byte)(g * 255f);
                rgba[p + 2] = (byte)(b * 255f);
                rgba[p + 3] = 255;
            }
            return rgba;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private static int ClampDimension(int value)
        {
            if (value < 64) return 64;
            if (value > 1024) return 1024;
            return value;
        }
#endif

        private static void WriteBigEndianInt(byte[] dst, int offset, int value)
        {
            dst[offset + 0] = (byte)((value >> 24) & 0xFF);
            dst[offset + 1] = (byte)((value >> 16) & 0xFF);
            dst[offset + 2] = (byte)((value >> 8) & 0xFF);
            dst[offset + 3] = (byte)(value & 0xFF);
        }

        private static void WriteChunk(BinaryWriter bw, string type, byte[] data)
        {
            var typeBytes = new byte[4];
            typeBytes[0] = (byte)type[0];
            typeBytes[1] = (byte)type[1];
            typeBytes[2] = (byte)type[2];
            typeBytes[3] = (byte)type[3];

            var lenBytes = new byte[4];
            WriteBigEndianInt(lenBytes, 0, data.Length);
            bw.Write(lenBytes);
            bw.Write(typeBytes);
            if (data.Length > 0) bw.Write(data);

            var crcBuf = new byte[4 + data.Length];
            Buffer.BlockCopy(typeBytes, 0, crcBuf, 0, 4);
            if (data.Length > 0) Buffer.BlockCopy(data, 0, crcBuf, 4, data.Length);
            var crc = Crc32(crcBuf);
            var crcBytes = new byte[4];
            WriteBigEndianInt(crcBytes, 0, (int)crc);
            bw.Write(crcBytes);
        }

        private static byte[] ZlibStore(byte[] raw)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((byte)0x78);
                bw.Write((byte)0x01);

                int pos = 0;
                while (pos < raw.Length)
                {
                    int chunk = Math.Min(65535, raw.Length - pos);
                    bool last = (pos + chunk) == raw.Length;
                    bw.Write((byte)(last ? 1 : 0));
                    bw.Write((byte)(chunk & 0xFF));
                    bw.Write((byte)((chunk >> 8) & 0xFF));
                    int nchunk = ~chunk;
                    bw.Write((byte)(nchunk & 0xFF));
                    bw.Write((byte)((nchunk >> 8) & 0xFF));
                    bw.Write(raw, pos, chunk);
                    pos += chunk;
                }

                uint adler = Adler32(raw);
                var adlerBytes = new byte[4];
                WriteBigEndianInt(adlerBytes, 0, (int)adler);
                bw.Write(adlerBytes);
                return ms.ToArray();
            }
        }

        private static uint Adler32(byte[] data)
        {
            const uint mod = 65521;
            uint a = 1;
            uint b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % mod;
                b = (b + a) % mod;
            }
            return (b << 16) | a;
        }

        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? (0xEDB88320u ^ (c >> 1)) : (c >> 1);
                table[n] = c;
            }
            return table;
        }

        private static uint Crc32(byte[] data)
        {
            uint c = 0xFFFFFFFFu;
            for (int i = 0; i < data.Length; i++)
                c = CrcTable[(c ^ data[i]) & 0xFF] ^ (c >> 8);
            return c ^ 0xFFFFFFFFu;
        }

        private static string DeriveSiblingModel(string knownModelPath, string siblingFolderName)
        {
            if (string.IsNullOrEmpty(knownModelPath)) return string.Empty;
            var textEncoderDir = Path.GetDirectoryName(knownModelPath);
            var sdxlRoot = textEncoderDir == null ? null : Path.GetDirectoryName(textEncoderDir);
            if (string.IsNullOrEmpty(sdxlRoot)) return string.Empty;
            return Path.Combine(sdxlRoot, siblingFolderName, "model.onnx");
        }

        private static string DeriveTokenizerSibling(string vocabPath, string fileName)
        {
            if (string.IsNullOrEmpty(vocabPath)) return string.Empty;
            var dir = Path.GetDirectoryName(vocabPath);
            if (string.IsNullOrEmpty(dir)) return string.Empty;
            return Path.Combine(dir, fileName);
        }

        public void Dispose()
        {
            DisposeSessions();
        }

        private void DisposeSessions()
        {
#if USE_ONNX_RUNTIME
            try { _textEncoderSession?.Dispose(); } catch { }
            try { _textEncoder2Session?.Dispose(); } catch { }
            try { _unetSession?.Dispose(); } catch { }
            try { _vaeDecoderSession?.Dispose(); } catch { }
            _textEncoderSession = null;
            _textEncoder2Session = null;
            _unetSession = null;
            _vaeDecoderSession = null;
            _tokenizer = null;
            _ = OrtMemory;
#endif
        }

#if USE_ONNX_RUNTIME
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
            public SdxlConditioning(float[] hiddenStates2048, float[] pooled1280)
            {
                HiddenStates2048 = hiddenStates2048;
                Pooled1280 = pooled1280;
            }

            public float[] HiddenStates2048 { get; }
            public float[] Pooled1280 { get; }
        }

        private sealed class Sd15Conditioning
        {
            public Sd15Conditioning(float[] hiddenStates768)
            {
                HiddenStates768 = hiddenStates768;
            }

            public float[] HiddenStates768 { get; }
        }
#endif
    }
}

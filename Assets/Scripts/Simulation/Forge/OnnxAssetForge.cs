using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;

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

        private readonly OnnxModelBundle _models;
        private readonly IDiffusionPipeline _pipeline;
        private readonly object _initLock = new object();

        private bool _initialised;
        private bool _placeholderMode;
        private string _lastInitError = string.Empty;
        private string _hardFailureReason = string.Empty;

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

            Flavor = flavor;
            UsesCuda = providerPreference == OnnxExecutionProviderPreference.PreferCuda;
            _models = OnnxModelBundle.From(modelPaths, flavor);

            var sessionFactory = new OnnxSessionFactory(providerPreference);
            _pipeline = flavor == OnnxDiffusionFlavor.SdxlTurbo
                ? (IDiffusionPipeline)new SdxlTurboPipeline(_models, sessionFactory)
                : new Sd15LcmPipeline(_models, sessionFactory);
        }

        public OnnxDiffusionFlavor Flavor { get; }

        public bool UsesCuda { get; }

        public bool PlaceholderMode
        {
            get
            {
                lock (_initLock) return _placeholderMode;
            }
        }

        public string LastInitError
        {
            get
            {
                lock (_initLock) return _lastInitError;
            }
        }

        public bool IsAvailable()
        {
            return _models.RequiredFilesExist(Flavor);
        }

        public bool TryWarmup(out string error)
        {
            EnsureInitialised();
            lock (_initLock)
            {
                error = _lastInitError;
                return !_placeholderMode && string.IsNullOrEmpty(_hardFailureReason);
            }
        }

        public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return Task.Run(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var stopwatch = Stopwatch.StartNew();
                EnsureInitialised();

                bool placeholder;
                string initError;
                string hardFailure;
                lock (_initLock)
                {
                    placeholder = _placeholderMode;
                    initError = _lastInitError;
                    hardFailure = _hardFailureReason;
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

                if (!string.IsNullOrEmpty(hardFailure))
                {
                    stopwatch.Stop();
                    return new AssetGenerationResult(request.RequestId, null, "image/png", stopwatch.ElapsedMilliseconds, false, hardFailure);
                }

                try
                {
                    var pngBytes = await _pipeline.RunAsync(request, cancellationToken).ConfigureAwait(false);
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
                    var reason = Flavor == OnnxDiffusionFlavor.SdxlTurbo && OnnxSessionFactory.IsCudaProviderFailure(ex)
                        ? "sdxl_requires_cuda"
                        : "onnx_inference_failed:" + ex.GetType().Name;
                    return new AssetGenerationResult(request.RequestId, null, "image/png", stopwatch.ElapsedMilliseconds, false, reason);
                }
            }, cancellationToken);
        }

        public void Dispose()
        {
        }

        internal static byte[] PlaceholderPng(AssetGenerationRequest request)
        {
            byte gray = (byte)((request != null ? (int)(request.Seed & 0xFFu) : 128) & 0xFF);
            return OnnxPngEncoder.EncodeRgba(8, 8, BuildSolidGrayRgba(8, 8, gray));
        }

        private void EnsureInitialised()
        {
            lock (_initLock)
            {
                if (_initialised) return;
                _initialised = true;

                if (!_models.RequiredFilesExist(Flavor))
                {
                    _placeholderMode = true;
                    _lastInitError = "model_files_missing";
                    return;
                }

                if (!_pipeline.ProbeAvailability(out var error))
                {
                    _lastInitError = string.IsNullOrEmpty(error) ? "onnx_initialization_failed" : error;
                    if (_lastInitError == "onnx_runtime_define_missing")
                    {
                        _placeholderMode = true;
                    }
                    else
                    {
                        _hardFailureReason = _lastInitError;
                    }
                }
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
    }

    internal interface IDiffusionPipeline
    {
        Task<byte[]> RunAsync(AssetGenerationRequest request, CancellationToken cancellationToken);
        bool ProbeAvailability(out string error);
    }

    internal sealed class OnnxModelBundle
    {
        private OnnxModelBundle(
            string textEncoder,
            string textEncoder2,
            string unet,
            string vaeDecoder,
            string tokenizerVocab,
            string tokenizerMerges,
            string tokenizerConfig)
        {
            TextEncoder = textEncoder ?? string.Empty;
            TextEncoder2 = textEncoder2 ?? string.Empty;
            Unet = unet ?? string.Empty;
            VaeDecoder = vaeDecoder ?? string.Empty;
            TokenizerVocab = tokenizerVocab ?? string.Empty;
            TokenizerMerges = tokenizerMerges ?? string.Empty;
            TokenizerConfig = tokenizerConfig ?? string.Empty;
        }

        public string TextEncoder { get; }
        public string TextEncoder2 { get; }
        public string Unet { get; }
        public string VaeDecoder { get; }
        public string TokenizerVocab { get; }
        public string TokenizerMerges { get; }
        public string TokenizerConfig { get; }

        public static OnnxModelBundle From(string[] modelPaths, OnnxDiffusionFlavor flavor)
        {
            if (flavor == OnnxDiffusionFlavor.SdxlTurbo)
            {
                if (modelPaths.Length >= 6)
                {
                    return new OnnxModelBundle(
                        modelPaths[0],
                        modelPaths[1],
                        modelPaths[2],
                        modelPaths[3],
                        modelPaths[4],
                        modelPaths[5],
                        modelPaths.Length > 6 ? modelPaths[6] : DeriveTokenizerSibling(modelPaths[4], "tokenizer_config.json"));
                }

                return new OnnxModelBundle(
                    modelPaths[0],
                    DeriveSiblingModel(modelPaths[0], "text_encoder_2"),
                    modelPaths[1],
                    modelPaths[2],
                    modelPaths[3],
                    DeriveTokenizerSibling(modelPaths[3], "merges.txt"),
                    DeriveTokenizerSibling(modelPaths[3], "tokenizer_config.json"));
            }

            return new OnnxModelBundle(
                modelPaths[0],
                string.Empty,
                modelPaths[1],
                modelPaths[2],
                modelPaths[3],
                modelPaths.Length > 4 ? modelPaths[4] : DeriveTokenizerSibling(modelPaths[3], "merges.txt"),
                modelPaths.Length > 5 ? modelPaths[5] : DeriveTokenizerSibling(modelPaths[3], "tokenizer_config.json"));
        }

        public bool RequiredFilesExist(OnnxDiffusionFlavor flavor)
        {
            if (!File.Exists(TextEncoder) || !File.Exists(Unet) || !File.Exists(VaeDecoder))
                return false;
            if (!File.Exists(TokenizerVocab) || !File.Exists(TokenizerMerges))
                return false;
            if (!string.IsNullOrEmpty(TokenizerConfig) && !File.Exists(TokenizerConfig))
                return false;
            return flavor != OnnxDiffusionFlavor.SdxlTurbo || File.Exists(TextEncoder2);
        }

        private static string DeriveSiblingModel(string knownModelPath, string siblingFolderName)
        {
            if (string.IsNullOrEmpty(knownModelPath)) return string.Empty;
            // Unity 6.3+ Mono is stricter than older runtimes: Path.GetDirectoryName throws
            // ArgumentException("Invalid path") on inputs that previously returned "" (e.g. fake
            // placeholder strings from tests like "n0"). Treat such inputs as "no sibling discoverable".
            string textEncoderDir;
            try { textEncoderDir = Path.GetDirectoryName(knownModelPath); }
            catch (ArgumentException) { return string.Empty; }
            string root;
            try { root = textEncoderDir == null ? null : Path.GetDirectoryName(textEncoderDir); }
            catch (ArgumentException) { return string.Empty; }
            return string.IsNullOrEmpty(root) ? string.Empty : Path.Combine(root, siblingFolderName, "model.onnx");
        }

        private static string DeriveTokenizerSibling(string vocabPath, string fileName)
        {
            if (string.IsNullOrEmpty(vocabPath)) return string.Empty;
            // See DeriveSiblingModel: Unity 6.3+ Mono throws on placeholder paths instead of returning "".
            string dir;
            try { dir = Path.GetDirectoryName(vocabPath); }
            catch (ArgumentException) { return string.Empty; }
            return string.IsNullOrEmpty(dir) ? string.Empty : Path.Combine(dir, fileName);
        }
    }
}

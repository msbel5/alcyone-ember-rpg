// EMB-019/ARCH-05: this non-deterministic LLM provider lives in the EmberCrpg.Infrastructure
// assembly AND namespace (EmberCrpg.Infrastructure.AiDm), so the deterministic, headless Simulation
// core can never reference HTTP/native inference at compile time and the namespace matches the
// assembly that actually owns the type.
using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.AiDm;
#if USE_LLAMASHARP
using LLama;
using LLama.Common;
#endif

namespace EmberCrpg.Infrastructure.AiDm
{
    /// <summary>
    /// Native LLM client using LLamaSharp for local GGUF inference.
    /// Lazy-downloads the model on first use if DownloadUrl is provided.
    /// Falls back to another client (e.g. LocalQwenClient) if native is unavailable.
    /// </summary>
    public sealed class NativeLlmClient : IDisposable
    {
        public const string DefaultModelFileName = "qwen2.5-1.5b-instruct-q4_k_m.gguf";
        public const string DefaultDownloadUrl = "https://huggingface.co/Qwen/Qwen2.5-1.5B-Instruct-GGUF/resolve/main/qwen2.5-1.5b-instruct-q4_k_m.gguf";

        private readonly string _modelPath;
        private readonly LocalQwenClient _fallback;
        private readonly string _downloadUrl;

#if USE_LLAMASHARP
        private LLamaWeights _weights;
        private LLamaContext _context;
        private InteractiveExecutor _executor;
#endif
#pragma warning disable CS0649 // _isInitialised is only written under USE_LLAMASHARP — that's intentional.
        private bool _isInitialised;
#pragma warning restore CS0649

        /// <summary>
        /// Constructs a NativeLlmClient with a concrete on-disk model path.
        /// Caller is responsible for choosing the model file (e.g. via ModelManifest).
        /// </summary>
        public NativeLlmClient(string modelDirectory, LocalQwenClient fallback, string downloadUrl = DefaultDownloadUrl)
            : this(BuildDefaultPath(modelDirectory), fallback, downloadUrl, modelPathIsFile: false)
        {
        }

        /// <summary>
        /// Explicit overload: caller passes the full path to the .gguf file directly.
        /// Use this when the model filename is resolved via ModelManifest at startup
        /// (e.g. switching between the 3B primary and 1.5B low-VRAM fallback).
        /// </summary>
        public static NativeLlmClient FromModelFile(string modelFilePath, LocalQwenClient fallback, string downloadUrl = DefaultDownloadUrl)
        {
            return new NativeLlmClient(modelFilePath, fallback, downloadUrl, modelPathIsFile: true);
        }

        private NativeLlmClient(string modelPath, LocalQwenClient fallback, string downloadUrl, bool modelPathIsFile)
        {
            if (string.IsNullOrEmpty(modelPath)) throw new ArgumentNullException(nameof(modelPath));
            _modelPath = modelPath;
            _fallback = fallback;
            _downloadUrl = downloadUrl;
            // modelPathIsFile is a marker — both constructors normalize to a full path.
            _ = modelPathIsFile;
        }

        private static string BuildDefaultPath(string modelDirectory)
        {
            if (string.IsNullOrEmpty(modelDirectory)) throw new ArgumentNullException(nameof(modelDirectory));
            return Path.Combine(modelDirectory, DefaultModelFileName);
        }

        public string ModelPath => _modelPath;

        // LEFT-005: a Git-LFS pointer stub (~130 bytes, begins "version https://git-lfs…") or a
        // truncated download would pass a bare File.Exists check and make the game believe a real
        // local Qwen is present (then fail hard inside llama.cpp). A genuine GGUF is many hundred MB
        // and starts with the ASCII magic "GGUF". Gate availability on size + magic so pointer/corrupt
        // files are treated as missing and the labelled fallback engages instead of a false "real LLM".
        public const long MinUsableModelBytes = 1_000_000;

        public static bool IsUsableModelFile(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
                if (new FileInfo(path).Length < MinUsableModelBytes) return false;
                var magic = new byte[4];
                using (var fs = File.OpenRead(path))
                {
                    if (fs.Read(magic, 0, 4) < 4) return false;
                }
                return magic[0] == (byte)'G' && magic[1] == (byte)'G' && magic[2] == (byte)'U' && magic[3] == (byte)'F';
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsAvailable => _isInitialised || IsUsableModelFile(_modelPath);

        // The runtime LLM proof (docs/proofs) showed llama.cpp's AntiPrompt stops generation only AFTER
        // emitting the "User:" turn-marker, so a trailing "User:"/"<|im…"/"Memory:" can leak into the raw
        // response. The adapter's SanitizeNpcLine already cuts this on the gameplay dialog path; do it at
        // the source too so NO consumer of Complete() can surface a turn-marker. Markers are colon/tag
        // anchored to avoid clipping ordinary prose that merely contains the word.
        private static readonly string[] TurnMarkers =
            { "User:", "Assistant:", "System:", "Memory:", "<|im", "\nUser", "\nMemory" };

        public static string StripTrailingTurnMarkers(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            int cut = raw.Length;
            foreach (var m in TurnMarkers)
            {
                int idx = raw.IndexOf(m, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0 && idx < cut) cut = idx;
            }
            return raw.Substring(0, cut).TrimEnd(' ', '\t', '\r', '\n');
        }

        public LlmResponse Complete(LlmRequest request)
        {
            return SyncTaskBridge.Run(() => CompleteAsync(request, CancellationToken.None));
        }

        public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken)
        {
#if USE_LLAMASHARP
            if (!_isInitialised)
            {
                if (!IsUsableModelFile(_modelPath))
                {
                    return _fallback != null
                        ? await _fallback.CompleteAsync(request, cancellationToken).ConfigureAwait(false)
                        : new LlmResponse("Native model missing and no fallback.", null, 0);
                }

                await Task.Run(() => LoadModelSync(), cancellationToken).ConfigureAwait(false);
            }

            try
            {
                var prompt = BuildPrompt(request);
                // LLamaSharp 0.27 API: InferenceParams.Seed removed (now lives
                // on the SamplingPipeline), and InteractiveExecutor.Infer()
                // renamed to InferAsync() returning IAsyncEnumerable<string>.
                var inferenceParams = new InferenceParams()
                {
                    MaxTokens = request.MaxTokens,
                    AntiPrompts = new[] { "User:", "Memory" },
                    SamplingPipeline = new LLama.Sampling.DefaultSamplingPipeline
                    {
                        Seed = (uint)request.Seed,
                        Temperature = 0.7f
                    }
                };

                string resultText = "";
                // DET-04: bound native generation with a timeout so a stalled inference can't pin the
                // calling (worker) thread forever — the HTTP client got this in EMB-018, native did not.
                // On timeout the CancellationToken trips MoveNextAsync, which throws and is caught below,
                // degrading to the fallback/empty response (mirrors LlmHttpClientCore).
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(60));
                    // Drain the async stream synchronously to keep the existing sync Complete() signature.
                    var enumerator = _executor.InferAsync(prompt, inferenceParams, timeout.Token)
                        .GetAsyncEnumerator(timeout.Token);
                    try
                    {
                        while (await enumerator.MoveNextAsync().AsTask().ConfigureAwait(false))
                        {
                            resultText += enumerator.Current;
                        }
                    }
                    finally
                    {
                        await enumerator.DisposeAsync().AsTask().ConfigureAwait(false);
                    }
                }

                return new LlmResponse(StripTrailingTurnMarkers(resultText), null, 0);
            }
            catch (Exception ex)
            {
                return _fallback != null
                    ? await _fallback.CompleteAsync(request, cancellationToken).ConfigureAwait(false)
                    : new LlmResponse($"Native error: {ex.Message}", null, 0);
            }
#else
            return _fallback != null
                ? await _fallback.CompleteAsync(request, cancellationToken).ConfigureAwait(false)
                : new LlmResponse("Native LLM (LLamaSharp) not enabled or package missing. Falling back.", null, 0);
#endif
        }

        private string BuildPrompt(LlmRequest request)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(request.SystemPrompt))
                sb.Append("<|im_start|>system\n").Append(request.SystemPrompt).Append("<|im_end|>\n");
            
            foreach (var turn in request.RecentTurns)
            {
                sb.Append("<|im_start|>user\n").Append(turn).Append("<|im_end|>\n");
            }
            
            sb.Append("<|im_start|>assistant\n");
            return sb.ToString();
        }

        // E7-016: on-demand model download is opt-in only. Returns true exclusively when the user has set
        // EMBER_ALLOW_MODEL_DOWNLOAD to 1/true — so the default build never pulls a multi-GB file silently.
        private static bool IsModelDownloadAllowed()
        {
            var v = Environment.GetEnvironmentVariable("EMBER_ALLOW_MODEL_DOWNLOAD");
            return v == "1" || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
        }

        public async Task EnsureModelReady(Action<float> progressCallback)
        {
            // LEFT-005: only short-circuit when a *usable* model (real GGUF, not an LFS pointer/truncated
            // stub) is already on disk; otherwise fall through and re-fetch real bytes over _downloadUrl.
            if (IsUsableModelFile(_modelPath))
            {
                await LoadModelAsync();
                progressCallback?.Invoke(1f);
                return;
            }

            // E7-016: a multi-GB GGUF fetch must be an EXPLICIT opt-in, never a silent first-run/loading
            // background pull. Default = NO download — the clearly-labelled fallback client answers until a
            // real GGUF is present. The user enables the on-demand download by setting
            // EMBER_ALLOW_MODEL_DOWNLOAD=1 (documented in docs/AI_STACK.md). Without it, report ready and
            // let availability stay false so Complete() degrades to the fallback instead of stalling.
            if (!IsModelDownloadAllowed())
            {
                progressCallback?.Invoke(1f);
                return;
            }

            string dir = Path.GetDirectoryName(_modelPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (var client = new HttpClient())
            {
                using (var response = await client.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(_modelPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var buffer = new byte[8192];
                        long totalRead = 0;
                        int read;
                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;
                            if (totalBytes.HasValue)
                                progressCallback?.Invoke((float)totalRead / totalBytes.Value);
                        }
                    }
                }
            }

            await LoadModelAsync();
            progressCallback?.Invoke(1f);
        }

        private void LoadModelSync()
        {
#if USE_LLAMASHARP
            if (_isInitialised) return;
            var parameters = new ModelParams(_modelPath) { ContextSize = 2048, GpuLayerCount = -1 };
            _weights = LLamaWeights.LoadFromFile(parameters);
            _context = _weights.CreateContext(parameters);
            _executor = new InteractiveExecutor(_context);
            _isInitialised = true;
#endif
        }

        private async Task LoadModelAsync()
        {
            if (_isInitialised) return;
            await Task.Run(() => LoadModelSync());
        }

        public void Dispose()
        {
#if USE_LLAMASHARP
            _context?.Dispose();
            _weights?.Dispose();
#endif
        }
    }
}

using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using EmberCrpg.Domain.AiDm;
#if USE_LLAMASHARP
using LLama;
using LLama.Common;
#endif

namespace EmberCrpg.Simulation.AiDm
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

        public bool IsAvailable => _isInitialised || File.Exists(_modelPath);

        public LlmResponse Complete(LlmRequest request)
        {
#if USE_LLAMASHARP
            if (!_isInitialised)
            {
                if (!File.Exists(_modelPath))
                {
                    return _fallback?.Complete(request) ?? new LlmResponse("Native model missing and no fallback.", null, 0);
                }

                LoadModelSync();
            }

            try
            {
                var prompt = BuildPrompt(request);
                var inferenceParams = new InferenceParams()
                {
                    MaxTokens = request.MaxTokens,
                    AntiPrompts = new[] { "User:", "Memory" },
                    Seed = (uint)request.Seed
                };

                string resultText = "";
                foreach (var text in _executor.Infer(prompt, inferenceParams))
                {
                    resultText += text;
                }

                return new LlmResponse(resultText, null, 0);
            }
            catch (Exception ex)
            {
                return _fallback?.Complete(request) ?? new LlmResponse($"Native error: {ex.Message}", null, 0);
            }
#else
            return _fallback?.Complete(request) ?? new LlmResponse("Native LLM (LLamaSharp) not enabled or package missing. Falling back.", null, 0);
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

        public async Task EnsureModelReady(Action<float> progressCallback)
        {
            if (File.Exists(_modelPath))
            {
                await LoadModelAsync();
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

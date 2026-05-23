using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Presentation.Ember.Forge
{
    public sealed class ComfyUiAssetForge : IAssetForge
    {
        private readonly string _baseUrl;
        private readonly HttpClient _http;
        private readonly TimeSpan _pollDelay;

        public ComfyUiAssetForge(string baseUrl = "http://localhost:8188", HttpClient http = null)
        {
            _baseUrl = (baseUrl ?? "http://localhost:8188").TrimEnd('/');
            _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            _pollDelay = TimeSpan.FromMilliseconds(500);
        }

        public bool IsAvailable()
        {
            try
            {
                var response = _http.GetAsync(_baseUrl + "/system_stats").GetAwaiter().GetResult();
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (!IsAvailable()) return AssetGenerationResult.Failed(request.RequestId, "comfyui_unavailable");

            var stopwatch = Stopwatch.StartNew();
            var promptJson = BuildPromptJson(request);
            using (var content = new StringContent(promptJson, Encoding.UTF8, "application/json"))
            {
                var promptResponse = await _http.PostAsync(_baseUrl + "/prompt", content, cancellationToken).ConfigureAwait(false);
                if (!promptResponse.IsSuccessStatusCode)
                    return AssetGenerationResult.Failed(request.RequestId, "prompt_http_" + (int)promptResponse.StatusCode);
                var promptBody = await promptResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var promptId = ExtractString(promptBody, "prompt_id");
                if (string.IsNullOrWhiteSpace(promptId))
                    return AssetGenerationResult.Failed(request.RequestId, "missing_prompt_id");

                for (int i = 0; i < 720; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(_pollDelay, cancellationToken).ConfigureAwait(false);
                    var historyResponse = await _http.GetAsync(_baseUrl + "/history/" + Uri.EscapeDataString(promptId), cancellationToken).ConfigureAwait(false);
                    if (!historyResponse.IsSuccessStatusCode) continue;
                    var history = await historyResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var filename = ExtractString(history, "filename");
                    if (string.IsNullOrWhiteSpace(filename)) continue;
                    var subfolder = ExtractString(history, "subfolder") ?? string.Empty;
                    var type = ExtractString(history, "type") ?? "output";
                    var url = _baseUrl + "/view?filename=" + Uri.EscapeDataString(filename)
                        + "&subfolder=" + Uri.EscapeDataString(subfolder)
                        + "&type=" + Uri.EscapeDataString(type);
                    var bytes = await _http.GetByteArrayAsync(url).ConfigureAwait(false);
                    stopwatch.Stop();
                    return new AssetGenerationResult(request.RequestId, bytes, "image/png", stopwatch.ElapsedMilliseconds, true, string.Empty);
                }
            }

            return AssetGenerationResult.Failed(request.RequestId, "timeout");
        }

        private static string BuildPromptJson(AssetGenerationRequest request)
        {
            var sb = new StringBuilder(2048);
            sb.Append("{\"prompt\":{\"3\":{\"class_type\":\"KSampler\",\"inputs\":{\"seed\":").Append(request.Seed)
                .Append(",\"steps\":8,\"cfg\":1.5,\"sampler_name\":\"euler\",\"scheduler\":\"normal\",\"denoise\":1.0")
                .Append(",\"model\":[\"4\",0],\"positive\":[\"6\",0],\"negative\":[\"7\",0],\"latent_image\":[\"5\",0]}}")
                .Append(",\"4\":{\"class_type\":\"CheckpointLoaderSimple\",\"inputs\":{\"ckpt_name\":\"sd_xl_turbo_1.0.safetensors\"}}")
                .Append(",\"5\":{\"class_type\":\"EmptyLatentImage\",\"inputs\":{\"width\":").Append(request.Width)
                .Append(",\"height\":").Append(request.Height).Append(",\"batch_size\":1}}")
                .Append(",\"6\":{\"class_type\":\"CLIPTextEncode\",\"inputs\":{\"text\":");
            AppendJsonString(sb, request.Prompt);
            sb.Append(",\"clip\":[\"4\",1]}}")
                .Append(",\"7\":{\"class_type\":\"CLIPTextEncode\",\"inputs\":{\"text\":");
            AppendJsonString(sb, request.NegativePrompt);
            sb.Append(",\"clip\":[\"4\",1]}}")
                .Append(",\"8\":{\"class_type\":\"VAEDecode\",\"inputs\":{\"samples\":[\"3\",0],\"vae\":[\"4\",2]}}")
                .Append(",\"9\":{\"class_type\":\"SaveImage\",\"inputs\":{\"filename_prefix\":\"ember_")
                .Append(request.PromptHash.Substring(0, Math.Min(12, request.PromptHash.Length)))
                .Append("\",\"images\":[\"8\",0]}}}}}");
            return sb.ToString();
        }

        private static string ExtractString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var marker = "\"" + key + "\"";
            var idx = json.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) return null;
            var colon = json.IndexOf(':', idx + marker.Length);
            if (colon < 0) return null;
            var start = json.IndexOf('"', colon + 1);
            if (start < 0) return null;
            var end = json.IndexOf('"', start + 1);
            return end < 0 ? null : json.Substring(start + 1, end - start - 1);
        }

        private static void AppendJsonString(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (var c in value ?? string.Empty)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append("\\n");
                else if (c == '\r') sb.Append("\\r");
                else if (c == '\t') sb.Append("\\t");
                else sb.Append(c);
            }
            sb.Append('"');
        }
    }
}

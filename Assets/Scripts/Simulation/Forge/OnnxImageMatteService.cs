// Why this file is intentionally long: it owns the full ONNX matte lifecycle end-to-end, including lazy model download, session warmup, preprocessing, inference, and mask upscaling in one boundary.
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using EmberCrpg.Domain.Forge;
#if USE_ONNX_RUNTIME
using Microsoft.ML.OnnxRuntime;
#endif

namespace EmberCrpg.Simulation.Forge
{
    public sealed class OnnxImageMatteService : IImageMatteService, IDisposable
    {
        public const string DefaultModelFileName = "u2net.onnx";
        public const string DefaultDownloadUrl = "https://github.com/danielgatis/rembg/releases/download/v0.0.0/u2net.onnx";
        public const long DefaultExpectedBytes = 175997641L;
        private const string ManifestFileName = "u2net.manifest.json";
        private const int ModelInputSize = 320;

        private static readonly float[] Mean = { 0.485f, 0.456f, 0.406f };
        private static readonly float[] Std = { 0.229f, 0.224f, 0.225f };
        private static readonly HttpClient HttpClient = new HttpClient();

        private readonly string _modelRoot;
        private readonly string _downloadUrl;
        private readonly long _expectedBytes;
        private readonly OnnxSessionFactory _sessionFactory;
        private readonly object _gate = new object();

#if USE_ONNX_RUNTIME
        private InferenceSession _session;
#endif
        private bool _initialised;
        private Exception _initializationError;

        public OnnxImageMatteService(string modelRoot, OnnxExecutionProviderPreference providerPreference = OnnxExecutionProviderPreference.PreferCuda, string downloadUrl = DefaultDownloadUrl, long expectedBytes = DefaultExpectedBytes)
        {
            if (string.IsNullOrWhiteSpace(modelRoot)) throw new ArgumentException("Model root is required.", nameof(modelRoot));
            _modelRoot = modelRoot;
            _downloadUrl = string.IsNullOrWhiteSpace(downloadUrl) ? DefaultDownloadUrl : downloadUrl.Trim();
            _expectedBytes = expectedBytes <= 0 ? DefaultExpectedBytes : expectedBytes;
            _sessionFactory = new OnnxSessionFactory(providerPreference);
        }

        public MatteResult Matte(ReadOnlySpan<byte> rgba, int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (rgba.Length != width * height * 4) throw new ArgumentException("RGBA buffer size does not match dimensions.", nameof(rgba));

            EnsureInitialised();
#if !USE_ONNX_RUNTIME
            throw new InvalidOperationException("onnx_runtime_define_missing");
#else
            var input = BuildInput(rgba, width, height);
            lock (_gate)
            {
                using (var outputs = _session.Run(new[] { _sessionFactory.CreateFloatInput(_session, "images", input, new[] { 1, 3, ModelInputSize, ModelInputSize }) }))
                {
                    return new MatteResult(width, height, ResizeToSource(NormalizeMask(OnnxSessionFactory.ReadFloatTensor(outputs, "d0")), width, height));
                }
            }
#endif
        }

        public void Dispose()
        {
#if USE_ONNX_RUNTIME
            lock (_gate)
            {
                _session?.Dispose();
                _session = null;
            }
#endif
        }

        private void EnsureInitialised()
        {
            lock (_gate)
            {
                if (_initialised)
                {
                    if (_initializationError != null) throw new InvalidOperationException("Matte model initialization failed.", _initializationError);
                    return;
                }

                try
                {
#if !USE_ONNX_RUNTIME
                    var defineMissing = new InvalidOperationException("onnx_runtime_define_missing");
                    _initialised = true;
                    _initializationError = defineMissing;
                    throw defineMissing;
#else
                    var modelPath = EnsureModelOnDisk();
                    _session = _sessionFactory.CreateSession(modelPath);
                    _initialised = true;
#endif
                }
                catch (Exception ex)
                {
                    if (!_initialised)
                    {
                        _initialised = true;
                        _initializationError = ex;
                    }
                    throw;
                }
            }
        }

        private string EnsureModelOnDisk()
        {
            var matteRoot = Path.Combine(_modelRoot, "matte");
            Directory.CreateDirectory(matteRoot);
            var modelPath = Path.Combine(matteRoot, DefaultModelFileName);
            if (!HasExpectedLength(modelPath))
            {
                var bytes = HttpClient.GetByteArrayAsync(_downloadUrl).GetAwaiter().GetResult();
                if (bytes.LongLength != _expectedBytes)
                    throw new IOException("Downloaded matte model size mismatch.");
                var tempPath = modelPath + ".download";
                File.WriteAllBytes(tempPath, bytes);
                if (File.Exists(modelPath)) File.Delete(modelPath);
                File.Move(tempPath, modelPath);
            }

            WriteManifest(matteRoot);
            return modelPath;
        }

        private void WriteManifest(string matteRoot)
        {
            var manifestPath = Path.Combine(matteRoot, ManifestFileName);
            var json = "{\"model\":\"u2net\",\"license\":\"MIT\",\"url\":\"" + Escape(_downloadUrl) + "\",\"expectedBytes\":" + _expectedBytes.ToString(CultureInfo.InvariantCulture) + "}";
            File.WriteAllText(manifestPath, json, Encoding.UTF8);
        }

        private bool HasExpectedLength(string path)
        {
            return File.Exists(path) && new FileInfo(path).Length == _expectedBytes;
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static float[] BuildInput(ReadOnlySpan<byte> rgba, int width, int height)
        {
            var input = new float[3 * ModelInputSize * ModelInputSize];
            for (var y = 0; y < ModelInputSize; y++)
            {
                for (var x = 0; x < ModelInputSize; x++)
                {
                    var dstIndex = (y * ModelInputSize) + x;
                    input[dstIndex] = Normalize(SampleChannel(rgba, width, height, x, y, 0), 0);
                    input[(ModelInputSize * ModelInputSize) + dstIndex] = Normalize(SampleChannel(rgba, width, height, x, y, 1), 1);
                    input[(ModelInputSize * ModelInputSize * 2) + dstIndex] = Normalize(SampleChannel(rgba, width, height, x, y, 2), 2);
                }
            }

            return input;
        }

        private static byte[] NormalizeMask(float[] values)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] < min) min = values[i];
                if (values[i] > max) max = values[i];
            }

            var range = max - min;
            var alpha = new byte[values.Length];
            if (range <= 1e-6f)
                return alpha;

            for (var i = 0; i < values.Length; i++)
            {
                var normalized = (values[i] - min) / range;
                if (normalized <= 0f) alpha[i] = 0;
                else if (normalized >= 1f) alpha[i] = 255;
                else alpha[i] = (byte)(normalized * 255f);
            }

            return alpha;
        }

        private static byte[] ResizeToSource(byte[] mask320, int width, int height)
        {
            var output = new byte[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    output[(y * width) + x] = SampleMask(mask320, x, y, width, height);
                }
            }

            return output;
        }

        private static byte SampleChannel(ReadOnlySpan<byte> rgba, int sourceWidth, int sourceHeight, int targetX, int targetY, int channel)
        {
            var fx = sourceWidth <= 1 ? 0f : targetX * (sourceWidth - 1f) / (ModelInputSize - 1f);
            var fy = sourceHeight <= 1 ? 0f : targetY * (sourceHeight - 1f) / (ModelInputSize - 1f);
            return SampleByteBilinear(rgba, sourceWidth, sourceHeight, fx, fy, channel, 4);
        }

        private static byte SampleMask(byte[] alpha, int targetX, int targetY, int targetWidth, int targetHeight)
        {
            var fx = targetWidth <= 1 ? 0f : targetX * (ModelInputSize - 1f) / (targetWidth - 1f);
            var fy = targetHeight <= 1 ? 0f : targetY * (ModelInputSize - 1f) / (targetHeight - 1f);
            return SampleByteBilinear(alpha, ModelInputSize, ModelInputSize, fx, fy, 0, 1);
        }

        private static byte SampleByteBilinear(ReadOnlySpan<byte> bytes, int width, int height, float fx, float fy, int channel, int stride)
        {
            var x0 = Clamp((int)MathF.Floor(fx), 0, width - 1);
            var y0 = Clamp((int)MathF.Floor(fy), 0, height - 1);
            var x1 = Clamp(x0 + 1, 0, width - 1);
            var y1 = Clamp(y0 + 1, 0, height - 1);
            var tx = fx - x0;
            var ty = fy - y0;

            var top = Lerp(Read(bytes, width, x0, y0, channel, stride), Read(bytes, width, x1, y0, channel, stride), tx);
            var bottom = Lerp(Read(bytes, width, x0, y1, channel, stride), Read(bytes, width, x1, y1, channel, stride), tx);
            return (byte)Lerp(top, bottom, ty);
        }

        private static float Read(ReadOnlySpan<byte> bytes, int width, int x, int y, int channel, int stride)
        {
            return bytes[(((y * width) + x) * stride) + channel];
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * t);
        }

        private static float Normalize(byte channel, int axis)
        {
            var value = channel / 255f;
            return (value - Mean[axis]) / Std[axis];
        }
    }
}

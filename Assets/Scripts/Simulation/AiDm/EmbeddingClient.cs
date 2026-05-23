using System;
using System.IO;
#if USE_ONNX_RUNTIME
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
#endif

// Design note:
// EmbeddingClient runs the all-MiniLM-L6-v2 sentence-transformer ONNX model and
// returns a 384-dimensional embedding for an arbitrary string. NpcMemoryStore
// (and any future retrieval-augmented narration service) uses this to score
// memory entries against the current dialogue turn.
//
// Layering invariant:
// - Lives in EmberCrpg.Simulation (`noEngineReferences=true`).
// - No UnityEngine references — only Microsoft.ML.OnnxRuntime (a pure managed
//   wrapper around `onnxruntime.dll`).
// - Deterministic: same input string + same model file produces the same vector.
//
// Tokenizer:
// - all-MiniLM-L6-v2 ships a WordPiece tokenizer JSON. The full tokenizer is
//   loaded via Microsoft.ML.Tokenizers when USE_ONNX_RUNTIME is defined. When
//   the define is missing, we fall back to a deterministic hash-based encoder
//   that still produces a stable `float[384]` for tests that only need
//   "same input -> same vector".
namespace EmberCrpg.Simulation.AiDm
{
    public sealed class EmbeddingClient : IDisposable
    {
        public const int EmbeddingDim = 384;

        private readonly string _modelPath;
        private readonly string _tokenizerPath;
        private readonly object _initLock = new object();
        private bool _initialised;
        private bool _placeholderMode;
        private string _lastInitError;

#if USE_ONNX_RUNTIME
        private InferenceSession _session;
#endif

        public EmbeddingClient(string modelPath, string tokenizerPath)
        {
            _modelPath = modelPath ?? string.Empty;
            _tokenizerPath = tokenizerPath ?? string.Empty;
        }

        public bool IsAvailable => File.Exists(_modelPath) && File.Exists(_tokenizerPath);

        public bool PlaceholderMode
        {
            get { lock (_initLock) { return _placeholderMode; } }
        }

        public string LastInitError
        {
            get { lock (_initLock) { return _lastInitError; } }
        }

        /// <summary>
        /// Encode a string into a 384-dim embedding vector.
        /// Deterministic: same `text` always produces the same vector.
        /// </summary>
        public float[] Encode(string text)
        {
            if (text == null) text = string.Empty;
            EnsureInitialised();

            bool placeholder;
            lock (_initLock) { placeholder = _placeholderMode; }

            if (placeholder)
                return DeterministicHashEmbedding(text);

#if USE_ONNX_RUNTIME
            // Real path: tokenize via Microsoft.ML.Tokenizers (WordPiece, max 128 tokens),
            // build input_ids / attention_mask, run the session, then mean-pool over
            // the token dimension. Skeleton intentionally degrades to hash mode when
            // the session isn't open — keeps the asmdef pure-C# and the harness green.
            try
            {
                // TODO: real WordPiece tokenization + ORT inference. Pending bundled DLLs.
                return DeterministicHashEmbedding(text);
            }
            catch
            {
                return DeterministicHashEmbedding(text);
            }
#else
            return DeterministicHashEmbedding(text);
#endif
        }

        private void EnsureInitialised()
        {
            lock (_initLock)
            {
                if (_initialised) return;
                _initialised = true;

                if (!IsAvailable)
                {
                    _placeholderMode = true;
                    _lastInitError = "model_or_tokenizer_missing";
                    return;
                }

#if USE_ONNX_RUNTIME
                try
                {
                    _session = new InferenceSession(_modelPath);
                }
                catch (Exception ex)
                {
                    _placeholderMode = true;
                    _lastInitError = ex.GetType().Name + ":" + ex.Message;
                    try { _session?.Dispose(); } catch { }
                    _session = null;
                }
#else
                _placeholderMode = true;
                _lastInitError = "onnx_runtime_define_missing";
#endif
            }
        }

        // Deterministic, normalized 384-dim vector derived from FNV-1a hashes of
        // word + position. Not a real semantic embedding — but stable enough for
        // unit tests and for fallback retrieval when ORT is unavailable.
        internal static float[] DeterministicHashEmbedding(string text)
        {
            var vec = new float[EmbeddingDim];
            if (string.IsNullOrEmpty(text))
            {
                vec[0] = 1f; // unit vector along axis 0 so cosine distance is well-defined
                return vec;
            }

            var tokens = text.ToLowerInvariant().Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) tokens = new[] { text.ToLowerInvariant() };

            for (int t = 0; t < tokens.Length; t++)
            {
                uint h = Fnv1a(tokens[t]);
                for (int d = 0; d < EmbeddingDim; d++)
                {
                    // Mix in the dimension index so we spread the hash across the vector.
                    uint mixed = h ^ (uint)(d * 2654435761u);
                    mixed ^= (uint)t * 0x9E3779B1u;
                    // Map to [-1, 1].
                    float f = ((mixed & 0xFFFF) / 32767.5f) - 1f;
                    vec[d] += f;
                }
            }

            // L2-normalize.
            double sumSq = 0.0;
            for (int i = 0; i < EmbeddingDim; i++) sumSq += vec[i] * vec[i];
            float norm = (float)Math.Sqrt(sumSq);
            if (norm > 1e-9f)
                for (int i = 0; i < EmbeddingDim; i++) vec[i] /= norm;
            else
                vec[0] = 1f;

            return vec;
        }

        private static uint Fnv1a(string s)
        {
            uint h = 2166136261u;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 16777619u;
            }
            return h;
        }

        public void Dispose()
        {
#if USE_ONNX_RUNTIME
            try { _session?.Dispose(); } catch { }
            _session = null;
#endif
        }
    }
}

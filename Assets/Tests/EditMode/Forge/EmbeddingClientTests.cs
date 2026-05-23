using System;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class EmbeddingClientTests
    {
        [Test]
        public void Encode_SameText_ProducesSameVector()
        {
            // Placeholder-mode (no model files) — the hash-based fallback should
            // still be deterministic. This is what NpcMemoryStore relies on for
            // retrieval ranking until the real MiniLM bundle lands.
            using (var c = new EmbeddingClient("nonexistent-model.onnx", "nonexistent-tokenizer.json"))
            {
                var v1 = c.Encode("the king is dead");
                var v2 = c.Encode("the king is dead");
                Assert.That(v1.Length, Is.EqualTo(EmbeddingClient.EmbeddingDim));
                Assert.That(v2, Is.EqualTo(v1));
                Assert.That(c.PlaceholderMode, Is.True);
            }
        }

        [Test]
        public void Encode_DifferentText_DifferentVector()
        {
            using (var c = new EmbeddingClient("nonexistent-model.onnx", "nonexistent-tokenizer.json"))
            {
                var v1 = c.Encode("king");
                var v2 = c.Encode("queen");
                Assert.That(v1, Is.Not.EqualTo(v2));
            }
        }

        [Test]
        public void Encode_NullText_ProducesUnitVector()
        {
            using (var c = new EmbeddingClient("nonexistent.onnx", "nonexistent.json"))
            {
                var v = c.Encode(null);
                Assert.That(v.Length, Is.EqualTo(EmbeddingClient.EmbeddingDim));
                Assert.That(v[0], Is.EqualTo(1f));
                for (int i = 1; i < v.Length; i++) Assert.That(v[i], Is.EqualTo(0f));
            }
        }

        [Test]
        public void Encode_VectorIsNormalized()
        {
            using (var c = new EmbeddingClient("nonexistent.onnx", "nonexistent.json"))
            {
                var v = c.Encode("the quick brown fox jumps over the lazy dog");
                double sumSq = 0;
                for (int i = 0; i < v.Length; i++) sumSq += v[i] * v[i];
                Assert.That(Math.Sqrt(sumSq), Is.EqualTo(1.0).Within(1e-4),
                    "embedding should be L2-normalized for cosine similarity");
            }
        }

        [Test]
        public void Encode_RealModel_DimensionMatches()
        {
            // Acceptance: when MiniLM bundle ships and USE_ONNX_RUNTIME is defined,
            // this test will assert the model returns exactly 384-dim vectors.
            // Skipped by default until models are bundled.
            Assert.Ignore("Requires bundled all-MiniLM-L6-v2 ONNX model + USE_ONNX_RUNTIME define.");
        }
    }
}

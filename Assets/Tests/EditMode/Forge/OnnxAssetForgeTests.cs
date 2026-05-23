using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class OnnxAssetForgeTests
    {
        [Test]
        public void OnnxAssetForge_NoModels_FallsBackToPlaceholder()
        {
            // No actual ONNX files on disk -> forge should enter placeholder mode.
            var tempDir = Path.Combine(Path.GetTempPath(), "ember-onnx-tests-" + Guid.NewGuid().ToString("N"));
            try
            {
                var paths = new[]
                {
                    Path.Combine(tempDir, "missing-text.onnx"),
                    Path.Combine(tempDir, "missing-unet.onnx"),
                    Path.Combine(tempDir, "missing-vae.onnx"),
                    Path.Combine(tempDir, "missing-tokenizer.json"),
                };
                using (var forge = new OnnxAssetForge(paths, OnnxDiffusionFlavor.SdxlTurbo))
                {
                    Assert.That(forge.IsAvailable(), Is.False);

                    var request = MakeRequest("abc123", 42);
                    var result = forge.GenerateAsync(request, CancellationToken.None).GetAwaiter().GetResult();
                    Assert.That(result.Success, Is.True);
                    Assert.That(result.ImageBytes, Is.Not.Null);
                    Assert.That(result.ImageBytes.Length, Is.GreaterThan(0));
                    Assert.That(forge.PlaceholderMode, Is.True);
                    Assert.That(forge.LastInitError, Is.EqualTo("model_files_missing"));

                    // PNG signature check
                    var sig = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
                    for (int i = 0; i < sig.Length; i++)
                        Assert.That(result.ImageBytes[i], Is.EqualTo(sig[i]), "PNG signature mismatch at byte " + i);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void OnnxAssetForge_DeterministicSeed_ProducesSameOutput()
        {
            // Same seed -> same placeholder bytes. This proves the deterministic
            // contract holds even in placeholder mode, which mirrors the contract
            // we want for the real diffusion path.
            var paths = new[] { "n0", "n1", "n2", "n3" };
            using (var forge = new OnnxAssetForge(paths, OnnxDiffusionFlavor.SdxlTurbo))
            {
                var req1 = MakeRequest("seed-test", 1234);
                var req2 = MakeRequest("seed-test", 1234);
                var r1 = forge.GenerateAsync(req1, CancellationToken.None).GetAwaiter().GetResult();
                var r2 = forge.GenerateAsync(req2, CancellationToken.None).GetAwaiter().GetResult();
                Assert.That(r1.ImageBytes, Is.EqualTo(r2.ImageBytes));
            }
        }

        [Test]
        public void OnnxAssetForge_DifferentSeed_ProducesDifferentOutput()
        {
            var paths = new[] { "n0", "n1", "n2", "n3" };
            using (var forge = new OnnxAssetForge(paths, OnnxDiffusionFlavor.SdxlTurbo))
            {
                var r1 = forge.GenerateAsync(MakeRequest("a", 1), CancellationToken.None).GetAwaiter().GetResult();
                var r2 = forge.GenerateAsync(MakeRequest("a", 2), CancellationToken.None).GetAwaiter().GetResult();
                Assert.That(r1.ImageBytes, Is.Not.EqualTo(r2.ImageBytes));
            }
        }

        [Test]
        public void OnnxAssetForge_RejectsInvalidConstructorArgs()
        {
            Assert.Throws<ArgumentNullException>(() => new OnnxAssetForge(null));
            Assert.Throws<ArgumentException>(() => new OnnxAssetForge(new[] { "only", "three", "paths" }));
        }

        [Test]
        public void OnnxAssetForge_RealModels_DimensionMatchesRequest()
        {
            // Acceptance: when real ONNX models are bundled and USE_ONNX_RUNTIME
            // is defined, this test will assert the decoded image dimensions
            // match the request. Skipped by default — flip [Ignore] when the
            // model bundle is on disk and the define is set.
            Assert.Ignore("Requires bundled SDXL Turbo ONNX models + USE_ONNX_RUNTIME define.");
        }

        private static AssetGenerationRequest MakeRequest(string hash, uint seed)
        {
            return new AssetGenerationRequest(
                requestId: "req-" + hash + "-" + seed,
                subject: AssetSubjectKind.Npc,
                style: WorldStyle.DarkFantasyGrim,
                genre: WorldGenre.PoliticalIntrigue,
                moodKeyword: "grim",
                promptHash: hash,
                width: 512,
                height: 512,
                seed: seed,
                prompt: "a bearded knight, dark fantasy",
                negativePrompt: "blurry, low quality");
        }
    }
}

using System.IO;
using System.Threading;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class SdxlPipelineCompileTests
    {
        [Test]
        public void SdxlTurbo_GeneratedPng_HasValidHeader()
        {
#if !USE_ONNX_RUNTIME
            Assert.Ignore("USE_ONNX_RUNTIME not defined.");
#else
            var modelRoot = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "StreamingAssets", "Models", "sdxl-turbo");
            var cudaRoot = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Plugins", "x86_64", "cuda");

            var paths = new[]
            {
                Path.Combine(modelRoot, "text_encoder", "model.onnx"),
                Path.Combine(modelRoot, "text_encoder_2", "model.onnx"),
                Path.Combine(modelRoot, "unet", "model.onnx"),
                Path.Combine(modelRoot, "vae_decoder", "model.onnx"),
                Path.Combine(modelRoot, "tokenizer", "vocab.json"),
                Path.Combine(modelRoot, "tokenizer", "merges.txt"),
                Path.Combine(modelRoot, "tokenizer", "tokenizer_config.json"),
            };

            if (!AllExist(paths)
                || !File.Exists(Path.Combine(cudaRoot, "onnxruntime.dll"))
                || !File.Exists(Path.Combine(cudaRoot, "onnxruntime_providers_cuda.dll"))
                || !File.Exists(Path.Combine(cudaRoot, "onnxruntime_providers_shared.dll"))
                || !File.Exists(Path.Combine(cudaRoot, "onnxruntime_providers_tensorrt.dll")))
            {
                Assert.Ignore("SDXL/CUDA prerequisites are absent.");
            }

            using (var forge = new OnnxAssetForge(paths, OnnxDiffusionFlavor.SdxlTurbo, OnnxExecutionProviderPreference.PreferCuda))
            {
                if (!forge.TryWarmup(out var initError))
                    Assert.Ignore("SDXL warmup unavailable: " + initError);

                var request = new AssetGenerationRequest(
                    requestId: "compile-sdxl-256",
                    subject: AssetSubjectKind.Npc,
                    style: WorldStyle.DarkFantasyGrim,
                    genre: WorldGenre.PoliticalIntrigue,
                    moodKeyword: "grim",
                    promptHash: "compile-sdxl-256",
                    width: 256,
                    height: 256,
                    seed: 7,
                    prompt: "portrait of a weathered fantasy mercenary, painterly, detailed face",
                    negativePrompt: "blurry, low quality, malformed face");

                var result = forge.GenerateAsync(request, CancellationToken.None).GetAwaiter().GetResult();
                Assert.That(result.Success, Is.True);
                Assert.That(result.ImageBytes, Is.Not.Null);
                Assert.That(result.ImageBytes.Length, Is.GreaterThan(8));

                var sig = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
                for (int i = 0; i < sig.Length; i++)
                    Assert.That(result.ImageBytes[i], Is.EqualTo(sig[i]), "PNG signature mismatch at byte " + i);
            }
#endif
        }

        private static bool AllExist(string[] paths)
        {
            if (paths == null) return false;
            for (int i = 0; i < paths.Length; i++)
            {
                if (!File.Exists(paths[i]))
                    return false;
            }
            return true;
        }
    }
}

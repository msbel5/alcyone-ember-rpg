using System.IO;
using System.Text;
using EmberCrpg.Infrastructure.AiDm;
using NUnit.Framework;

// LEFT-005 regression: NativeLlmClient.IsAvailable used to be `_isInitialised || File.Exists(_modelPath)`,
// so a Git-LFS pointer stub (~130 bytes, "version https://git-lfs…") or a truncated download made the game
// believe a real local Qwen was present, then fail hard inside llama.cpp. IsUsableModelFile now gates on a
// real GGUF magic header + a size floor. These tests pin "pointer rejected, real GGUF accepted".
namespace EmberCrpg.Tests.EditMode.AiDm
{
    public sealed class NativeLlmModelReadinessTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ember-llm-readiness-" + TestContext.CurrentContext.Test.ID);
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
        }

        private string WriteFile(string name, byte[] bytes)
        {
            var path = Path.Combine(_dir, name);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        private static byte[] GgufBytes(long totalLength)
        {
            var bytes = new byte[totalLength];
            bytes[0] = (byte)'G';
            bytes[1] = (byte)'G';
            bytes[2] = (byte)'U';
            bytes[3] = (byte)'F';
            return bytes;
        }

        [Test]
        public void RealGgufHeaderAboveFloor_IsUsable()
        {
            var path = WriteFile("model.gguf", GgufBytes(NativeLlmClient.MinUsableModelBytes + 16));
            Assert.That(NativeLlmClient.IsUsableModelFile(path), Is.True);
        }

        [Test]
        public void LfsPointerStub_IsRejected()
        {
            // What a Git-LFS pointer actually looks like on disk in a source-only checkout.
            var pointer = Encoding.ASCII.GetBytes(
                "version https://git-lfs.github.com/spec/v1\n" +
                "oid sha256:0000000000000000000000000000000000000000000000000000000000000000\n" +
                "size 986000000\n");
            var path = WriteFile("model.gguf", pointer);
            Assert.That(NativeLlmClient.IsUsableModelFile(path), Is.False);
        }

        [Test]
        public void TruncatedGgufUnderFloor_IsRejected()
        {
            // Correct magic but far below any real model size (interrupted download).
            var path = WriteFile("model.gguf", GgufBytes(2048));
            Assert.That(NativeLlmClient.IsUsableModelFile(path), Is.False);
        }

        [Test]
        public void WrongMagicAboveFloor_IsRejected()
        {
            var bytes = new byte[NativeLlmClient.MinUsableModelBytes + 16];
            bytes[0] = (byte)'P'; bytes[1] = (byte)'K'; // a ZIP, not a GGUF
            var path = WriteFile("model.bin", bytes);
            Assert.That(NativeLlmClient.IsUsableModelFile(path), Is.False);
        }

        [Test]
        public void MissingFile_IsRejected()
        {
            Assert.That(NativeLlmClient.IsUsableModelFile(Path.Combine(_dir, "does-not-exist.gguf")), Is.False);
        }

        [Test]
        public void NullOrEmptyPath_IsRejected()
        {
            Assert.That(NativeLlmClient.IsUsableModelFile(null), Is.False);
            Assert.That(NativeLlmClient.IsUsableModelFile(string.Empty), Is.False);
        }
    }
}

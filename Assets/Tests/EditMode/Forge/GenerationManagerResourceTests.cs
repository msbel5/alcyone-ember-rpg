using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class GenerationManagerResourceTests
    {
        [Test]
        public async Task GenerateAsync_LowVideoMemory_DownscalesLargeRequestTo512()
        {
            var forge = new RecordingFakeForge();
            var probe = new FixedResourceProbe(videoMemoryMb: 512, systemMemoryMb: long.MaxValue);
            using (var manager = new GenerationManager(forge, resourceProbe: probe))
            {
                await manager.GenerateAsync(Request("low-vram", 1024, 1024), AssetForgePriority.PlayerFacing, CancellationToken.None);
            }

            Assert.That(forge.LastWidth, Is.EqualTo(512));
            Assert.That(forge.LastHeight, Is.EqualTo(512));
        }

        [Test]
        public async Task GenerateAsync_PlentyVideoMemory_KeepsLargeRequestSize()
        {
            var forge = new RecordingFakeForge();
            var probe = new FixedResourceProbe(videoMemoryMb: long.MaxValue, systemMemoryMb: long.MaxValue);
            using (var manager = new GenerationManager(forge, resourceProbe: probe))
            {
                await manager.GenerateAsync(Request("plenty-vram", 1024, 1024), AssetForgePriority.PlayerFacing, CancellationToken.None);
            }

            Assert.That(forge.LastWidth, Is.EqualTo(1024));
            Assert.That(forge.LastHeight, Is.EqualTo(1024));
        }

        private static AssetGenerationRequest Request(string id, int width, int height)
        {
            return new AssetGenerationRequest(
                requestId: id,
                subject: AssetSubjectKind.Item,
                style: WorldStyle.DarkFantasyGrim,
                genre: WorldGenre.PoliticalIntrigue,
                moodKeyword: "grim",
                promptHash: id.PadRight(64, 'x'),
                width: width,
                height: height,
                seed: 42,
                prompt: "test prompt",
                negativePrompt: string.Empty);
        }

        private sealed class RecordingFakeForge : IAssetForge
        {
            public int LastWidth { get; private set; }
            public int LastHeight { get; private set; }

            public bool IsAvailable() => true;

            public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                LastWidth = request.Width;
                LastHeight = request.Height;
                return Task.FromResult(new AssetGenerationResult(request.RequestId, new byte[] { 7 }, "image/png", 1, true, string.Empty));
            }
        }

        private sealed class FixedResourceProbe : IResourceProbe
        {
            private readonly long _videoMemoryMb;
            private readonly long _systemMemoryMb;

            public FixedResourceProbe(long videoMemoryMb, long systemMemoryMb)
            {
                _videoMemoryMb = videoMemoryMb;
                _systemMemoryMb = systemMemoryMb;
            }

            public long AvailableVideoMemoryMb() => _videoMemoryMb;

            public long AvailableSystemMemoryMb() => _systemMemoryMb;
        }
    }
}

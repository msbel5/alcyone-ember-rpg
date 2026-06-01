using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class SerializedAssetForgeTests
    {
        [Test]
        public async Task GenerateAsync_UsesGenerationManagerResourceGuard()
        {
            var fakeForge = new RecordingFakeForge();
            var lowVram = new FixedResourceProbe(videoMemoryMb: 512, systemMemoryMb: long.MaxValue);
            using (var forge = new SerializedAssetForge(fakeForge, lowVram))
            {
                await forge.GenerateAsync(Request("serialized-low-vram", 1024, 1024), CancellationToken.None);
            }

            Assert.That(fakeForge.LastWidth, Is.EqualTo(512));
            Assert.That(fakeForge.LastHeight, Is.EqualTo(512));
        }

        private static AssetGenerationRequest Request(string id, int width, int height)
        {
            return new AssetGenerationRequest(
                requestId: id,
                subject: AssetSubjectKind.Item,
                style: WorldStyle.DarkFantasyGrim,
                genre: WorldGenre.PoliticalIntrigue,
                moodKeyword: "grim",
                promptHash: id.PadRight(64, 'g'),
                width: width,
                height: height,
                seed: 13,
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
                return Task.FromResult(new AssetGenerationResult(request.RequestId, new byte[] { 1 }, "image/png", 1, true, string.Empty));
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

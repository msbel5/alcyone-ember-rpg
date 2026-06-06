// Why this file is intentionally long: it keeps the full single-figure gate acceptance matrix together so retry, crop, predicate, and null-matte behavior stay locked by one focused edit-mode suite.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Simulation.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class SingleFigureSpriteRefiningAssetForgeTests
    {
        [Test]
        public void NullImageMatteService_ReturnsOpaqueMask()
        {
            var matte = new NullImageMatteService();
            var result = matte.Matte(new byte[4 * 4 * 4], 4, 4);

            Assert.That(result.Width, Is.EqualTo(4));
            Assert.That(result.Height, Is.EqualTo(4));
            Assert.That(result.SoftAlpha.Length, Is.EqualTo(16));
            Assert.That(result.SoftAlpha, Has.All.EqualTo((byte)255));
        }

        [Test]
        public void ConnectedComponentGate_RejectsTwoLargeBlobs()
        {
            var gate = new ConnectedComponentSingleFigureGate(1, 4, 0.7f, 0.42f, 2);
            var matte = new MatteResult(8, 8, BuildAlpha(8, 8, (x, y) =>
                ((x >= 1 && x <= 2) || (x >= 5 && x <= 6)) && y >= 1 && y <= 4 ? (byte)255 : (byte)0));

            var result = gate.Evaluate(matte);

            Assert.That(result.IsSingleFigure, Is.False);
            Assert.That(result.ComponentCount, Is.EqualTo(2));
            Assert.That(result.Bounds.Width, Is.GreaterThan(0));
        }

        [Test]
        public void ConnectedComponentGate_AcceptsOneDominantBlob()
        {
            var gate = new ConnectedComponentSingleFigureGate(1, 4, 0.7f, 0.42f, 2);
            var matte = new MatteResult(8, 8, BuildAlpha(8, 8, (x, y) =>
                x >= 2 && x <= 4 && y >= 1 && y <= 6 ? (byte)255 : (byte)0));

            var result = gate.Evaluate(matte);

            Assert.That(result.IsSingleFigure, Is.True);
            Assert.That(result.ComponentCount, Is.EqualTo(1));
            Assert.That(result.Bounds.Height, Is.EqualTo(6));
            Assert.That(result.TouchesFrameEdge, Is.False);
        }

        [Test]
        public async Task Refiner_StopsAtFirstAcceptedAttempt()
        {
            var forge = new SequenceForge(new byte[] { 1 }, new byte[] { 2 });
            var matte = new SequenceMatteService(
                new MatteResult(8, 8, BuildAlpha(8, 8, (x, y) => x <= 1 ? (byte)255 : (byte)0)),
                new MatteResult(8, 8, BuildAlpha(8, 8, (x, y) => x >= 2 && x <= 5 && y >= 1 && y <= 6 ? (byte)255 : (byte)0)));
            var gate = new ConnectedComponentSingleFigureGate(1, 4, 0.7f, 0.42f, 2);
            var options = new SingleFigureRefinementOptions(6, 1, 1, 4, 0.7f);
            var codec = new PassthroughCodec(new SpriteImageFrame(8, 8, SolidRgba(8, 8, 100)), new SpriteImageFrame(8, 8, SolidRgba(8, 8, 180)));
            var refiner = new SingleFigureSpriteRefiner(forge, matte, gate, codec, options, NpcOnly, _ => { });

            var result = await refiner.GenerateAsync(Request("npc_guard", 99), CancellationToken.None);

            Assert.That(result.Success, Is.True);
            Assert.That(forge.RequestSeeds, Is.EqualTo(new uint[] { 99u, 100u }));
            Assert.That(codec.LastEncoded.Width, Is.LessThan(8));
            Assert.That(codec.LastEncoded.Height, Is.EqualTo(8));
        }

        [Test]
        public async Task Refiner_FallsBackToBestAttemptWhenNoAttemptFullyAccepted()
        {
            var forge = new SequenceForge(new byte[] { 3 }, new byte[] { 4 }, new byte[] { 5 });
            var matte = new SequenceMatteService(
                new MatteResult(8, 8, BuildAlpha(8, 8, (x, y) => x <= 1 && y <= 1 ? (byte)255 : (byte)0)),
                new MatteResult(8, 8, BuildAlpha(8, 8, (x, y) => x >= 0 && x <= 3 && y >= 1 && y <= 5 ? (byte)255 : (byte)0)),
                new MatteResult(8, 8, BuildAlpha(8, 8, (x, y) => x >= 1 && x <= 5 && y >= 0 && y <= 6 ? (byte)255 : (byte)0)));
            var gate = new ConnectedComponentSingleFigureGate(1, 4, 0.95f, 0.42f, 2);
            var options = new SingleFigureRefinementOptions(3, 1, 1, 4, 0.95f);
            var codec = new PassthroughCodec(
                new SpriteImageFrame(8, 8, SolidRgba(8, 8, 10)),
                new SpriteImageFrame(8, 8, SolidRgba(8, 8, 20)),
                new SpriteImageFrame(8, 8, SolidRgba(8, 8, 30)));
            var refiner = new SingleFigureSpriteRefiner(forge, matte, gate, codec, options, NpcOnly, _ => { });

            var result = await refiner.GenerateAsync(Request("npc_sage", 5), CancellationToken.None);

            Assert.That(result.Success, Is.True);
            Assert.That(forge.RequestSeeds, Is.EqualTo(new uint[] { 5u, 6u, 7u }));
            Assert.That(codec.LastEncoded.Width, Is.LessThan(8));
            Assert.That(codec.LastEncoded.Height, Is.LessThanOrEqualTo(8));
        }

        [Test]
        public void OnnxPngSpriteImageCodec_RoundTripsRgba()
        {
            var codec = new OnnxPngSpriteImageCodec();
            var frame = new SpriteImageFrame(2, 2, new byte[]
            {
                255, 0, 0, 255,
                0, 255, 0, 128,
                0, 0, 255, 64,
                255, 255, 255, 0,
            });

            var png = codec.Encode(frame);
            var decoded = codec.Decode(png);

            Assert.That(decoded.Width, Is.EqualTo(2));
            Assert.That(decoded.Height, Is.EqualTo(2));
            Assert.That(decoded.Rgba, Is.EqualTo(frame.Rgba));
        }

        [Test]
        public void SpritePredicate_ExcludesTexturesAndIcons()
        {
            Assert.That(NpcOnly(Request("npc_guard", 1)), Is.True);
            Assert.That(NpcOnly(Request("wall_tavernflavour", 1)), Is.False);
            Assert.That(NpcOnly(Request("env_tavernflavour", 1)), Is.False);
            Assert.That(NpcOnly(Request("dice", 1)), Is.False);
        }

        [Test]
        public void ConnectedComponentGate_RejectsConnectedDoubleFigureInUpperBody()
        {
            var gate = new ConnectedComponentSingleFigureGate(1, 4, 0.7f, 0.42f, 2);
            var matte = new MatteResult(8, 8, BuildAlpha(8, 8, (x, y) =>
            {
                var leftUpper = x >= 1 && x <= 2 && y >= 0 && y <= 2;
                var rightUpper = x >= 5 && x <= 6 && y >= 0 && y <= 2;
                var lowerBridge = x >= 2 && x <= 5 && y >= 3 && y <= 7;
                return leftUpper || rightUpper || lowerBridge ? (byte)255 : (byte)0;
            }));

            var result = gate.Evaluate(matte);

            Assert.That(result.ComponentCount, Is.EqualTo(1));
            Assert.That(result.UpperBodyComponentCount, Is.EqualTo(2));
            Assert.That(result.IsSingleFigure, Is.False);
        }

        private static AssetGenerationRequest Request(string id, uint seed)
        {
            return new AssetGenerationRequest(
                requestId: id,
                subject: AssetSubjectKind.Npc,
                style: WorldStyle.DarkFantasyGrim,
                genre: WorldGenre.PoliticalIntrigue,
                moodKeyword: "grim",
                promptHash: id.PadRight(64, 'n'),
                width: 8,
                height: 8,
                seed: seed,
                prompt: "npc prompt",
                negativePrompt: string.Empty);
        }

        private static byte[] BuildAlpha(int width, int height, System.Func<int, int, byte> pixel)
        {
            var alpha = new byte[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                alpha[(y * width) + x] = pixel(x, y);
            return alpha;
        }

        private static byte[] SolidRgba(int width, int height, byte value)
        {
            var rgba = new byte[width * height * 4];
            for (var i = 0; i < width * height; i++)
            {
                var offset = i * 4;
                rgba[offset + 0] = value;
                rgba[offset + 1] = value;
                rgba[offset + 2] = value;
                rgba[offset + 3] = 255;
            }
            return rgba;
        }

        private static bool NpcOnly(AssetGenerationRequest request)
        {
            var id = request?.RequestId ?? string.Empty;
            return id.StartsWith("npc_", System.StringComparison.OrdinalIgnoreCase)
                || id.StartsWith("creature_", System.StringComparison.OrdinalIgnoreCase)
                || id.StartsWith("portrait_", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "dm_portrait", System.StringComparison.OrdinalIgnoreCase);
        }

        private sealed class SequenceForge : IAssetForge
        {
            private readonly Queue<byte[]> _results;

            public SequenceForge(params byte[][] results)
            {
                _results = new Queue<byte[]>(results);
                RequestSeeds = new List<uint>();
            }

            public List<uint> RequestSeeds { get; }

            public bool IsAvailable() => true;

            public Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                RequestSeeds.Add(request.Seed);
                return Task.FromResult(new AssetGenerationResult(request.RequestId, _results.Dequeue(), "image/png", 1, true, string.Empty));
            }
        }

        private sealed class SequenceMatteService : IImageMatteService
        {
            private readonly Queue<MatteResult> _results;

            public SequenceMatteService(params MatteResult[] results)
            {
                _results = new Queue<MatteResult>(results);
            }

            public MatteResult Matte(System.ReadOnlySpan<byte> rgba, int width, int height)
            {
                return _results.Dequeue();
            }
        }

        private sealed class PassthroughCodec : ISpriteImageCodec
        {
            private readonly Queue<SpriteImageFrame> _decoded;

            public PassthroughCodec(params SpriteImageFrame[] decoded)
            {
                _decoded = new Queue<SpriteImageFrame>(decoded);
            }

            public SpriteImageFrame LastEncoded { get; private set; }

            public SpriteImageFrame Decode(byte[] encodedBytes)
            {
                return _decoded.Dequeue();
            }

            public byte[] Encode(SpriteImageFrame frame)
            {
                LastEncoded = frame;
                return frame.Rgba;
            }
        }
    }
}

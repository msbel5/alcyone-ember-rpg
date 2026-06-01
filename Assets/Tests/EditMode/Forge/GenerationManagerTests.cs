using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class GenerationManagerTests
    {
        [Test]
        public async Task GenerateAsync_ConcurrentCallers_RunSequentially()
        {
            var fake = new SequentialFakeForge();
            using (var manager = new GenerationManager(fake))
            {
                var tasks = new List<Task<AssetGenerationResult>>();
                for (int i = 0; i < 5; i++)
                {
                    tasks.Add(manager.GenerateAsync(Request("seq-" + i), AssetForgePriority.Background, CancellationToken.None));
                }

                var results = await Task.WhenAll(tasks);
                Assert.That(results.Length, Is.EqualTo(5));
                for (int i = 0; i < results.Length; i++)
                {
                    Assert.That(results[i].Success, Is.True);
                }

                Assert.That(fake.MaxActive, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task GenerateAsync_PlayerFacingPreemptsQueuedBackground()
        {
            var fake = new PriorityProbeFakeForge();
            using (var manager = new GenerationManager(fake))
            {
                var bg1 = manager.GenerateAsync(Request("bg-1"), AssetForgePriority.Background, CancellationToken.None);
                Assert.That(fake.WaitForFirstStart(TimeSpan.FromSeconds(2)), Is.True);

                var bg2 = manager.GenerateAsync(Request("bg-2"), AssetForgePriority.Background, CancellationToken.None);
                var bg3 = manager.GenerateAsync(Request("bg-3"), AssetForgePriority.Background, CancellationToken.None);
                var player = manager.GenerateAsync(Request("player"), AssetForgePriority.PlayerFacing, CancellationToken.None);

                fake.ReleaseFirst();

                await Task.WhenAll(bg1, bg2, bg3, player);
                var completionOrder = fake.CompletionOrder;

                Assert.That(IndexOf(completionOrder, "player"), Is.LessThan(IndexOf(completionOrder, "bg-2")));
                Assert.That(IndexOf(completionOrder, "player"), Is.LessThan(IndexOf(completionOrder, "bg-3")));
            }
        }

        private static int IndexOf(IReadOnlyList<string> ordered, string requestId)
        {
            for (int i = 0; i < ordered.Count; i++)
            {
                if (string.Equals(ordered[i], requestId, StringComparison.Ordinal))
                    return i;
            }

            return int.MaxValue;
        }

        private static AssetGenerationRequest Request(string id)
        {
            return new AssetGenerationRequest(
                requestId: id,
                subject: AssetSubjectKind.Item,
                style: WorldStyle.DarkFantasyGrim,
                genre: WorldGenre.PoliticalIntrigue,
                moodKeyword: "grim",
                promptHash: id.PadRight(64, 'h'),
                width: 64,
                height: 64,
                seed: 7,
                prompt: id,
                negativePrompt: string.Empty);
        }

        private sealed class SequentialFakeForge : IAssetForge
        {
            private int _active;
            private int _maxActive;

            public int MaxActive => _maxActive;

            public bool IsAvailable() => true;

            public async Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                var now = Interlocked.Increment(ref _active);
                if (now != 1)
                {
                    throw new InvalidOperationException("GenerationManager allowed concurrent forge execution.");
                }

                TrySetMax(now);
                try
                {
                    await Task.Delay(40, cancellationToken);
                    return new AssetGenerationResult(request.RequestId, new byte[] { 1 }, "image/png", 1, true, string.Empty);
                }
                finally
                {
                    Interlocked.Decrement(ref _active);
                }
            }

            private void TrySetMax(int candidate)
            {
                while (true)
                {
                    var current = _maxActive;
                    if (candidate <= current) return;
                    if (Interlocked.CompareExchange(ref _maxActive, candidate, current) == current) return;
                }
            }
        }

        private sealed class PriorityProbeFakeForge : IAssetForge
        {
            private readonly ManualResetEventSlim _firstStarted = new ManualResetEventSlim(false);
            private readonly object _gate = new object();
            private readonly List<string> _completionOrder = new List<string>();
            private volatile bool _releaseFirst;
            private int _started;

            public bool IsAvailable() => true;

            public IReadOnlyList<string> CompletionOrder
            {
                get
                {
                    lock (_gate)
                    {
                        return _completionOrder.ToArray();
                    }
                }
            }

            public bool WaitForFirstStart(TimeSpan timeout)
            {
                return _firstStarted.Wait(timeout);
            }

            public void ReleaseFirst()
            {
                _releaseFirst = true;
            }

            public async Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                var started = Interlocked.Increment(ref _started);
                if (started == 1)
                {
                    _firstStarted.Set();
                    while (!_releaseFirst)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(5, cancellationToken);
                    }
                }
                else
                {
                    await Task.Delay(10, cancellationToken);
                }

                lock (_gate)
                {
                    _completionOrder.Add(request.RequestId);
                }

                return new AssetGenerationResult(request.RequestId, new byte[] { 2 }, "image/png", 1, true, string.Empty);
            }
        }
    }
}

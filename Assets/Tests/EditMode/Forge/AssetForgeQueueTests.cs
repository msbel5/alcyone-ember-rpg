using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class AssetForgeQueueTests
    {
        [Test]
        public async Task Queue_DequeueHonorsPriority()
        {
            var queue = new AssetForgeQueue(4);
            await queue.EnqueueAsync(Request("background"), AssetForgePriority.Background, CancellationToken.None);
            await queue.EnqueueAsync(Request("player"), AssetForgePriority.PlayerFacing, CancellationToken.None);

            Assert.That((await queue.DequeueAsync(CancellationToken.None)).RequestId, Is.EqualTo("player"));
            Assert.That((await queue.DequeueAsync(CancellationToken.None)).RequestId, Is.EqualTo("background"));
        }

        [Test]
        public void Queue_CancellationHonoredWhenFull()
        {
            var queue = new AssetForgeQueue(1);
            queue.EnqueueAsync(Request("one"), AssetForgePriority.Background, CancellationToken.None).GetAwaiter().GetResult();
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                Assert.ThrowsAsync<TaskCanceledException>(() => queue.EnqueueAsync(Request("two"), AssetForgePriority.Background, cts.Token));
            }
        }

        [Test]
        public async Task Queue_BackpressureReleasesAfterDequeue()
        {
            var queue = new AssetForgeQueue(1);
            await queue.EnqueueAsync(Request("one"), AssetForgePriority.Background, CancellationToken.None);
            var pending = queue.EnqueueAsync(Request("two"), AssetForgePriority.Background, CancellationToken.None);
            Assert.That(pending.IsCompleted, Is.False);
            await queue.DequeueAsync(CancellationToken.None);
            await pending;
            Assert.That(queue.Count, Is.EqualTo(1));
        }

        private static AssetGenerationRequest Request(string id)
        {
            return new AssetGenerationRequest(id, AssetSubjectKind.Npc, WorldStyle.DarkFantasyGrim, WorldGenre.PoliticalIntrigue, "grim", id.PadRight(64, 'a'), 64, 64, 1, id, "");
        }
    }
}

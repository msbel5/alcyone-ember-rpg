using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Simulation.Forge
{
    public enum AssetForgePriority
    {
        PlayerFacing = 0,
        Nearby = 1,
        Background = 2,
    }

    public sealed class AssetForgeQueue
    {
        private readonly int _capacity;
        private readonly Queue<AssetGenerationRequest>[] _queues =
        {
            new Queue<AssetGenerationRequest>(),
            new Queue<AssetGenerationRequest>(),
            new Queue<AssetGenerationRequest>(),
        };
        private readonly SemaphoreSlim _items = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _slots;
        private readonly object _gate = new object();
        private int _count;

        public AssetForgeQueue(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _slots = new SemaphoreSlim(capacity);
        }

        public int Count
        {
            get { lock (_gate) return _count; }
        }

        public async Task EnqueueAsync(AssetGenerationRequest request, AssetForgePriority priority, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            await _slots.WaitAsync(cancellationToken).ConfigureAwait(false);
            lock (_gate)
            {
                _queues[(int)priority].Enqueue(request);
                _count++;
            }
            _items.Release();
        }

        public async Task<AssetGenerationRequest> DequeueAsync(CancellationToken cancellationToken)
        {
            await _items.WaitAsync(cancellationToken).ConfigureAwait(false);
            lock (_gate)
            {
                for (int i = 0; i < _queues.Length; i++)
                {
                    if (_queues[i].Count == 0) continue;
                    _count--;
                    _slots.Release();
                    return _queues[i].Dequeue();
                }
            }

            _slots.Release();
            throw new InvalidOperationException("Queue semaphore and queue state diverged.");
        }
    }
}

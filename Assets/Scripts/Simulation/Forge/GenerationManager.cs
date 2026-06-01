using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Simulation.Forge
{
    public sealed class GenerationManager : IDisposable
    {
        private readonly IAssetForge _forge;
        private readonly IResourceProbe _resourceProbe;
        private readonly AssetForgeQueue _queue;
        private readonly Dictionary<AssetGenerationRequest, Queue<WorkItem>> _pendingByRequest =
            new Dictionary<AssetGenerationRequest, Queue<WorkItem>>();
        private readonly object _gate = new object();
        private readonly CancellationTokenSource _workerCancellation = new CancellationTokenSource();
        private readonly Task _workerTask;
        private bool _disposed;

        // ONE generation at a time is enforced by the SINGLE worker loop below (the hard invariant — never
        // run two heavy gens at once / OOM the GPU). `maxPending` is the queue's BACKPRESSURE (how many
        // requests may sit queued), NOT a concurrency limit: it must be large enough to hold all pending
        // requests so the queue can order them by priority (PlayerFacing first). A value of 1 here would let
        // only one request sit in the queue, forcing the rest to block on enqueue and race for the freed
        // slot in undefined order — which breaks priority preemption.
        public GenerationManager(IAssetForge forge, int maxPending = 4096, IResourceProbe resourceProbe = null)
        {
            if (forge == null) throw new ArgumentNullException(nameof(forge));
            if (maxPending <= 0) throw new ArgumentOutOfRangeException(nameof(maxPending));

            _forge = forge;
            _resourceProbe = resourceProbe ?? new NullResourceProbe();
            _queue = new AssetForgeQueue(maxPending);
            _workerTask = Task.Run(WorkerLoopAsync);
        }

        public async Task<AssetGenerationResult> GenerateAsync(AssetGenerationRequest request, AssetForgePriority priority, CancellationToken ct)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            ThrowIfDisposed();

            var workItem = new WorkItem(request, ct);
            if (ct.CanBeCanceled)
            {
                workItem.CancellationRegistration = ct.Register(() => workItem.Completion.TrySetCanceled(ct));
            }

            AddPending(workItem);

            try
            {
                await _queue.EnqueueAsync(request, priority, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                RemovePending(workItem);
                workItem.Completion.TrySetCanceled(ct);
            }
            catch (Exception ex)
            {
                RemovePending(workItem);
                workItem.Completion.TrySetException(ex);
            }

            return await workItem.Completion.Task.ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _workerCancellation.Cancel();
            try
            {
                _workerTask.GetAwaiter().GetResult();
            }
            catch
            {
            }

            FailPending(new ObjectDisposedException(nameof(GenerationManager)));
            _workerCancellation.Dispose();
        }

        private async Task WorkerLoopAsync()
        {
            while (true)
            {
                AssetGenerationRequest request;
                try
                {
                    request = await _queue.DequeueAsync(_workerCancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                var workItem = TakePending(request);
                if (workItem == null) continue;

                if (workItem.Completion.Task.IsCompleted)
                {
                    workItem.CancellationRegistration.Dispose();
                    continue;
                }

                try
                {
                    var guardedRequest = ApplyResourceGuard(workItem.Request);
                    var result = await _forge.GenerateAsync(guardedRequest, workItem.CancellationToken).ConfigureAwait(false);
                    workItem.Completion.TrySetResult(result);
                }
                catch (OperationCanceledException)
                {
                    workItem.Completion.TrySetCanceled(workItem.CancellationToken);
                }
                catch (Exception ex)
                {
                    workItem.Completion.TrySetException(ex);
                }
                finally
                {
                    workItem.CancellationRegistration.Dispose();
                }
            }
        }

        private void AddPending(WorkItem workItem)
        {
            lock (_gate)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(GenerationManager));

                if (!_pendingByRequest.TryGetValue(workItem.Request, out var bucket))
                {
                    bucket = new Queue<WorkItem>();
                    _pendingByRequest[workItem.Request] = bucket;
                }

                bucket.Enqueue(workItem);
            }
        }

        private void RemovePending(WorkItem workItem)
        {
            lock (_gate)
            {
                if (!_pendingByRequest.TryGetValue(workItem.Request, out var bucket) || bucket.Count == 0) return;
                if (ReferenceEquals(bucket.Peek(), workItem))
                {
                    bucket.Dequeue();
                    if (bucket.Count == 0) _pendingByRequest.Remove(workItem.Request);
                    return;
                }

                var retained = new Queue<WorkItem>();
                while (bucket.Count > 0)
                {
                    var candidate = bucket.Dequeue();
                    if (!ReferenceEquals(candidate, workItem))
                    {
                        retained.Enqueue(candidate);
                    }
                }

                if (retained.Count == 0)
                {
                    _pendingByRequest.Remove(workItem.Request);
                    return;
                }

                _pendingByRequest[workItem.Request] = retained;
            }
        }

        private WorkItem TakePending(AssetGenerationRequest request)
        {
            lock (_gate)
            {
                if (!_pendingByRequest.TryGetValue(request, out var bucket) || bucket.Count == 0) return null;
                var item = bucket.Dequeue();
                if (bucket.Count == 0) _pendingByRequest.Remove(request);
                return item;
            }
        }

        private void FailPending(Exception exception)
        {
            List<WorkItem> pending = null;

            lock (_gate)
            {
                foreach (var pair in _pendingByRequest)
                {
                    if (pair.Value.Count == 0) continue;
                    if (pending == null) pending = new List<WorkItem>();
                    while (pair.Value.Count > 0)
                    {
                        pending.Add(pair.Value.Dequeue());
                    }
                }

                _pendingByRequest.Clear();
            }

            if (pending == null) return;

            for (int i = 0; i < pending.Count; i++)
            {
                pending[i].Completion.TrySetException(exception);
                pending[i].CancellationRegistration.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GenerationManager));
        }

        private AssetGenerationRequest ApplyResourceGuard(AssetGenerationRequest request)
        {
            var requiredVideoMemoryMb = EstimateRequiredVideoMemoryMb(request.Width, request.Height);
            var availableVideoMemoryMb = _resourceProbe.AvailableVideoMemoryMb();
            var isLargerThan512 = request.Width > 512 || request.Height > 512;
            if (availableVideoMemoryMb >= requiredVideoMemoryMb || !isLargerThan512)
            {
                return request;
            }

            // Resource guard decision: downscale heavy requests before forge execution to reduce OOM risk.
            return new AssetGenerationRequest(
                requestId: request.RequestId,
                subject: request.Subject,
                style: request.Style,
                genre: request.Genre,
                moodKeyword: request.MoodKeyword,
                promptHash: request.PromptHash,
                width: 512,
                height: 512,
                seed: request.Seed,
                prompt: request.Prompt,
                negativePrompt: request.NegativePrompt,
                timeoutSeconds: request.TimeoutSeconds,
                modelHint: request.ModelHint,
                steps: request.Steps);
        }

        private static long EstimateRequiredVideoMemoryMb(int width, int height)
        {
            var pixels = (long)width * height;
            return pixels <= 0 ? 1 : pixels / 256;
        }

        private sealed class WorkItem
        {
            public WorkItem(AssetGenerationRequest request, CancellationToken cancellationToken)
            {
                Request = request;
                CancellationToken = cancellationToken;
                Completion = new TaskCompletionSource<AssetGenerationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public AssetGenerationRequest Request { get; }
            public CancellationToken CancellationToken { get; }
            public TaskCompletionSource<AssetGenerationResult> Completion { get; }
            public CancellationTokenRegistration CancellationRegistration { get; set; }
        }
    }
}

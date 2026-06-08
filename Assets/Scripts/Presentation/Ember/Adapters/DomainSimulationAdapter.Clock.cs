namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        // ----- IEmberSimulationClock -----
        public void AdvanceTick(int tickIndex)
        {
            DrainMainThreadApply(); // DET-02: apply queued off-thread LLM results on the main thread
            _tick = tickIndex;
            _tickComposer.Advance(_world, tickIndex);
        }

        // DET-02: post-await LLM continuations enqueue their _world / dialog-state writes here instead
        // of mutating shared state on whatever thread the await resumes on. Relying on Unity's
        // SynchronizationContext to marshal them back to the main thread is implicit and null in a
        // headless run, which would reopen the EMB-007 race on _world. Draining here, at the top of the
        // deterministic main-thread tick, guarantees those writes land on the main thread in order.
        private readonly System.Collections.Concurrent.ConcurrentQueue<System.Action> _mainThreadApply
            = new System.Collections.Concurrent.ConcurrentQueue<System.Action>();

        private void DrainMainThreadApply()
        {
            while (_mainThreadApply.TryDequeue(out var apply))
            {
                try { apply(); }
                catch (System.Exception) { /* a queued apply must never break the tick */ }
            }
        }
        public int TickIndex => _tick;

    }
}

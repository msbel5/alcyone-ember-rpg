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
            PublishFieldMirror();
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

        // F1/CROPS: publish the home site's REAL PlantGrowth stage census to the field mirror each tick —
        // the realized farm plot's stalks read it and rise from seed to ripe as sim days pass. Cheap scan
        // (a handful of plant components); dominant stage keeps the visual stable.
        private void PublishFieldMirror()
        {
            var plants = _world?.Plants;
            if (plants == null) return;

            var site = SettlementSiteId(CurrentSettlementOrStart);
            int seedCount = 0, sproutCount = 0, ripeCount = 0;
            foreach (var row in plants.Rows)
            {
                var p = row.Value;
                if (p == null || !p.SiteId.Equals(site)) continue;
                switch (p.StageId.Value)
                {
                    case "ripe": ripeCount++; break;
                    case "sprout": sproutCount++; break;
                    default: seedCount++; break;
                }
            }

            int stage = ripeCount > 0 && ripeCount >= sproutCount && ripeCount >= seedCount ? 2
                : (sproutCount > 0 && sproutCount >= seedCount ? 1 : 0);
            EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.Publish(
                seedCount + sproutCount + ripeCount, stage);
            // F6/night staging: the street empties after dark — curfew views read this hour.
            EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.HourOfDay =
                (int)((_world.Time.TotalMinutes / EmberCrpg.Domain.Core.GameTime.MinutesPerHour) % 24);

            // F1/CARAVANS: how many caravans are AT the home site right now — the plaza trade cart shows
            // itself only while one is in town, so the daily CaravanSystem becomes watchable.
            int atSite = 0;
            var caravans = _world.Caravans;
            if (caravans != null)
            {
                for (int i = 0; i < caravans.Count; i++)
                {
                    if (caravans[i] != null && caravans[i].CurrentSiteId.Equals(site))
                        atSite++;
                }
            }
            EmberCrpg.Presentation.Ember.WorldDirector.RuntimeCaravanMirror.Publish(atSite);
        }
    }
}

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
            PublishEventEchoes();
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
        private int _echoCursor = -1;
        private ulong _lastPlantsHash;

        // M6: every NEW world event that names actors becomes a floating echo. Cursor starts at
        // the CURRENT end so loading a 10k-event save replays nothing; per-tick scan is capped.
        private void PublishEventEchoes()
        {
            var events = _world?.Events?.Events;
            if (events == null) return;
            if (_echoCursor < 0 || _echoCursor > events.Count) { _echoCursor = events.Count; return; }
            int start = events.Count - _echoCursor > 256 ? events.Count - 256 : _echoCursor;
            for (int i = start; i < events.Count; i++)
            {
                var evt = events[i];
                ulong subject = evt.ActorId.Value;
                switch (evt.Kind)
                {
                    case EmberCrpg.Domain.World.WorldEventKind.WitnessRecorded:
                        bool isReport = evt.Reason != null && evt.Reason.StartsWith("reported", System.StringComparison.Ordinal);
                        if (subject != 0UL)
                            EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.Raise(
                                subject, isReport
                                    ? EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.KindReport
                                    : EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.KindWitness);
                        break;
                    case EmberCrpg.Domain.World.WorldEventKind.GuardResponded:
                        if (subject != 0UL)
                            EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.Raise(
                                subject, EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.KindGuard);
                        break;
                    case EmberCrpg.Domain.World.WorldEventKind.PlantHarvested:
                        if (subject != 0UL)
                            EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.Raise(
                                subject, EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.KindHarvest);
                        break;
                    case EmberCrpg.Domain.World.WorldEventKind.ActorTalked:
                        if (subject != 0UL)
                        {
                            EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.Raise(
                                subject, EmberCrpg.Presentation.Ember.WorldDirector.NpcEventEchoFeed.KindTalk);
                            // W31: the chat pictogram and the spatial mutter come from the SAME event
                            EmberCrpg.Presentation.Ember.Audio.AmbientVoiceDirector.Offer(
                                subject, EmberCrpg.Simulation.Living.RumorMillSystem.PickFor(
                                    _world, subject, evt.SiteId, _world.Time));
                        }
                        break;
                }
            }
            _echoCursor = events.Count;
        }

        private void PublishFieldMirror()
        {
            var plants = _world?.Plants;
            if (plants == null) return;

            var site = SettlementSiteId(CurrentSettlementOrStart);
            var origin = BillboardOrigin();
            int seedCount = 0, sproutCount = 0, ripeCount = 0;
            ulong plantsHash = 1469598103934665603UL;
            var plantCells = new System.Collections.Generic.List<
                EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.PlantCell>();
            foreach (var row in plants.Rows)
            {
                var p = row.Value;
                if (p == null || !p.SiteId.Equals(site)) continue;
                int stageIdx = p.StageId.Value == "ripe" ? 2 : (p.StageId.Value == "sprout" ? 1 : 0);
                if (plantCells.Count < 64)
                    plantCells.Add(new EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.PlantCell
                    {
                        Id = p.Id.Value,
                        LocalX = p.Position.X - origin.X,
                        LocalZ = p.Position.Y - origin.Y,
                        Stage = stageIdx,
                    });
                plantsHash = (plantsHash ^ (p.Id.Value + (ulong)stageIdx)) * 1099511628211UL;
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
            // REFORM #1: per-plant cells go out only when the composition actually changed.
            if (plantsHash != _lastPlantsHash)
            {
                _lastPlantsHash = plantsHash;
                EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.PublishPlants(plantCells.ToArray());
            }
            // F6/night staging: the street empties after dark — curfew views read this hour.
            EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.HourOfDay =
                (int)((_world.Time.TotalMinutes / EmberCrpg.Domain.Core.GameTime.MinutesPerHour) % 24);
            // F24: the sky reads WORLD-TIME TRUTH in minutes — the old tick re-derivation drifted
            // after clock jumps (respawn +8h, travel days) and left bright skies at midnight.
            EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.MinutesOfDay =
                (int)(_world.Time.TotalMinutes % EmberCrpg.Domain.Core.GameTime.MinutesPerDay);
            // F25: the deterministic weather pick keys on the world day.
            EmberCrpg.Presentation.Ember.WorldDirector.RuntimeFieldMirror.WorldDay =
                (int)(_world.Time.TotalMinutes / EmberCrpg.Domain.Core.GameTime.MinutesPerDay) + 1;

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

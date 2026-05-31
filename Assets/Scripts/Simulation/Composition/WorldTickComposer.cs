using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Magic;
using EmberCrpg.Simulation.Process;
using EmberCrpg.Simulation.Time;
using EmberCrpg.Simulation.World;

namespace EmberCrpg.Simulation.Composition
{
    /// <summary>
    /// Codex audit (sixth pass A-P0): the live game runtime previously had no
    /// per-tick orchestration — <c>DomainSimulationAdapter.AdvanceTick(int)</c>
    /// was a plain setter, so every Ember scene ran a frozen simulation.
    /// This composer owns the ordered call list of deterministic systems
    /// that must move forward per Ember tick. It is intentionally conservative:
    /// only systems whose schedule is well-defined for "1 ember-tick =
    /// <c>MinutesPerTick</c> game minutes" are wired here. Higher-frequency
    /// system composition (plant growth, faction reputation decay, caravan
    /// motion) will land in subsequent passes once their tick contracts are
    /// re-examined; the audit's primary risk call specifically warned
    /// against wiring them at the raw 10 Hz Presentation tick driver (EmberTickDriver)
    /// rate without first auditing their cost gating.
    /// </summary>
    public sealed class WorldTickComposer
    {
        /// <summary>
        /// 1 ember-tick == 1 in-game minute. Aligns with
        /// <c>EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter.HudText</c>
        /// which computes day-rollover as <c>1 + _tick / 240</c> (240
        /// minutes-per-in-game-day).
        /// </summary>
        public const long MinutesPerTick = 1L;

        /// <summary>
        /// Game-day length in ember ticks. Matches <c>DomainSimulationAdapter.HudText</c>
        /// (which formats `Day = 1 + tick / 240`). Anything that wants to run
        /// "once per game day" should gate on this constant.
        /// </summary>
        public const int TicksPerGameDay = 240;

        /// <summary>
        /// Game-hour length in ember ticks. Used to gate <see cref="NeedsSystem"/>
        /// so per-actor needs decay at its design rate (HungerIncreasePerTick=20
        /// per logical tick, which the system author sized as "per game hour")
        /// rather than every minute.
        /// </summary>
        public const int TicksPerGameHour = TicksPerGameDay / 24;

        private readonly GameTimeAdvanceSystem _timeAdvance;
        private readonly NeedsSystem _needs;
        private readonly MagicTickDriver _magic;
        private readonly CaravanSystem _caravans;
        // SOUL-01/03: production-economy + living systems wired into the cadence blocks below.
        private readonly PlantGrowthSystem _plantGrowth;
        private readonly JobAssignmentSystem _jobAssignment;
        private readonly PriceUpdateSystem _priceUpdate;
        private readonly ScheduleSystem _schedule;
        // The composer owns its own SeasonCalendar (same canonical 4-season layout the time-advance
        // system uses) so the daily growth block can resolve a Season from world.Time without reaching
        // into the time system. The species catalog is the deterministic set of crops growth runs over.
        private readonly SeasonCalendar _seasonCalendar;
        private readonly IReadOnlyList<PlantSpeciesDef> _plantSpecies;

        // SOUL-01 price-update gating constants. A stockpile tag below LowStock pushes its site price up
        // by PriceStep; above HighStock pushes it down. Conservative defaults kept local to the composer.
        private const int LowStock = 4;
        private const int HighStock = 64;
        private const int PriceStep = 1;

        private int _lastTickIndex = -1;
        private int _ticksSinceHourly;
        private int _ticksSinceDaily;

        public WorldTickComposer()
            : this(
                new GameTimeAdvanceSystem(BuildDefaultCalendar()),
                new NeedsSystem(),
                new MagicTickDriver(new SpellCooldownService(), new ShieldBuffService()),
                new CaravanSystem(),
                new PlantGrowthSystem(),
                new JobAssignmentSystem(),
                new PriceUpdateSystem(),
                new ScheduleSystem())
        {
        }

        private static SeasonCalendar BuildDefaultCalendar()
        {
            // Canonical 4-season calendar: 90 days each, Spring opens day 1.
            return new SeasonCalendar(new[]
            {
                new SeasonDefinition(Season.Spring, 1, 90),
                new SeasonDefinition(Season.Summer, 91, 180),
                new SeasonDefinition(Season.Autumn, 181, 270),
                new SeasonDefinition(Season.Winter, 271, 360),
            });
        }

        // SOUL-01: the deterministic crop catalog the daily growth block advances. Mirrors the species
        // id ("wheat") seeded by DomainSimulationAdapter so worldgen-seeded crops actually grow. Stage
        // chain seed -> sprout -> ripe; each non-terminal stage advances after one game-day. Grows in
        // every season (Season.None rule) and is not snow-blocked so growth is visible from day 1.
        private static IReadOnlyList<PlantSpeciesDef> BuildDefaultPlantSpecies()
        {
            var wheat = new PlantSpeciesDef(
                "wheat",
                "wheat_seed",
                "wheat_grain",
                new[]
                {
                    new PlantGrowthStageDef(new PlantStageId("seed"), "Seed", daysToNextStage: 1, isHarvestable: false),
                    new PlantGrowthStageDef(new PlantStageId("sprout"), "Sprout", daysToNextStage: 1, isHarvestable: false),
                    new PlantGrowthStageDef(new PlantStageId("ripe"), "Ripe", daysToNextStage: 0, isHarvestable: true),
                },
                new[]
                {
                    new PlantGrowthRule(Season.None, allowsGrowth: true, blockedBySnow: false),
                });

            return new[] { wheat };
        }

        // Back-compat overload: callers that only customise the time/needs/magic/caravan systems get
        // the default production-economy + schedule systems.
        public WorldTickComposer(
            GameTimeAdvanceSystem timeAdvance,
            NeedsSystem needs,
            MagicTickDriver magic,
            CaravanSystem caravans)
            : this(
                timeAdvance,
                needs,
                magic,
                caravans,
                new PlantGrowthSystem(),
                new JobAssignmentSystem(),
                new PriceUpdateSystem(),
                new ScheduleSystem())
        {
        }

        // SOUL-01/03 canonical ctor: injects the production-economy (plant growth, job assignment,
        // price update) and living (schedule) systems alongside the original four. Defaults are wired
        // by the parameterless and back-compat ctors so existing call sites are unaffected.
        public WorldTickComposer(
            GameTimeAdvanceSystem timeAdvance,
            NeedsSystem needs,
            MagicTickDriver magic,
            CaravanSystem caravans,
            PlantGrowthSystem plantGrowth,
            JobAssignmentSystem jobAssignment,
            PriceUpdateSystem priceUpdate,
            ScheduleSystem schedule)
        {
            _timeAdvance = timeAdvance ?? throw new ArgumentNullException(nameof(timeAdvance));
            _needs = needs ?? throw new ArgumentNullException(nameof(needs));
            _magic = magic ?? throw new ArgumentNullException(nameof(magic));
            _caravans = caravans ?? throw new ArgumentNullException(nameof(caravans));
            _plantGrowth = plantGrowth ?? throw new ArgumentNullException(nameof(plantGrowth));
            _jobAssignment = jobAssignment ?? throw new ArgumentNullException(nameof(jobAssignment));
            _priceUpdate = priceUpdate ?? throw new ArgumentNullException(nameof(priceUpdate));
            _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            _seasonCalendar = BuildDefaultCalendar();
            _plantSpecies = BuildDefaultPlantSpecies();
        }

        // Codex audit (seventh pass review on PR #198): single-arg constructor
        // kept for any caller that already passed a custom calendar; defaults
        // the NeedsSystem to the canonical mood evaluator.
        public WorldTickComposer(GameTimeAdvanceSystem timeAdvance)
            : this(
                timeAdvance,
                new NeedsSystem(),
                new MagicTickDriver(new SpellCooldownService(), new ShieldBuffService()),
                new CaravanSystem())
        {
        }

        /// <summary>
        /// Advance the deterministic simulation by the delta between the
        /// caller's previous tickIndex and the new tickIndex. Idempotent:
        /// calling with the same tickIndex twice is a no-op. Going backwards
        /// (e.g. after a Restore that resets the driver) re-anchors without
        /// rewinding domain state.
        /// </summary>
        public void Advance(WorldState world, int tickIndex)
        {
            if (world == null) return;
            int delta = _lastTickIndex < 0 ? 0 : tickIndex - _lastTickIndex;
            _lastTickIndex = tickIndex;
            if (delta <= 0) return;

            // 1) Always advance game time (cheap, monotonic).
            world.Time = _timeAdvance.Advance(world.Time, delta * MinutesPerTick);
            if (world.PlayerSpellCooldowns != null && world.PlayerShieldBuffs != null)
                _magic.AdvanceTicks(world.PlayerSpellCooldowns, world.PlayerShieldBuffs, delta);

            // 2) Hourly tick: NeedsSystem decays per-actor needs at its
            // design rate. Codex audit (seventh pass A-P1 #1): previously the
            // composer only advanced time, so colony needs never moved in a
            // running game. Gate at TicksPerGameHour so the existing
            // HungerIncreasePerTick=20 numbers stay calibrated.
            // Codex ninth-pass A-P2 / G-P2: catch-up events must be stamped
            // at the cadence-boundary timestamp, not at the post-advance
            // time, so event-log replays show needs ticking at hour
            // boundaries. We compute the number of boundary crossings up
            // front, then iterate forward stamping each crossing at its
            // exact minute. This is deterministically equivalent to the
            // original modulus juggling but avoids the off-by-one when more
            // than one boundary is crossed in a single Advance call.
            _ticksSinceHourly += delta;
            int hourlyCrossings = _ticksSinceHourly / TicksPerGameHour;
            _ticksSinceHourly -= hourlyCrossings * TicksPerGameHour;
            for (int i = 1; i <= hourlyCrossings; i++)
            {
                if (world.Actors == null || world.Events == null) continue;
                // Boundary i was reached at world.Time - (totalRemaining) where
                // totalRemaining = (hourlyCrossings - i)*hour + _ticksSinceHourly.
                long stampMinutes = world.Time.TotalMinutes
                                    - (long)_ticksSinceHourly
                                    - ((long)hourlyCrossings - i) * TicksPerGameHour;
                var stamp = new GameTime(stampMinutes < 0 ? 0 : stampMinutes);

                // SOUL-01: assign pending jobs to idle, willing actors BEFORE needs decay this hour, so
                // the first crossing can claim while hunger is still below the refusal threshold. The
                // basic TryAssignNext overload already sets the claimed actor's ScheduleState to
                // Assigned; we re-affirm it (idempotent) and append a JobAssigned event for the log.
                AssignPendingJobs(world, stamp);

                // SOUL-03: step every assigned actor one tile toward its worksite (or home at night).
                if (world.Actors != null)
                    _schedule.Advance(world.Actors, stamp);

                foreach (var actor in world.Actors.Records)
                {
                    if (actor == null) continue;
                    _needs.TickActorNeeds(actor, world.Events, stamp, ticks: 1);
                }
            }

            _ticksSinceDaily += delta;
            int dailyCrossings = _ticksSinceDaily / TicksPerGameDay;
            _ticksSinceDaily -= dailyCrossings * TicksPerGameDay;
            for (int i = 1; i <= dailyCrossings; i++)
            {
                if (world.Events == null) continue;
                long stampMinutes = world.Time.TotalMinutes
                                    - (long)_ticksSinceDaily
                                    - ((long)dailyCrossings - i) * TicksPerGameDay;
                var stamp = new GameTime(stampMinutes < 0 ? 0 : stampMinutes);

                if (world.Caravans != null)
                    _caravans.Tick(world.Caravans, world.FindTradeRoute, world.FindStockpile, stamp, world.Events);

                // SOUL-01: advance crops one game-day and drift site prices with stockpile levels.
                AdvancePlantGrowth(world, stamp);
                RecomputePrices(world, stamp);
            }
        }

        /// <summary>
        /// SOUL-01: claim as many pending jobs as currently possible. The basic assignment overload is
        /// deterministic and self-terminating (returns false once no idle, willing actor matches a
        /// pending job). Each claim re-affirms the actor's Assigned schedule state and logs an event.
        /// </summary>
        private void AssignPendingJobs(WorldState world, GameTime stamp)
        {
            if (world.Actors == null || world.Jobs == null || world.Worksites == null)
                return;

            while (_jobAssignment.TryAssignNext(world.Actors, world.Jobs, world.Worksites, out var result))
            {
                if (world.Actors.TryGet(result.ActorId, out var actor) && actor != null)
                {
                    actor.ApplyScheduleState(ActorScheduleState.Assigned(
                        result.JobId, result.SiteId, result.WorksitePosition));
                }

                world.Events?.Append(new WorldEvent(
                    stamp,
                    WorldEventKind.JobAssigned,
                    result.ActorId,
                    result.SiteId,
                    $"job_assigned:{result.JobId.Value}",
                    new ReasonTrace(new[]
                    {
                        $"job:{result.JobId.Value}",
                        $"actor:{result.ActorId.Value}",
                        $"site:{result.SiteId.Value}",
                        $"worksite:{result.WorksitePosition.X},{result.WorksitePosition.Y}",
                    })));
            }
        }

        /// <summary>
        /// SOUL-01: advance every catalogued crop species by one game-day. PlantGrowthSystem no-ops for
        /// species/seasons that cannot grow and for empty plant stores, so this is safe every day.
        /// </summary>
        private void AdvancePlantGrowth(WorldState world, GameTime stamp)
        {
            if (world.Plants == null || world.Events == null || _plantSpecies == null)
                return;

            var season = ResolveSeason(stamp);
            for (int s = 0; s < _plantSpecies.Count; s++)
            {
                _plantGrowth.AdvanceOneDay(
                    _plantSpecies[s],
                    world.Plants,
                    world.Events,
                    stamp,
                    season,
                    isSnowing: false);
            }
        }

        /// <summary>
        /// SOUL-01: drift each site price toward scarcity/surplus. For every stockpile, every tracked
        /// item tag is recomputed: below LowStock the price rises by PriceStep, above HighStock it
        /// falls. PriceUpdateSystem only emits a PriceChanged event when a price actually moves.
        /// </summary>
        private void RecomputePrices(WorldState world, GameTime stamp)
        {
            if (world.Prices == null || world.Stockpiles == null || world.Events == null)
                return;

            foreach (var stockpile in world.Stockpiles)
            {
                if (stockpile == null) continue;
                foreach (var entry in stockpile.Entries)
                {
                    _priceUpdate.Recompute(
                        world.Prices,
                        stockpile,
                        entry.Key,
                        LowStock,
                        HighStock,
                        PriceStep,
                        stamp,
                        world.Events);
                }
            }
        }

        private Season ResolveSeason(GameTime time)
        {
            return _seasonCalendar != null && _seasonCalendar.TryGetSeason(time, out var season)
                ? season
                : Season.Spring;
        }

        /// <summary>
        /// Reset the composer's tick anchor — call after a save restore so
        /// the next Advance does not double-tick the restored time.
        ///
        /// Codex audit (eighth pass A-P2): previously this also zeroed the
        /// hourly accumulator, which silently dropped any overdue needs/daily
        /// progress that had accumulated across a save/load boundary. The
        /// _ticksSinceHourly / _ticksSinceDaily accumulators are now
        /// preserved so the next Advance flushes the pending hourly tick
        /// instead of restarting the gate. Callers must still invoke
        /// ResetAnchor only after <see cref="WorldState.Time"/> has
        /// been restored — anchor reset itself does not rewind domain state.
        /// </summary>
        public void ResetAnchor()
        {
            _lastTickIndex = -1;
            // _ticksSinceHourly / _ticksSinceDaily intentionally preserved.
        }

        /// <summary>
        /// Codex ninth-pass A-P2: deterministically rebuild the
        /// hourly/daily accumulators from a restored <see cref="GameTime"/>.
        /// Use after save/load when the in-memory accumulator state can't
        /// be trusted. The remainder of (TotalMinutes mod TicksPerGameHour)
        /// is the in-flight progress toward the next hourly tick.
        /// </summary>
        public void RebuildAccumulatorsFrom(GameTime worldTime)
        {
            long minutes = worldTime.TotalMinutes;
            _ticksSinceHourly = (int)(minutes % TicksPerGameHour);
            _ticksSinceDaily = (int)(minutes % TicksPerGameDay);
            _lastTickIndex = -1;
        }
    }
}

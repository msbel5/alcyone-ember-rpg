// Why this file is intentionally long: WorldTickComposer is the deterministic cadence contract for every world tick system; keeping the ordered bands in one file makes replay drift reviewable.
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
    /// that move forward per Ember tick, gated into three cadence bands so
    /// each system runs at its design rate ("1 ember-tick =
    /// <c>MinutesPerTick</c> game minutes"):
    /// <list type="bullet">
    ///   <item>per-tick: <see cref="GameTimeAdvanceSystem"/> (clock) and
    ///   <see cref="MagicTickDriver"/> (spell cooldowns / shield buffs);</item>
    ///   <item>hourly (<see cref="TicksPerGameHour"/>): <see cref="JobAssignmentSystem"/>
    ///   claims pending jobs, <see cref="ScheduleSystem"/> steps assigned actors
    ///   toward their worksite/home, then <see cref="NeedsSystem"/> decays needs;</item>
    ///   <item>daily (<see cref="TicksPerGameDay"/>): <see cref="CaravanSystem"/>
    ///   motion, <see cref="PlantGrowthSystem"/> crop growth, and
    ///   <see cref="PriceUpdateSystem"/> stockpile-driven price drift.</item>
    /// </list>
    /// The SOUL-01/03 production-economy + living systems (plant growth, job
    /// assignment, price update, schedule) were originally deferred here; they
    /// are now wired into the cadence bands above. The sixth-pass audit's
    /// primary risk call — never run them at the raw 10 Hz Presentation tick
    /// driver (EmberTickDriver) rate — is honored by the hourly/daily gating,
    /// not by leaving them out. Faction reputation decay runs last in the
    /// daily band so explicit same-day reputation deltas land before drift.
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

        private readonly WorldTickRegistry _tickRegistry;

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
            : this(
                timeAdvance,
                needs,
                magic,
                caravans,
                plantGrowth,
                jobAssignment,
                priceUpdate,
                schedule,
                new FactionReputationDecaySystem(),
                FactionDecayConfig.Default)
        {
        }

        public WorldTickComposer(
            GameTimeAdvanceSystem timeAdvance,
            NeedsSystem needs,
            MagicTickDriver magic,
            CaravanSystem caravans,
            PlantGrowthSystem plantGrowth,
            JobAssignmentSystem jobAssignment,
            PriceUpdateSystem priceUpdate,
            ScheduleSystem schedule,
            FactionReputationDecaySystem factionDecay,
            FactionDecayConfig factionDecayConfig)
        {
            _tickRegistry = DefaultTickSystems.Create(
                timeAdvance ?? throw new ArgumentNullException(nameof(timeAdvance)),
                needs ?? throw new ArgumentNullException(nameof(needs)),
                magic ?? throw new ArgumentNullException(nameof(magic)),
                caravans ?? throw new ArgumentNullException(nameof(caravans)),
                plantGrowth ?? throw new ArgumentNullException(nameof(plantGrowth)),
                jobAssignment ?? throw new ArgumentNullException(nameof(jobAssignment)),
                priceUpdate ?? throw new ArgumentNullException(nameof(priceUpdate)),
                schedule ?? throw new ArgumentNullException(nameof(schedule)),
                factionDecay ?? throw new ArgumentNullException(nameof(factionDecay)),
                factionDecayConfig,
                BuildDefaultCalendar(),
                BuildDefaultPlantSpecies());
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

            // 1) Always advance game time and per-tick magic through the declarative registry.
            foreach (var system in _tickRegistry.PerTick)
                system.Run(new TickContext(world, world.Time, delta));

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

                foreach (var system in _tickRegistry.Hourly)
                    system.Run(new TickContext(world, stamp, delta));
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

                foreach (var system in _tickRegistry.Daily)
                    system.Run(new TickContext(world, stamp, delta));
            }
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

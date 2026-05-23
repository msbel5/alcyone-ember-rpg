using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Magic;
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
    /// against wiring them at the raw 10 Hz <see cref="EmberCrpg.Presentation.Ember.Tick.EmberTickDriver"/>
    /// rate without first auditing their cost gating.
    /// </summary>
    public sealed class SliceTickComposer
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
        private int _lastTickIndex = -1;
        private int _ticksSinceHourly;
        private int _ticksSinceDaily;

        public SliceTickComposer()
            : this(
                new GameTimeAdvanceSystem(BuildDefaultCalendar()),
                new NeedsSystem(),
                new MagicTickDriver(new SpellCooldownService(), new ShieldBuffService()),
                new CaravanSystem())
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

        public SliceTickComposer(
            GameTimeAdvanceSystem timeAdvance,
            NeedsSystem needs,
            MagicTickDriver magic,
            CaravanSystem caravans)
        {
            _timeAdvance = timeAdvance ?? throw new ArgumentNullException(nameof(timeAdvance));
            _needs = needs ?? throw new ArgumentNullException(nameof(needs));
            _magic = magic ?? throw new ArgumentNullException(nameof(magic));
            _caravans = caravans ?? throw new ArgumentNullException(nameof(caravans));
        }

        // Codex audit (seventh pass review on PR #198): single-arg constructor
        // kept for any caller that already passed a custom calendar; defaults
        // the NeedsSystem to the canonical mood evaluator.
        public SliceTickComposer(GameTimeAdvanceSystem timeAdvance)
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
        public void Advance(SliceWorldState world, int tickIndex)
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
            _ticksSinceHourly += delta;
            while (_ticksSinceHourly >= TicksPerGameHour)
            {
                _ticksSinceHourly -= TicksPerGameHour;
                if (world.Actors == null || world.Events == null) continue;
                foreach (var actor in world.Actors.Records)
                {
                    if (actor == null) continue;
                    _needs.TickActorNeeds(actor, world.Events, world.Time, ticks: 1);
                }
            }

            _ticksSinceDaily += delta;
            while (_ticksSinceDaily >= TicksPerGameDay)
            {
                _ticksSinceDaily -= TicksPerGameDay;
                if (world.Caravans == null || world.Events == null) continue;
                _caravans.Tick(world.Caravans, world.FindTradeRoute, world.FindStockpile, world.Time, world.Events);
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
        /// ResetAnchor only after <see cref="SliceWorldState.Time"/> has
        /// been restored — anchor reset itself does not rewind domain state.
        /// </summary>
        public void ResetAnchor()
        {
            _lastTickIndex = -1;
            // _ticksSinceHourly / _ticksSinceDaily intentionally preserved.
        }
    }
}

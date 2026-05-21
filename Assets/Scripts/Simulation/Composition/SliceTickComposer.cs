using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Time;

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

        private readonly GameTimeAdvanceSystem _timeAdvance;
        private int _lastTickIndex = -1;

        public SliceTickComposer()
            : this(new GameTimeAdvanceSystem(BuildDefaultCalendar()))
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

        public SliceTickComposer(GameTimeAdvanceSystem timeAdvance)
        {
            _timeAdvance = timeAdvance ?? throw new ArgumentNullException(nameof(timeAdvance));
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
            world.Time = _timeAdvance.Advance(world.Time, delta * MinutesPerTick);
        }

        /// <summary>
        /// Reset the composer's tick anchor — call after a save restore so
        /// the next Advance does not double-tick the restored time.
        /// </summary>
        public void ResetAnchor() => _lastTickIndex = -1;
    }
}

using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// ScheduleSystem (SOUL-03 → CAN SUYU H2) is the living mover. V1 was a clock-anchor router with
// a HARDCODED 12:00-13:59 lunch window — choreography, not behavior (the audit's canonical
// example). H2 replaces it with a UTILITY SELECTOR: each hour every civilian scores its options
// (eat / rest / work / idle) from its OWN needs and the clock, and walks toward the winner. The
// midday tavern crowd is no longer routed — it EMERGES, because hunger rises through the morning
// until eating outbids work. Pure Domain/Simulation: deterministic, no Unity, no I/O.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Steps living actors one tile toward the behavior their needs currently choose.</summary>
    public sealed class ScheduleSystem
    {
        /// <summary>First game-hour (inclusive) of the working day.</summary>
        public const int WorkStartHour = 6;

        /// <summary>End game-hour (exclusive) of the working day.</summary>
        public const int WorkEndHour = 20;

        // Utility weights: work is a steady pull (55); eating wins once hunger climbs past it
        // (the +20/h ratchet crosses 55 around midday when fed at dawn); resting wins in the
        // evening as fatigue + the night bonus overtake everything.
        public const int WorkScore = 55;
        public const int IdleScore = 35;
        public const int NightRestBonus = 25;

        public void Advance(ActorStore actors, GameTime time)
        {
            Advance(actors, time, foodSpot: null);
        }

        /// <summary>H2 overload: the world's communal FOOD SPOT (the tavern / market stall where
        /// the stockpile lives). No window — hunger decides when anyone goes.</summary>
        public void Advance(ActorStore actors, GameTime time, GridPosition? foodSpot)
        {
            Advance(actors, time, foodSpot.HasValue
                ? new[] { foodSpot.Value }
                : System.Array.Empty<GridPosition>());
        }

        /// <summary>Multi-larder overload: each actor walks to their NEAREST food spot — one
        /// town's lunch crowd no longer marches to another town's table.</summary>
        public void Advance(ActorStore actors, GameTime time, System.Collections.Generic.IReadOnlyList<GridPosition> foodSpots)
            => Advance(actors, time, foodSpots, pursuits: null);

        /// <summary>P0 pursuit overload: active guard chases (from WitnessResponse) outrank the
        /// return-to-post routing at the SAME PerTick cadence - the chase can finally win.</summary>
        public void Advance(ActorStore actors, GameTime time,
            System.Collections.Generic.IReadOnlyList<GridPosition> foodSpots,
            System.Collections.Generic.List<PursuitRecord> pursuits)
        {
            if (actors == null)
                return;

            int seatOrdinal = 0;
            foreach (var actor in actors.Records)
            {
                if (actor == null || !actor.IsAlive)
                    continue;

                // F18 lair guards: a pinned Enemy (home == dayAnchor, the F10 dungeon-dweller contract)
                // has no daily rhythm — its only mover is the hostile-pursuit AI. Stepping it back home
                // every tick rubber-bands an active chase (proof: 1.8m closed instead of ~5.7m over 2.6s).
                if (actor.Role == ActorRole.Enemy && actor.Home.Equals(actor.DayAnchor))
                    continue;

                // PERSONAL SPACE: each civilian owns a stable seat ordinal (insertion order,
                // deterministic) so shared destinations fan out over distinct cells instead of
                // stacking every billboard on one tile ("birbirlerinin uzerinden yuruyorlar").
                GridPosition target;
                if (actor.Role == ActorRole.Guard && TryResolvePursuit(pursuits, actors, actor, time, out var quarryCell))
                    target = quarryCell; // the chase, at full tick speed
                else
                    target = ChooseTarget(actor, time, NearestSpot(foodSpots, actor.Position), seatOrdinal++);
                var next = StepToward(actor.Position, target);
                if (!next.Equals(actor.Position))
                    actor.MoveTo(next);
            }
        }

        /// <summary>Resolve this guard's active chase to the quarry's LIVE cell; prunes
        /// expired / dead-quarry / lost (>40 cells) entries in place.</summary>
        private static bool TryResolvePursuit(System.Collections.Generic.List<PursuitRecord> pursuits,
            ActorStore actors, ActorRecord guard, GameTime time, out GridPosition target)
        {
            target = default;
            if (pursuits == null) return false;
            for (int i = pursuits.Count - 1; i >= 0; i--)
            {
                var pursuit = pursuits[i];
                if (pursuit.GuardId != guard.Id.Value) continue;
                if (time.TotalMinutes > pursuit.UntilMinutes) { pursuits.RemoveAt(i); return false; }
                if (!actors.TryGet(new ActorId(pursuit.TargetId), out var quarry) || quarry == null || !quarry.IsAlive)
                { pursuits.RemoveAt(i); return false; }
                int dist = System.Math.Max(
                    System.Math.Abs(guard.Position.X - quarry.Position.X),
                    System.Math.Abs(guard.Position.Y - quarry.Position.Y));
                if (dist > 40) { pursuits.RemoveAt(i); return false; } // lost them - back to post
                target = quarry.Position;
                return true;
            }
            return false;
        }

        /// <summary>True when the supplied timestamp falls within the working day.</summary>
        public static bool IsWorkHour(GameTime time)
        {
            var hour = time.Hour;
            return hour >= WorkStartHour && hour < WorkEndHour;
        }

        /// <summary>H2 UTILITY CORE: the highest-scoring behavior wins; needs drive the score.
        /// Public so tests can pin the decision table without simulating movement.</summary>
        public static GridPosition ChooseTarget(ActorRecord actor, GameTime time, GridPosition? foodSpot)
            => ChooseTarget(actor, time, foodSpot, 0);

        public static GridPosition ChooseTarget(ActorRecord actor, GameTime time, GridPosition? foodSpot, int seatOrdinal)
        {
            bool workHour = IsWorkHour(time);

            // The watch holds its post (guards eat off-shift — an honest simplification, logged
            // in ROADMAP_V2), and enemies keep the curfew commute: classic routing for both.
            if (actor.Role == ActorRole.Guard || actor.Role == ActorRole.Enemy)
                return ClassicTarget(actor, workHour);

            // PLAYTEST FIX ("herkes town merkezinde"): below HungerEatThreshold the table CANNOT
            // feed you (TryEat refuses), yet sub-threshold hunger still outbid idle/rest and parked
            // whole towns at the centre food spot, standing hungry-but-not-hungry-enough. The food
            // spot only pulls when the meal will actually happen.
            int eat = foodSpot.HasValue && actor.Needs.Hunger.Value >= NeedConsumptionSystem.HungerEatThreshold
                ? actor.Needs.Hunger.Value
                : -1;
            int rest = actor.Needs.Fatigue.Value + (workHour ? 0 : NightRestBonus);
            int work = workHour && !actor.ScheduleState.IsIdle ? WorkScore : 0;
            int idle = workHour ? IdleScore : 0;

            // Deterministic tie order: eat > rest > work > idle (the hungrier impulse wins ties).
            if (eat >= rest && eat >= work && eat >= idle && foodSpot.HasValue)
                return Seat(foodSpot.Value, seatOrdinal);
            if (rest >= work && rest >= idle)
                return actor.Home;
            if (work >= idle)
                return actor.ScheduleState.TargetWorksitePosition;
            return actor.DayAnchor;
        }

        private static GridPosition ClassicTarget(ActorRecord actor, bool workHour)
        {
            if (!workHour)
                return actor.Home;
            var schedule = actor.ScheduleState;
            return schedule.IsIdle ? actor.DayAnchor : schedule.TargetWorksitePosition;
        }

        private static GridPosition? NearestSpot(System.Collections.Generic.IReadOnlyList<GridPosition> spots, GridPosition from)
        {
            if (spots == null || spots.Count == 0) return null;
            GridPosition best = spots[0];
            long bestDist = long.MaxValue;
            for (int i = 0; i < spots.Count; i++)
            {
                long d = System.Math.Max(System.Math.Abs(from.X - spots[i].X), System.Math.Abs(from.Y - spots[i].Y));
                if (d < bestDist) { bestDist = d; best = spots[i]; }
            }
            return best;
        }

        // The 25 seats of the communal table: a Chebyshev spiral over the 5x5 block centred on
        // the food spot. Every cell stays within EatReachCells (2) of the site centre, so eating
        // works from every seat -- but no two ordinals share a cell, so no two diners share one.
        private static readonly (int dx, int dy)[] SeatOffsets =
        {
            (0, 0), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1), (0, -1), (1, -1),
            (2, 0), (2, 1), (2, 2), (1, 2), (0, 2), (-1, 2), (-2, 2), (-2, 1), (-2, 0),
            (-2, -1), (-2, -2), (-1, -2), (0, -2), (1, -2), (2, -2), (2, -1),
        };

        private static GridPosition Seat(GridPosition table, int seatOrdinal)
        {
            var (dx, dy) = SeatOffsets[((seatOrdinal % SeatOffsets.Length) + SeatOffsets.Length) % SeatOffsets.Length];
            return new GridPosition(table.X + dx, table.Y + dy);
        }

        // Deterministic one-tile move toward the target on the integer grid. Each axis advances by at
        // most one, so movement is a Chebyshev (8-direction) step that converges monotonically and
        // never overshoots the destination cell.
        private static GridPosition StepToward(GridPosition from, GridPosition to)
        {
            return new GridPosition(
                from.X + System.Math.Sign(to.X - from.X),
                from.Y + System.Math.Sign(to.Y - from.Y));
        }
    }
}

using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// ScheduleSystem (SOUL-03 → CAN SUYU H2) is the living mover. V1 was a clock-anchor router with
// a HARDCODED 12:00-13:59 lunch window — choreography, not behavior (the audit's canonical
// example). H2 replaced it with a UTILITY SELECTOR over the actor's OWN needs and the clock.
// W32 EAT: the eat option moved to ActionLifecycleSystem (decision + persistent EatAction);
// this system routes only ACTIONLESS actors between rest / work / idle and resolves pursuits.
// Pure Domain/Simulation: deterministic, no Unity, no I/O.
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
            Advance(actors, time, pursuits: null);
        }

        /// <summary>P0 pursuit overload: active guard chases (from WitnessResponse) outrank the
        /// return-to-post routing at the SAME PerTick cadence - the chase can finally win.</summary>
        public void Advance(ActorStore actors, GameTime time,
            System.Collections.Generic.List<PursuitRecord> pursuits)
        {
            if (actors == null)
                return;

            foreach (var actor in actors.Records)
            {
                if (actor == null || !actor.IsAlive)
                    continue;

                // W32 EAT: the action layer owns this actor's legs now — an active (or terminal,
                // not yet consumed) action means the schedule may not touch its cell this tick.
                if (actor.ActionState.CurrentAction != ActorActionType.None)
                    continue;

                // F18 lair guards: a pinned Enemy (home == dayAnchor, the F10 dungeon-dweller contract)
                // has no daily rhythm — its only mover is the hostile-pursuit AI. Stepping it back home
                // every tick rubber-bands an active chase (proof: 1.8m closed instead of ~5.7m over 2.6s).
                if (actor.Role == ActorRole.Enemy && actor.Home.Equals(actor.DayAnchor))
                    continue;

                GridPosition target;
                if (actor.Role == ActorRole.Guard && TryResolvePursuit(pursuits, actors, actor, time, out var quarryCell))
                    target = quarryCell; // the chase, at full tick speed
                else
                    target = ChooseTarget(actor, time);
                var next = MovementService.StepToward(actor.Position, target);
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
        /// W32 EAT: the eat score left for ActionLifecycleSystem (living.decision) — hungry
        /// civilians carry an EatAction and never reach this table. Public so tests can pin
        /// the decision table without simulating movement.</summary>
        public static GridPosition ChooseTarget(ActorRecord actor, GameTime time)
        {
            bool workHour = IsWorkHour(time);

            // The watch holds its post (guards eat off-shift — an honest simplification, logged
            // in ROADMAP_V2), and enemies keep the curfew commute: classic routing for both.
            if (actor.Role == ActorRole.Guard || actor.Role == ActorRole.Enemy)
                return ClassicTarget(actor, workHour);

            int rest = actor.Needs.Fatigue.Value + (workHour ? 0 : NightRestBonus);
            int work = workHour && !actor.ScheduleState.IsIdle ? WorkScore : 0;
            int idle = workHour ? IdleScore : 0;

            // Deterministic tie order: rest > work > idle.
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

    }
}

using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W36 GUARD+COMBAT: MoveToWorksite's mould with WorldState.HuntTargets as the claim ledger
// (ReservationLedger's mirror for the enemy side). The struct carries ids only (W32 rule):
// the target position is RE-READ from the hunter's HuntTargetRecord every step, so a fleeing
// prey drags the hunter (no wrong-cell path lock-in). Target row missing / expired / target
// gone or dead => Fail(NoFoodFound-adjacent: TargetGone); one shared reason with EAT keeps
// the log grammar small (ActionAdvancer.ToLogReason maps NoFoodFound→TargetGone).
// CONSTRAINT (matter conservation): no world-mutation beyond Actor.Position + ActionState;
// combat lives entirely in StrikeQuarry.
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Walks the hunter one step per hour toward the prey named by its HuntTargets row.</summary>
    public sealed class HuntAdvancer : ActionAdvancer
    {
        public HuntAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.Hunt;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            if (state.CurrentIntent == ActorIntent.GuardCompanion
                && !CompanionService.IsCompanion(world, actor.Id))
            {
                Fail(world, actor, ActionFailureReason.Interrupted, stamp);
                return;
            }
            if (!TryResolvePrey(world, actor, stamp, out var prey))
            {
                Fail(world, actor, ActionFailureReason.TargetGone, stamp);
                return;
            }
            var dist = actor.Position.ChebyshevDistanceTo(prey.Position);
            if (dist <= CombatOperations.StrikeReach)
            {
                // Adjacent: NextLink hands over to StrikeQuarry on the SAME tick (the W32-03
                // arrival T, take T+1 timeline). The Succeeded gate keeps advance idempotent.
                TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Arrived, stamp);
                return;
            }
            // Cadence gate: only step on the game-hour boundary — preserves pre-W36 baseline.
            if (!EmberCrpg.Simulation.Composition.WorldTickComposer.IsHourlyBoundary(stamp))
            {
                // Cadence waiting is not action progress: retain the exact state.
                TransitionTo(world, actor, state, ActionLogReason.ProgressTicked, stamp);
                return;
            }
            var movement = MovementService.RouteToward(
                actor.Position, prey.Position, world?.NavView, CombatOperations.StrikeReach);
            if (!movement.Moved)
            {
                // The shared Fail gate retires the HuntTargets relationship.
                Fail(world, actor, ActionFailureReason.Unreachable, stamp);
                return;
            }
            actor.MoveTo(movement.Position);
            TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
        }

        // W36: reads-only probe used by both Hunt and StrikeQuarry. Prunes NOTHING (the ledger
        // trim lives on the DECIDE side — schedule prunes GuardPursuits similarly). Bounded
        // TTL means an orphan row expires without any code needing to see the corpse first.
        internal static bool TryResolvePrey(WorldState world, ActorRecord hunter, GameTime stamp, out ActorRecord prey)
        {
            prey = null;
            var rows = world.HuntTargets;
            if (rows == null) return false;
            var durableTarget = hunter.ActionState.TargetActorId;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.HunterId != hunter.Id.Value) continue;
                if (!durableTarget.IsEmpty && row.TargetId != durableTarget.Value) continue;
                if (stamp.TotalMinutes > row.UntilMinutes) return false;
                if (!world.Actors.TryGet(new ActorId(row.TargetId), out var candidate)
                    || candidate == null || !candidate.IsAlive)
                    return false;
                prey = candidate;
                return true;
            }
            return false;
        }
    }
}

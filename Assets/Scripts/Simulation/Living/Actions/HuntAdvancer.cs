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
        /// <summary>Movement cadence — one cell per game hour, matching the retired
        /// PredationSystem@Hourly:40 hunter step. Without this the hunter would close in ~60x
        /// faster than pre-W36 baseline and 2-day soak gates (Gate8 personal-space, ProofLivingCensus
        /// meal peaks) would drift. In-cooldown ticks Advance in place — same B21-safe idiom
        /// StrikeQuarry uses (TransitionTo Advanced→Advanced writes no log row).</summary>
        public const int StepCadenceMinutes = 60;

        public HuntAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.Hunt;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            if (!TryResolvePrey(world, actor, stamp, out var prey))
            {
                Fail(world, actor, ActionFailureReason.NoFoodFound, stamp);
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
            if (stamp.TotalMinutes % StepCadenceMinutes != 0)
            {
                TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
                return;
            }
            actor.MoveTo(MovementService.StepToward(actor.Position, prey.Position, world?.NavView));
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
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.HunterId != hunter.Id.Value) continue;
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

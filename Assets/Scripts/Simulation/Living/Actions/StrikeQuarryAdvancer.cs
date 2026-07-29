using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

// Design note:
// W36 GUARD+COMBAT: HaulCrop's terminal-in-one-Step mould. StrikeQuarry runs the deterministic
// dice ONCE per Running step, then decides:
//   - target down (predator-vs-predator) OR maul-clamped (civilian): Succeeded → NextLink None → Idle
//   - target still up + still adjacent: Succeeded → NextLink Hunt again (adjacency held, another swing next tick)
//   - target still up + fled out of reach: Succeeded → NextLink Hunt (chase resumes)
// This is the FIRST cyclic NextLink in the project — infinite-loop protection lives here:
// StrikeQuarry Succeeded fires EVERY tick, but the linker returns to Hunt only while the prey
// still exists. Ledger removal on kill closes the loop cleanly (HuntAdvancer.TryResolvePrey
// returns false next tick → NoFoodFound → Idle, matching MoveToFood's drain semantics).
// CONSTRAINT (single-writer, chunking invariance): Actor.Vitals mutation flows through
// CombatOperations.ResolveStrike; the mercy clamp mirrors PredationSystem's civilian-survives
// rule verbatim, so matter conservation is preserved (civilians never die of predation).
namespace EmberCrpg.Simulation.Living.Actions
{
    /// <summary>Strikes the hunter's quarry once; loops via NextLink until the target is down or gone.</summary>
    public sealed class StrikeQuarryAdvancer : ActionAdvancer
    {
        public StrikeQuarryAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.StrikeQuarry;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            if (state.CurrentIntent == ActorIntent.GuardCompanion
                && !CompanionService.IsCompanion(world, actor.Id))
            {
                Fail(world, actor, ActionFailureReason.Interrupted, stamp);
                return;
            }
            if (state.CurrentIntent == ActorIntent.Pursue)
            {
                if (!PursueAdvancer.TryResolveLivePursuit(
                        world, actor, stamp, out var pursuit, out var pursuitTarget, out var failure))
                {
                    Fail(world, actor, failure, stamp);
                    return;
                }
                if (!pursuitTarget.Id.Equals(state.TargetActorId))
                {
                    world.HuntTargets ??= new System.Collections.Generic.List<HuntTargetRecord>();
                    PursuitLedgerQuery.UpsertHunt(
                        world.HuntTargets, actor.Id.Value, pursuitTarget.Id.Value, pursuit.UntilMinutes);
                    var retargeted = ActorActionState.ForIntent(ActorIntent.Pursue).Start(
                        ActorActionType.Pursue,
                        PredationSystem.FallbackSite(world, pursuitTarget.Position),
                        ItemId.Empty,
                        ReservationId.Empty,
                        stamp.TotalMinutes,
                        ActionInterruptPolicy.Interruptible,
                        pursuitTarget.Id);
                    TransitionTo(world, actor, retargeted, ActionLogReason.TargetSelected, stamp);
                    return;
                }
            }
            if (!HuntAdvancer.TryResolvePrey(world, actor, stamp, out var prey))
            {
                Fail(world, actor, ActionFailureReason.TargetGone, stamp);
                return;
            }
            // Adjacency probe BEFORE the swing: a fled quarry does not eat a free hit — the
            // ledger stays (target still valid) and NextLink routes back to Hunt to re-close.
            if (actor.Position.ChebyshevDistanceTo(prey.Position) > CombatOperations.StrikeReach)
            {
                TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Arrived, stamp);
                return;
            }
            // Cooldown: swings align to the game-hour boundary so damage-per-hour matches
            // pre-W36 PredationSystem (Hourly:40). Waiting is exact state retention: no movement,
            // strike, or other work happened, so progress must not advance.
            if (!EmberCrpg.Simulation.Composition.WorldTickComposer.IsHourlyBoundary(stamp))
            {
                return;
            }
            var targetWasAlive = prey.IsAlive;
            CombatOperations.ResolveStrike(world, actor, prey, stamp);
            var strikeKilledTarget = targetWasAlive && !prey.IsAlive;
            if (state.CurrentIntent == ActorIntent.Pursue)
                world.Events?.Append(new WorldEvent(
                    stamp, WorldEventKind.GuardResponded, actor.Id,
                    PredationSystem.FallbackSite(world, prey.Position),
                    $"guard_strikes_hunter target:{prey.Id.Value}"));
            CombatOperations.MaybeMaulClamp(world, actor, prey, stamp);
            if (strikeKilledTarget)
                RetireDeadTargetAction(world, prey, stamp);
            // Kill / clamp: clear the row so the chain terminates on the next Advance (Hunt
            // → NoFoodFound → Idle). Predator-vs-predator: the row also clears when the prey
            // died-for-real (IsAlive false and no clamp — that path is Enemy-vs-Guard only).
            if (strikeKilledTarget)
            {
                // Health.Current == 1 is the mauled-survives clamp signature (civilian target
                // resurrected at 1 HP): the hunt is over for THIS quarry either way.
                ClearHuntRow(world, actor.Id.Value);
                if (state.CurrentIntent == ActorIntent.Pursue)
                    ClearPursuitRow(world, actor.Id.Value);
            }
            // Succeeded every tick; NextLink returns Hunt while the row is live, None once
            // it is cleared (the loop's fixed point). One log per swing (phase boundary).
            TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Completed, stamp);
        }

        private void RetireDeadTargetAction(WorldState world, ActorRecord target, GameTime stamp)
        {
            var targetState = target.ActionState;
            if (targetState.CurrentAction == ActorActionType.None) return;
            if (targetState.Phase == ActionPhase.Running)
            {
                Fail(world, target, ActionFailureReason.Interrupted, stamp);
                targetState = target.ActionState;
            }
            else
            {
                targetState = RecoverMatterAndReleaseReservation(
                    world, target, targetState, stamp);
            }
            ClearHuntRow(world, target.Id.Value);
            ClearPursuitRow(world, target.Id.Value);
            TransitionTo(world, target, ActorActionState.Idle,
                ActionLogReason.InterruptPreempted, stamp);
        }

        private static void ClearHuntRow(WorldState world, ulong hunterId)
        {
            var rows = world.HuntTargets;
            if (rows == null) return;
            for (var i = rows.Count - 1; i >= 0; i--)
                if (rows[i].HunterId == hunterId)
                    rows.RemoveAt(i);
        }

        private static void ClearPursuitRow(WorldState world, ulong guardId)
        {
            var rows = world.GuardPursuits;
            if (rows == null) return;
            for (var i = rows.Count - 1; i >= 0; i--)
                if (rows[i].GuardId == guardId)
                    rows.RemoveAt(i);
        }
    }
}

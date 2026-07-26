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
        /// <summary>Swings are gated to the game-hour boundary — the retired PredationSystem's
        /// Hourly:40 cadence preserved on the PerTick advancer. Without this, damage-per-second
        /// ~60x-es and the 2-day Gate soaks (LivingWorldGate Gate8/civilian stacking, ProofLivingCensus
        /// meal counters) drift wildly from the pre-W36 baseline.</summary>
        public const int SwingCadenceMinutes = 60;

        public StrikeQuarryAdvancer(ActionLogManager log) : base(log) { }

        public override ActorActionType Handles => ActorActionType.StrikeQuarry;

        protected override void Step(WorldState world, ActorRecord actor, GameTime stamp)
        {
            var state = actor.ActionState;
            if (!HuntAdvancer.TryResolvePrey(world, actor, stamp, out var prey))
            {
                Fail(world, actor, ActionFailureReason.NoFoodFound, stamp);
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
            // pre-W36 PredationSystem (Hourly:40). In-cooldown ticks Advance in place — the
            // TransitionTo no-op branch (same action + same phase) writes NO log row, so the
            // hourly rhythm survives without B21 spam.
            if (stamp.TotalMinutes % SwingCadenceMinutes != 0)
            {
                TransitionTo(world, actor, state.Advanced(), ActionLogReason.ProgressTicked, stamp);
                return;
            }
            CombatOperations.ResolveStrike(world, actor, prey, stamp);
            CombatOperations.MaybeMaulClamp(world, actor, prey, stamp);
            // Kill / clamp: clear the row so the chain terminates on the next Advance (Hunt
            // → NoFoodFound → Idle). Predator-vs-predator: the row also clears when the prey
            // died-for-real (IsAlive false and no clamp — that path is Enemy-vs-Guard only).
            if (!prey.IsAlive || prey.Vitals.Health.Current <= 1)
            {
                // Health.Current == 1 is the mauled-survives clamp signature (civilian target
                // resurrected at 1 HP): the hunt is over for THIS quarry either way.
                ClearHuntRow(world, actor.Id.Value);
            }
            // Succeeded every tick; NextLink returns Hunt while the row is live, None once
            // it is cleared (the loop's fixed point). One log per swing (phase boundary).
            TransitionTo(world, actor, state.Succeeded(), ActionLogReason.Completed, stamp);
        }

        private static void ClearHuntRow(WorldState world, ulong hunterId)
        {
            var rows = world.HuntTargets;
            if (rows == null) return;
            for (var i = rows.Count - 1; i >= 0; i--)
                if (rows[i].HunterId == hunterId)
                    rows.RemoveAt(i);
        }
    }
}

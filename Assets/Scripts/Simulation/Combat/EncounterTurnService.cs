using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Simulation.Rng;

// Design note:
// EncounterTurnService advances Sprint 1's explicit bounded turn loop.
// Inputs: encounter state, player actor, enemy actor, and deterministic RNG.
// Outputs: one resolved strike per call plus finish/winner updates.
// Bible reference: PRD approved Sprint 1 deviation budget for FR-02.
namespace EmberCrpg.Simulation.Combat
{
    /// <summary>Approved Sprint 1 one-vs-one encounter progression built on top of pure combat math.</summary>
    public sealed class EncounterTurnService
    {
        private readonly CombatMathService _combat = new CombatMathService();

        public CombatStrikeResult Advance(EncounterState encounter, ActorRecord player, ActorRecord enemy, IDeterministicRng rng)
        {
            var attacker = encounter.PlayerActsNext ? player : enemy;
            var defender = encounter.PlayerActsNext ? enemy : player;
            var strike = _combat.ResolveAttack(attacker, defender, rng);
            encounter.AddLog(strike.Summary);

            if (!defender.IsAlive)
            {
                encounter.Finish(attacker.Name);
                encounter.AddLog($"{attacker.Name} wins the slice encounter.");
                return strike;
            }

            encounter.PlayerActsNext = !encounter.PlayerActsNext;
            return strike;
        }
    }
}

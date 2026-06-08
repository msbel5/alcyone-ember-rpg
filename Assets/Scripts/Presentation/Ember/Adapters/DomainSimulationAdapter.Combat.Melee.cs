using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed partial class DomainSimulationAdapter
    {
        // "Attack nearest" resolver: closest non-player actor to the player within maxRange tiles (Chebyshev
        // distance), deterministic tie-break by ascending actor id so the choice is reproducible. Null if none.
        private ActorRecord NearestStrikeTarget(int maxRange)
        {
            var player = _world.Actors?.FirstByRole(ActorRole.Player);
            if (player == null) return null;
            ActorRecord best = null;
            int bestDist = int.MaxValue;
            ulong bestId = ulong.MaxValue;
            foreach (var a in _world.Actors.Records)
            {
                if (a == null || a.Role == ActorRole.Player) continue;
                int d = System.Math.Max(System.Math.Abs(a.Position.X - player.Position.X),
                                        System.Math.Abs(a.Position.Y - player.Position.Y));
                if (d > maxRange) continue;
                if (d < bestDist || (d == bestDist && a.Id.Value < bestId)) { best = a; bestDist = d; bestId = a.Id.Value; }
            }
            return best;
        }

        public bool TryMeleeStrike(string targetActorName, int rawDamage)
        {
            // Codex audit (fourth pass A-P1): concrete melee command. Resolves
            // the target by stable actor name on WorldState and applies
            // damage; emits a CombatResolved event so the deterministic log
            // captures the strike.
            if (rawDamage <= 0) { LogCombat("Strike whiffs."); return false; }
            // HUD "Attack nearest" sends an empty target -> resolve the closest actor in reach so the action is
            // real instead of a guaranteed refusal. A named target still resolves by exact stable name as before.
            ActorRecord target;
            if (string.IsNullOrEmpty(targetActorName))
            {
                target = NearestStrikeTarget(maxRange: 6);
                if (target == null) { LogCombat("No enemy within reach."); return false; }
            }
            else
            {
                target = _world.Actors.Records.FirstOrDefault(a => string.Equals(a.Name, targetActorName, System.StringComparison.Ordinal));
                if (target == null) { LogCombat($"No target: {targetActorName}"); return false; }
            }
            // Codex audit (sixth pass A-P0 #4): previously bypassed the
            // CombatActionResolver chain entirely (auto-hit, no armor / dodge /
            // accuracy / stamina). Route through CombatActionResolver so the
            // hit roll, damage roll, armor mitigation, stamina cost, and the
            // canonical CombatResolved event all match the deterministic
            // kernel. The action template is a synthetic "melee_swing"
            // CombatActionDef; the band-width matches the existing baseline
            // (rawDamage parameter).
            var attacker = _world.Actors.FirstByRole(ActorRole.Player) ?? target;
            var meleeAction = new CombatActionDef(
                id: new CombatActionId("melee_swing"),
                staminaCost: 0,
                hitFormulaKey: "accuracy_vs_dodge",
                damageFormulaKey: "base_minus_armor",
                animationTag: "melee_swing");
            // Eighth-pass A-P1: previous code constructed a fresh RNG seeded
            // only by _tick. Two strikes in the same tick produced identical
            // hit + damage rolls, making combat feel broken. Cache one RNG
            // instance and advance it monotonically; replay determinism still
            // holds because the seed is worldSeed-anchored.
            // Codex ninth-pass A-P2: derive the melee RNG seed from
            // world.Time so save/load reproduces strike outcomes
            // deterministically. (Previously the RNG was a fresh instance
            // per adapter; reload meant identical seed → identical first
            // strike post-load, but a save mid-fight would re-roll.) Now
            // the seed advances with simulation time.
            // Codex review (PR #203 P1): persisting _meleeRng on the adapter
            // means save/load loses RNG state. Re-derive a fresh RNG per
            // strike from (world.Time + world.Events.Count) so two sessions
            // restored from the same snapshot produce the same next roll.
            // The event-count XOR advances each strike (a CombatResolved
            // event lands every Resolve call), so distinct strikes in the
            // same tick still produce distinct rolls — same property the
            // cached-RNG path had — while remaining deterministic across
            // save/load.
            uint timeSeed = (uint)(_world.Time.TotalMinutes & 0xFFFFFFFFL);
            uint eventSeed = (uint)((_world.Events?.Events?.Count ?? 0) & 0xFFFFFFFFL);
            var rng = new EmberCrpg.Simulation.Rng.XorShiftRng(
                (timeSeed * 2654435761u) ^ (eventSeed * 1597334677u) ^ 0xE3B6_1EE7u);
            // Codex audit (seventh pass A-P2 #6): previously hard-coded
            // SiteId(1UL) so every combat event was logged under a synthetic
            // location. Derive the site from the actual world: closest
            // authored site to the attacker, falling back to the first
            // site, falling back to SiteId.Empty so the event log stays
            // honest if no sites exist (e.g. tutorial / dialog-only scenes).
            var siteId = ResolveCombatSiteId(attacker, target);
            if (_world.Events == null)
            {
                // Defensive: events log is required by CombatActionResolver.
                target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Damage(rawDamage)));
                LogCombat($"You strike {target.Name} for {rawDamage}.");
                return true;
            }
            var resolver = new EmberCrpg.Simulation.Combat.CombatActionResolver(
                new EmberCrpg.Simulation.Combat.CombatHitRollService(),
                new EmberCrpg.Simulation.Combat.CombatDamageService());
            var outcome = resolver.Resolve(meleeAction, attacker, target, damageBandWidth: rawDamage / 2,
                rng: rng, now: _world.Time, siteId: siteId, events: _world.Events);
            LogCombat(outcome.Hit
                ? $"You strike {target.Name} for {outcome.Damage}."
                : $"You miss {target.Name}.");
            return outcome.Hit;
        }

    }
}

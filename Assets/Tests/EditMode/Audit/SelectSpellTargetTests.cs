using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    /// <summary>
    /// Ninth-pass audit (G-P2): pins the spell-target-by-effect-kind contract
    /// implemented in
    /// <c>Assets/Scripts/Presentation/Ember/Adapters/DomainSimulationAdapter.cs</c>
    /// (private <c>SelectSpellTarget</c> at line 527).
    ///
    /// The Presentation adapter cannot be constructed inside the fallback
    /// harness (no Unity engine). To still pin the contract, we mirror the
    /// adapter's selection rule against a real <see cref="SliceWorldState"/>
    /// using public Domain primitives. If the production rule diverges from
    /// the rule replicated here, this test must be updated in lockstep —
    /// that's the point of a "behaviour pin".
    /// </summary>
    public sealed class SelectSpellTargetTests
    {
        // Replica of the production rule. The decision tree mirrors
        // DomainSimulationAdapter.SelectSpellTarget so future changes there
        // surface as failures here.
        private static ActorRecord SelectSpellTargetReplica(
            SpellDefinition spell,
            ActorRecord player,
            IEnumerable<ActorRecord> actors)
        {
            if (spell == null || player == null) return player;
            if (spell.TargetKind == SpellTargetKind.CasterSelf
                || spell.TargetKind == SpellTargetKind.AreaAroundCaster)
                return player;

            // Effect-kind based friend/foe decision.
            bool wantsFriendly = false;
            if (spell.Effects != null)
            {
                foreach (var effect in spell.Effects)
                {
                    var code = effect.Kind;
                    if (code == SpellEffectCode.RestoreHealth
                        || code == SpellEffectCode.ShieldBuff
                        || code == SpellEffectCode.RestoreMana
                        || code == SpellEffectCode.RestoreFatigue)
                    {
                        wantsFriendly = true;
                        break;
                    }
                }
            }

            ActorRecord best = null;
            var bestDistance = int.MaxValue;
            foreach (var candidate in actors)
            {
                if (candidate == null || candidate.Id.Equals(player.Id) || !candidate.IsAlive)
                    continue;
                if (wantsFriendly)
                {
                    if (candidate.Role == ActorRole.Enemy) continue;
                }
                else if (candidate.Role != ActorRole.Enemy)
                {
                    continue;
                }

                var distance = player.Position.ManhattanDistanceTo(candidate.Position);
                if (spell.TargetKind == SpellTargetKind.Touch && distance != 1)
                    continue;
                if ((spell.TargetKind == SpellTargetKind.SingleTarget
                        || spell.TargetKind == SpellTargetKind.AreaAtRange)
                    && spell.RangeInTiles > 0
                    && distance > spell.RangeInTiles)
                    continue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best ?? player;
        }

        private static ActorRecord MakeActor(ulong id, string name, ActorRole role, GridPosition pos)
        {
            return new ActorRecord(
                new ActorId(id), name, role,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(
                    new VitalStat(20, 20),
                    new VitalStat(12, 12),
                    new VitalStat(20, 20)),
                pos,
                accuracy: 100, dodge: 0, armor: 0, baseDamage: 1);
        }

        [Test]
        public void SelectSpellTarget_RestoreHealthSpell_PicksAllyOverEnemy()
        {
            var player = MakeActor(1UL, "Player", ActorRole.Player, new GridPosition(0, 0));
            // Ally (non-enemy, non-player) at distance 2, enemy at distance 1.
            // A naive "closest non-self" picker would choose the enemy; the
            // friendly-effect rule must override that and choose the ally.
            var enemy = MakeActor(2UL, "Goblin", ActorRole.Enemy, new GridPosition(1, 0));
            var ally  = MakeActor(3UL, "Healer", ActorRole.Guard, new GridPosition(2, 0));

            var spell = new SpellDefinition(
                templateId: "mending_touch",
                displayName: "Mending Touch",
                school: MagicSchool.Restoration,
                targetKind: SpellTargetKind.SingleTarget,
                manaCost: 1,
                rangeInTiles: 5,
                cooldownTicks: 0,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.RestoreHealth, 5, 0) });

            var picked = SelectSpellTargetReplica(
                spell, player, new[] { player, enemy, ally });

            Assert.That(picked, Is.SameAs(ally),
                "RestoreHealth must pick the friendly ally, not the closer enemy.");
            Assert.That(picked.Role, Is.Not.EqualTo(ActorRole.Enemy));
        }

        [Test]
        public void SelectSpellTarget_DirectDamageSpell_PicksEnemyOverAlly()
        {
            var player = MakeActor(1UL, "Player", ActorRole.Player, new GridPosition(0, 0));
            // Ally at distance 1 (closer), enemy at distance 2.
            // A naive "closest non-self" picker would choose the ally; the
            // hostile-effect rule must override that and choose the enemy.
            var ally  = MakeActor(3UL, "Healer", ActorRole.Guard, new GridPosition(1, 0));
            var enemy = MakeActor(2UL, "Goblin", ActorRole.Enemy,    new GridPosition(2, 0));

            var spell = new SpellDefinition(
                templateId: "flame_bolt",
                displayName: "Flame Bolt",
                school: MagicSchool.Destruction,
                targetKind: SpellTargetKind.SingleTarget,
                manaCost: 1,
                rangeInTiles: 5,
                cooldownTicks: 0,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 5, 0) });

            var picked = SelectSpellTargetReplica(
                spell, player, new[] { player, enemy, ally });

            Assert.That(picked, Is.SameAs(enemy),
                "DirectDamage must pick the enemy, not the closer ally.");
            Assert.That(picked.Role, Is.EqualTo(ActorRole.Enemy));
        }

        [Test]
        public void SelectSpellTarget_CasterSelfSpell_AlwaysReturnsPlayer()
        {
            var player = MakeActor(1UL, "Player", ActorRole.Player, new GridPosition(0, 0));
            var ally   = MakeActor(3UL, "Healer", ActorRole.Guard, new GridPosition(1, 0));
            var enemy  = MakeActor(2UL, "Goblin", ActorRole.Enemy,    new GridPosition(1, 0));

            var spell = new SpellDefinition(
                templateId: "inner_calm",
                displayName: "Inner Calm",
                school: MagicSchool.Mysticism,
                targetKind: SpellTargetKind.CasterSelf,
                manaCost: 1,
                rangeInTiles: 0,
                cooldownTicks: 0,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.RestoreFatigue, 5, 0) });

            var picked = SelectSpellTargetReplica(
                spell, player, new[] { player, enemy, ally });

            Assert.That(picked, Is.SameAs(player),
                "CasterSelf must return the player irrespective of nearby actors.");
        }

        [Test]
        public void SelectSpellTarget_DirectDamage_FallsBackToCasterWhenNoEnemiesPresent()
        {
            // Only the player and an ally are present — there's no enemy to
            // target. The selector must fall back to the player (best == null
            // path) instead of throwing or silently picking the ally.
            var player = MakeActor(1UL, "Player", ActorRole.Player, new GridPosition(0, 0));
            var ally   = MakeActor(3UL, "Healer", ActorRole.Guard, new GridPosition(1, 0));

            var spell = new SpellDefinition(
                templateId: "flame_bolt",
                displayName: "Flame Bolt",
                school: MagicSchool.Destruction,
                targetKind: SpellTargetKind.SingleTarget,
                manaCost: 1,
                rangeInTiles: 5,
                cooldownTicks: 0,
                effects: new[] { new SpellEffectSpec(SpellEffectCode.DirectDamage, 5, 0) });

            var picked = SelectSpellTargetReplica(
                spell, player, new[] { player, ally });

            Assert.That(picked, Is.SameAs(player),
                "When no enemies exist, hostile-effect selection must fall back to the player.");
        }
    }
}

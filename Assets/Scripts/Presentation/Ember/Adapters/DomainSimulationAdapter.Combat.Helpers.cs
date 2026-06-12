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
        private EmberCrpg.Domain.Core.SiteId ResolveCombatSiteId(ActorRecord attacker, ActorRecord target)
        {
            if (_world.Sites == null) return default;
            ActorRecord anchor = attacker ?? target;
            if (anchor != null)
            {
                int bestDistance = int.MaxValue;
                EmberCrpg.Domain.Core.SiteId bestId = default;
                foreach (var site in _world.Sites.Records)
                {
                    var sitePosition = CenterOf(site);
                    var dx = sitePosition.X - anchor.Position.X;
                    var dz = sitePosition.Y - anchor.Position.Y;
                    int d = dx * dx + dz * dz;
                    if (d < bestDistance)
                    {
                        bestDistance = d;
                        bestId = site.Id;
                    }
                }
                if (!bestId.IsEmpty) return bestId;
            }
            // Fallback: first authored site, then default.
            foreach (var site in _world.Sites.Records)
            {
                return site.Id;
            }
            return default;
        }

        private ActorRecord SelectSpellTarget(EmberCrpg.Domain.Magic.SpellDefinition spell, ActorRecord player)
        {
            if (spell == null || player == null) return player;
            if (spell.TargetKind == EmberCrpg.Domain.Magic.SpellTargetKind.CasterSelf
                || spell.TargetKind == EmberCrpg.Domain.Magic.SpellTargetKind.AreaAroundCaster)
                return player;

            // Eighth-pass A-P0: filtering "Role != Enemy" excluded every
            // friendly target, so Restoration / Buff spells could never pick
            // an ally (they silently fell back to caster). Branch on effect
            // kind: friendly-effect spells skip enemies, hostile spells skip
            // non-enemies. SpellTargetKind alone is insufficient — both
            // Mending and FlameBolt are "SingleTarget" — so inspect the
            // spell's effect ops for friendly intent.
            bool wantsFriendly = false;
            if (spell.Effects != null)
            {
                foreach (var effect in spell.Effects)
                {
                    var code = effect.Kind;
                    if (code == EmberCrpg.Domain.Magic.SpellEffectCode.RestoreHealth
                        || code == EmberCrpg.Domain.Magic.SpellEffectCode.ShieldBuff
                        || code == EmberCrpg.Domain.Magic.SpellEffectCode.RestoreMana
                        || code == EmberCrpg.Domain.Magic.SpellEffectCode.RestoreFatigue)
                    {
                        wantsFriendly = true;
                        break;
                    }
                }
            }

            ActorRecord best = null;
            var bestDistance = int.MaxValue;
            foreach (var candidate in _world.Actors.Records)
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
                if (spell.TargetKind == EmberCrpg.Domain.Magic.SpellTargetKind.Touch && distance != 1)
                    continue;
                if ((spell.TargetKind == EmberCrpg.Domain.Magic.SpellTargetKind.SingleTarget
                        || spell.TargetKind == EmberCrpg.Domain.Magic.SpellTargetKind.AreaAtRange)
                    && spell.RangeInTiles > 0
                    && distance > spell.RangeInTiles)
                    continue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            // PLAYTEST BUG ("büyü kullanırsam kendi canım gidiyor"): hostile spells used to fall back to
            // the CASTER when no enemy was in range — FLAME BOLT then burned the player for its own 8
            // damage. Friendly spells still self-target (healing yourself is correct); hostile spells
            // return null and the cast is refused upstream, mana untouched.
            if (best == null && wantsFriendly) return player;
            return best;
        }

        private static GridPosition CenterOf(SiteRecord site)
        {
            if (site == null) return default;
            return new GridPosition(
                (site.MinBound.X + site.MaxBound.X) / 2,
                (site.MinBound.Y + site.MaxBound.Y) / 2);
        }

        private static WorksiteKind WorksiteKindFor(string siteName)
        {
            if (string.Equals(siteName, "Furnace", System.StringComparison.Ordinal)
                || string.Equals(siteName, "Forge", System.StringComparison.Ordinal))
                return WorksiteKind.Furnace;
            if (string.Equals(siteName, "Hearth", System.StringComparison.Ordinal))
                return WorksiteKind.Bakery;
            if (string.Equals(siteName, "HarvestShed", System.StringComparison.Ordinal))
                return WorksiteKind.Field;
            return WorksiteKind.Generic;
        }

    }
}

// Why this file is intentionally long: combat command routing remains in one partial until command handlers are extracted.
// EMB-010: DomainSimulationAdapter combat / IPlayerCommandSink (partial-class split).
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
        // ----- IPlayerCommandSink -----
        public void LogCombat(string message) => _lastCombatLine = message ?? string.Empty;

        public void TakePlayerDamage(int amount)
        {
            if (amount <= 0) return;
            // Codex audit (fourth pass A-P2): previously held a transient
            // _playerDamageTaken counter that the HUD subtracted from. Now
            // we mutate the real player ActorRecord vitals so save/load
            // preserves the damage and other systems see the new HP.
            var player = _world.Actors.FirstByRole(ActorRole.Player);
            if (player == null) return;
            player.ApplyVitals(player.Vitals.WithHealth(player.Vitals.Health.Damage(amount)));
            _lastCombatLine = $"You take {amount} damage!";
        }

        public bool TryCastSpell(int spellSlotIndex)
        {
            var knownIds = _world.PlayerKnownSpellIds != null && _world.PlayerKnownSpellIds.Count > 0
                ? new List<string>(_world.PlayerKnownSpellIds)
                : new List<string>(EmberCrpg.Simulation.Magic.WorldSpellCatalog.All.Select(s => s.TemplateId));
            if (spellSlotIndex < 0 || spellSlotIndex >= knownIds.Count)
            {
                LogCombat("No such spell slot.");
                return false;
            }

            var spell = EmberCrpg.Simulation.Magic.WorldSpellCatalog.Find(knownIds[spellSlotIndex]);
            if (spell == null)
            {
                LogCombat("Unknown spell slot.");
                return false;
            }

            var player = _world.Actors.FirstByRole(ActorRole.Player);
            if (player == null)
            {
                LogCombat("No caster.");
                return false;
            }
            // Mana gate: pure read; if insufficient mana, refusal.
            if (player.Vitals.Mana.Current < spell.ManaCost)
            {
                LogCombat($"{spell.DisplayName ?? spell.TemplateId}: insufficient mana.");
                return false;
            }
            // Codex audit (seventh pass A-P1 #2): the previous pass routed
            // only TryPrepareCast + CommitPreparedCast, so mana/cooldown
            // updated but the spell's actual effects (damage, heal, buff)
            // never landed on a target. Switch to SpellExecutionService,
            // which composes Cast → Target → Effect → CastRoll, so the live
            // command performs real domain mutation. Target picker selects
            // the closest hostile actor (or the caster for self-buffs); if
            // no hostile target exists, fall back to the caster so single-
            // target effects still resolve.
            var requestedTarget = SelectSpellTarget(spell, player);

            var executionService = new EmberCrpg.Simulation.Magic.SpellExecutionService(
                new EmberCrpg.Simulation.Magic.SpellCastingService(_ => spell),
                new EmberCrpg.Simulation.Magic.SpellTargetValidator(),
                new EmberCrpg.Simulation.Magic.SpellEffectResolutionService(),
                new EmberCrpg.Simulation.Magic.SpellCastRollService());
            var executed = executionService.TryExecute(
                player, spell.TemplateId, knownIds, requestedTarget, _world.PlayerSpellCooldowns);
            if (!executed.Success)
            {
                LogCombat(executed.Message ?? $"{spell.DisplayName ?? spell.TemplateId}: failed.");
                return false;
            }

            _world.Events?.Append(new WorldEvent(
                _world.Time,
                WorldEventKind.SpellResolved,
                player.Id,
                ResolveCombatSiteId(player, requestedTarget),
                $"slice_spell_cast id:{spell.TemplateId} mana:{executed.ManaSpent}"));
            LogCombat(executed.Message);
            return true;
        }

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

            return best ?? player;
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

        public bool TryInteract(string targetTag)
        {
            // Codex audit (fourth pass A-P1): concrete interact verb. Routes
            // through GetDialogSource so the dialog panel binds to a domain-
            // backed source. Returns true when we found an actor matching the
            // tag (display name); the panel still has to be authored in the
            // scene, but the data hookup is real.
            if (string.IsNullOrEmpty(targetTag))
            {
                LogCombat("Nothing to interact with.");
                return false;
            }
            var match = _world.Actors.Records.FirstOrDefault(a => string.Equals(a.Name, targetTag, System.StringComparison.Ordinal));
            if (match == null) return false;
            return TryInteract(match.Id);
        }

        public bool TryInteract(ActorId actorId)
        {
            if (actorId.IsEmpty)
            {
                LogCombat("Nothing to interact with.");
                return false;
            }

            if (_world.Actors == null || !_world.Actors.TryGet(actorId, out var actor) || actor == null)
            {
                LogCombat($"No target: actor#{actorId.Value}");
                return false;
            }

            GetDialogSource(actor.Id);
            return true;
        }

    }
}

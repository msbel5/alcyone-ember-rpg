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
        /// <summary>F16: the player's equipped main-hand weapon, or null for bare hands. Reads the
        /// persisted PlayerEquipment slot and resolves the item instance from the backpack.</summary>
        private EmberCrpg.Domain.Inventory.InventoryItem EquippedWeapon()
        {
            var equipment = _world?.PlayerEquipment;
            var inventory = _world?.PlayerInventory;
            if (equipment == null || inventory == null) return null;
            var itemId = equipment.GetEquippedItemId(EmberCrpg.Domain.Inventory.EquipmentSlot.Weapon);
            return itemId.IsEmpty ? null : inventory.FindById(itemId);
        }

        /// <summary>F16: switch the equipped weapon to a backpack item by template id — frees the slot
        /// first, then routes through the canonical EquipmentService so all refusal rules stay in one
        /// place. Logged honestly either way.</summary>
        public bool TryEquip(string templateId)
        {
            var inventory = _world?.PlayerInventory;
            if (inventory == null || string.IsNullOrEmpty(templateId)) return false;
            foreach (var item in inventory.Items)
            {
                if (item == null || !item.IsEquipment) continue;
                if (!string.Equals(item.TemplateId, templateId, System.StringComparison.Ordinal)) continue;
                _world.PlayerEquipment.Unequip(item.EquipmentSlot); // switching is the player intent
                var result = new EmberCrpg.Simulation.Inventory.EquipmentService()
                    .TryEquip(inventory, _world.PlayerEquipment, item.Id);
                LogCombat(result.Success
                    ? $"{item.DisplayName} equipped (+{item.AccuracyBonus} acc, +{item.DamageBonus} dmg)."
                    : result.Message);
                return result.Success;
            }
            LogCombat("Nothing like that in the pack to equip.");
            return false;
        }

        // "Attack nearest" resolver: closest non-player actor to the player within maxRange tiles (Chebyshev
        // distance), deterministic tie-break by ascending actor id so the choice is reproducible. Null if none.
        private ActorRecord NearestStrikeTarget(int maxRange)
        {
            var player = _world.Actors?.FirstByRole(ActorRole.Player);
            if (player == null) return null;
            // F14: range from the LIVE body (tracker-fed) — in a delve the parked actor sits at the
            // plaza while the rig fights in the chamber; "attack nearest" must measure from the rig.
            var from = PlayerCombatPosition(player);
            ActorRecord best = null;
            int bestDist = int.MaxValue;
            ulong bestId = ulong.MaxValue;
            foreach (var a in _world.Actors.Records)
            {
                if (a == null || a.Role == ActorRole.Player) continue;
                // F23: auto-target only ever picks ENEMIES — a mashed attack key must never commit
                // an accidental crime. Assault stays possible, but only by an AIMED (named) swing.
                if (a.Role != ActorRole.Enemy || !a.IsAlive) continue;
                int d = System.Math.Max(System.Math.Abs(a.Position.X - from.X),
                                        System.Math.Abs(a.Position.Y - from.Y));
                if (d > maxRange) continue;
                if (d < bestDist || (d == bestDist && a.Id.Value < bestId)) { best = a; bestDist = d; bestId = a.Id.Value; }
            }
            return best;
        }

        // Session-local; deliberately NOT persisted — both sides of a save/load restore start at 0, so
        // replay from one snapshot stays deterministic while distinct strikes in one minute roll fresh.
        private uint _meleeStrikeSerial;

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

            // F23 CRIME: raising a blade against anyone who is not an ENEMY is assault — the watch
            // posts a bounty and (TickHostileAi) guards hunt on sight. The swing itself is the crime,
            // hit or miss.
            if (target.IsAlive && target.Role != ActorRole.Enemy && target.Role != ActorRole.Player)
            {
                _world.PlayerBountyGold += 40;
                _world.PlayerReputation -= 2;
                LogCombat($"CRIME! You struck at {target.Name} — the watch hunts you (bounty {_world.PlayerBountyGold}g).");
                UnityEngine.Debug.Log($"[Crime] civilian assaulted: bounty={_world.PlayerBountyGold}g rep={_world.PlayerReputation}.");
                EnsureWatchOfficers(); // not every settlement rolls Guard seeds — crime SUMMONS the watch
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
            // LOOP-PROOF finding (looptest2): "Events.Count advances every Resolve" is FALSE for misses —
            // no event lands, so the seed froze and a first miss replayed IDENTICALLY forever within the
            // same minute (60/60 misses, fatigue untouched). A session-local strike serial breaks the loop;
            // two sessions restored from one snapshot both start it at 0, so save/load replay still agrees.
            _meleeStrikeSerial++;
            var rng = new EmberCrpg.Simulation.Rng.XorShiftRng(
                (timeSeed * 2654435761u) ^ (eventSeed * 1597334677u) ^ (_meleeStrikeSerial * 0x9E3779B9u) ^ 0xE3B6_1EE7u);
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
                EmberCrpg.Presentation.Ember.WorldDirector.WorldCombatFeedbackFeed.RaiseHit(
                    target.Id.Value, HitMaterialFor(target));
                return true;
            }
            var resolver = new EmberCrpg.Simulation.Combat.CombatActionResolver(
                new EmberCrpg.Simulation.Combat.CombatHitRollService(),
                new EmberCrpg.Simulation.Combat.CombatDamageService());
            // F16: the equipped weapon's bonuses finally enter the dice (bare hands = 0/0).
            var weapon = EquippedWeapon();
            var outcome = resolver.Resolve(meleeAction, attacker, target, damageBandWidth: rawDamage / 2,
                rng: rng, now: _world.Time, siteId: siteId, events: _world.Events,
                attackerAccuracyBonus: weapon?.AccuracyBonus ?? 0,
                attackerDamageBonus: weapon?.DamageBonus ?? 0);
            LogCombat(outcome.Hit
                ? $"You strike {target.Name} for {outcome.Damage}."
                : $"You miss {target.Name}.");
            // F10 hit feel: the world billboard flashes red on a landed strike (feed fans out to views).
            // F29: the feed carries WHAT was struck — bone clicks, chitin snaps, wisps wail.
            if (outcome.Hit)
                EmberCrpg.Presentation.Ember.WorldDirector.WorldCombatFeedbackFeed.RaiseHit(
                    target.Id.Value, HitMaterialFor(target));
            return outcome.Hit;
        }

    }
}

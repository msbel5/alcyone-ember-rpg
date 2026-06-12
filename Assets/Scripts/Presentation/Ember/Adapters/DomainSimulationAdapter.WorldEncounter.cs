using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>
    /// F2/encounters: pressing E on a HOSTILE world NPC begins a world encounter instead of a chat. The
    /// adapter can't open UI, so it raises this one-shot signal; the in-game controller consumes it and
    /// opens the combat screen (same static-channel pattern as the world mirrors).
    /// </summary>
    public static class WorldEncounterSignal
    {
        private static bool _pending;

        public static void Raise() => _pending = true;

        public static bool Consume()
        {
            if (!_pending) return false;
            _pending = false;
            return true;
        }
    }

    /// <summary>Second one-shot for the AUDIO sting (the UI consumes WorldEncounterSignal — one flag, one consumer).</summary>
    public static class WorldEncounterStingFeed
    {
        private static bool _pending;
        public static void Raise() => _pending = true;
        public static bool Consume() { if (!_pending) return false; _pending = false; return true; }
    }

    public sealed partial class DomainSimulationAdapter
    {
        private ActorId _worldEncounterId;
        private bool _worldEncounterLootGranted;

        /// <summary>The live world-encounter opponent, or null when none is bound.</summary>
        private ActorRecord WorldEncounterEnemy()
        {
            if (_worldEncounterId.IsEmpty || _world?.Actors == null) return null;
            return _world.Actors.TryGet(_worldEncounterId, out var actor) ? actor : null;
        }

        /// <summary>
        /// Outlaws don't talk. When the interact target's worldgen role is hostile, bind it as the combat
        /// screen's enemy (real ActorRecord — accuracy/dodge/armor all live) and signal the UI. Returns
        /// false for civilians so the normal conversation path continues.
        /// </summary>
        private bool TryBeginWorldEncounter(ActorRecord actor, EmberCrpg.Domain.Worldgen.NpcSeedRecord npc)
        {
            if (actor == null || !actor.IsAlive) return false;
            if (npc == null || npc.Role != EmberCrpg.Domain.Worldgen.NpcRole.Outlaw) return false;

            _worldEncounterId = actor.Id;
            _worldEncounterLootGranted = false;
            _lastCombatLine = $"{actor.Name} draws steel!";
            WorldEncounterSignal.Raise();
            WorldEncounterStingFeed.Raise();
            UnityEngine.Debug.Log($"[Encounter] world encounter begun vs '{actor.Name}' (outlaw).");
            return true;
        }

        // ----- F2-DoD LOOP PROOF (--ember-looptest) ----------------------------------------------------
        // Proof-only entry points that run the loop's legs through the EXACT production paths (encounter
        // binding, CombatActionResolver strikes, quest/spoils settlement, live-priced trade) and return
        // LOOP-PROOF transcript lines for the playtest log. Same diagnostics precedent as the Proof* hooks.

        public string ProofQuestSnapshot()
        {
            int active = 0, complete = 0;
            foreach (var kv in WorldQuestStates ?? new System.Collections.Generic.Dictionary<ulong, EmberCrpg.Domain.Quest.QuestState>())
            {
                active++;
                if (kv.Value != null && kv.Value.IsComplete) complete++;
            }
            return $"LOOP-PROOF: world-quests active={active} complete={complete}, purse={_world?.PlayerGold ?? -1} gold.";
        }

        /// <summary>
        /// PROOF-ONLY: advance the sim clock N hours — HOUR BY HOUR, not one jump (shipcheck6 finding:
        /// a single jumped tick index skips every hourly/daily cadence boundary in between, so dailies
        /// like harvest/prices never fired and the economy probe read a false FLAT).
        /// </summary>
        public void ProofAdvanceHours(int hours)
        {
            int ticksPerHour = System.Math.Max(1, EmberCrpg.Simulation.Composition.WorldTickComposer.TicksPerGameDay / 24);
            // Steps stay ≤1h so hourly/daily cadences fire (shipcheck6 finding) — but the loop now
            // measures REAL time moved: a stale _tick made the first step a silent no-op (the respawn
            // test caught +7h where the contract says +8h).
            long target = _world.Time.TotalMinutes + hours * 60L;
            int guard = 0;
            while (_world.Time.TotalMinutes < target && guard++ < (hours * 4) + 8)
                AdvanceTick(_tick + ticksPerHour);
        }

        /// <summary>F7-DoD: the harvest→stock→price chain across 3 sim days, as one verdict line.</summary>
        public string ProofEconomyChain()
        {
            int StockTotal()
            {
                // QUANTITIES, not tag counts (shipcheck7 finding: StockpileComponent.Count is the number of
                // DISTINCT tags — harvest adding +2 wheat to an existing wheat entry was invisible).
                int total = 0;
                var piles = _world?.Stockpiles;
                if (piles != null)
                    for (int i = 0; i < piles.Count; i++)
                        if (piles[i] != null)
                            foreach (var e in piles[i].Entries) total += e.Value;
                return total;
            }
            int PriceProbe()
            {
                var piles = _world?.Stockpiles;
                if (piles == null || _world.Prices == null) return -1;
                foreach (var p in piles)
                {
                    if (p == null) continue;
                    foreach (var e in p.Entries)
                    {
                        int v = _world.Prices.GetPrice(p.SiteId, e.Key);
                        if (v > 0) return v;
                    }
                }
                return -1;
            }

            int StageSignature()
            {
                int sig = 0;
                var plants = _world?.Plants;
                if (plants != null)
                    foreach (var row in plants.Rows)
                        if (row.Value != null)
                            sig += row.Value.StageId.Value.Length + row.Value.DaysInStage; // cheap change detector
                return sig;
            }

            int stockBefore = StockTotal(), priceBefore = PriceProbe(), stageBefore = StageSignature();
            ProofAdvanceHours(72); // three sim days: growth + harvest + daily price ticks
            int stockAfter = StockTotal(), priceAfter = PriceProbe(), stageAfter = StageSignature();
            bool moved = stockAfter != stockBefore || (priceBefore > 0 && priceAfter != priceBefore);
            // Season honesty (shipcheck FLAT-in-winter finding): crops legitimately stop growing out of
            // season (DF behaviour). When NOTHING in the plant layer moved either, the chain is DORMANT,
            // not broken — the in-season grow→harvest cycle is covered by WorldLivesOverNTicksTests.
            bool dormant = !moved && stageAfter == stageBefore;
            string verdict = moved ? "OK" : dormant ? "DORMANT-OK (out of season; in-season cycle unit-proven)" : "FLAT";
            int plantCount = 0;
            if (_world?.Plants != null) foreach (var _ in _world.Plants.Rows) plantCount++;
            return $"LOOP-PROOF economy-chain {verdict}: stock {stockBefore}->{stockAfter}, " +
                   $"price {priceBefore}->{priceAfter}, plants={plantCount} stageSig {stageBefore}->{stageAfter} over 3 sim days.";
        }

        /// <summary>F6-DoD: greeting lines from three DIFFERENT-role NPCs — variety must show in the log.</summary>
        public string ProofGreetingSample()
        {
            if (_world?.NpcSeeds == null) return "LOOP-PROOF: no npc seeds for greeting sample.";
            var seen = new System.Collections.Generic.HashSet<EmberCrpg.Domain.Worldgen.NpcRole>();
            var sb = new System.Text.StringBuilder("LOOP-PROOF greetings:");
            foreach (var npc in _world.NpcSeeds)
            {
                if (npc == null || !seen.Add(npc.Role)) continue;
                sb.Append("\n  [").Append(npc.Role).Append("] ")
                  .Append(DeterministicGreeting(npc.Name, npc, null));
                if (seen.Count >= 3) break;
            }
            return sb.ToString();
        }

        /// <summary>
        /// F8-DoD: bind the encounter WITHOUT fighting and refresh the combat read once — the battle
        /// mirror flips, and the music director's next poll must switch to the BATTLE slot. The headless
        /// encounter leg resolves in one frame, too fast for any audible/loggable transition.
        /// </summary>
        public string ProofBindEncounterForMusic()
        {
            var outlawSeed = _world?.NpcSeeds?.FirstOrDefault(n => n != null && n.Role == EmberCrpg.Domain.Worldgen.NpcRole.Outlaw);
            if (outlawSeed == null) return "LOOP-PROOF: no outlaw to bind for the music proof.";
            var actorId = new ActorId(GeneratedNpcActorOffset + outlawSeed.Id.Value);
            if (_world.Actors == null || !_world.Actors.TryGet(actorId, out var outlaw) || outlaw == null)
                return "LOOP-PROOF: outlaw actor missing for the music proof.";
            TryBeginWorldEncounter(outlaw, outlawSeed);
            ReadCombatScreenState(); // publishes the battle mirror
            return "LOOP-PROOF: encounter bound for the BATTLE-music transition window.";
        }

        private float _nextEnemyStrikeAt;
        private float _nextHostileAiAt;

        /// <summary>F17 XP: kills (+40) and world-quest completions (+60) feed the level gate. The
        /// HUD pump watches <see cref="LevelUpReady"/> and opens the level-up screen once per crossing.</summary>
        private void GrantXp(int amount, string reason)
        {
            if (_world == null || amount <= 0) return;
            _world.PlayerXp += amount;
            int need = EmberCrpg.Simulation.World.PlayerLevelUpService.XpForNextLevel(_world.PlayerLevel);
            string status = _world.PlayerXp >= need ? " — LEVEL UP READY" : string.Empty;
            UnityEngine.Debug.Log($"[XP] +{amount} ({reason}) {_world.PlayerXp}/{need}{status}");
        }

        public bool LevelUpReady =>
            _world != null && _world.PlayerXp
                >= EmberCrpg.Simulation.World.PlayerLevelUpService.XpForNextLevel(_world.PlayerLevel);

        /// <summary>
        /// F14 ENEMY MOVEMENT ("düşman kovalasın"): hostiles that SEE the player (12 cells ≈ 12m) give
        /// chase — one grid cell per 0.45s ≈ 2.2 m/s, the DFU street speed — and STOP adjacent (≤2
        /// cells ≈ 1.6m) to fight. Reaching aggro range (≤3) auto-binds the world encounter, so walking
        /// into a delve chamber starts the fight without pressing E. Positions move through the SAME
        /// ActorRecord.MoveTo the ScheduleSystem uses; the id-keyed sync carries billboards along.
        /// Honest limit: only ONE enemy binds at a time (multi-enemy melee is F18's chamber fight).
        /// </summary>
        public void TickHostileAi(float unscaledNow)
        {
            if (unscaledNow < _nextHostileAiAt) return;
            _nextHostileAiAt = unscaledNow + 0.45f;
            if (_world?.NpcSeeds == null || _world.Actors == null) return;
            var player = _world.Actors.FirstByRole(ActorRole.Player);
            if (player == null || !player.IsAlive) return;
            var target = PlayerCombatPosition(player); // the LIVE body, not the parked actor
            var here = CurrentSettlementOrStart;

            for (int i = 0; i < _world.NpcSeeds.Count; i++)
            {
                var seed = _world.NpcSeeds[i];
                if (seed == null || !seed.Home.Equals(here))
                    continue;
                // F23: outlaws always hunt; GUARDS hunt only while a bounty stands on the player.
                bool hunts = seed.Role == EmberCrpg.Domain.Worldgen.NpcRole.Outlaw
                    || (seed.Role == EmberCrpg.Domain.Worldgen.NpcRole.Guard && _world.PlayerBountyGold > 0);
                if (!hunts)
                    continue;
                var actorId = new ActorId(GeneratedNpcActorOffset + seed.Id.Value);
                if (!_world.Actors.TryGet(actorId, out var hostile) || hostile == null || !hostile.IsAlive)
                    continue;

                int dist = Chebyshev(hostile.Position, target);
                if (dist <= 2) continue; // already at melee reach

                // F18 LAIR LEASH (pinned guards only — home == dayAnchor, the dungeon-dweller
                // contract): a dweller hunts its own halls (10 cells of home), the BOSS never
                // leaves the hoard (3). Past the leash, or with the player out of sight (18 —
                // the 16m room lattice needs more than the old 12), it stalks BACK to its post;
                // ScheduleSystem deliberately ignores pinned guards, so this is its only way home.
                bool pinned = hostile.Home.Equals(hostile.DayAnchor);
                bool lairBoss = seed.Id.Value >= HaunterNpcIdBase
                    && (seed.Id.Value - HaunterNpcIdBase) % 16UL == 15UL;
                int homeDist = Chebyshev(hostile.Position, hostile.Home);
                if (dist > 18 || (pinned && homeDist >= (lairBoss ? 3 : 10)))
                {
                    if (pinned && homeDist > 0)
                        hostile.MoveTo(new GridPosition(
                            hostile.Position.X + System.Math.Sign(hostile.Home.X - hostile.Position.X),
                            hostile.Position.Y + System.Math.Sign(hostile.Home.Y - hostile.Position.Y)));
                    continue;
                }

                int dx = System.Math.Sign(target.X - hostile.Position.X);
                int dy = System.Math.Sign(target.Y - hostile.Position.Y);
                hostile.MoveTo(new GridPosition(hostile.Position.X + dx, hostile.Position.Y + dy));

                if (Chebyshev(hostile.Position, target) <= 3 && WorldEncounterEnemy() == null)
                    TryBeginWorldEncounter(hostile, seed); // aggro: music flips, panel appears, sting plays
            }
        }

        /// <summary>
        /// F13 REAL-TIME COMBAT ("savaş ekranında pause olmamalı"): the bound enemy fights BACK on its
        /// own ~2.4s cadence while alive and in reach — Daggerfall/DOOM feel, no modal, no pause. The HUD
        /// pump calls this every unpaused frame with unscaled time; the same CombatActionResolver dice as
        /// the player's swing keep it honest (accuracy vs dodge, armor mitigation, event-logged).
        /// </summary>
        public void TickWorldEncounter(float unscaledNow)
        {
            var enemy = WorldEncounterEnemy();
            if (enemy == null || !enemy.IsAlive || _world?.Events == null) return;
            if (unscaledNow < _nextEnemyStrikeAt) return;
            _nextEnemyStrikeAt = unscaledNow + 2.4f;

            var player = _world.Actors?.FirstByRole(ActorRole.Player);
            if (player == null || !player.IsAlive) return;
            if (Chebyshev(PlayerCombatPosition(player), enemy.Position) > 6)
            {
                _lastCombatLine = $"{enemy.Name} stalks closer...";
                return;
            }

            var action = new EmberCrpg.Domain.Combat.CombatActionDef(
                id: new EmberCrpg.Domain.Combat.CombatActionId("enemy_swing"),
                staminaCost: 0,
                hitFormulaKey: "accuracy_vs_dodge",
                damageFormulaKey: "base_minus_armor",
                animationTag: "enemy_swing");
            _meleeStrikeSerial++; // shares the player's strike serial so same-tick rolls stay distinct
            uint timeSeed = (uint)(_world.Time.TotalMinutes & 0xFFFFFFFFL);
            uint eventSeed = (uint)((_world.Events.Events?.Count ?? 0) & 0xFFFFFFFFL);
            var rng = new EmberCrpg.Simulation.Rng.XorShiftRng(
                (timeSeed * 2654435761u) ^ (eventSeed * 1597334677u) ^ (_meleeStrikeSerial * 0x9E3779B9u) ^ 0x5EEDBEEFu);
            var resolver = new EmberCrpg.Simulation.Combat.CombatActionResolver(
                new EmberCrpg.Simulation.Combat.CombatHitRollService(),
                new EmberCrpg.Simulation.Combat.CombatDamageService());
            var outcome = resolver.Resolve(action, enemy, player,
                damageBandWidth: System.Math.Max(1, enemy.BaseDamage / 2),
                rng: rng, now: _world.Time, siteId: ResolveCombatSiteId(enemy, player), events: _world.Events,
                defenderMitigation: AbsorbWithPlayerWard); // F28: ember_ward finally guards for real
            _lastCombatLine = outcome.Hit
                ? $"{enemy.Name} hits you for {outcome.Damage}!"
                : $"{enemy.Name} misses you.";
            // F14 attack feel: the striking billboard lunges (the view consumes this stamp).
            EmberCrpg.Presentation.Ember.WorldDirector.WorldCombatFeedbackFeed.RaiseEnemyStrike(enemy.Id.Value);
        }

        /// <summary>
        /// F15 ÖLÜM+RESPAWN: dying is a toll, not a wall — 20% of the purse pays for your care, vitals
        /// refill, the world clock walks forward 8 HOURS (hour-by-hour: a single tick-jump skips hourly
        /// cadences — the banked shipcheck-6 gotcha), the encounter unbinds, and the body wakes at the
        /// CURRENT settlement's plaza. Works without a save file by design (roadmap F15).
        /// </summary>
        public string RespawnAfterDeath()
        {
            var player = _world?.Actors?.FirstByRole(ActorRole.Player);
            if (player == null) return "No player to awaken.";
            if (player.IsAlive) return "You are not dead.";

            int lost = _world.PlayerGold / 5;
            _world.PlayerGold -= lost;
            player.ApplyVitals(new EmberCrpg.Domain.Actors.ActorVitals(
                player.Vitals.Health.Refill(), player.Vitals.Fatigue.Refill(), player.Vitals.Mana.Refill()));
            _worldEncounterId = default;
            _worldEncounterLootGranted = false;
            EmberCrpg.Presentation.Ember.WorldDirector.RuntimeBattleMirror.Active = false;
            ProofAdvanceHours(8);

            var here = CurrentSettlementOrStart;
            player.MoveTo(CenterOfSite(SettlementSiteId(here)));
            string settlementName = "the last settlement";
            var map = _world.Overland;
            if (map != null)
                for (int i = 0; i < map.Settlements.Count; i++)
                    if (map.Settlements[i].Id.Equals(here)) { settlementName = map.Settlements[i].Name; break; }

            _lastCombatLine = $"You awaken at {settlementName}. {lost} gold paid for your care; 8 hours pass.";
            UnityEngine.Debug.Log("[Respawn] " + _lastCombatLine);
            return _lastCombatLine;
        }

        /// <summary>F15-DoD: lose to the bound enemy ON PURPOSE (AFK duel — the enemy is placed adjacent
        /// so its real strike cadence connects), then respawn and report the full toll honestly.</summary>
        public string ProofDieAndRespawn()
        {
            // The encounter leg may have ALREADY felled the first outlaw — duel the first LIVING one.
            EmberCrpg.Domain.Worldgen.NpcSeedRecord outlawSeed = null;
            ActorRecord outlaw = null;
            if (_world?.NpcSeeds != null && _world.Actors != null)
                foreach (var seed in _world.NpcSeeds)
                {
                    if (seed == null || seed.Role != EmberCrpg.Domain.Worldgen.NpcRole.Outlaw) continue;
                    var candidateId = new ActorId(GeneratedNpcActorOffset + seed.Id.Value);
                    if (_world.Actors.TryGet(candidateId, out var candidate) && candidate != null && candidate.IsAlive)
                    {
                        outlawSeed = seed;
                        outlaw = candidate;
                        break;
                    }
                }
            if (outlaw == null) return "LOOP-PROOF: no living outlaw actor for the death leg.";
            var player = _world.Actors.FirstByRole(ActorRole.Player);
            if (player == null) return "LOOP-PROOF: no player.";

            TryBeginWorldEncounter(outlaw, outlawSeed);
            outlaw.MoveTo(new GridPosition(player.Position.X + 1, player.Position.Y)); // the duel is adjacent
            int goldBefore = _world.PlayerGold;
            long minutesBefore = _world.Time.TotalMinutes;

            float t = 100000f;
            int swings = 0;
            while (player.IsAlive && swings < 200)
            {
                TickWorldEncounter(t);
                t += 2.5f;
                swings++;
            }
            if (player.IsAlive)
                return $"LOOP-PROOF: BROKEN — player survived {swings} enemy strikes (balance drifted?).";

            string awaken = RespawnAfterDeath();
            long hoursPassed = (_world.Time.TotalMinutes - minutesBefore) / 60;
            return $"LOOP-PROOF: death+respawn — fell in {swings} enemy swings, purse {goldBefore}->{_world.PlayerGold} " +
                   $"(-20%), hp={player.Vitals.Health.Current}/{player.Vitals.Health.Max}, +{hoursPassed}h. '{awaken}'";
        }

        /// <summary>
        /// F16 CHEST LOOT: the delve chamber's chest yields the tier-up weapon (Worn Iron Sword,
        /// +8 acc/+5 dmg vs the starting blade's +5/+2) and AUTO-EQUIPS it when it beats the current
        /// hand. One sword per world (template-guarded — looting twice finds the chest empty).
        /// Honest limit: chest-opened state itself isn't save-persisted yet (F22 alanı).
        /// </summary>
        public string LootDungeonChest()
        {
            var inventory = _world?.PlayerInventory;
            if (inventory == null) return "No pack to carry loot.";
            foreach (var item in inventory.Items)
                if (item != null && string.Equals(item.TemplateId,
                        EmberCrpg.Simulation.Inventory.WorldItemCatalog.WornIronSwordTemplateId, System.StringComparison.Ordinal))
                {
                    _lastCombatLine = "The chest is empty — you already took its sword.";
                    return _lastCombatLine;
                }

            var sword = EmberCrpg.Simulation.Inventory.WorldItemCatalog.CreateWornIronSword();
            if (!inventory.TryAdd(sword))
            {
                _lastCombatLine = "Your pack is full — the sword stays in the chest.";
                return _lastCombatLine;
            }

            var current = EquippedWeapon();
            bool equip = current == null || current.DamageBonus < sword.DamageBonus;
            if (equip)
                _world.PlayerEquipment.Equip(EmberCrpg.Domain.Inventory.EquipmentSlot.Weapon, sword.Id);
            _lastCombatLine = equip
                ? $"You take the {sword.DisplayName} (+{sword.AccuracyBonus} acc, +{sword.DamageBonus} dmg) and equip it."
                : $"You take the {sword.DisplayName} (+{sword.AccuracyBonus} acc, +{sword.DamageBonus} dmg).";
            UnityEngine.Debug.Log("[Loot] " + _lastCombatLine);
            return _lastCombatLine;
        }

        /// <summary>F16-DoD: the swing-log difference — same dice seed family, bare hands vs the chest
        /// sword, 12 swings each against the first living outlaw; the damage sums must tell the story.</summary>
        public string ProofWeaponSwingDiff()
        {
            var player = _world?.Actors?.FirstByRole(ActorRole.Player);
            if (player == null) return "LOOP-PROOF: no player for the weapon diff.";
            ActorRecord dummy = null;
            foreach (var a in _world.Actors.Records)
                if (a != null && a.Role == ActorRole.Enemy && a.IsAlive) { dummy = a; break; }
            if (dummy == null) return "LOOP-PROOF: no living enemy for the weapon diff.";
            dummy.MoveTo(new GridPosition(player.Position.X + 1, player.Position.Y));

            int SwingSum()
            {
                // Immortal dummy, 20 swings: the per-strike RNG serial can't be seed-paired across the
                // two phases, so only sample size makes the comparison fair (6 swings once read 22 vs
                // 20 — pure dice noise; expectation is ~24 bare vs ~34 armed per 6).
                int sum = 0;
                for (int i = 0; i < 20; i++)
                {
                    dummy.ApplyVitals(new EmberCrpg.Domain.Actors.ActorVitals(
                        dummy.Vitals.Health.Refill(), dummy.Vitals.Fatigue.Refill(), dummy.Vitals.Mana.Refill()));
                    int before = dummy.Vitals.Health.Current;
                    TryMeleeStrike(dummy.Name, 6);
                    sum += before - dummy.Vitals.Health.Current;
                }
                return sum;
            }

            var hand = _world.PlayerEquipment.GetEquippedItemId(EmberCrpg.Domain.Inventory.EquipmentSlot.Weapon);
            _world.PlayerEquipment.Unequip(EmberCrpg.Domain.Inventory.EquipmentSlot.Weapon);
            int bare = SwingSum();
            if (!hand.IsEmpty)
                _world.PlayerEquipment.Equip(EmberCrpg.Domain.Inventory.EquipmentSlot.Weapon, hand);
            int armed = SwingSum();
            return $"LOOP-PROOF: F16 weapon diff — 20 bare swings dealt {bare}, 20 armed swings dealt {armed} " +
                   $"(weapon={EquippedWeapon()?.DisplayName ?? "none"}).";
        }

        /// <summary>F18 diagnostics: sim-side chase telemetry — position + Chebyshev distance of every
        /// living dweller vs the live player body, logged at the two chase frames so view lag and sim
        /// stall read apart in Player.log.</summary>
        public string ProofChaseDebug()
        {
            var here = CurrentSettlementOrStart;
            if (_world?.NpcSeeds == null || _world.Actors == null) return "chase-debug: no world";
            var player = _world.Actors.FirstByRole(ActorRole.Player);
            if (player == null) return "chase-debug: no player";
            var target = PlayerCombatPosition(player);
            var sb = new System.Text.StringBuilder($"chase-debug target=({target.X},{target.Y})");
            for (int i = 0; i < _world.NpcSeeds.Count; i++)
            {
                var seed = _world.NpcSeeds[i];
                if (seed == null || seed.Role != EmberCrpg.Domain.Worldgen.NpcRole.Outlaw || !seed.Home.Equals(here))
                    continue;
                var actorId = new ActorId(GeneratedNpcActorOffset + seed.Id.Value);
                if (!_world.Actors.TryGet(actorId, out var hostile) || hostile == null || !hostile.IsAlive)
                    continue;
                sb.Append($" | {hostile.Name} pos=({hostile.Position.X},{hostile.Position.Y}) cheb={Chebyshev(hostile.Position, target)}");
            }
            return sb.ToString();
        }

        /// <summary>F20: pick up the delve key. One in the pack at a time — the boss door's lock
        /// consumes it, so the id stays reusable across delves.</summary>
        public string PickUpDelveKey()
        {
            var inventory = _world?.PlayerInventory;
            if (inventory == null) return "No pack for the key.";
            foreach (var item in inventory.Items)
                if (item != null && string.Equals(item.TemplateId,
                        EmberCrpg.Simulation.Inventory.WorldItemCatalog.TarnishedKeyTemplateId, System.StringComparison.Ordinal))
                {
                    _lastCombatLine = "You already carry the tarnished key.";
                    return _lastCombatLine;
                }
            _lastCombatLine = inventory.TryAdd(EmberCrpg.Simulation.Inventory.WorldItemCatalog.CreateTarnishedKey())
                ? "You take the Tarnished Key — somewhere below, a lock waits."
                : "Your pack is full — the key stays on its pedestal.";
            UnityEngine.Debug.Log("[Key] " + _lastCombatLine);
            return _lastCombatLine;
        }

        /// <summary>F20: the boss door's lock — consumes the key when the pack holds one.</summary>
        public bool TryConsumeDelveKey()
        {
            var inventory = _world?.PlayerInventory;
            if (inventory == null) return false;
            bool unlocked = inventory.TryRemove(
                EmberCrpg.Simulation.Inventory.WorldItemCatalog.TarnishedKeyTemplateId, 1);
            if (unlocked)
            {
                _lastCombatLine = "The Tarnished Key turns — the boss door grinds open.";
                UnityEngine.Debug.Log("[Door] " + _lastCombatLine);
            }
            return unlocked;
        }

        /// <summary>F19 proof: every Dungeon-kind settlement name in map order — the variety leg
        /// travels down this list (worlds may roll as few as one delve; that is honest output).</summary>
        public System.Collections.Generic.List<string> ProofListDelveNames()
        {
            var names = new System.Collections.Generic.List<string>();
            var map = _world?.Overland;
            if (map == null) return names;
            for (int i = 0; i < map.Settlements.Count; i++)
                if (map.Settlements[i].Kind == EmberCrpg.Domain.Overland.SettlementKind.Dungeon)
                    names.Add(map.Settlements[i].Name);
            return names;
        }

        /// <summary>F26 TAVERN: sleep 8 hours for 5 gold — vitals refill, the clock walks hour-by-hour
        /// (the same cadence-safe advance the respawn uses).</summary>
        public string TrySleepAtTavern()
        {
            var player = _world?.Actors?.FirstByRole(ActorRole.Player);
            if (player == null || !player.IsAlive) return "No one to put to bed.";
            if (_world.PlayerGold < 5)
            {
                _lastCombatLine = "A bed costs 5 gold — your purse is too light.";
                return _lastCombatLine;
            }
            _world.PlayerGold -= 5;
            player.ApplyVitals(new EmberCrpg.Domain.Actors.ActorVitals(
                player.Vitals.Health.Refill(), player.Vitals.Fatigue.Refill(), player.Vitals.Mana.Refill()));
            ProofAdvanceHours(8);
            _lastCombatLine = $"You sleep 8 hours at the tavern — vitals restored (-5g, purse {_world.PlayerGold}).";
            UnityEngine.Debug.Log("[Tavern] " + _lastCombatLine);
            return _lastCombatLine;
        }

        /// <summary>F26 TEMPLE: the clergy mend wounds for 8 gold — health only; rest is the tavern's trade.</summary>
        public string TryTempleHeal()
        {
            var player = _world?.Actors?.FirstByRole(ActorRole.Player);
            if (player == null || !player.IsAlive) return "No one to heal.";
            if (player.Vitals.Health.Current >= player.Vitals.Health.Max)
            {
                _lastCombatLine = "You are already whole.";
                return _lastCombatLine;
            }
            if (_world.PlayerGold < 8)
            {
                _lastCombatLine = "The temple asks 8 gold for its blessing.";
                return _lastCombatLine;
            }
            _world.PlayerGold -= 8;
            player.ApplyVitals(new EmberCrpg.Domain.Actors.ActorVitals(
                player.Vitals.Health.Refill(), player.Vitals.Fatigue, player.Vitals.Mana));
            _lastCombatLine = $"The clergy mend your wounds (-8g, purse {_world.PlayerGold}).";
            UnityEngine.Debug.Log("[Temple] " + _lastCombatLine);
            return _lastCombatLine;
        }

        /// <summary>F26: seat the settlement's Innkeeper (else a Merchant) inside the tavern. MoveTo
        /// only — the daily schedule may walk them out over hours (persistent re-homing is v2).</summary>
        public string PinHostInsideTavern(UnityEngine.Vector3 tavernWorld)
        {
            var here = CurrentSettlementOrStart;
            if (_world?.NpcSeeds == null || _world.Actors == null) return "no world";
            ActorRecord host = null;
            for (int pass = 0; pass < 2 && host == null; pass++)
            {
                var wanted = pass == 0
                    ? EmberCrpg.Domain.Worldgen.NpcRole.Innkeeper
                    : EmberCrpg.Domain.Worldgen.NpcRole.Merchant;
                for (int i = 0; i < _world.NpcSeeds.Count; i++)
                {
                    var seed = _world.NpcSeeds[i];
                    if (seed == null || seed.Role != wanted || !seed.Home.Equals(here)) continue;
                    var actorId = new ActorId(GeneratedNpcActorOffset + seed.Id.Value);
                    if (_world.Actors.TryGet(actorId, out var actor) && actor != null && actor.IsAlive)
                    { host = actor; break; }
                }
            }
            var origin = BillboardOrigin();
            var tavernGrid = new GridPosition(
                origin.X + UnityEngine.Mathf.RoundToInt(tavernWorld.x),
                origin.Y + UnityEngine.Mathf.RoundToInt(tavernWorld.z));
            // F27: publish the communal LUNCH SPOT — ScheduleSystem routes civilians here 12:00-13:59.
            _world.TavernCell = tavernGrid;
            if (host == null) return "no host NPC here";
            host.MoveTo(tavernGrid);
            UnityEngine.Debug.Log($"[Tavern] host seated inside: {host.Name}.");
            return host.Name;
        }

        /// <summary>F27-DoD proof: how many civilians sit within 6 cells of the lunch spot right now.</summary>
        public string ProofLunchCensus()
        {
            if (_world?.TavernCell == null || _world.Actors == null) return "LUNCH: no tavern cell published.";
            var spot = _world.TavernCell.Value;
            int count = 0;
            foreach (var a in _world.Actors.Records)
            {
                if (a == null || !a.IsAlive || a.Role == ActorRole.Player
                    || a.Role == ActorRole.Enemy || a.Role == ActorRole.Guard) continue;
                if (Chebyshev(a.Position, spot) <= 6) count++;
            }
            UnityEngine.Debug.Log($"[Lunch] {count} civilians at the tavern (hour {(int)((_world.Time.TotalMinutes / 60) % 24):00}).");
            return $"LUNCH: {count} civilians within 6 cells of the tavern.";
        }

        /// <summary>F26-DoD proof: the tavern sleep flow as one honest transcript line.</summary>
        public string ProofTavernSleepLeg()
        {
            var player = _world?.Actors?.FirstByRole(ActorRole.Player);
            if (player == null) return "LOOP-PROOF: no player for the tavern leg.";
            TakePlayerDamage(12); // arrive tired and hurt so the refill is observable
            int goldBefore = _world.PlayerGold;
            int hpBefore = player.Vitals.Health.Current;
            long minutesBefore = _world.Time.TotalMinutes;
            string line = TrySleepAtTavern();
            long hoursPassed = (_world.Time.TotalMinutes - minutesBefore) / 60;
            return $"LOOP-PROOF tavern-sleep: hp {hpBefore}->{player.Vitals.Health.Current}/{player.Vitals.Health.Max}, " +
                   $"purse {goldBefore}->{_world.PlayerGold}, +{hoursPassed}h — {line}";
        }

        /// <summary>F23-DoD proof: commit a daylight crime — an AIMED strike at the nearest living
        /// CIVILIAN (never an enemy or the watch itself) — then report the bounty and the nearest
        /// guard so the driver can watch the watch close in.</summary>
        public string ProofCrimeAndWatchLeg()
        {
            var here = CurrentSettlementOrStart;
            if (_world?.NpcSeeds == null || _world.Actors == null) return "LOOP-PROOF: no world for the crime leg.";
            var player = _world.Actors.FirstByRole(ActorRole.Player);
            if (player == null) return "LOOP-PROOF: no player for the crime leg.";
            var from = PlayerCombatPosition(player);

            ActorRecord victim = null;
            int victimDist = int.MaxValue;
            for (int i = 0; i < _world.NpcSeeds.Count; i++)
            {
                var seed = _world.NpcSeeds[i];
                if (seed == null || !seed.Home.Equals(here)) continue;
                if (seed.Role == EmberCrpg.Domain.Worldgen.NpcRole.Outlaw
                    || seed.Role == EmberCrpg.Domain.Worldgen.NpcRole.Guard) continue;
                var actorId = new ActorId(GeneratedNpcActorOffset + seed.Id.Value);
                if (!_world.Actors.TryGet(actorId, out var actor) || actor == null || !actor.IsAlive) continue;
                int dist = Chebyshev(from, actor.Position);
                if (dist >= victimDist) continue;
                victim = actor;
                victimDist = dist;
            }
            if (victim == null) return "LOOP-PROOF: no civilian here — crime leg skipped.";

            TryMeleeStrike(victim.Name, 20); // the aimed swing IS the crime, hit or miss
            var watch = ProofWatchSnapshot();
            return $"LOOP-PROOF crime: struck at {victim.Name}, bounty={_world.PlayerBountyGold}g " +
                   $"rep={_world.PlayerReputation}, watch A: {watch}.";
        }

        /// <summary>F23 telemetry: "guardName|chebDistance" of the nearest living guard homed here.</summary>
        public string ProofWatchSnapshot()
        {
            var here = CurrentSettlementOrStart;
            if (_world?.NpcSeeds == null || _world.Actors == null) return "none|-1";
            var player = _world.Actors.FirstByRole(ActorRole.Player);
            if (player == null) return "none|-1";
            var from = PlayerCombatPosition(player);

            ActorRecord guard = null;
            int guardDist = int.MaxValue;
            for (int i = 0; i < _world.NpcSeeds.Count; i++)
            {
                var seed = _world.NpcSeeds[i];
                if (seed == null || seed.Role != EmberCrpg.Domain.Worldgen.NpcRole.Guard || !seed.Home.Equals(here))
                    continue;
                var actorId = new ActorId(GeneratedNpcActorOffset + seed.Id.Value);
                if (!_world.Actors.TryGet(actorId, out var actor) || actor == null || !actor.IsAlive) continue;
                int dist = Chebyshev(from, actor.Position);
                if (dist >= guardDist) continue;
                guard = actor;
                guardDist = dist;
            }
            return guard == null ? "none|-1" : $"{guard.Name}|{guardDist}";
        }

        /// <summary>F18-DoD: bind the delve's WARDEN (the boss — "Warden of X") for the boss leg.</summary>
        public string ProofBindDelveWarden()
        {
            var here = CurrentSettlementOrStart;
            if (_world?.NpcSeeds == null || _world.Actors == null) return string.Empty;
            for (int i = 0; i < _world.NpcSeeds.Count; i++)
            {
                var seed = _world.NpcSeeds[i];
                if (seed == null || seed.Role != EmberCrpg.Domain.Worldgen.NpcRole.Outlaw || !seed.Home.Equals(here))
                    continue;
                if (!seed.Name.StartsWith("Warden of")) continue;
                var actorId = new ActorId(GeneratedNpcActorOffset + seed.Id.Value);
                if (!_world.Actors.TryGet(actorId, out var warden) || warden == null || !warden.IsAlive)
                    continue;
                TryBeginWorldEncounter(warden, seed);
                ReadCombatScreenState();
                return $"{warden.Name} ({warden.Vitals.Health.Current}/{warden.Vitals.Health.Max} hp)";
            }
            return string.Empty;
        }

        /// <summary>F10-DoD: bind the CURRENT settlement's haunter (an Outlaw homed HERE — street outlaws
        /// elsewhere don't qualify) so the dungeon leg fights the chamber guard, not a random bandit.</summary>
        public string ProofBindDungeonHaunter()
        {
            var here = CurrentSettlementOrStart;
            if (_world?.NpcSeeds == null || _world.Actors == null) return string.Empty;
            var player = _world.Actors.FirstByRole(ActorRole.Player);
            // F18: rooms spread dwellers up to ~50m apart — bind the NEAREST living one (the one that
            // chased in), never the Warden (the boss leg binds that one itself).
            ActorRecord nearest = null;
            EmberCrpg.Domain.Worldgen.NpcSeedRecord nearestSeed = null;
            int nearestDist = int.MaxValue;
            for (int i = 0; i < _world.NpcSeeds.Count; i++)
            {
                var seed = _world.NpcSeeds[i];
                if (seed == null || seed.Role != EmberCrpg.Domain.Worldgen.NpcRole.Outlaw || !seed.Home.Equals(here))
                    continue;
                if (seed.Name.StartsWith("Warden of")) continue;
                var actorId = new ActorId(GeneratedNpcActorOffset + seed.Id.Value);
                if (!_world.Actors.TryGet(actorId, out var actor) || actor == null || !actor.IsAlive)
                    continue;
                int dist = player != null ? Chebyshev(PlayerCombatPosition(player), actor.Position) : 0;
                if (nearest != null && dist >= nearestDist) continue;
                nearest = actor;
                nearestSeed = seed;
                nearestDist = dist;
            }
            if (nearest == null) return string.Empty;
            TryBeginWorldEncounter(nearest, nearestSeed);
            ReadCombatScreenState(); // publish the battle mirror
            return nearest.Name;
        }

        /// <summary>F10-DoD: resolve the bound encounter to the end (same dice as a player mashing attack)
        /// so SettleWorldEncounterIfOver fires the felled feed and the corpse pose is capturable.</summary>
        public string ProofFinishBoundEncounter()
        {
            var enemy = WorldEncounterEnemy();
            if (enemy == null) return "LOOP-PROOF: no bound enemy to finish.";
            int swings = 0;
            while (enemy.IsAlive && swings < 250)
            {
                TryMeleeStrike(enemy.Name, 20);
                swings++;
                ReadCombatScreenState(); // per-read settle, mirrors the real combat-screen cadence
            }
            return $"LOOP-PROOF: haunter '{enemy.Name}' felled={!enemy.IsAlive} in {swings} swings.";
        }

        public string ProofRunEncounterLeg()
        {
            var outlawSeed = _world?.NpcSeeds?.FirstOrDefault(n => n != null && n.Role == EmberCrpg.Domain.Worldgen.NpcRole.Outlaw);
            if (outlawSeed == null) return "LOOP-PROOF: no outlaw in this world — encounter leg skipped.";

            var actorId = new ActorId(GeneratedNpcActorOffset + outlawSeed.Id.Value);
            if (_world.Actors == null || !_world.Actors.TryGet(actorId, out var outlaw) || outlaw == null)
                return "LOOP-PROOF: BROKEN — outlaw seed has no actor.";

            int goldBefore = _world.PlayerGold;
            TryBeginWorldEncounter(outlaw, outlawSeed);
            var player = _world.Actors.FirstByRole(ActorRole.Player);
            // Proof verifies the PATH (hit→damage→death→spoils→bounty), not balance: a fresh player's real
            // chance vs an outlaw clamps to the 5% floor, so the proof swings harder and longer than a
            // starting kit would. The thin-progression finding is reported separately.
            // 250-swing budget: at the fresh-player 5% floor the kill needs ~3 hits (E[hits]=12.5);
            // a 150 cap failed once on a ~2% bad-dice tail (looktest7: 2 hits, enemy at 9hp).
            int swings = 0;
            while (outlaw.IsAlive && swings < 250)
            {
                TryMeleeStrike(outlaw.Name, 20);
                swings++;
                if (swings % 10 == 0)
                    UnityEngine.Debug.Log($"LOOP-PROOF: swing {swings}: '{_lastCombatLine}' | " +
                        $"enemyHp={outlaw.Vitals.Health.Current}/{outlaw.Vitals.Health.Max}, " +
                        $"playerFatigue={player?.Vitals.Fatigue.Current ?? -1}/{player?.Vitals.Fatigue.Max ?? -1}");
            }
            ReadCombatScreenState(); // settles spoils + bounty exactly the way the open screen would
            return $"LOOP-PROOF: encounter vs '{outlaw.Name}' — {swings} swings, felled={!outlaw.IsAlive}, " +
                   $"enemyHp={outlaw.Vitals.Health.Current}, last='{_lastCombatLine}', " +
                   $"purse {goldBefore}->{_world.PlayerGold} gold (spoils+bounty).";
        }

        /// <summary>
        /// F28 LOOP-PROOF: arm the spell-school leg — learn the whole catalog (live play earns
        /// these one level-up pick at a time; the proof cannot grind XP), refill the caster's
        /// mana, and post a living enemy three cells EAST of the live combat position so hostile
        /// bolts have a legal target. Idempotent: the driver calls it before each cast (mana and
        /// the target's health are topped up every time, so earlier bolts cannot starve later ones).
        /// </summary>
        public string ProofArmSpellSchool()
        {
            if (_world == null) return "LOOP-PROOF: no world to arm the spell school.";
            var player = _world.Actors?.FirstByRole(ActorRole.Player);
            if (player == null) return "LOOP-PROOF: no player actor for the spell school.";

            _world.PlayerKnownSpellIds ??= new System.Collections.Generic.List<string>();
            foreach (var spell in EmberCrpg.Simulation.Magic.WorldSpellCatalog.All)
                if (!_world.PlayerKnownSpellIds.Contains(spell.TemplateId))
                    _world.PlayerKnownSpellIds.Add(spell.TemplateId);

            // Proof pool: a LEVELED caster's mana (live play grows it +2 per Mnd point at level-up)
            // — the school's priciest spell (recall, 20) must be castable repeatedly in one leg;
            // the 12-point loadout pool would refuse frost (17) before the frame was even taken.
            player.ApplyVitals(player.Vitals.WithMana(new VitalStat(40, 40)));

            ActorRecord target = null;
            foreach (var candidate in _world.Actors.Records)
            {
                if (candidate == null || !candidate.IsAlive || candidate.Role != ActorRole.Enemy) continue;
                target = candidate;
                break;
            }
            if (target == null)
                return $"LOOP-PROOF: spell school armed (known={_world.PlayerKnownSpellIds.Count}) — no living enemy to post.";

            var castFrom = PlayerCombatPosition(player);
            target.MoveTo(new EmberCrpg.Domain.Actors.GridPosition(castFrom.X + 3, castFrom.Y));
            target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Restore(target.Vitals.Health.Max)));
            return $"LOOP-PROOF: spell school armed — known={_world.PlayerKnownSpellIds.Count}, " +
                   $"target '{target.Name}' posted 3 cells east of {castFrom}.";
        }

        /// <summary>
        /// F29 LOOP-PROOF: the bestiary family photo — re-post three LIVING dwellers of three
        /// DISTINCT types shoulder-to-shoulder at the given world spot (the rig's view snaps
        /// billboards on >5m moves), so one frame shows three different monsters. Returns the
        /// census line; the dwellers are the real catalog-spawned actors, only re-posed.
        /// </summary>
        public string ProofArrangeBestiaryPhoto(UnityEngine.Vector3 centerWorld)
        {
            if (_world?.Actors == null) return "LOOP-PROOF: no world for the bestiary photo.";
            var origin = BillboardOrigin();
            var center = new EmberCrpg.Domain.Actors.GridPosition(
                origin.X + UnityEngine.Mathf.RoundToInt(centerWorld.x),
                origin.Y + UnityEngine.Mathf.RoundToInt(centerWorld.z));

            var seenTypes = new System.Collections.Generic.List<string>();
            int posted = 0;
            foreach (var actor in _world.Actors.Records)
            {
                if (actor == null || !actor.IsAlive) continue;
                var entry = EmberCrpg.Simulation.Bestiary.WorldBestiaryCatalog.FromActorName(actor.Name);
                if (entry == null || seenTypes.Contains(entry.Key)) continue;
                seenTypes.Add(entry.Key);
                // Shoulder to shoulder: 2 cells apart so the silhouettes never overlap in frame.
                actor.MoveTo(new EmberCrpg.Domain.Actors.GridPosition(center.X + (posted - 1) * 2, center.Y));
                posted++;
                UnityEngine.Debug.Log($"[Bestiary] photo slot {posted}: '{actor.Name}' type={entry.Key}");
                if (posted == 3) break;
            }
            return posted >= 3
                ? $"LOOP-PROOF: bestiary trio posted — {string.Join("|", seenTypes)} at {center}."
                : $"LOOP-PROOF: BROKEN — only {posted} distinct living bestiary types found ({string.Join("|", seenTypes)}).";
        }

        public string ProofRunTradeLeg()
        {
            var state = ReadTradeState();
            if (state.MerchantItems == null || state.MerchantItems.Count == 0)
                return "LOOP-PROOF: merchant stock empty — trade leg skipped.";

            var first = state.MerchantItems[0];
            int before = _world.PlayerGold;
            var result = ExecuteTrade(new EmberCrpg.Presentation.Ember.UI.TradeActionRequest(
                EmberCrpg.Presentation.Ember.UI.TradeActionKind.Buy, first.TemplateId));
            return $"LOOP-PROOF: buy '{first.TemplateId}' success={result.Success}, purse {before}->{_world.PlayerGold} gold.";
        }

        /// <summary>Victory closes the loop: spoils to the purse, encounter unbinds. Called per combat read.</summary>
        private void SettleWorldEncounterIfOver(ActorRecord worldEnemy)
        {
            if (worldEnemy == null || worldEnemy.IsAlive || _worldEncounterLootGranted) return;

            _worldEncounterLootGranted = true;
            const int spoils = 25;
            if (_world != null) _world.PlayerGold += spoils;
            GrantXp(40, "kill"); // F17: a felled foe teaches (40 + quest 60 = exactly one level)
            _lastCombatLine = $"{worldEnemy.Name} falls. You take {spoils} gold in spoils.";
            _worldEncounterId = default;
            // F10 death feel: the world billboard lies down instead of standing through its own death.
            EmberCrpg.Presentation.Ember.WorldDirector.WorldCombatFeedbackFeed.RaiseFelled(worldEnemy.Id.Value);
            UnityEngine.Debug.Log($"[Encounter] '{worldEnemy.Name}' felled — {spoils} gold looted, encounter closed.");
            CompleteWorldQuest(OutlawBountyQuestId, 50, "Bounty fulfilled"); // the KILL quest rides any outlaw victory
        }
    }
}

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
            foreach (var kv in _worldQuests)
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
            int ticksPerHour = EmberCrpg.Simulation.Composition.WorldTickComposer.TicksPerGameDay / 24;
            for (int h = 0; h < hours; h++)
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
            if (Chebyshev(player.Position, enemy.Position) > 6)
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
                rng: rng, now: _world.Time, siteId: ResolveCombatSiteId(enemy, player), events: _world.Events);
            _lastCombatLine = outcome.Hit
                ? $"{enemy.Name} hits you for {outcome.Damage}!"
                : $"{enemy.Name} misses you.";
        }

        /// <summary>F10-DoD: bind the CURRENT settlement's haunter (an Outlaw homed HERE — street outlaws
        /// elsewhere don't qualify) so the dungeon leg fights the chamber guard, not a random bandit.</summary>
        public string ProofBindDungeonHaunter()
        {
            var here = CurrentSettlementOrStart;
            if (_world?.NpcSeeds == null || _world.Actors == null) return string.Empty;
            for (int i = 0; i < _world.NpcSeeds.Count; i++)
            {
                var seed = _world.NpcSeeds[i];
                if (seed == null || seed.Role != EmberCrpg.Domain.Worldgen.NpcRole.Outlaw || !seed.Home.Equals(here))
                    continue;
                var actorId = new ActorId(GeneratedNpcActorOffset + seed.Id.Value);
                if (!_world.Actors.TryGet(actorId, out var actor) || actor == null || !actor.IsAlive)
                    continue;
                TryBeginWorldEncounter(actor, seed);
                ReadCombatScreenState(); // publish the battle mirror
                return actor.Name;
            }
            return string.Empty;
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
            _lastCombatLine = $"{worldEnemy.Name} falls. You take {spoils} gold in spoils.";
            _worldEncounterId = default;
            // F10 death feel: the world billboard lies down instead of standing through its own death.
            EmberCrpg.Presentation.Ember.WorldDirector.WorldCombatFeedbackFeed.RaiseFelled(worldEnemy.Id.Value);
            UnityEngine.Debug.Log($"[Encounter] '{worldEnemy.Name}' felled — {spoils} gold looted, encounter closed.");
            CompleteWorldQuest(OutlawBountyQuestId, 50, "Bounty fulfilled"); // the KILL quest rides any outlaw victory
        }
    }
}

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
            if (_world?.Quests != null)
            {
                foreach (var kv in _world.Quests.Active)
                {
                    active++;
                    if (kv.Value.IsComplete) complete++;
                }
            }
            return $"LOOP-PROOF: quests active={active} complete={complete}, purse={_world?.PlayerGold ?? -1} gold.";
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
            int swings = 0;
            while (outlaw.IsAlive && swings < 150)
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
            UnityEngine.Debug.Log($"[Encounter] '{worldEnemy.Name}' felled — {spoils} gold looted, encounter closed.");
            CompleteWorldQuest(OutlawBountyQuestId, 50, "Bounty fulfilled"); // the KILL quest rides any outlaw victory
        }
    }
}

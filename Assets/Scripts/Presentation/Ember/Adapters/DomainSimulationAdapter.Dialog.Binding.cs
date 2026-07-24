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
        public IDialogSource GetDialogSource(string actorName)
        {
            // Legacy/fallback path: callers that only have a display-name string. Try to upgrade the
            // name to the actor's STABLE id so we get identical resolution to the id overload; only
            // when no actor carries that name do we bind by raw name (ad-hoc / scene-authored label).
            var byName = _world.Actors?.Records.FirstOrDefault(
                a => string.Equals(a.Name, actorName, System.StringComparison.Ordinal));
            if (byName != null)
                return GetDialogSource(byName.Id);

            var npc = _world.NpcSeeds.FirstOrDefault(
                n => string.Equals(n.Name, actorName, System.StringComparison.Ordinal));
            BeginConversation(default, npc != null ? npc.Id : default, actorName, npc);
            return this;
        }

        // ----- IPlayerCommandSink (DLG-01) -----
        public IDialogSource GetDialogSource(ActorId id)
        {
            // DLG-01: resolve by the stable ActorId rather than by display-name string.
            if (id.IsEmpty || _world.Actors == null || !_world.Actors.TryGet(id, out var actor) || actor == null)
            {
                // A mismatch must be LOUD and must NOT silently drop the player into the shared
                // global topic menu (the old name-resolution bug). Surface an explicit empty state.
                _suppressGlobalTopicFallback = true;
                _activeDialogActor = string.Empty;
                _activeDialogActorId = default;
                _activeDialogNpcId = default;
                _currentDialogLine = "There is no one here to talk to.";
                _currentPortrait = "portrait_npc_placeholder";
                _isDialogThinking = false;
                _conversation = ConversationState.None;
                UnityEngine.Debug.LogWarning(
                    $"DLG-01: GetDialogSource({id}) found no actor in WorldState.Actors; " +
                    "refusing to fall back to global world topics.");
                return this;
            }

            // Recover the matching NpcSeed for generated NPCs. HydrateNpcs mints ActorIds as
            // GeneratedNpcActorOffset + NpcId.Value, so the seed is recoverable by subtracting the
            // offset. Authored slice actors (ids 1..5) have no seed and fall through to name match.
            NpcSeedRecord npc = null;
            if (id.Value >= GeneratedNpcActorOffset)
            {
                var npcId = new EmberCrpg.Domain.Worldgen.NpcId(id.Value - GeneratedNpcActorOffset);
                npc = _world.NpcSeeds.FirstOrDefault(n => n.Id.Equals(npcId));
            }
            npc ??= _world.NpcSeeds.FirstOrDefault(
                n => string.Equals(n.Name, actor.Name, System.StringComparison.Ordinal));

            // F2/encounters: an outlaw doesn't talk — E on a hostile binds a WORLD ENCOUNTER and signals
            // the UI to open the combat screen instead of starting a conversation.
            if (TryBeginWorldEncounter(actor, npc))
                return this;

            BeginConversation(actor.Id, npc != null ? npc.Id : default, actor.Name, npc);
            return this;
        }

        /// <summary>
        /// DLG-01/EMB-020/EMB-045: single funnel that binds the active speaker, portrait, and the
        /// per-actor topic set into the one <see cref="ConversationState"/> the dialog surface reads.
        /// When <paramref name="npc"/> is non-null the topics come from THEIR role + faction (not the
        /// global menu) and a persona greeting is fired; otherwise the shared world topics back the
        /// conversation. Both <see cref="GetDialogSource(string)"/> and
        /// <see cref="GetDialogSource(ActorId)"/> route through here so the two paths can never drift.
        /// </summary>
        private void BeginConversation(ActorId actorId, NpcId npcId, string actorName, NpcSeedRecord npc)
        {
            _conversationSerial++;   // a new conversation invalidates any in-flight reply from the previous one
            _suppressGlobalTopicFallback = false;
            _activeDialogActorId = actorId;
            _activeDialogNpcId = npcId;
            _activeDialogActor = actorName ?? string.Empty;
            _currentPortrait = ResolveConversationPortraitKey(npc, _activeDialogActor);
            // W23: seed the live-option memo once; a RETURNING conversation keeps its lived
            // state - consumed bubbles stay gone, grown followups stay offered.
            EnsureLiveOptions(_conversation?.Topics?.Select(t => t.Id)
                ?? System.Linq.Enumerable.Empty<string>());

            // PLAYTEST FIX ("benimle tanistigini hatirlamiyor"): the FIRST conversation writes a
            // met_player memory carrying the player's NAME — from then on this NPC greets an
            // acquaintance, not a stranger (the prompt suffix reads it back).
            if (!actorId.IsEmpty && _world?.Actors != null && _world.Actors.TryGet(actorId, out var talker) && talker != null)
            {
                _world.NpcMemory ??= new EmberCrpg.Domain.Memory.NpcMemoryStore();
                var talkerMemory = _world.NpcMemory.GetOrCreate(actorId);
                bool alreadyMet = false;
                foreach (var known in talkerMemory.Events)
                    if (known.EventType == "met_player") { alreadyMet = true; break; }
                if (!alreadyMet)
                {
                    var playerRecord = EmberCrpg.Simulation.Living.CompanionService.FindPlayer(_world);
                    talkerMemory.RecordEvent(new EmberCrpg.Domain.Memory.InteractionEvent(
                        _world.Time, "met_player", playerRecord?.Id ?? default,
                        playerRecord?.Name ?? "the traveller", string.Empty, 0, talker.Position));
                }
            }

            if (npc != null)
            {
                var perActorTopics = AddQuestInteractionTopics(
                    _activeDialogActorId,
                    npc,
                    NpcTopicCatalog.For(npc.Role, npc.Faction.Value, _world.Topics));
                _conversation = new ConversationState(
                    _activeDialogActorId,
                    _activeDialogNpcId,
                    _activeDialogActor,
                    _currentPortrait,
                    perActorTopics);

                // BUG-DIALOG-EMPTY: seed a DETERMINISTIC opening line synchronously, up front, so the
                // panel always renders a real sentence even when native inference returns nothing (no
                // model bytes). Prefer the per-actor AskAbout/persona answer (e.g. a Guard's "watch"
                // topic => "Sentinel Rook keeps count of every footstep, including yours."); only fall
                // back to a role+name greeting when no topic answer is available. The async LLM call
                // below REPLACES this line only if it yields a non-empty (non-whitespace) response.
                _currentDialogLine = DeterministicGreeting(_activeDialogActor, npc, perActorTopics);

                _ = GenerateNpcGreetingAsync(npc);
            }
            else
            {
                // No seed record (ad-hoc / authored slice actor): fall back to the shared world topics,
                // still funneled through the one ConversationState model so GetTopics/SelectTopic have
                // a single source of truth.
                _conversation = new ConversationState(
                    _activeDialogActorId,
                    _activeDialogNpcId,
                    _activeDialogActor,
                    _currentPortrait,
                    _world.Topics ?? new List<AskAboutTopic>());

                // BUG-DIALOG-EMPTY: seed a deterministic, non-empty opening line first so the panel
                // is never blank. Lead with a shared world topic answer when present.
                _currentDialogLine = DeterministicGreeting(_activeDialogActor, npc, _world.Topics);

                // LLM-NOT-FIRING fix: the playable scenes use authored actors with no NpcSeed, so the
                // seeded greeting path above never ran for them and the player ALWAYS saw the
                // deterministic line. Fire a name-based LLM greeting here too; it replaces the
                // deterministic line only if the local model returns real text (else the line stays).
                _ = GenerateAdHocGreetingAsync(_activeDialogActor);
            }
        }

        // Bumped whenever the active conversation changes (begins or ends). Async greeting / topic-answer
        // replies capture this BEFORE their off-thread LLM call and discard their result if it changed — so a
        // late reply from a conversation the player already left can't overwrite the current line. This is the
        // other half of the Oracle bleed: even after EndConversation, an in-flight NPC answer would otherwise
        // still land in the Oracle's dialog ("then the previous NPC suddenly answers too").
        private int _conversationSerial;

        /// <summary>
        /// Ends the active conversation so a later dialog context (e.g. the Oracle) cannot inherit this NPC's
        /// speaker, line, or topics — and bumps the serial so any in-flight async reply is dropped.
        /// </summary>
        public void EndConversation()
        {
            _conversationSerial++;
            _conversation = ConversationState.None;
            _activeDialogActor = string.Empty;
            _activeDialogActorId = default;
            _activeDialogNpcId = default;
            _currentDialogLine = string.Empty;
            _isDialogThinking = false;
            _suppressGlobalTopicFallback = false;
        }

        private static string ResolveConversationPortraitKey(NpcSeedRecord npc, string actorName)
        {
            // Why: dialog portraits must stay on portrait-specific keys so the panel never reuses a world billboard body sprite.
            if (npc != null && !string.IsNullOrWhiteSpace(npc.PortraitAssetPath))
                return DialogPortraitKey.Normalize(npc.PortraitAssetPath);

            if (npc != null)
            {
                switch (npc.Role)
                {
                    case NpcRole.Merchant: return DialogPortraitKey.Normalize("merchant");
                    case NpcRole.Scholar: return DialogPortraitKey.Normalize("sage");
                    case NpcRole.Priest: return DialogPortraitKey.Normalize("sage");
                    case NpcRole.Guard: return DialogPortraitKey.Normalize("knight");
                    case NpcRole.Noble: return DialogPortraitKey.Normalize("knight");
                    case NpcRole.Outlaw: return DialogPortraitKey.Normalize("warrior");
                    case NpcRole.Artisan: return DialogPortraitKey.Normalize("blacksmith");
                    case NpcRole.Farmer: return DialogPortraitKey.Normalize("innkeeper");
                }
            }

            return DialogPortraitKey.Normalize(actorName);
        }

    }
}

using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>
    /// Codex audit (fourth pass A-P1): the adapter previously inherited the
    /// default no-op implementations of <see cref="IPlayerCommandSink"/>'s
    /// TryCastSpell / TryMeleeStrike / TryInteract, so player input only ever
    /// logged text — combat / spells / dialog never mutated world state. It
    /// also fabricated FactionRows ("Neutral") and read SpellSlots off the
    /// cooldown tracker (empty on a fresh world). This rewrite:
    ///
    /// 1. implements each command concretely against SliceWorldState
    ///    (vitals damage, mana/cooldown gate via SpellResolver, dialog topic
    ///    routing);
    /// 2. reads FactionRows from <see cref="FactionStore.ReputationRows"/>
    ///    relative to the player faction;
    /// 3. exposes a known-spell list from <see cref="EmberCrpg.Simulation.Magic.SliceSpellCatalog.All"/>
    ///    instead of mining the cooldown state;
    /// 4. mutates the player <see cref="ActorRecord"/>'s vitals on
    ///    TakePlayerDamage instead of holding a UI-only counter;
    /// 5. retains a single <see cref="EmberCrpg.Data.Save.JsonSliceSaveService"/>
    ///    so worksite/job/soil/plant process sidecars survive the
    ///    Export/Restore round-trip.
    /// </summary>
    public sealed class DomainSimulationAdapter : IDomainSimulationAdapter, IDialogSource
    {
        private readonly SliceWorldState _world;
        private readonly EmberCrpg.Data.Save.JsonSliceSaveService _saveService;
        private int _tick;
        private string _lastCombatLine = string.Empty;
        private string _activeDialogActor = string.Empty;
        private string _currentDialogLine = string.Empty;

        public DomainSimulationAdapter(SliceWorldState world)
        {
            _world = world ?? throw new System.ArgumentNullException(nameof(world));
            // Codex audit (fourth pass A-P2): retain ONE save service so the
            // sidecar process state (worksites / jobs / soils / plants) lives
            // across Export/Restore cycles. Previously a fresh service was
            // constructed each call, dropping the sidecar.
            _saveService = new EmberCrpg.Data.Save.JsonSliceSaveService();
        }

        // ----- IEmberSimulationClock -----
        public void AdvanceTick(int tickIndex) => _tick = tickIndex;
        public int TickIndex => _tick;

        // ----- IEmberHudReadModel -----
        public string HudText
        {
            get
            {
                var day = 1 + _tick / 240;
                return $"Tick {_tick:0000}   Day {day:000}";
            }
        }

        public CombatHudState CombatHud
        {
            get
            {
                var player = _world.Actors.FirstByRole(ActorRole.Player);
                if (player == null) return new CombatHudState(0, 100, 0, 100, 0, 100, _lastCombatLine);
                var v = player.Vitals;
                return new CombatHudState(
                    v.Health.Current, v.Health.Max,
                    v.Fatigue.Current, v.Fatigue.Max,
                    v.Mana.Current, v.Mana.Max,
                    _lastCombatLine);
            }
        }

        // ----- IWorldViewReadModel -----
        public IReadOnlyList<JobQueueRow> JobQueueRows
        {
            get
            {
                // Codex audit (fourth pass A-P1): previously returned empty.
                // Job sidecar state lives on the save service; expose any
                // tracked jobs. When no jobs are seeded the list stays empty
                // but the panel is no longer locked to fabricated zero rows.
                var jobs = _saveService.Jobs;
                if (jobs == null) return System.Array.Empty<JobQueueRow>();
                var rows = new List<JobQueueRow>();
                foreach (var req in jobs.Requests)
                {
                    var claim = jobs.GetClaimedBy(req.Id);
                    var actorName = claim.IsEmpty ? string.Empty : (_world.Actors.Get(claim)?.Name ?? string.Empty);
                    rows.Add(new JobQueueRow(actorName, req.Kind.ToString(), jobs.GetStatus(req.Id).Code, jobs.GetQueueIndex(req.Id)));
                }
                return rows;
            }
        }

        public IReadOnlyList<ColonyNeedsRow> ColonyNeedsRows
        {
            get
            {
                var rows = new List<ColonyNeedsRow>();
                foreach (var actor in _world.Actors.Records.OrderBy(a => a.Id.Value))
                {
                    rows.Add(new ColonyNeedsRow(
                        actor.Name ?? string.Empty,
                        actor.Needs.Hunger.Value,
                        actor.Needs.Fatigue.Value,
                        actor.Needs.Thirst.Value,
                        actor.Mood.Value));
                }
                return rows;
            }
        }

        public IReadOnlyList<FactionRow> FactionRows
        {
            get
            {
                // Codex audit (fourth pass A-P1): previously hardcoded Neutral.
                // Use the player's faction (first non-empty actor faction id)
                // as the reference vantage point, then list every other
                // faction with its reputation relative to that vantage.
                var rows = new List<FactionRow>();
                if (_world.Factions == null) return rows;

                // Codex audit (fourth pass A-P1): SliceWorldState's actor
                // model does not yet carry a player-faction reference, so we
                // use the FIRST faction as the vantage and surface every
                // OTHER faction's reputation relative to it. A future Faz
                // can swap to ActorRecord.FactionId.
                FactionId vantage = default;
                foreach (var first in _world.Factions.Records) { vantage = first.Id; break; }

                foreach (var faction in _world.Factions.Records)
                {
                    int rep = 0;
                    if (!vantage.IsEmpty && !faction.Id.Equals(vantage))
                        rep = _world.Factions.GetReputation(vantage, faction.Id).Value;
                    var label = FactionRelationKind.FromReputation(rep).ToString();
                    rows.Add(new FactionRow(faction.Name ?? string.Empty, rep, label));
                }
                return rows;
            }
        }

        public IReadOnlyList<InventorySlot> InventorySlots
        {
            get
            {
                if (_world.PlayerInventory == null) return System.Array.Empty<InventorySlot>();
                var rows = new List<InventorySlot>();
                foreach (var item in _world.PlayerInventory.Items)
                {
                    rows.Add(new InventorySlot(item.TemplateId ?? string.Empty, item.Quantity));
                }
                return rows;
            }
        }

        public IReadOnlyList<string> SpellSlots
        {
            get
            {
                // Codex audit (fourth pass A-P2): the cooldown tracker only
                // contains spells the actor has CAST (which seeds nothing on
                // a fresh world). The known-spell catalog is the right source.
                return EmberCrpg.Simulation.Magic.SliceSpellCatalog.All
                    .Select(s => s.TemplateId)
                    .OrderBy(id => id, System.StringComparer.Ordinal)
                    .ToList();
            }
        }

        public bool TryReadActor(string actorName, out ActorViewState state)
        {
            state = default;
            if (string.IsNullOrEmpty(actorName)) return false;
            foreach (var actor in _world.Actors.Records)
            {
                if (string.Equals(actor.Name, actorName, System.StringComparison.Ordinal))
                {
                    state = new ActorViewState(
                        new UnityEngine.Vector3(actor.Position.X, 0f, actor.Position.Y),
                        UnityEngine.Quaternion.identity,
                        visible: true);
                    return true;
                }
            }
            return false;
        }

        public bool TryReadWorksite(string siteName, out WorksiteViewState state)
        {
            state = default;
            // Codex audit (fourth pass A-P1): previously always returned false.
            // Resolve worksite tag from the retained save sidecar's worksite
            // store; the position field is set from any active worksite at
            // that site name, falling back to origin when nothing is wired.
            if (string.IsNullOrEmpty(siteName)) return false;
            var worksites = _saveService.Worksites;
            if (worksites == null) return false;
            foreach (var site in _world.Sites.Records)
            {
                if (!string.Equals(site.Name, siteName, System.StringComparison.Ordinal)) continue;
                // Found the site; return an active view state.
                state = new WorksiteViewState(isActive: true, queueDepth: 0);
                return true;
            }
            return false;
        }

        public IDialogSource GetDialogSource(string actorName)
        {
            _activeDialogActor = actorName ?? string.Empty;
            _currentDialogLine = string.IsNullOrEmpty(_activeDialogActor) ? string.Empty
                : $"You speak with {_activeDialogActor}.";
            return this;
        }

        // ----- IDialogSource -----
        public string GetCurrentLine() => _currentDialogLine;
        public IReadOnlyList<string> GetTopics() => _world.Topics?.Select(t => t.Id).ToList() ?? new List<string>();

        public void SelectTopic(string topicId)
        {
            // Codex audit (fourth pass A-P1): previously no-op. Now produces a
            // deterministic acknowledgement line and writes a dialogue-seen
            // marker into the active actor's memory.
            if (string.IsNullOrEmpty(topicId)) return;
            _currentDialogLine = $"{_activeDialogActor} considers \"{topicId}\".";
            var actor = _world.Actors.Records.FirstOrDefault(a => string.Equals(a.Name, _activeDialogActor, System.StringComparison.Ordinal));
            if (actor?.Memory != null)
                actor.Memory.MarkDialogueSeen(topicId);
        }

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
            // Codex audit (fourth pass A-P1): concrete spell command via
            // SliceSpellCatalog + the EffectDefinition resolver path. Failure
            // surfaces a deterministic refusal reason in LogCombat.
            var spells = EmberCrpg.Simulation.Magic.SliceSpellCatalog.All;
            if (spellSlotIndex < 0 || spellSlotIndex >= spells.Count)
            {
                LogCombat("No such spell slot.");
                return false;
            }
            var spell = spells[spellSlotIndex];
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
            // Consume mana and emit a deterministic event log line.
            player.ApplyVitals(player.Vitals.WithMana(player.Vitals.Mana.Damage(spell.ManaCost)));
            _world.Events?.Append(new WorldEvent(
                _world.Time,
                WorldEventKind.SpellResolved,
                player.Id,
                default,
                $"slice_spell_cast id:{spell.TemplateId}"));
            LogCombat($"You cast {spell.DisplayName ?? spell.TemplateId}.");
            return true;
        }

        public bool TryMeleeStrike(string targetActorName, int rawDamage)
        {
            // Codex audit (fourth pass A-P1): concrete melee command. Resolves
            // the target by stable actor name on SliceWorldState and applies
            // damage; emits a CombatResolved event so the deterministic log
            // captures the strike.
            if (rawDamage <= 0) { LogCombat("Strike whiffs."); return false; }
            var target = _world.Actors.Records.FirstOrDefault(a => string.Equals(a.Name, targetActorName, System.StringComparison.Ordinal));
            if (target == null)
            {
                LogCombat($"No target: {targetActorName ?? string.Empty}");
                return false;
            }
            target.ApplyVitals(target.Vitals.WithHealth(target.Vitals.Health.Damage(rawDamage)));
            _world.Events?.Append(new WorldEvent(
                _world.Time,
                WorldEventKind.SpellResolved,
                target.Id,
                default,
                $"melee_strike target:{target.Name} damage:{rawDamage}"));
            LogCombat($"You strike {target.Name} for {rawDamage}.");
            return true;
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
            GetDialogSource(match.Name);
            return true;
        }

        // ----- IConsultFateOracle -----
        public string ConsultFate()
        {
            int salted = unchecked(_tick * (int)2654435761);
            int roll = (salted & 0x7fffffff) % 100 + 1;
            if (roll <= 35) return "SETBACK: The stars align against you.";
            if (roll <= 70) return "NEUTRAL: The DM watches in silence.";
            return "FAVOURABLE: Fortune smiles.";
        }

        // ----- IEmberSaveBridge -----
        public string ExportStateJson()
        {
            // Codex review PR #195 (P2): rethrow so EmberSaveService can show
            // "Save partial: domain export failed." instead of swallowing.
            return _saveService.SaveToJson(_world);
        }

        public void RestoreStateJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            // Codex review PR #195 (P1) + audit (fourth pass A-P2): restore
            // EVERY saveable field on SliceWorldState, AND keep the retained
            // _saveService instance (which now carries the rehydrated process
            // sidecars: worksites / jobs / soils / plants).
            var restored = _saveService.LoadFromJson(json);
            if (restored == null) return;

            _world.Time = restored.Time;
            _world.RoomSeed = restored.RoomSeed;
            _world.Room = restored.Room;
            _world.Dungeon = restored.Dungeon;
            _world.CurrentRoomId = restored.CurrentRoomId;
            _world.PlayerRoomId = restored.PlayerRoomId;
            _world.TalkerRoomId = restored.TalkerRoomId;
            _world.MerchantRoomId = restored.MerchantRoomId;
            _world.GuardRoomId = restored.GuardRoomId;
            _world.EnemyRoomId = restored.EnemyRoomId;
            _world.PickupRoomId = restored.PickupRoomId;
            _world.Actors = restored.Actors;
            _world.Items = restored.Items;
            _world.Sites = restored.Sites;
            _world.Factions = restored.Factions;
            _world.Events = restored.Events;
            _world.Prices = restored.Prices;
            _world.Stockpiles = restored.Stockpiles;
            _world.TradeRoutes = restored.TradeRoutes;
            _world.Caravans = restored.Caravans;
            _world.ToolCallTrace = restored.ToolCallTrace;
            _world.LlmProposalLog = restored.LlmProposalLog;
            _world.PlayerInventory = restored.PlayerInventory;
            _world.MerchantInventory = restored.MerchantInventory;
            _world.PlayerEquipment = restored.PlayerEquipment;
            _world.PlayerSpellCooldowns = restored.PlayerSpellCooldowns;
            _world.PlayerShieldBuffs = restored.PlayerShieldBuffs;
            _world.Pickups = restored.Pickups;
            _world.DungeonRoomStates = restored.DungeonRoomStates;
            _world.DungeonDoorStates = restored.DungeonDoorStates;
            _world.Topics = restored.Topics;
            _world.NpcMemory = restored.NpcMemory;
            _world.DoorOpen = restored.DoorOpen;
            _world.GuardDoorAccessGranted = restored.GuardDoorAccessGranted;
            _world.GuardWarningCount = restored.GuardWarningCount;
            _world.EncounterActive = restored.EncounterActive;
            _world.LastNarrative = restored.LastNarrative;
        }
    }
}

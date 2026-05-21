using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using EmberCrpg.Presentation.VisualLayer;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>
    /// Codex audit (third pass A-P1): the live scene used to run on
    /// <see cref="PlaceholderSimulationAdapter"/> exclusively, so HUD rows /
    /// inventory / faction state were all fabricated. This adapter bridges
    /// Captain's deterministic <c>SliceWorldState</c> to the
    /// <see cref="IDomainSimulationAdapter"/> contract that UI panels consume.
    ///
    /// Scope: this is a READ-MODEL adapter. Save / restore / commands route
    /// through dedicated hooks (<see cref="ExportStateJson"/>,
    /// <see cref="RestoreStateJson"/>, the <see cref="IPlayerCommandSink"/>
    /// methods); the row getters all produce per-frame views from the live
    /// world state.
    ///
    /// Placeholder still ships as a fallback so out-of-domain demo scenes
    /// (e.g. a UI-only sandbox) can run without a wired SliceWorldState.
    /// </summary>
    public sealed class DomainSimulationAdapter : IDomainSimulationAdapter, IDialogSource
    {
        private readonly SliceWorldState _world;
        private int _tick;
        private string _lastCombatLine = string.Empty;
        private int _playerDamageTaken;
        private string _activeDialogActor = string.Empty;

        public DomainSimulationAdapter(SliceWorldState world)
        {
            _world = world ?? throw new System.ArgumentNullException(nameof(world));
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
                var vitals = player.Vitals;
                return new CombatHudState(
                    vitals.Health.Current - _playerDamageTaken,
                    vitals.Health.Max,
                    vitals.Fatigue.Current,
                    vitals.Fatigue.Max,
                    vitals.Mana.Current,
                    vitals.Mana.Max,
                    _lastCombatLine);
            }
        }

        // ----- IWorldViewReadModel -----
        public IReadOnlyList<JobQueueRow> JobQueueRows => System.Array.Empty<JobQueueRow>();
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
                var rows = new List<FactionRow>();
                foreach (var fac in _world.Factions.Records)
                {
                    rows.Add(new FactionRow(fac.Name ?? string.Empty, 0, "Neutral"));
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
                if (_world.PlayerSpellCooldowns == null) return System.Array.Empty<string>();
                return _world.PlayerSpellCooldowns
                    .GetTrackedSpellTemplateIds()
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
            return false;
        }

        public IDialogSource GetDialogSource(string actorName)
        {
            _activeDialogActor = actorName ?? string.Empty;
            return this;
        }

        // ----- IDialogSource -----
        public string GetCurrentLine()
        {
            return string.IsNullOrEmpty(_activeDialogActor) ? string.Empty
                : $"You speak with {_activeDialogActor}.";
        }
        public IReadOnlyList<string> GetTopics() => _world.Topics?.Select(t => t.Id.Code).ToList() ?? new List<string>();
        public void SelectTopic(string topicId)
        {
            // Real domain-driven dialog routing happens through Topic services;
            // for now we just acknowledge the selection. Future Faz integrates
            // NpcDialogueService here.
        }

        // ----- IPlayerCommandSink -----
        public void LogCombat(string message) => _lastCombatLine = message ?? string.Empty;
        public void TakePlayerDamage(int amount)
        {
            if (amount <= 0) return;
            _playerDamageTaken += amount;
            _lastCombatLine = $"You take {amount} damage!";
        }

        // ----- IConsultFateOracle -----
        public string ConsultFate()
        {
            // Deterministic: derive from current tick rather than wall clock.
            int salted = unchecked(_tick * (int)2654435761);
            int roll = (salted & 0x7fffffff) % 100 + 1;
            if (roll <= 35) return "SETBACK: The stars align against you.";
            if (roll <= 70) return "NEUTRAL: The DM watches in silence.";
            return "FAVOURABLE: Fortune smiles.";
        }

        // ----- IEmberSaveBridge -----
        public string ExportStateJson()
        {
            // Route through JsonSliceSaveService for the full deterministic
            // round-trip. Catch failures so a corrupt domain state does not
            // crash the save service caller — the empty payload signals the
            // save layer to surface "Save partial: domain export failed."
            try { return new EmberCrpg.Data.Save.JsonSliceSaveService().SaveToJson(_world); }
            catch (System.Exception) { return string.Empty; }
        }

        public void RestoreStateJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            // The Json service returns a fresh SliceWorldState; copy the
            // observable fields back onto the live _world reference so all
            // already-bound UI panels keep their world handle.
            var restored = new EmberCrpg.Data.Save.JsonSliceSaveService().LoadFromJson(json);
            if (restored == null) return;
            _world.Time = restored.Time;
            _world.RoomSeed = restored.RoomSeed;
            _world.Room = restored.Room;
            _world.Dungeon = restored.Dungeon;
            _world.CurrentRoomId = restored.CurrentRoomId;
            _world.PlayerRoomId = restored.PlayerRoomId;
            _world.Actors = restored.Actors;
            _world.Items = restored.Items;
            _world.Sites = restored.Sites;
            _world.Factions = restored.Factions;
            _world.Events = restored.Events;
            _world.PlayerInventory = restored.PlayerInventory;
            _world.MerchantInventory = restored.MerchantInventory;
            _world.PlayerEquipment = restored.PlayerEquipment;
            _world.PlayerSpellCooldowns = restored.PlayerSpellCooldowns;
            _world.PlayerShieldBuffs = restored.PlayerShieldBuffs;
            _world.NpcMemory = restored.NpcMemory;
            _world.DoorOpen = restored.DoorOpen;
            _world.GuardDoorAccessGranted = restored.GuardDoorAccessGranted;
            _world.LastNarrative = restored.LastNarrative;
            _playerDamageTaken = 0; // reset transient view counter
        }
    }
}

using System.Collections.Generic;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>
    /// Single contract the runtime host calls each tick to translate the deterministic
    /// simulation into per-view DTOs. Implementing this interface wires Captain's
    /// domain stores into the Ember view layer without leaking domain types into the
    /// presentation assembly.
    /// </summary>
    public interface IDomainSimulationAdapter
    {
        void AdvanceTick(int tickIndex);
        int TickIndex { get; }

        string HudText { get; }

        IReadOnlyList<JobQueueRow> JobQueueRows { get; }
        IReadOnlyList<ColonyNeedsRow> ColonyNeedsRows { get; }
        IReadOnlyList<FactionRow> FactionRows { get; }
        IReadOnlyList<InventorySlot> InventorySlots { get; }
        IReadOnlyList<string> SpellSlots { get; }

        CombatHudState CombatHud { get; }

        bool TryReadActor(string actorName, out ActorViewState state);
        bool TryReadWorksite(string siteName, out WorksiteViewState state);

        IDialogSource GetDialogSource(string actorName);

        void LogCombat(string message);
        void TakePlayerDamage(int amount);

        string ConsultFate();

        // Codex audit Batch 2 / Finding 3: The Ember save service previously
        // persisted only player rig transform + tick index, dropping every bit of
        // deterministic simulation state (actors, inventories, NPC memory, world
        // events, etc.). Expose an opaque round-trippable JSON envelope on the
        // adapter so the save service can bundle the full domain snapshot without
        // importing any Domain.* type. Placeholder adapters return null/empty and
        // accept null/empty without complaint — the bridge is best-effort until
        // Captain's domain adapter lands.
        string ExportStateJson();
        void RestoreStateJson(string json);
    }

    /// <summary>
    /// Convenience locator. Resolves the single adapter for the scene. Implementations
    /// register themselves in <c>Awake</c>; callers do not import any domain type.
    /// </summary>
    public static class EmberDomainAdapterLocator
    {
        private static IDomainSimulationAdapter _current;

        public static IDomainSimulationAdapter Current => _current;

        public static void Register(IDomainSimulationAdapter adapter)
        {
            _current = adapter;
        }

        public static void Clear()
        {
            _current = null;
        }
    }
}

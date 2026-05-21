using System.Collections.Generic;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    // Codex audit Batch 6 / C-P2: the single fat adapter interface mixed clock,
    // read-model rows, command sink, fate oracle, and save round-trip in one
    // surface — callers had no way to take a dependency on just the slice they
    // needed. Split into five role interfaces; the legacy
    // IDomainSimulationAdapter aggregates them so existing implementations
    // (PlaceholderSimulationAdapter, EmberWorldHost.EmptySimulationAdapter)
    // and existing call sites stay compile-compatible.

    /// <summary>Tick clock advancement and read-back.</summary>
    public interface IEmberSimulationClock
    {
        void AdvanceTick(int tickIndex);
        int TickIndex { get; }
    }

    /// <summary>HUD strings + combat HUD rows the on-screen overlay consumes.</summary>
    public interface IEmberHudReadModel
    {
        string HudText { get; }
        CombatHudState CombatHud { get; }
    }

    /// <summary>Per-view DTO rows: jobs, needs, factions, inventory, spells, actor/worksite spot lookups.</summary>
    public interface IWorldViewReadModel
    {
        IReadOnlyList<JobQueueRow> JobQueueRows { get; }
        IReadOnlyList<ColonyNeedsRow> ColonyNeedsRows { get; }
        IReadOnlyList<FactionRow> FactionRows { get; }
        IReadOnlyList<InventorySlot> InventorySlots { get; }
        IReadOnlyList<string> SpellSlots { get; }

        bool TryReadActor(string actorName, out ActorViewState state);
        bool TryReadWorksite(string siteName, out WorksiteViewState state);

        IDialogSource GetDialogSource(string actorName);
    }

    /// <summary>Player-driven write surface: log combat, take damage, future input commands.</summary>
    public interface IPlayerCommandSink
    {
        void LogCombat(string message);
        void TakePlayerDamage(int amount);
    }

    /// <summary>Narrator-flavour oracle for the consult-fate UI ribbon.</summary>
    public interface IConsultFateOracle
    {
        string ConsultFate();
    }

    /// <summary>Save / load round-trip envelope for the full deterministic snapshot.</summary>
    public interface IEmberSaveBridge
    {
        // Codex audit Batch 2 / Finding 3: The Ember save service previously
        // persisted only player rig transform + tick index, dropping every bit of
        // deterministic simulation state (actors, inventories, NPC memory, world
        // events, etc.). Expose an opaque round-trippable JSON envelope so the
        // save service can bundle the full domain snapshot without importing
        // any Domain.* type. Placeholder adapters return null/empty and accept
        // null/empty without complaint — best-effort until Captain's domain
        // adapter lands.
        string ExportStateJson();
        void RestoreStateJson(string json);
    }

    /// <summary>
    /// Aggregate adapter the runtime host calls each tick to translate the
    /// deterministic simulation into per-view DTOs. Implementing this interface
    /// wires Captain's domain stores into the Ember view layer without leaking
    /// domain types into the presentation assembly. New callers should depend
    /// on the narrower role interfaces above; this aggregate exists for the
    /// existing one-implementation-runs-everything pattern.
    /// </summary>
    public interface IDomainSimulationAdapter :
        IEmberSimulationClock,
        IEmberHudReadModel,
        IWorldViewReadModel,
        IPlayerCommandSink,
        IConsultFateOracle,
        IEmberSaveBridge
    {
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

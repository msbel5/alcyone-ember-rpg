// Why this file is intentionally long: it documents the temporary aggregate adapter contract while role interfaces are extracted.
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
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
    // (PlaceholderSimulationAdapter)
    // and existing call sites stay compile-compatible.

    /// <summary>
    /// Write side of the simulation clock: host calls <see cref="AdvanceTick"/> each frame.
    /// Codex audit (sixth pass C-P2 #C1): split out from <see cref="IEmberSimulationClock"/>
    /// so HUD/telemetry/test consumers that only read the tick index can take
    /// <see cref="IEmberClockSource"/> instead of the aggregate.
    /// </summary>
    public interface IEmberClockSink
    {
        void AdvanceTick(int tickIndex);
    }

    /// <summary>Read side of the simulation clock: query current tick.</summary>
    public interface IEmberClockSource
    {
        int TickIndex { get; }
    }

    /// <summary>Tick clock advancement and read-back. Composes sink + source.</summary>
    public interface IEmberSimulationClock : IEmberClockSink, IEmberClockSource
    {
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

        /// <summary>The generated overland map for the world-map panel (M key), or null if no world is seeded.</summary>
        EmberCrpg.Domain.Overland.OverlandMap Overland { get; }

        /// <summary>The player's current overland region tile (their starting settlement's tile, or map centre).</summary>
        EmberCrpg.Domain.Actors.GridPosition PlayerOverlandTile { get; }

        /// <summary>Display name of the settlement the player started in (for the HUD + map), or null if unknown.</summary>
        string StartingSettlementName { get; }
        System.Collections.Generic.IReadOnlyList<EmberCrpg.Presentation.Visual.WorldEventRow> RecentWorldEvents(int maxRows);

        bool TryReadActor(string actorName, out ActorViewState state);

        /// <summary>
        /// SOUL-04: read an actor's per-tick visual snapshot by its STABLE
        /// <see cref="ActorId"/> instead of its display-name string. This is the
        /// id-keyed position-sync path the host uses to project SOUL-03
        /// (ScheduleSystem) movement onto an existing billboard view: it looks
        /// the record up in <c>WorldState.Actors</c> by id and projects its
        /// <c>GridPosition</c> to world space. Returns false (default state)
        /// when the id is empty or unregistered.
        /// </summary>
        bool TryReadActor(ActorId id, out ActorViewState state);
        bool TryReadWorksite(string siteName, out WorksiteViewState state);

        /// <summary>
        /// SOUL-04 (spawn-from-worldgen): enumerate the actors that a runtime spawner may
        /// materialise as billboards, as flat presentation DTOs (<see cref="SpawnableActor"/>)
        /// so the spawner never imports a Domain store type. The adapter projects each
        /// <c>WorldState.Actors</c> record's <c>GridPosition</c> to the same world XZ point
        /// <c>TryReadActor</c> uses and carries the actor's stable id (as a ulong) plus name.
        /// The host filters/caps this list (nearest-N to the player) and skips ids that already
        /// have an authored ActorView, so implementations should return the full candidate set
        /// in deterministic order and NOT pre-cull. Placeholder adapters return an empty list.
        /// </summary>
        IReadOnlyList<SpawnableActor> GetSpawnableActors();
    }

    /// <summary>
    /// SOUL-04 flat DTO for a worldgen-generated actor the host may spawn as a billboard.
    /// Presentation-only: <see cref="Id"/> is the actor's stable id as a raw ulong (the host
    /// rebuilds an <see cref="ActorId"/> from it via <c>ActorView.BindDomainActorId</c>), and
    /// <see cref="WorldX"/>/<see cref="WorldZ"/> are the already-projected world position so the
    /// spawner does no Domain math. Carries no Domain type, keeping the spawner Domain-free.
    /// </summary>
    public readonly struct SpawnableActor
    {
        public readonly ulong Id;
        public readonly string Name;
        public readonly float WorldX;
        public readonly float WorldZ;

        public SpawnableActor(ulong id, string name, float worldX, float worldZ)
        {
            Id = id;
            Name = name;
            WorldX = worldX;
            WorldZ = worldZ;
        }
    }

    /// <summary>Player-driven write surface: log combat, take damage, cast spells, interact.</summary>
    public interface IPlayerCommandSink
    {
        void LogCombat(string message);
        void TakePlayerDamage(int amount);

        // Codex audit (seventh pass C-P3 #12): the default-method shims used
        // to hide silent "command not implemented" paths — adapters could
        // omit TryCastSpell / TryMeleeStrike / TryInteract and the host
        // would call a default that just logged a hint and returned false,
        // making missing routing invisible. Defaults removed: every
        // implementer must now explicitly handle each verb (or explicitly
        // return false with a deliberate refusal). Both production adapters
        // (DomainSimulationAdapter, PlaceholderSimulationAdapter) already
        // override these; the test EmptySimulationAdapter was retired in
        // the sixth pass.

        /// <summary>
        /// Player triggers a spell cast on slot index. Returns true when the
        /// adapter accepted the command (mana/cooldown/target valid). Failed
        /// commands surface a refusal string via <see cref="LogCombat"/>.
        /// </summary>
        bool TryCastSpell(int spellSlotIndex);

        /// <summary>
        /// Player triggers a melee strike on a target actor. Returns true when
        /// the strike resolved against a domain actor.
        /// </summary>
        bool TryMeleeStrike(string targetActorName, int rawDamage);

        /// <summary>
        /// Player interacts (E key) with a world object identified by tag.
        /// Returns true when the adapter routed the interaction.
        /// </summary>
        bool TryInteract(string targetTag);

        /// <summary>
        /// Player interacts with a world actor by stable id. Use this for NPCs/interactables that
        /// carry an <see cref="ActorId"/>; the string overload is legacy and reserved for non-actor
        /// tags or old authored scenes.
        /// </summary>
        bool TryInteract(ActorId actorId);

        /// <summary>
        /// Acquire the command channel for an NPC conversation. Returns the
        /// player-facing source that exposes topic selection and reply hooks.
        /// Codex audit (sixth pass C-P2 #C2): used to live in
        /// <see cref="IWorldViewReadModel"/>, which is a snapshot-DTO surface.
        /// Dialog is a stateful command channel, not a read model — moved here
        /// next to the other player-driven verbs.
        /// </summary>
        IDialogSource GetDialogSource(string actorName);

        /// <summary>
        /// DLG-01: resolve an NPC conversation by the actor's STABLE
        /// <see cref="ActorId"/> instead of its display-name string. This is
        /// the canonical lookup path now that SOUL-01/03 give every world
        /// actor a stable id in <c>WorldState.Actors</c>: the adapter finds the
        /// ActorRecord by id (and the matching NpcSeed when the id maps to a
        /// generated NPC) rather than scanning by name, which broke whenever
        /// two actors shared a name or a view authored a divergent label.
        /// Implementations MUST log a warning and return a non-null but
        /// deliberately empty/global-less source when the id does not resolve,
        /// rather than silently dropping the player into global world topics.
        /// The <see cref="GetDialogSource(string)"/> overload stays as a
        /// best-effort fallback for legacy callers that only have a name.
        /// </summary>
        IDialogSource GetDialogSource(ActorId id);

        /// <summary>
        /// World seeding command — applies the MainMenu/world-gen wizard's
        /// mood/calling/start-location text to the freshly bootstrapped world
        /// before the first tick advances. Codex ninth-pass C-P2: previously
        /// sat on the aggregate IDomainSimulationAdapter; moved here so the
        /// host can call it through its narrow IPlayerCommandSink handle.
        /// </summary>
        void SeedWorld(string mood, string calling, string startLocation, uint? worldSeed = null);
    }

    /// <summary>
    /// Narrator-flavour oracle for the consult-fate UI ribbon.
    /// Codex audit (sixth pass C-P3 #C3): documents the contract — implementations
    /// MUST return a non-null, non-empty string for every call. The 35/35/30
    /// distribution and bucket→string mapping is canonised by
    /// <c>EmberCrpg.Domain.AiDm.ConsultFateOutcomeBucket</c>; adapters that
    /// want different copy still use that bucket as the threshold table.
    /// </summary>
    public interface IConsultFateOracle
    {
        // Kicks off a fate consult. Returns an IMMEDIATE line (a "consulting…" placeholder for the async
        // LLM path, or the resolved line for a synchronous adapter).
        string ConsultFate();

        // BUG-4: the async LLM prophecy resolves a frame or more after ConsultFate() returns. The host
        // polls this each frame; it returns the resolved line (LLM-flavoured, or the deterministic fate
        // bucket as a floor) exactly once when ready, then null until the next consult — so the real
        // prophecy can replace the placeholder in the dialog panel instead of only hitting the combat log.
        string TryConsumeResolvedFate();
    }

    /// <summary>
    /// Save / load round-trip envelope for the full deterministic snapshot.
    /// Codex audit (sixth pass C-P3 #C4): contract clarified — exports are
    /// expected to be valid JSON (or null when there is no state yet), and
    /// imports MUST tolerate null/empty input without throwing. Round-trip
    /// fidelity is enforced by the EditMode round-trip tests under
    /// Assets/Tests/EditMode/Save/.
    /// </summary>
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
    /// Codex audit (sixth pass C-P3 #C5): the locator is a deliberate
    /// scene-scoped singleton — there is exactly one IDomainSimulationAdapter
    /// per scene (the EmberWorldHost or its placeholder), and `Register`
    /// overwrites without warning so additive scene loads do not double-register.
    /// Tests reset by calling <c>Register(null)</c> in <c>[TearDown]</c>.
    /// </summary>
    public static class EmberDomainAdapterLocator
    {
        private static IDomainSimulationAdapter _current;

        public static IDomainSimulationAdapter Current => _current;

        // Codex audit (seventh pass C-P3 #13): role-specific accessors let
        // narrow consumers (HUD panels, telemetry, AI-DM clients) take a
        // single role interface dependency without grabbing the aggregate.
        // Returns null when no adapter is registered yet.
        public static IEmberClockSource ClockSource => _current;
        public static IEmberHudReadModel HudReadModel => _current;
        public static IWorldViewReadModel WorldViewReadModel => _current;
        public static IPlayerCommandSink PlayerCommandSink => _current;
        public static IConsultFateOracle ConsultFateOracle => _current;
        public static IEmberSaveBridge SaveBridge => _current;

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

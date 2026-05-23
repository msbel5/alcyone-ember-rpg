// Design note:
// Worldgen enums kept together so the FOUNDATION pass lands as ~5-10 files
// instead of fragmenting into one-enum-per-file. Every enum carries a `None`
// zero sentinel so default-initialized values are rejected by record
// constructors — same defensive-constructor contract as
// WorldEventKind / SiteKind / FactionRelationKind.
namespace EmberCrpg.Domain.Worldgen
{
    /// <summary>
    /// Coarse biome category for a procedurally generated region. The seed set
    /// is intentionally small (Daggerfall-scale, not Dwarf-Fortress-scale) so
    /// the FOUNDATION generator can map biomes to settlement densities and
    /// name-banks without a full climate sim.
    /// </summary>
    public enum BiomeKind
    {
        None = 0,
        TemperatePlain = 1,
        BorealForest = 2,
        CoastalMarsh = 3,
        AridSteppe = 4,
        MountainHighland = 5,
        DesertWaste = 6,
        TropicalJungle = 7,
        FrozenTundra = 8,
    }

    /// <summary>
    /// Settlement size bucket used by the FOUNDATION generator to drive
    /// population sampling and NPC slot counts. The brief targets a roughly
    /// Daggerfall-style distribution: a few cities (50K-100K), dozens of
    /// towns (1K-10K), hundreds of villages (100-1K), with one capital
    /// (~100K-150K) anchoring the world.
    /// </summary>
    public enum SettlementSize
    {
        None = 0,
        Hamlet = 1,
        Village = 2,
        Town = 3,
        City = 4,
        Capital = 5,
    }

    /// <summary>
    /// NPC role bucket used by the FOUNDATION generator. Roles are coarse
    /// because the simulation kernel that consumes them (jobs, dialog,
    /// economy) is still under construction — finer professions belong to a
    /// Faz-N follow-up that hydrates an NpcSeedRecord into a runtime
    /// ActorRecord.
    /// </summary>
    public enum NpcRole
    {
        None = 0,
        Farmer = 1,
        Merchant = 2,
        Guard = 3,
        Noble = 4,
        Priest = 5,
        Scholar = 6,
        Artisan = 7,
        Outlaw = 8,
    }

    /// <summary>
    /// Historical-event category emitted by the deterministic 100-year history
    /// pass. Distinct from <c>EmberCrpg.Domain.World.WorldEventKind</c>: that
    /// enum chronicles per-tick PROCESS-box events on the runtime
    /// WorldEventLog (ActorSpawned, SiteEntered, ...), while
    /// WorldHistoryKind logs the year-resolution macro events that seed the
    /// world's story before play begins.
    /// </summary>
    public enum WorldHistoryKind
    {
        None = 0,
        SettlementFounded = 1,
        FactionWar = 2,
        FactionAlliance = 3,
        NobleMarriage = 4,
        NobleDeath = 5,
        Calamity = 6,
        TradeRouteOpened = 7,
        Migration = 8,
    }

    /// <summary>High-level world tone selected by the world-generation wizard.</summary>
    public enum WorldStyle
    {
        LowFantasyMorrowind = 0,
        HighFantasyTolkien = 1,
        DarkFantasyGrim = 2,
        SteampunkRevolution = 3,
        AncientMythology = 4,
    }

    /// <summary>Campaign pressure lens selected by the world-generation wizard.</summary>
    public enum WorldGenre
    {
        Survival = 0,
        PoliticalIntrigue = 1,
        MonsterHunt = 2,
        MerchantEmpire = 3,
        Pilgrimage = 4,
    }
}

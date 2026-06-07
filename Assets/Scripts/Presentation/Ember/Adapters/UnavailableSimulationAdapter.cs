using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Core;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using WorldEventRow = EmberCrpg.Presentation.Visual.WorldEventRow;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    /// <summary>
    /// Honest fallback used only when a real DomainSimulationAdapter cannot boot. It shows unavailable/empty
    /// states instead of fabricating jobs, needs, inventory, factions, combat, or quests as real gameplay.
    /// </summary>
    public sealed class UnavailableSimulationAdapter : IDomainSimulationAdapter, IDialogSourcePortrait,
        IJournalSource, ITradeSource, ITradeCommandSink, ICraftingSource, ICraftingCommandSink,
        ILevelUpSource, ILevelUpCommandSink, ICombatScreenSource
    {
        private const string Message = "Simulation unavailable: no real domain adapter is active.";
        private static readonly IReadOnlyList<JobQueueRow> EmptyJobs = Array.Empty<JobQueueRow>();
        private static readonly IReadOnlyList<ColonyNeedsRow> EmptyNeeds = Array.Empty<ColonyNeedsRow>();
        private static readonly IReadOnlyList<FactionRow> EmptyFactions = Array.Empty<FactionRow>();
        private static readonly IReadOnlyList<InventorySlot> EmptyInventory = Array.Empty<InventorySlot>();
        private static readonly IReadOnlyList<string> EmptySpells = Array.Empty<string>();
        private static readonly IReadOnlyList<SpawnableActor> EmptyActors = Array.Empty<SpawnableActor>();
        private static readonly IReadOnlyList<JournalChapterRow> EmptyJournal = Array.Empty<JournalChapterRow>();
        private static readonly CombatHudState UnavailableCombat = new CombatHudState(0, 0, 0, 0, 0, 0, Message);
        private static readonly PlayerSheetState UnavailablePlayer = new PlayerSheetState("Unavailable", 0, 0, 0, 0, 0, 0);

        private int _tick;

        public int TickIndex => _tick;
        public string HudText => "BROKEN: " + Message;
        public IReadOnlyList<JobQueueRow> JobQueueRows => EmptyJobs;
        public IReadOnlyList<ColonyNeedsRow> ColonyNeedsRows => EmptyNeeds;
        public IReadOnlyList<FactionRow> FactionRows => EmptyFactions;
        public IReadOnlyList<InventorySlot> InventorySlots => EmptyInventory;
        public IReadOnlyList<string> SpellSlots => EmptySpells;
        public EmberCrpg.Domain.Overland.OverlandMap Overland => null;
        public EmberCrpg.Domain.Actors.GridPosition PlayerOverlandTile => default;
        public string StartingSettlementName => null;
        public CombatHudState CombatHud => UnavailableCombat;
        public PlayerSheetState PlayerSheet => UnavailablePlayer;

        public void AdvanceTick(int tickIndex) => _tick = tickIndex;
        public IReadOnlyList<WorldEventRow> RecentWorldEvents(int maxRows) => Array.Empty<WorldEventRow>();
        public bool TryReadActor(string actorName, out ActorViewState state) { state = default; return false; }
        public bool TryReadActor(ActorId id, out ActorViewState state) { state = default; return false; }
        public bool TryReadWorksite(string siteName, out WorksiteViewState state) { state = default; return false; }
        public IReadOnlyList<SpawnableActor> GetSpawnableActors() => EmptyActors;

        public void LogCombat(string message) { }
        public void TakePlayerDamage(int amount) { }
        public bool TryCastSpell(int spellSlotIndex) => false;
        public bool TryMeleeStrike(string targetActorName, int rawDamage) => false;
        public bool TryInteract(string targetTag) => false;
        public bool TryInteract(ActorId actorId) => false;
        public void SeedWorld(string mood, string calling, string startLocation, uint? worldSeed = null) { }

        public IDialogSource GetDialogSource(string actorName) => this;
        public IDialogSource GetDialogSource(ActorId id) => this;
        public string GetCurrentLine() => Message;
        public IReadOnlyList<string> GetTopics() => EmptySpells;
        public void SelectTopic(string topicId) { }
        public void AskFreeText(string question) { }
        public string GetPortraitName() => string.Empty;

        public string ConsultFate() => Message;
        public string ConsultFate(string question) => Message;
        public string TryConsumeResolvedFate() => null;
        public string ExportStateJson() => null;
        public void RestoreStateJson(string json) { }

        public IReadOnlyList<JournalChapterRow> GetChapters() => EmptyJournal;
        public int GetCurrentChapter() => 0;
        public TradeLedgerState ReadTradeState() => new TradeLedgerState("Unavailable", "Unavailable", 0, 0, Array.Empty<TradeItemRow>(), Array.Empty<TradeItemRow>());
        public TradeActionResult ExecuteTrade(TradeActionRequest request) => new TradeActionResult(false, Message);
        public CraftingLedgerState ReadCraftingState() => new CraftingLedgerState("Unavailable", Array.Empty<CraftingRecipeRow>());
        public CraftingActionResult ExecuteCraft(string recipeId) => new CraftingActionResult(false, Message);
        public LevelUpScreenState ReadLevelUpState() => new LevelUpScreenState("Unavailable", 1, 0, Array.Empty<LevelUpStatRow>(), Array.Empty<LevelUpSpellRow>());
        public LevelUpActionResult ApplyLevelUp(LevelUpSelection selection) => new LevelUpActionResult(false, Message);
        public CombatScreenState ReadCombatScreenState() => new CombatScreenState(false, "Unavailable", 0, 0, 0, 0, 0, 0, string.Empty, 0, 0, Message, Array.Empty<CombatSpellActionRow>());
    }
}

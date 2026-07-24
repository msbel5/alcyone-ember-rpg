using System.Collections.Generic;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Views;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Bootstrap
{
    public sealed partial class EmberWorldHost
    {
        private void BindUiPanels()
        {
            foreach (var hud in Object.FindObjectsByType<EmberHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                hud.Source = this;
            foreach (var q in Object.FindObjectsByType<JobQueuePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                q.Source = this;
            foreach (var n in Object.FindObjectsByType<ColonyNeedsPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                n.Source = this;
            foreach (var d in Object.FindObjectsByType<DialogBoxPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                d.Source = this;
            foreach (var inventory in Object.FindObjectsByType<InventoryGrid>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                inventory.Source = this;
                inventory.SpriteLookup = this;
            }
            foreach (var faction in Object.FindObjectsByType<FactionPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                faction.Source = this;
            // HUD consistency (T3 + UI-SINGLE-SOURCE): every scene must show the standard EmberHud
            // (vitals pills + numbered hotbar), never the divergent bottom-bar CombatHud. EnsureEmberHud()
            // ran before this bind, so an EmberHud is always present and reads combat vitals via
            // ICombatHudSource (this host) — no combat info is lost. Any CombatHud a scene still carries
            // is therefore redundant; disable it so it never stacks under the standard HUD.
            foreach (var combat in Object.FindObjectsByType<CombatHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (combat == null) continue;
                combat.gameObject.SetActive(false);
            }
            foreach (var spellBar in Object.FindObjectsByType<SpellBar>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                spellBar.Source = this;
                spellBar.SpriteLookup = this;
            }
        }

        public void OnTick(int tickIndex)
        {
            _worldViewProjector?.ProjectTick(tickIndex);
        }

        /// <summary>F14/F18: real-time movers (hostile chase, 1 cell/0.45s) step the sim BETWEEN world
        /// ticks; refresh view targets WITHOUT advancing the clock so billboards see every chase step.
        /// Proof-measured: targets only refreshed on the ~0.83s tick made the pursuit read half-speed.</summary>
        public void ProjectWorldViewsNow() => _worldViewProjector?.Project();

        /// <summary>Pull late-spawned ActorViews into the projector's sync set.</summary>
        public void RescanActorViews()
            => _worldViewProjector?.ReplaceActorViews(
                Object.FindObjectsByType<ActorView>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        public string GetHudText() => _hud.HudText;
        IReadOnlyList<JobQueueRow> IJobQueueSource.GetRows() => _worldView.JobQueueRows;
        IReadOnlyList<ColonyNeedsRow> IColonyNeedsSource.GetRows() => _worldView.ColonyNeedsRows;
        IReadOnlyList<FactionRow> IFactionSource.GetRows() => _worldView.FactionRows;
        public IReadOnlyList<InventorySlot> GetSlots() => _worldView.InventorySlots;
        IReadOnlyList<string> ISpellBarSource.GetSlots() => _worldView.SpellSlots;
        int ISpellBarSource.GetSelectedSlot() => _selectedSpellSlot;
        CombatHudState ICombatHudSource.Read() => _hud.CombatHud;
        CombatScreenState ICombatScreenSource.ReadCombatScreenState() => (_adapter as ICombatScreenSource)?.ReadCombatScreenState()
            ?? new CombatScreenState(false, "Unknown", 0, 0, 0, 0, 0, 0, string.Empty, 0, 0, string.Empty, System.Array.Empty<CombatSpellActionRow>());
        PlayerSheetState IPlayerSheetSource.ReadPlayerSheet() => _hud.PlayerSheet;
        IReadOnlyList<JournalChapterRow> IJournalSource.GetChapters() => (_adapter as IJournalSource)?.GetChapters() ?? System.Array.Empty<JournalChapterRow>();
        int IJournalSource.GetCurrentChapter() => (_adapter as IJournalSource)?.GetCurrentChapter() ?? 0;
        QuestGuidanceRow IQuestGuidanceSource.ReadQuestGuidance() => (_adapter as IQuestGuidanceSource)?.ReadQuestGuidance() ?? QuestGuidanceRow.None;
        QuestGuidanceRow IQuestGuidanceSource.ReadDelveGuidance() => (_adapter as IQuestGuidanceSource)?.ReadDelveGuidance() ?? QuestGuidanceRow.None;
        TradeLedgerState ITradeSource.ReadTradeState() => (_adapter as ITradeSource)?.ReadTradeState() ?? new TradeLedgerState("Quartermaster", "Current Holding", 0, 0, System.Array.Empty<TradeItemRow>(), System.Array.Empty<TradeItemRow>());
        TradeActionResult ITradeCommandSink.ExecuteTrade(TradeActionRequest request) => (_adapter as ITradeCommandSink)?.ExecuteTrade(request) ?? new TradeActionResult(false, "Trade commands are unavailable.");
        CraftingLedgerState ICraftingSource.ReadCraftingState() => (_adapter as ICraftingSource)?.ReadCraftingState() ?? new CraftingLedgerState("No Workstation", System.Array.Empty<CraftingRecipeRow>());
        CraftingActionResult ICraftingCommandSink.ExecuteCraft(string recipeId) => (_adapter as ICraftingCommandSink)?.ExecuteCraft(recipeId) ?? new CraftingActionResult(false, "Craft commands are unavailable.");
        SaveLoadScreenState ISaveLoadSource.ReadSaveLoadState()
        {
            var service = GetComponent<EmberCrpg.Presentation.Ember.Save.EmberSaveService>();
            return service != null
                ? new SaveLoadScreenState(service.ManualSlotCap, service.ListSlots())
                : new SaveLoadScreenState(10, System.Array.Empty<EmberCrpg.Data.Save.SaveSlotMetadata>());
        }
        SaveLoadActionResult ISaveLoadCommandSink.SaveToSlot(EmberCrpg.Data.Save.SaveSlotId slot)
        {
            var service = GetComponent<EmberCrpg.Presentation.Ember.Save.EmberSaveService>();
            return service != null ? service.SaveFromUi(slot) : new SaveLoadActionResult(false, "Save service is unavailable.");
        }
        SaveLoadActionResult ISaveLoadCommandSink.LoadFromSlot(EmberCrpg.Data.Save.SaveSlotId slot)
        {
            var service = GetComponent<EmberCrpg.Presentation.Ember.Save.EmberSaveService>();
            return service != null ? service.LoadFromUi(slot) : new SaveLoadActionResult(false, "Load service is unavailable.");
        }
        SaveLoadActionResult ISaveLoadCommandSink.LoadLatestSave()
        {
            var service = GetComponent<EmberCrpg.Presentation.Ember.Save.EmberSaveService>();
            return service != null ? service.LoadLatestFromUi() : new SaveLoadActionResult(false, "Load service is unavailable.");
        }
        LevelUpScreenState ILevelUpSource.ReadLevelUpState()
        {
            return (_adapter as ILevelUpSource)?.ReadLevelUpState()
                ?? new LevelUpScreenState("Unknown", 1, 5, System.Array.Empty<LevelUpStatRow>(), System.Array.Empty<LevelUpSpellRow>());
        }
        LevelUpActionResult ILevelUpCommandSink.ApplyLevelUp(LevelUpSelection selection)
        {
            return (_adapter as ILevelUpCommandSink)?.ApplyLevelUp(selection)
                ?? new LevelUpActionResult(false, "Level-up commands are unavailable.");
        }
    }
}

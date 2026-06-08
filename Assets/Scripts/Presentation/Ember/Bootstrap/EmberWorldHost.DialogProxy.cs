using System.Collections.Generic;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Presentation.Ember.UI;

namespace EmberCrpg.Presentation.Ember.Bootstrap
{
    public sealed partial class EmberWorldHost
    {
        public string GetCurrentLine()
        {
            // T-Dialog-AskAbout slice 2 fix — transparent proxy. When _adapter is also an
            // IDialogSource (DomainSimulationAdapter implements IDialogSourcePortrait), forward
            // the live line through it so picking a real topic id from GetTopics (e.g.
            // embers/gate/watch) returns the deterministic AskAboutService answer / streaming
            // LLM line instead of the generic fallback below. _fateLine takes precedence so a
            // ConsultFate result still surfaces. Host-owned dialog fallback (no adapter): keep
            // the legacy {work/trade/fate} switch + default flavor line.
            if (!string.IsNullOrEmpty(_fateLine)) return _fateLine;

            if (_adapter is IDialogSource adapterSource)
                return adapterSource.GetCurrentLine();

            switch (_selectedTopic)
            {
                case "work": return "The forge queue is moving. Watch the left panel for job state.";
                case "trade": return "Caravans shift prices as stock moves between settlements.";
                case "fate": return "The oracle can surface a deterministic world query without mutating state.";
                default: return "Ask clean questions. The world remembers what matters.";
            }
        }

        // Async LLM gate — when the adapter is generating an NPC line, the panel renders the
        // "thinking…" placeholder. Default false when host owns the dialog (synchronous path).
        bool IDialogSource.IsThinking => _adapter is IDialogSource adapterSource && adapterSource.IsThinking;

        public IReadOnlyList<string> GetTopics()
        {
            // T-Dialog-AskAbout slice 2 — delegate to the per-NPC adapter source when it has
            // real deterministic topic IDs (DomainSimulationAdapter pulls them from
            // WorldState.Topics, the same source AskAboutService.Ask() reads). Falls back
            // to the {rumors, work, trade, fate} stub when no adapter is wired (offline /
            // editor sketch path). This is what makes TavernDialog finally surface the same
            // real topic IDs (embers/gate/watch/...) that Showroom already showed via the
            // adapter-owned dialog path.
            if (_adapter is IDialogSource adapterSource)
            {
                var adapterTopics = adapterSource.GetTopics();
                if (adapterTopics != null && adapterTopics.Count > 0)
                    return adapterTopics;
            }
            return Topics;
        }

        public string GetPortraitName()
        {
            if (!string.IsNullOrEmpty(_fateLine))
                return DialogPortraitKey.DungeonMaster;

            // Forward to the adapter when it carries a per-NPC portrait id so the dialog panel
            // gets a real sprite name (e.g. "portrait_sage_nera") instead of the gray
            // placeholder. Falls back to the neutral placeholder when no adapter / no portrait.
            if (_adapter is IDialogSourcePortrait portraitSource)
            {
                var name = portraitSource.GetPortraitName();
                if (!string.IsNullOrEmpty(name)) return name;
            }
            return DialogPortraitKey.Default;
        }

        public void SelectTopic(string topicId)
        {
            if (string.IsNullOrEmpty(topicId)) return;

            // T-Dialog-AskAbout slice 2 fix — close the loop the prior comment flagged. Slice 2
            // made GetTopics() advertise adapter topics (embers/gate/watch/...). Without this
            // forward, DialogBoxPanel.Source.SelectTopic("embers") only mutated _selectedTopic
            // here and the host's GetCurrentLine fell back to the generic flavor line — the
            // player saw the new topic listed but never the deterministic answer. When the
            // adapter is the real dialog source, forward the selection so its SelectTopic
            // appends the WorldEvent, mutates NpcMemory, and fires the async LLM topic answer.
            if (_adapter is IDialogSource adapterSource)
            {
                adapterSource.SelectTopic(topicId);
                return;
            }

            // Host-owned fallback path (no adapter wired): keep the legacy _selectedTopic
            // mutation so the GetCurrentLine() switch (work/trade/fate) still picks a sane
            // canned line.
            _selectedTopic = topicId;
        }

        public void AskFreeText(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return;

            if (_adapter is IDialogSource adapterSource)
            {
                adapterSource.AskFreeText(question);
                return;
            }
        }


    }
}

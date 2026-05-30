// EMB-033: CharacterCreationController portrait generation concern (partial; view is in .Rendering.cs).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Worldgen;
using EmberCrpg.Simulation.CharacterCreation;
using EmberCrpg.Simulation.Generation;
using EmberCrpg.Ui.Foundation;
using EmberCrpg.Simulation.Worldgen;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public sealed partial class CharacterCreationController
    {
        private void GeneratePortrait()
        {
            var manifest = GenericNpcBaseManifest.CreateDefault();
            var raw = string.IsNullOrWhiteSpace(_llmJson) ? RequestPortraitJson(string.Empty) : _llmJson;
            if (!NpcPromptJsonValidator.TryValidate(raw, manifest, out var json, out var reason))
            {
                raw = RequestPortraitJson(reason);
                if (!NpcPromptJsonValidator.TryValidate(raw, manifest, out json, out reason))
                {
                    json = NpcPromptJsonDefaults.FromSeed(_seed + (uint)(3 - _rerollsRemaining), manifest);
                    AddLog("[portrait] LLM invalid twice; deterministic fallback used: " + reason + ".");
                }
            }

            PortraitJson = json.ToCanonicalJson();
            _panel?.SetText("portraitJson", PortraitJson);
            AddLog("[portrait] JSON ready.");
        }

        private string RequestPortraitJson(string correctionReason)
        {
            if (_portraitJsonProvider != null)
                return _portraitJsonProvider(_seed + (uint)(3 - _rerollsRemaining), correctionReason ?? string.Empty);
            return DefaultNpcPortraitJsonProvider.Request(_seed + (uint)(3 - _rerollsRemaining), correctionReason);
        }

        private void AddLog(string line)
        {
            _logLines.Add(line);
            _panel?.LogLine("log", UiLogSeverity.Info, line);
        }

        private string[] BuildAttributeRollSnapshot()
        {
            var rows = new string[StatOrder.Length];
            for (int i = 0; i < StatOrder.Length; i++)
                rows[i] = StatOrder[i] + "=" + SafeStat(_assignedStats, StatOrder[i]);
            return rows;
        }

        private static string ResolveBirthsignId(uint seed)
        {
            var rows = CharacterCreationCatalog.Birthsigns;
            if (rows.Count == 0) return string.Empty;
            return rows[(int)(seed % (uint)rows.Count)].Id;
        }

    }
}

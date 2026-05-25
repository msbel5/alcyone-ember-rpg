// Why this file is intentionally long: it contains the UI rendering half of the PRD v2 character-creation partial controller.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Presentation.Ember.Worldgen;
using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Ui.Foundation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public sealed partial class CharacterCreationController
    {
        private void Render()
        {
            if (_panel == null) return;

            _panel.SetText("header", "IMMERSIVE CHARACTER CREATION");
            _panel.SetText("step", StepLabel());
            _panel.SetProgress("progress", Mathf.Clamp01((int)_step / 5f));
            _panel.SetText("next", NextButtonText());
            _panel.SetVisible("back", _step != CreationStep.CommanderIdentity);
            _panel.SetVisible("next", _step != CreationStep.Complete);

            ClearDynamicSlots();
            _panel.SetText("body", BuildBodyText());

            if (_step == CreationStep.PersonalityQuestions)
                RenderQuestionButtons();
            else if (_step == CreationStep.CommanderIdentity)
                RenderCommanderButtons();
            else if (_step == CreationStep.StatRolling)
                RenderStatButtons();
            else if (_step == CreationStep.BuildSelection)
                RenderBuildButtons();
            else if (_step == CreationStep.DossierLaunch)
                RenderDossierButtons();
        }

        private string StepLabel()
        {
            switch (_step)
            {
                case CreationStep.CommanderIdentity: return "Step 0 - Commander Identity";
                case CreationStep.PersonalityQuestions: return "Step 1 - Personality Questions";
                case CreationStep.WorldHistoryReveal: return "Step 2 - World History Reveal";
                case CreationStep.StatRolling: return "Step 3 - Stat Rolling";
                case CreationStep.BuildSelection: return "Step 4 - Class Alignment Skills";
                case CreationStep.DossierLaunch: return "Step 5 - Dossier and Launch";
                default: return "Character Creation Complete";
            }
        }

        private string NextButtonText()
        {
            switch (_step)
            {
                case CreationStep.WorldHistoryReveal:
                    return IsHistoryAdvanceUnlocked() ? "Continue" : "Skip Reveal";
                case CreationStep.DossierLaunch:
                    return "Begin Your Story";
                case CreationStep.Complete:
                    return "Completed";
                default:
                    return "Continue";
            }
        }

        private string BuildBodyText()
        {
            switch (_step)
            {
                case CreationStep.CommanderIdentity:
                    return BuildCommanderBody();
                case CreationStep.PersonalityQuestions:
                    return BuildQuestionBody();
                case CreationStep.WorldHistoryReveal:
                    return BuildHistoryBody();
                case CreationStep.StatRolling:
                    return BuildStatsBody();
                case CreationStep.BuildSelection:
                    return BuildSelectionBody();
                case CreationStep.DossierLaunch:
                    return BuildDossierBody();
                default:
                    return "Your story has started.";
            }
        }

        private string BuildCommanderBody()
        {
            var builder = new StringBuilder();
            builder.AppendLine("What name will they remember?");
            builder.AppendLine("Name: " + (_commanderName.Length == 0 ? "<unset>" : _commanderName));
            builder.AppendLine("Adapter: " + _adapterId);
            builder.AppendLine("Default adapter remains fantasy_ember unless overridden.");
            if (_advancedSettingsVisible)
            {
                builder.AppendLine("Advanced settings visible:");
                builder.AppendLine("- world_seed: " + (_worldSeedInput.Length == 0 ? "<auto>" : _worldSeedInput));
                builder.AppendLine("- profile_hint: " + (_profileHint.Length == 0 ? "<none>" : _profileHint));
            }
            else
            {
                builder.AppendLine("Advanced settings hidden. Toggle Show Advanced Settings for seed/profile.");
            }

            if (_commanderName.Length > 0 && _commanderName.Length < 2)
                builder.AppendLine("Name must be at least 2 characters.");
            return builder.ToString();
        }

        private string BuildQuestionBody()
        {
            if (_questions.Count == 0)
                return "No questionnaire data available.";
            if (_questionIndex >= _questions.Count)
                return "All questions answered.";

            var question = _questions[_questionIndex];
            var builder = new StringBuilder();
            builder.AppendLine("Question " + (_questionIndex + 1) + " of " + _questions.Count);
            builder.AppendLine(question.Prompt);
            builder.AppendLine("Choose one visible answer button below.");
            return builder.ToString();
        }

        private string BuildHistoryBody()
        {
            var builder = new StringBuilder();
            builder.AppendLine("The ages unfold...");
            string visible = VisibleHistoryText();
            builder.AppendLine(visible.Length == 0 ? "[history streaming...]" : visible);
            if (IsHistoryAdvanceUnlocked())
                builder.AppendLine("Continue is unlocked.");
            else
                builder.AppendLine("Press Continue (or Enter/Space equivalent) to skip reveal.");
            return builder.ToString();
        }

        private string BuildStatsBody()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Body-position stat diagram");

            if (_assignedStats.Count == 0)
            {
                builder.AppendLine("Roll stats to generate your pool.");
                return builder.ToString();
            }

            builder.AppendLine("         [MND: " + FormatStat("MND") + "]");
            builder.AppendLine("    [INS: " + FormatStat("INS") + "]  [PRE: " + FormatStat("PRE") + "]");
            builder.AppendLine("         [END: " + FormatStat("END") + "]");
            builder.AppendLine("    [MIG: " + FormatStat("MIG") + "]  [AGI: " + FormatStat("AGI") + "]");

            var pool = _activeStats.Values.OrderByDescending(v => v).ToArray();
            builder.AppendLine("Roll Pool: [" + string.Join(", ", pool) + "]");
            builder.AppendLine("Assigned: " + string.Join(", ", StatOrder.Select(s => s + "=" + SafeStat(_assignedStats, s))));
            builder.AppendLine("Saved Pool: " + (_savedStats.Count == 0 ? "<none>" : string.Join(", ", StatOrder.Select(s => s + "=" + SafeStat(_savedStats, s)))));
            builder.AppendLine(_rollKept ? "Roll kept." : "Keep This Roll to continue.");
            return builder.ToString();
        }

        private string BuildSelectionBody()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Class cards, alignment chart, skill grid");
            builder.AppendLine("Suggested class: " + ClassName(_suggestedClassId));
            builder.AppendLine("Selected class: " + (_selectedClassId.Length == 0 ? "<none>" : ClassName(_selectedClassId)));
            builder.AppendLine("Selected alignment: " + (_selectedAlignmentId.Length == 0 ? "<none>" : AlignmentName(_selectedAlignmentId)));
            builder.AppendLine("Skills: " + _selectedSkills.Count + "/5 selected");
            builder.AppendLine(string.Join(", ", _selectedSkills.OrderBy(v => v)));
            builder.AppendLine("Use visible class/alignment/skill buttons below.");
            return builder.ToString();
        }

        private string BuildDossierBody()
        {
            var selectedClass = _selectedClassId.Length == 0 ? CharacterCreationCatalog.GetClass(_suggestedClassId) : CharacterCreationCatalog.GetClass(_selectedClassId);
            var builder = new StringBuilder();
            builder.AppendLine("Dossier Preview");
            builder.AppendLine("Name: " + _commanderName);
            builder.AppendLine("Class: " + selectedClass.Name);
            builder.AppendLine("Alignment: " + AlignmentName(_selectedAlignmentId));
            builder.AppendLine("Stats: " + string.Join(", ", StatOrder.Select(s => s + " " + SafeStat(_assignedStats, s))));
            builder.AppendLine("Skills: " + string.Join(", ", _selectedSkills.OrderBy(v => v)));
            builder.AppendLine("Starting Equipment: " + string.Join(", ", selectedClass.StartingEquipment));
            builder.AppendLine("World Premise: " + (_historyTimeline.Count == 0 ? "No history generated." : _historyTimeline[0]));
            if (_storyLaunched) builder.AppendLine("Launch confirmed.");
            return builder.ToString();
        }

        private void RenderQuestionButtons()
        {
            if (_questionIndex < 0 || _questionIndex >= _questions.Count) return;
            var question = _questions[_questionIndex];
            float progress = _questions.Count <= 0 ? 0f : (float)(_questionIndex + 1) / _questions.Count;
            _panel.SetProgress("progress", progress);

            for (int i = 0; i < question.Choices.Count; i++)
            {
                string slot = "answer" + i;
                _dynamicSlots.Add(slot);
                var captured = question.Choices[i];
                _panel.SetText(slot, captured.Text + "\n" + BuildChoiceDescription(captured));
                _panel.SetButtonHandler(slot, () => AnswerCurrentQuestion(captured.Id));
                _panel.SetVisible(slot, true);
            }
        }

        private void RenderCommanderButtons()
        {
            string[] names = { "Ash-Born Commander", "Cinder Vey", "Mora of the Red Road" };
            for (int i = 0; i < names.Length; i++)
            {
                string slot = "identity_button_" + i;
                _dynamicSlots.Add(slot);
                var captured = names[i];
                _panel.SetText(slot, captured);
                _panel.SetButtonHandler(slot, () => SetCommanderIdentity(captured, _worldSeedInput, _profileHint));
                _panel.SetVisible(slot, true);
            }

            _dynamicSlots.Add("advanced_button");
            _panel.SetText("advanced_button", _advancedSettingsVisible ? "Hide Advanced Settings" : "Show Advanced Settings");
            _panel.SetButtonHandler("advanced_button", () => SetAdvancedSettingsVisible(!_advancedSettingsVisible));
            _panel.SetVisible("advanced_button", true);
        }

        private void RenderStatButtons()
        {
            _dynamicSlots.Add("roll_button");
            _panel.SetText("roll_button", _assignedStats.Count == 0 ? "Roll 4d6 Drop Lowest" : "Roll Again");
            _panel.SetButtonHandler("roll_button", RollAgain);
            _panel.SetVisible("roll_button", true);

            _dynamicSlots.Add("keep_button");
            _panel.SetText("keep_button", "Keep This Roll");
            _panel.SetButtonHandler("keep_button", KeepThisRoll);
            _panel.SetVisible("keep_button", _assignedStats.Count > 0);

            _dynamicSlots.Add("swap_button");
            _panel.SetText("swap_button", "Swap Active/Saved Pool");
            _panel.SetButtonHandler("swap_button", SwapRoll);
            _panel.SetVisible("swap_button", _savedStats.Count > 0);
        }

        private void RenderDossierButtons()
        {
            _dynamicSlots.Add("portrait_reroll_button");
            _panel.SetText("portrait_reroll_button", "Reroll Portrait JSON (" + _rerollsRemaining + " left)");
            _panel.SetButtonHandler("portrait_reroll_button", RerollPortrait);
            _panel.SetVisible("portrait_reroll_button", _rerollsRemaining > 0);

            _dynamicSlots.Add("portrait_lock_button");
            _panel.SetText("portrait_lock_button", "Lock Portrait");
            _panel.SetButtonHandler("portrait_lock_button", LockPortrait);
            _panel.SetVisible("portrait_lock_button", true);
        }

        private IEnumerator BeginVisibleWorldgen()
        {
            LoadingScreen.ShowForContext(new LoadingScreenContext("worldgen", "Building World", "generation"));
            LoadingScreen.SetProgress(0.1f, "Generating deterministic world");
            LoadingScreen.LogLine(UiLogSeverity.Info, "[worldgen] seed=" + _seed + " class=" + _selectedClassId);
            yield return null;

            var world = WorldgenService.Generate(_seed == 0u ? 42u : _seed, WorldgenParameters.Default);
            LoadingScreen.SetProgress(0.45f, "Projecting regions, settlements, NPC seeds, and history");

            var go = new GameObject("WorldgenViewController");
            DontDestroyOnLoad(go);
            var view = go.AddComponent<WorldgenViewController>();
            view.Configure(_firstSceneName);
            view.PlayFromGeneratedWorld(world, new WorldgenProjectionOptions(
                maxRegions: 8,
                maxSettlements: 12,
                maxNpcs: 16,
                maxHistoryEvents: 20,
                includeQuestionPrompt: true,
                includeSyntheticFailure: false));

            LoadingScreen.SetProgress(view.QuestionOpen ? 0.85f : 1f, view.QuestionOpen ? "Awaiting worldgen question" : "Entering " + _firstSceneName);
            LoadingScreen.LogLine(UiLogSeverity.Success, "[worldgen] visible projection mounted");
        }

        private void RenderBuildButtons()
        {
            var allSkills = BuildSkillCatalog();

            int classOffset = 0;
            for (int i = 0; i < CharacterCreationCatalog.Classes.Count; i++)
            {
                var klass = CharacterCreationCatalog.Classes[i];
                string slot = "class_button_" + (classOffset + i);
                _dynamicSlots.Add(slot);
                bool selected = string.Equals(_selectedClassId, klass.Id, StringComparison.OrdinalIgnoreCase);
                bool recommended = string.Equals(_suggestedClassId, klass.Id, StringComparison.OrdinalIgnoreCase);
                string label = (selected ? "[X] " : "[ ] ") + klass.Name;
                if (recommended) label += "  (Recommended)";
                label += "\nBest with: " + DominantStats(klass);
                _panel.SetText(slot, label);
                _panel.SetButtonHandler(slot, () => SelectClass(klass.Id));
                _panel.SetVisible(slot, true);
            }

            int alignmentOffset = 0;
            for (int i = 0; i < AlignmentOrder.Length; i++)
            {
                string id = AlignmentOrder[i];
                string slot = "alignment_button_" + (alignmentOffset + i);
                _dynamicSlots.Add(slot);
                bool selected = string.Equals(_selectedAlignmentId, id, StringComparison.OrdinalIgnoreCase);
                string label = (selected ? "[X] " : "[ ] ") + AlignmentName(id);
                _panel.SetText(slot, label);
                _panel.SetButtonHandler(slot, () => SelectAlignment(id));
                _panel.SetVisible(slot, true);
            }

            for (int i = 0; i < allSkills.Count; i++)
            {
                string skill = allSkills[i];
                string slot = "skill_button_" + i;
                _dynamicSlots.Add(slot);
                bool selected = _selectedSkills.Contains(skill);
                string label = (selected ? "[X] " : "[ ] ") + skill + " (" + LinkedAbility(skill) + "-based)";
                _panel.SetText(slot, label);
                _panel.SetButtonHandler(slot, () => ToggleSkill(skill));
                _panel.SetVisible(slot, true);
            }
        }

        private static int SafeStat(IReadOnlyDictionary<string, int> stats, string key)
        {
            return stats.TryGetValue(key, out var value) ? value : 0;
        }

        private string BuildChoiceDescription(CreationChoice choice)
        {
            string topClass = choice.ClassWeights.OrderByDescending(p => p.Value).FirstOrDefault().Key;
            if (string.IsNullOrWhiteSpace(topClass)) return "Shapes your path.";
            return "Leans toward " + ClassName(topClass) + ".";
        }

        private string FormatStat(string stat)
        {
            int value = SafeStat(_assignedStats, stat);
            int modifier = Mathf.FloorToInt((value - 10) / 2f);
            string sign = modifier >= 0 ? "+" : string.Empty;
            return value + " (" + sign + modifier + ")";
        }

        private void ClearDynamicSlots()
        {
            for (int i = 0; i < _dynamicSlots.Count; i++)
                _panel.SetVisible(_dynamicSlots[i], false);
            _dynamicSlots.Clear();
        }

        private static string AlignmentName(string id)
        {
            switch ((id ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "lawful_good": return "Lawful Good";
                case "neutral_good": return "Neutral Good";
                case "chaotic_good": return "Chaotic Good";
                case "lawful_neutral": return "Lawful Neutral";
                case "true_neutral": return "True Neutral";
                case "chaotic_neutral": return "Chaotic Neutral";
                case "lawful_evil": return "Lawful Evil";
                case "neutral_evil": return "Neutral Evil";
                case "chaotic_evil": return "Chaotic Evil";
                default: return "Unaligned";
            }
        }

        private static string ClassName(string classId)
        {
            foreach (var klass in CharacterCreationCatalog.Classes)
            {
                if (string.Equals(klass.Id, classId, StringComparison.OrdinalIgnoreCase))
                    return klass.Name;
            }
            return classId;
        }

        private static string DominantStats(CharacterClass klass)
        {
            var scores = new[]
            {
                new KeyValuePair<string, int>("MIG", klass.PrimaryStats.Mig),
                new KeyValuePair<string, int>("AGI", klass.PrimaryStats.Agi),
                new KeyValuePair<string, int>("END", klass.PrimaryStats.End),
                new KeyValuePair<string, int>("MND", klass.PrimaryStats.Mnd),
                new KeyValuePair<string, int>("INS", klass.PrimaryStats.Ins),
                new KeyValuePair<string, int>("PRE", klass.PrimaryStats.Pre),
            };

            return string.Join(", ", scores.OrderByDescending(v => v.Value).Take(2).Select(v => v.Key));
        }

        private List<string> BuildSkillCatalog()
        {
            var list = new List<string>();
            foreach (var klass in CharacterCreationCatalog.Classes)
            {
                foreach (var skill in klass.MinorSkills)
                {
                    if (list.Contains(skill, StringComparer.OrdinalIgnoreCase)) continue;
                    list.Add(skill);
                }
            }

            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        private static string LinkedAbility(string skill)
        {
            switch ((skill ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "athletics":
                case "intimidation":
                    return "MIG";
                case "stealth":
                case "sleight_of_hand":
                    return "AGI";
                case "survival":
                case "medicine":
                    return "END";
                case "arcana":
                case "history":
                case "religion":
                    return "MND";
                case "investigation":
                case "insight":
                case "perception":
                    return "INS";
                case "persuasion":
                case "deception":
                    return "PRE";
                default:
                    return "INS";
            }
        }
    }
}

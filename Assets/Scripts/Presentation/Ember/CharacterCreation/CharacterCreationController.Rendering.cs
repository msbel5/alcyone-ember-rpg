// Why this file is intentionally long: it contains the UI rendering half of the PRD v2 character-creation partial controller.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Worldgen;
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
            _panel.SetProgress("progress", Mathf.Clamp01((int)_step / 10f));
            _panel.SetText("next", NextButtonText());
            _panel.SetVisible("back", _step != CreationStep.CommanderIdentity);
            _panel.SetVisible("next", _step != CreationStep.Complete);

            ClearDynamicSlots();
            _panel.SetText("body", BuildBodyText());

            // Portrait image + caption are shown from the Portrait stage onward (Portrait, Dossier,
            // Complete) and hidden on every earlier stage. ApplyPortrait fills the actual swatch;
            // Render owns visibility so navigating Back hides the box again. (LEFT-007.)
            // During "World Awakens" the image slot shows the GENERATED CONTINENT (the world the player just
            // shaped); from the dossier onward it shows the character portrait again (the reveal borrowed it).
            if (_step == CreationStep.WorldHistoryReveal && _revealMapTexture != null)
            {
                _panel.SetThumbnail("portrait", _revealMapTexture);
                _panel.SetText("portraitCaption", "The world you have shaped");
                _panel.SetVisible("portrait", true);
                _panel.SetVisible("portraitCaption", true);
            }
            else
            {
                bool showPortrait = _step == CreationStep.Portrait
                    || _step == CreationStep.DossierLaunch
                    || _step == CreationStep.Complete;
                if ((_step == CreationStep.DossierLaunch || _step == CreationStep.Complete) && _characterPortraitTexture != null)
                    _panel.SetThumbnail("portrait", _characterPortraitTexture); // restore the portrait after the reveal showed the map
                _panel.SetVisible("portrait", showPortrait);
                _panel.SetVisible("portraitCaption", showPortrait);
            }

            if (_step == CreationStep.PersonalityQuestions)
                RenderQuestionButtons();
            else if (_step == CreationStep.CommanderIdentity)
                RenderCommanderButtons();
            else if (_step == CreationStep.WorldMood)
                RenderMoodButtons();
            else if (_step == CreationStep.PlayerCalling)
                RenderCallingButtons();
            else if (_step == CreationStep.FateBegins)
                RenderFateButtons();
            else if (_step == CreationStep.Birthsign)
                RenderBirthsignButtons();
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
                case CreationStep.CommanderIdentity: return "Name";
                case CreationStep.WorldMood: return "The World's Mood";
                case CreationStep.PlayerCalling: return "Your Calling";
                case CreationStep.FateBegins: return "Where Fate Begins";
                case CreationStep.PersonalityQuestions: return "Trials of Character";
                case CreationStep.WorldHistoryReveal: return "The World Awakens";
                case CreationStep.Birthsign: return "Birthsign";
                case CreationStep.StatRolling: return "Abilities";
                case CreationStep.BuildSelection: return "Class, Alignment & Skills";
                case CreationStep.Portrait: return "Portrait";
                case CreationStep.DossierLaunch: return "Dossier";
                default: return "Your story begins";
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
                case CreationStep.WorldMood:
                    return "What is the world's mood? Grim, vibrant, haunted — the canvas the Forge will paint on.";
                case CreationStep.PlayerCalling:
                    return "What is your calling? Smith, mage, wanderer — the role fate first knows you by.";
                case CreationStep.FateBegins:
                    return "Where does fate begin? A forge, a tavern, a crossroads at dusk.";
                case CreationStep.PersonalityQuestions:
                    return BuildQuestionBody();
                case CreationStep.WorldHistoryReveal:
                    return BuildHistoryBody();
                case CreationStep.Birthsign:
                    return "Under which sign were you born? It marks your blood with a gift.";
                case CreationStep.StatRolling:
                    return BuildStatsBody();
                case CreationStep.BuildSelection:
                    return BuildSelectionBody();
                case CreationStep.Portrait:
                    return "The Forge paints your likeness. Keep it, or roll the embers again.";
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
            if (!ComputeCanAdvance())
                builder.AppendLine("Continue locked: enter a name with at least 2 characters.");
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
            if (!ComputeCanAdvance())
                builder.AppendLine("Continue locked: keep a roll first.");
            return builder.ToString();
        }

        private string BuildSelectionBody()
        {
            // The three columns below (SINIF / AHLAK / YETENEK) now carry the full picture with
            // [X] selection markers + a live counter, so this header stays to a single line —
            // a longer block used to crowd and overlap the columns at the top of the panel.
            if (!ComputeCanAdvance())
                return "Choose a class, an alignment, and 1-5 skills. Continue unlocks when all three are set.";
            return "Class, alignment and skills set. Press Continue to forge your dossier.";
        }

        private string BuildDossierBody()
        {
            var selectedClass = _selectedClassId.Length == 0 ? CharacterCreationCatalog.GetClass(_suggestedClassId) : CharacterCreationCatalog.GetClass(_selectedClassId);
            var builder = new StringBuilder();
            builder.AppendLine("Dossier Preview");
            builder.AppendLine("Name: " + _commanderName);
            builder.AppendLine("Class: " + selectedClass.Name);
            builder.AppendLine("Birthsign: " + (string.IsNullOrWhiteSpace(_selectedBirthsignId)
                ? "(unchosen)" : CharacterCreationCatalog.GetBirthsign(_selectedBirthsignId).Name));
            builder.AppendLine("Alignment: " + AlignmentName(_selectedAlignmentId));
            if (!string.IsNullOrWhiteSpace(_selectedBackgroundId))
                builder.AppendLine("Background: " + _selectedBackgroundId);
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
                // A. / B. / C. prefix; show only the in-world action. The class weighting
                // stays hidden so the trial infers your path instead of letting you min-max.
                string letter = ((char)('A' + i)).ToString();
                _panel.SetText(slot, letter + ".  " + captured.Text);
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
            LoadingScreen.ShowForContext(new LoadingScreenContext("worldgen", "Entering the World", "generation"));
            var style = MoodToStyle(_worldMood);
            var genre = CallingToGenre(_playerCalling);
            LoadingScreen.LogLine(UiLogSeverity.Info, "[worldgen] seed=" + _seed + " class=" + _selectedClassId
                + " mood=" + _worldMood + " calling=" + _playerCalling + " fate=" + _fateStart
                + " => style=" + style + " genre=" + genre);
            LoadingScreen.SetProgress(0.6f, "Weaving your world from the embers");
            yield return null;

            // DO NOT mount the visible worldgen reveal panel here. It was deliberately removed because it
            // flashed past before the player could read it AND its "where should the commander begin?"
            // start-scene question is NOT wired to the actual start location (it loaded the same scene
            // regardless). f3f6ef28 re-added it via AI drift — this re-removes it. The authoritative world
            // is generated in the TARGET SCENE from EmberWorldGenIntent.Pending. Keep this straight-to-game.
            LoadingScreen.SetProgress(1f, "Entering the generated world");
            yield return new WaitForSecondsRealtime(0.5f);
            // World pivot: ALWAYS enter the runtime-generated world. The _firstSceneName plumbing (and the
            // CharacterCreationUI [SerializeField] baked as "SmithingOverworld", which calls SetStartScene and
            // overrode the controller default) is bypassed here so New Game never lands in a baked
            // vertical-slice scene again — the World Scene Director builds the starting settlement instead.
            UnityEngine.SceneManagement.SceneManager.LoadScene(EmberScenes.GeneratedWorld);
        }

        // ----- World-genesis choice screens (stages 2-4) -----------------------------------
        // Single-focal lists like the birthsign screen. The chosen id feeds worldgen:
        // mood -> WorldStyle, calling -> WorldGenre (MoodToStyle / CallingToGenre below),
        // fate -> start-locale flavor (logged in BeginVisibleWorldgen). Controller defaults
        // keep Continue enabled before a pick, so the flow never locks on these stages.
        private static readonly (string id, string label)[] MoodChoices =
        {
            ("grim", "Grim and unforgiving - a dying age of ash"),
            ("mythic", "Mythic and ancient - the old gods still stir"),
            ("low", "Gritty and low - mud, steel, and rumor"),
            ("heroic", "High and heroic - banners raised against the dark"),
        };

        private static readonly (string id, string label)[] CallingChoices =
        {
            ("survival", "Endure the wilds - survival above all"),
            ("intrigue", "Play the courts - politics and quiet knives"),
            ("hunt", "Hunt what stalks - monsters in the dark"),
            ("merchant", "Build a fortune - trade roads and coin"),
            ("pilgrimage", "Walk the long road - faith, ruin, and relics"),
        };

        private static readonly (string id, string label)[] FateChoices =
        {
            ("forge", "At the forge - hammer, heat, and a trade to your name"),
            ("tavern", "In the tavern - a stranger, a job, and a debt"),
            ("crossroads", "At a crossroads - the open road and no master"),
        };

        private void RenderMoodButtons()
            => RenderGenesisChoices("mood_button_", MoodChoices, SetWorldMood);

        private void RenderCallingButtons()
            => RenderGenesisChoices("calling_button_", CallingChoices, SetPlayerCalling);

        private void RenderFateButtons()
            => RenderGenesisChoices("fate_button_", FateChoices, SetFateBegins);

        // Shared list renderer for the three genesis stages. Click-to-advance like the
        // personality trial: "A./B./C. <label>" rows; picking one sets the value and advances
        // immediately (no [X] marker, no separate Continue press).
        private void RenderGenesisChoices(string prefix, (string id, string label)[] options, Action<string> onPick)
        {
            for (int i = 0; i < options.Length; i++)
            {
                var opt = options[i];
                string slot = prefix + i;
                _dynamicSlots.Add(slot);
                string letter = ((char)('A' + i)).ToString();
                _panel.SetText(slot, letter + ".  " + opt.label);
                _panel.SetButtonHandler(slot, () => onPick(opt.id));
                _panel.SetVisible(slot, true);
            }
        }

        // World-genesis id -> worldgen enums. Unknown / default ids fall to the grim-survival
        // baseline so a never-touched stage still yields a coherent world.
        private static WorldStyle MoodToStyle(string mood)
        {
            return WorldGenesisMapper.ToStyle(mood);
        }

        private static WorldGenre CallingToGenre(string calling)
        {
            return WorldGenesisMapper.ToGenre(string.Empty, calling, string.Empty);
        }

        private void RenderBirthsignButtons()
        {
            var signs = CharacterCreationCatalog.Birthsigns;
            for (int i = 0; i < signs.Count; i++)
            {
                var sign = signs[i];
                string slot = "birthsign_button_" + i;
                _dynamicSlots.Add(slot);
                bool selected = string.Equals(_selectedBirthsignId, sign.Id, StringComparison.OrdinalIgnoreCase);
                _panel.SetText(slot, (selected ? "[X] " : "[ ] ") + sign.Name + "   " + sign.PassiveBonus);
                _panel.SetButtonHandler(slot, () => SelectBirthsign(sign.Id));
                _panel.SetVisible(slot, true);
            }
        }

        private void RenderBuildButtons()
        {
            var allSkills = BuildSkillCatalog();

            // Reveal the three-column build area (hidden on every other step) and label the columns.
            // Adding build_area to the dynamic-slot set means ClearDynamicSlots hides it again when
            // the player navigates to a different step.
            _panel.SetVisible("build_area", true);
            _dynamicSlots.Add("build_area");
            _panel.SetText("class_header", "SINIF");
            _panel.SetText("alignment_header", "AHLAK");
            _panel.SetText("skill_header", "YETENEK (" + _selectedSkills.Count + "/5)");

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

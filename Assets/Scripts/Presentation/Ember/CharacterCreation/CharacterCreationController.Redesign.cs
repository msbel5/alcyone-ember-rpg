using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Simulation.CharacterCreation;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    // Routes EVERY creation step through the redesigned UI-Toolkit view (CharCreationToolkitView), feeding it
    // real catalog data + live state + selection handlers — a 1:1 realization of the Claude Design handoff
    // (Reports/cc-design-ref). When a redesign view exists (the surface is UI Toolkit, the only backend today),
    // Render() delegates here; the legacy string-slot path in .Rendering.cs is the fallback when it does not.
    public sealed partial class CharacterCreationController
    {
        private void RenderRedesign()
        {
            int step = (int)_step;
            switch (_step)
            {
                case CreationStep.CommanderIdentity:
                    _redesignView.RenderName(step, _commanderName,
                        typed => _commanderName = typed ?? string.Empty, canBack: false);
                    break;

                case CreationStep.WorldMood:
                    _redesignView.RenderChoiceList(step, "The World's Mood",
                        "What is the world's mood?",
                        "Grim, vibrant, haunted — the canvas the Forge will paint on.",
                        Labels(MoodChoices), IndexOf(MoodChoices, _worldMood),
                        i => { _worldMood = MoodChoices[i].id; Render(); }, canBack: true);
                    break;

                case CreationStep.PlayerCalling:
                    _redesignView.RenderChoiceList(step, "Your Calling",
                        "What is your calling?",
                        "Smith, mage, wanderer — the role fate first knows you by.",
                        Labels(CallingChoices), IndexOf(CallingChoices, _playerCalling),
                        i => { _playerCalling = CallingChoices[i].id; Render(); }, canBack: true);
                    break;

                case CreationStep.FateBegins:
                    _redesignView.RenderChoiceList(step, "Where Fate Begins",
                        "Where does fate begin?",
                        "A forge, a tavern, a crossroads at dusk.",
                        Labels(FateChoices), IndexOf(FateChoices, _fateStart),
                        i => { _fateStart = FateChoices[i].id; Render(); }, canBack: true);
                    break;

                case CreationStep.PersonalityQuestions:
                    RenderTrialsRedesign(step);
                    break;

                case CreationStep.Birthsign:
                    RenderBirthsignRedesign(step);
                    break;

                case CreationStep.StatRolling:
                    RenderAbilitiesRedesign(step);
                    break;

                case CreationStep.BuildSelection:
                    RenderBuildRedesign(step);
                    break;

                case CreationStep.Portrait:
                    _redesignView.RenderPortrait(step, _characterPortraitTexture != null,
                        _characterPortraitTexture, _portraitRerolls, RerollPortrait);
                    break;

                case CreationStep.WorldHistoryReveal:
                    _redesignView.RenderWorldReveal(step, _revealMapTexture != null, _revealMapTexture,
                        RevealNarrative(), IsHistoryAdvanceUnlocked(),
                        IsHistoryAdvanceUnlocked() ? "Enter the World" : "Skip Reveal");
                    break;

                case CreationStep.DossierLaunch:
                    RenderDossierRedesign(step);
                    break;

                default:
                    _redesignView.SetVisible(false);
                    return;
            }
            _redesignView.SetVisible(true);
        }

        private void RenderTrialsRedesign(int step)
        {
            int total = _questions.Count;
            if (total == 0 || _questionIndex < 0 || _questionIndex >= total)
            {
                _redesignView.SetVisible(false);
                return;
            }
            var q = _questions[_questionIndex];
            var answered = new bool[total];
            for (int i = 0; i < total; i++) answered[i] = i < _answerChoiceIds.Count;

            int selected = -1;
            if (_questionIndex < _answerChoiceIds.Count)
            {
                string ans = _answerChoiceIds[_questionIndex];
                for (int i = 0; i < q.Choices.Count; i++)
                    if (string.Equals(q.Choices[i].Id, ans, StringComparison.OrdinalIgnoreCase)) { selected = i; break; }
            }

            var choices = q.Choices.Select(c => c.Text).ToList();
            _redesignView.RenderTrials(step, _questionIndex, total, answered, q.Prompt, choices, selected,
                i => SelectAnswerByIndex(i),
                i => { _questionIndex = i; Render(); });
        }

        private void RenderBirthsignRedesign(int step)
        {
            var signs = CharacterCreationCatalog.Birthsigns;
            var data = new List<(string, string, int)>(signs.Count);
            int selected = -1;
            for (int i = 0; i < signs.Count; i++)
            {
                var s = signs[i];
                data.Add((s.Name, s.Attribute.ToString().ToUpperInvariant(), s.Delta));
                if (string.Equals(s.Id, _selectedBirthsignId, StringComparison.OrdinalIgnoreCase)) selected = i;
            }
            _redesignView.RenderBirthsign(step, data, selected, i => SelectBirthsign(signs[i].Id));
        }

        private void RenderAbilitiesRedesign(int step)
        {
            var stats = new List<(string, string, int, string)>(StatOrder.Length);
            for (int i = 0; i < StatOrder.Length; i++)
            {
                string id = StatOrder[i];
                int val = SafeStat(_assignedStats, id);
                string roll = i < _lastRolls.Count ? KeptDice(_lastRolls[i]) : string.Empty;
                stats.Add((id, StatFullName(id), val, roll));
            }
            var pool = _activeStats.Values.OrderByDescending(v => v).ToList();
            _redesignView.RenderAbilities(step, pool, stats, _assignedStats.Count > 0, RollAgain, KeepThisRoll);
        }

        private void RenderBuildRedesign(int step)
        {
            var classes = new List<(string, string, bool)>();
            int selClass = -1;
            for (int i = 0; i < CharacterCreationCatalog.Classes.Count; i++)
            {
                var k = CharacterCreationCatalog.Classes[i];
                classes.Add((k.Name, DominantStats(k),
                    string.Equals(k.Id, _suggestedClassId, StringComparison.OrdinalIgnoreCase)));
                if (string.Equals(k.Id, _selectedClassId, StringComparison.OrdinalIgnoreCase)) selClass = i;
            }

            var aligns = new List<string>();
            int selAlign = -1;
            for (int i = 0; i < AlignmentOrder.Length; i++)
            {
                aligns.Add(AlignmentName(AlignmentOrder[i]));
                if (string.Equals(AlignmentOrder[i], _selectedAlignmentId, StringComparison.OrdinalIgnoreCase))
                    selAlign = i;
            }

            var skillIds = BuildSkillCatalog();
            var skills = new List<(string, string)>();
            var selSkills = new HashSet<int>();
            for (int i = 0; i < skillIds.Count; i++)
            {
                skills.Add((Prettify(skillIds[i]), LinkedAbility(skillIds[i])));
                if (_selectedSkills.Contains(skillIds[i])) selSkills.Add(i);
            }

            _redesignView.RenderBuild(step, classes, selClass, aligns, selAlign, skills, selSkills,
                ComputeCanAdvance(),
                i => SelectClass(CharacterCreationCatalog.Classes[i].Id),
                i => SelectAlignment(AlignmentOrder[i]),
                i => ToggleSkill(skillIds[i]));
        }

        private void RenderDossierRedesign(int step)
        {
            string classId = _selectedClassId.Length == 0 ? _suggestedClassId : _selectedClassId;
            string className = ClassName(classId);
            var sign = string.IsNullOrWhiteSpace(_selectedBirthsignId)
                ? null : CharacterCreationCatalog.GetBirthsign(_selectedBirthsignId);
            string signName = sign == null ? "—" : sign.Name;
            string alignName = AlignmentName(_selectedAlignmentId);
            string signLine = signName + " · " + alignName;

            var stats = new List<(string, int)>();
            foreach (var s in StatOrder) stats.Add((s, SafeStat(_assignedStats, s)));

            string birthsignVal = sign == null
                ? "—" : $"{sign.Name}  ({sign.Attribute.ToString().ToUpperInvariant()} +{sign.Delta})";
            string skillsVal = _selectedSkills.Count == 0
                ? "—" : string.Join(", ", _selectedSkills.Select(Prettify));

            var kv = new List<(string, string)>
            {
                ("Class", className),
                ("Birthsign", birthsignVal),
                ("Alignment", alignName),
                ("Skills", skillsVal),
                ("World Mood", LabelOf(MoodChoices, _worldMood)),
                ("Calling", LabelOf(CallingChoices, _playerCalling)),
                ("Fate Began", LabelOf(FateChoices, _fateStart)),
            };

            _redesignView.RenderDossier(step, _commanderName, className, signLine, stats, kv,
                _characterPortraitTexture, Continue);
        }

        private string RevealNarrative()
        {
            string streamed = VisibleHistoryText();
            if (!string.IsNullOrWhiteSpace(streamed)) return streamed;
            return _historyTimeline.Count > 0 ? _historyTimeline[0] : string.Empty;
        }

        // ── mapping helpers ───────────────────────────────────────────────────────────────────────────────────
        private static List<string> Labels((string id, string label)[] options)
            => options.Select(o => EmDash(o.label)).ToList();

        private static int IndexOf((string id, string label)[] options, string id)
        {
            for (int i = 0; i < options.Length; i++)
                if (string.Equals(options[i].id, id, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        private static string LabelOf((string id, string label)[] options, string id)
        {
            int i = IndexOf(options, id);
            return i >= 0 ? EmDash(options[i].label) : "—";
        }

        private static string EmDash(string s) => string.IsNullOrEmpty(s) ? s : s.Replace(" - ", " — ");

        private static string Prettify(string skill) => string.IsNullOrEmpty(skill) ? skill : skill.Replace('_', ' ');

        private static string StatFullName(string id)
        {
            switch (id)
            {
                case "MIG": return "Might";
                case "AGI": return "Agility";
                case "END": return "Endurance";
                case "MND": return "Mind";
                case "INS": return "Insight";
                case "PRE": return "Presence";
                default: return id;
            }
        }

        private static string KeptDice(AttributeRoll roll)
        {
            if (roll == null) return string.Empty;
            var parts = new List<string>(3);
            for (int i = 0; i < roll.Dice.Count; i++)
                if (i != roll.DroppedIndex) parts.Add(roll.Dice[i].ToString());
            return string.Join("+", parts);
        }
    }
}

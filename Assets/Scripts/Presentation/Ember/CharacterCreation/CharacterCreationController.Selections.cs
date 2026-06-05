// E7-014 (LEFT-020): player-choice apply handlers (identity / world-genesis / birthsign / class / alignment / skills / attribute rolls / portrait reroll) split out of CharacterCreationController.cs (partial, zero behaviour change).
using System;
using System.Collections.Generic;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Simulation.CharacterCreation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public sealed partial class CharacterCreationController
    {
        public void SetCommanderIdentity(string commanderName, string worldSeed = null, string profileHint = null)
        {
            _commanderName = (commanderName ?? string.Empty).Trim();
            _worldSeedInput = (worldSeed ?? string.Empty).Trim();
            _profileHint = (profileHint ?? string.Empty).Trim();
            if (uint.TryParse(_worldSeedInput, out var parsedSeed))
            {
                _seed = parsedSeed;
            }

            Render();
        }

        public void SetAdvancedSettingsVisible(bool visible)
        {
            _advancedSettingsVisible = visible;
            Render();
        }

        public void SetAdapterOverride(string adapterId)
        {
            if (string.IsNullOrWhiteSpace(adapterId)) return;
            _adapterId = adapterId.Trim().ToLowerInvariant();
            Render();
        }

        public void SelectSkill(string skill)
        {
            ToggleSkill(skill);
        }

        public void AnswerCurrentQuestion(string choiceId)
        {
            if (_step != CreationStep.PersonalityQuestions) return;
            if (_questionIndex < 0 || _questionIndex >= _questions.Count) return;

            var question = _questions[_questionIndex];
            var choice = question.FindChoice(choiceId);
            if (choice == null) return;

            if (_answerChoiceIds.Count == _questionIndex)
                _answerChoiceIds.Add(choice.Id);
            else if (_answerChoiceIds.Count > _questionIndex)
                _answerChoiceIds[_questionIndex] = choice.Id;

            AddLog("[question] " + question.Id + " -> " + choice.Id + ": " + choice.Text);
            AddLog("[atmosphere] The world listens to your answer...");

            _questionIndex++;
            if (_questionIndex >= _questions.Count)
            {
                _suggestedClassId = _creationService.SuggestClass(_answerChoiceIds).Id;
                // Questions done -> proceed into the character build (Birthsign). The DF-style world
                // GENERATION (BuildHistoryTimeline) now runs later, on entry to WorldHistoryReveal AFTER the
                // portrait and BEFORE the dossier, so the world is generated with the full character in hand.
                EnterStage(CreationStep.Birthsign);
                AddLog("[history] Campaign genesis begins.");
            }

            Render();
        }

        public void SelectAnswerByIndex(int index)
        {
            if (_step != CreationStep.PersonalityQuestions) return;
            if (_questionIndex < 0 || _questionIndex >= _questions.Count) return;
            var question = _questions[_questionIndex];
            if (index < 0 || index >= question.Choices.Count) return;
            AnswerCurrentQuestion(question.Choices[index].Id);
        }

        public List<AttributeRoll> RollAllAttributes()
        {
            var rolls = new List<AttributeRoll>(StatOrder.Length);
            uint rollSeedOffset = (uint)(_rollSerial * 131);
            for (int i = 0; i < StatOrder.Length; i++)
            {
                var roll = AttributeRoller.Roll4d6DropLowest(_seed + rollSeedOffset + (uint)i, StatOrder[i]);
                rolls.Add(roll);
                _activeStats[StatOrder[i]] = roll.Total;
                _assignedStats[StatOrder[i]] = roll.Total;
                AddLog(roll.LogLine);
            }

            _rollSerial++;
            _rollKept = false;
            _lastRolls = rolls;
            _step = CreationStep.StatRolling;
            Render();
            return rolls;
        }

        public void RollAgain()
        {
            if (_step != CreationStep.StatRolling) return;
            AddLog("[roll] Dice tumble through the hall...");
            RollAllAttributes();
        }

        public void KeepThisRoll()
        {
            if (_step != CreationStep.StatRolling) return;
            _savedStats.Clear();
            foreach (var pair in _activeStats)
                _savedStats[pair.Key] = pair.Value;

            _rollKept = true;
            AddLog("[roll] Roll kept. The sigils flare gold.");
            Render();
        }

        public void SwapRoll()
        {
            if (_step != CreationStep.StatRolling) return;
            if (_savedStats.Count == 0) return;

            var snapshot = new Dictionary<string, int>(_activeStats, StringComparer.OrdinalIgnoreCase);
            _activeStats.Clear();
            foreach (var pair in _savedStats)
                _activeStats[pair.Key] = pair.Value;

            _savedStats.Clear();
            foreach (var pair in snapshot)
                _savedStats[pair.Key] = pair.Value;

            foreach (var stat in StatOrder)
            {
                if (_activeStats.TryGetValue(stat, out var value))
                    _assignedStats[stat] = value;
            }

            AddLog("[roll] Active and saved pools swapped.");
            Render();
        }

        public void SwapAssignedStats(string firstStat, string secondStat)
        {
            if (_step != CreationStep.StatRolling) return;
            if (!_assignedStats.ContainsKey(firstStat ?? string.Empty)) return;
            if (!_assignedStats.ContainsKey(secondStat ?? string.Empty)) return;

            var first = _assignedStats[firstStat];
            _assignedStats[firstStat] = _assignedStats[secondStat];
            _assignedStats[secondStat] = first;
            AddLog("[assign] Swapped " + firstStat + " and " + secondStat + ".");
            Render();
        }

        public void ChooseBackground(string background)
        {
            if (!string.IsNullOrWhiteSpace(background))
                _selectedBackgroundId = background.Trim().ToLowerInvariant();
            AddLog("[choice] Background: " + _selectedBackgroundId + ".");
        }

        // World-genesis answers (stages 2-4) — feed WorldgenService via EmberWorldGenIntent.
        public void SetWorldMood(string mood)
        {
            if (_step != CreationStep.WorldMood) return;
            _worldMood = (mood ?? string.Empty).Trim();
            Continue(); // click-to-advance, consistent with the personality trial
        }

        public void SetPlayerCalling(string calling)
        {
            if (_step != CreationStep.PlayerCalling) return;
            _playerCalling = (calling ?? string.Empty).Trim();
            Continue(); // click-to-advance, consistent with the personality trial
        }

        public void SetFateBegins(string start)
        {
            if (_step != CreationStep.FateBegins) return;
            _fateStart = (start ?? string.Empty).Trim();
            Continue(); // click-to-advance, consistent with the personality trial
        }

        // Birthsign is now player-picked (was auto-resolved from seed). Only honored on its stage.
        public void SelectBirthsign(string birthsignId)
        {
            if (_step != CreationStep.Birthsign) return;
            if (string.IsNullOrWhiteSpace(birthsignId)) return;
            foreach (var sign in CharacterCreationCatalog.Birthsigns)
            {
                if (!string.Equals(sign.Id, birthsignId, StringComparison.OrdinalIgnoreCase)) continue;
                _selectedBirthsignId = sign.Id;
                AddLog("[build] Birthsign chosen: " + sign.Name + " (" + sign.PassiveBonus + ").");
                Render();
                return;
            }
        }

        public void SelectClass(string classId)
        {
            if (_step != CreationStep.BuildSelection && _step != CreationStep.DossierLaunch) return;
            if (string.IsNullOrWhiteSpace(classId)) return;

            foreach (var klass in CharacterCreationCatalog.Classes)
            {
                if (!string.Equals(klass.Id, classId, StringComparison.OrdinalIgnoreCase)) continue;
                _selectedClassId = klass.Id;
                if (_selectedSkills.Count == 0)
                {
                    foreach (var skill in klass.MinorSkills)
                    {
                        _selectedSkills.Add(skill);
                        if (_selectedSkills.Count >= 5) break;
                    }
                }
                // Playability: picking a class auto-fills skills but NOT alignment, so the
                // Step 4 gate (class+alignment+skill) stayed locked and the player got stuck
                // after only selecting a class. Default alignment to true_neutral here so one
                // class tap satisfies the gate; the player can still pick a different alignment.
                if (string.IsNullOrWhiteSpace(_selectedAlignmentId))
                    _selectedAlignmentId = "true_neutral";
                AddLog("[build] Class selected: " + klass.Name + ". Alignment defaulted to " + AlignmentName(_selectedAlignmentId) + " (changeable).");
                Render();
                return;
            }
        }

        public void SelectAlignment(string alignmentId)
        {
            if (_step != CreationStep.BuildSelection && _step != CreationStep.DossierLaunch) return;
            if (string.IsNullOrWhiteSpace(alignmentId)) return;
            foreach (var id in AlignmentOrder)
            {
                if (!string.Equals(id, alignmentId, StringComparison.OrdinalIgnoreCase)) continue;
                _selectedAlignmentId = id;
                AddLog("[build] Alignment selected: " + AlignmentName(id) + ".");
                Render();
                return;
            }
        }

        public void ToggleSkill(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId)) return;

            if (_selectedSkills.Contains(skillId))
            {
                _selectedSkills.Remove(skillId);
            }
            else if (_selectedSkills.Count < 5)
            {
                _selectedSkills.Add(skillId);
            }

            if (_panel != null)
                _panel.SetText("skills", "[" + _selectedSkills.Count + "/5] " + string.Join(", ", _selectedSkills));

            if (_step == CreationStep.BuildSelection || _step == CreationStep.DossierLaunch)
                Render();
        }

        public void RerollPortrait()
        {
            // Unlimited rerolls: just advance the count so each reroll varies the portrait seed.
            _portraitRerolls++;
            GeneratePortrait();
        }

        public void LockPortrait()
        {
            _portraitRerolls = 0;
            // Freeze whatever portrait is currently shown: bump the serial so any in-flight
            // off-thread LLM upgrade is discarded when it lands, and stop polling for it.
            _portraitGenSerial++;
            StopPortraitUpgrade();
        }
    }
}

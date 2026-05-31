// E7-014 (LEFT-020): step routing / gate logic + worldgen-launch transition + history-reveal timing split out of CharacterCreationController.cs (partial, zero behaviour change).
using System;
using System.Linq;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Presentation.Ember.UI; // EmberWorldGenIntent lives here (E7-014 fix: was wrongly .Worldgen)
using EmberCrpg.Presentation.Ember.Worldgen;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public sealed partial class CharacterCreationController
    {
        // Advance to a stage and run its on-enter side effects (question index clamp, stat roll,
        // portrait generation). Kept separate from Continue so Back/jumps reuse the same hooks.
        private void EnterStage(CreationStep next)
        {
            _step = next;
            switch (next)
            {
                case CreationStep.PersonalityQuestions:
                    _questionIndex = Mathf.Clamp(_questionIndex, 0, Mathf.Max(0, _questions.Count - 1));
                    break;
                case CreationStep.StatRolling:
                    if (_activeStats.Count == 0) RollAllAttributes();
                    break;
                case CreationStep.Portrait:
                    GeneratePortrait();
                    break;
            }
        }

        public void SkipHistoryReveal()
        {
            _historySkipped = true;
            Render();
        }

        public void BeginYourStory()
        {
            if (_step != CreationStep.DossierLaunch && _step != CreationStep.Complete) return;
            _storyLaunched = true;
            AddLog("[launch] You arrive at Ashford with nothing but your wits and a name that means nothing - yet.");
            // Genesis stages 2-4 author the world via WorldMood/PlayerCalling/FateBegins.
            // Those values flow into EmberWorldGenIntent.Mood/Calling/Start, then EmberWorldGenUI
            // hands them to WorldgenAdapter.SeedWorld (see EmberWorldGenUI line ~143). Earlier the
            // first three args were _profileHint / _selectedClassId / _firstSceneName, which silently
            // discarded the player's genesis answers — fixed here so creation actually authors the
            // world. _profileHint stays in the dossier dump; _firstSceneName still drives scene load.
            EmberWorldGenIntent.Pending = new EmberWorldGenIntent(
                _worldMood,
                _playerCalling,
                _fateStart,
                _commanderName,
                _selectedClassId,
                _selectedBirthsignId,
                _selectedAlignmentId,
                _selectedBackgroundId,
                _selectedSkills.OrderBy(v => v).ToArray(),
                BuildAttributeRollSnapshot(),
                _seed,
                _answerChoiceIds.ToArray(),
                PortraitJson);
            if (AutoLaunchWorldgen && Application.isPlaying)
                StartCoroutine(BeginVisibleWorldgen());
            Render();
        }

        private bool ComputeCanAdvance()
        {
            switch (_step)
            {
                case CreationStep.CommanderIdentity:
                    return _commanderName.Length >= 2;
                case CreationStep.WorldMood:
                    return !string.IsNullOrWhiteSpace(_worldMood);
                case CreationStep.PlayerCalling:
                    return !string.IsNullOrWhiteSpace(_playerCalling);
                case CreationStep.FateBegins:
                    return !string.IsNullOrWhiteSpace(_fateStart);
                case CreationStep.PersonalityQuestions:
                    return false;
                case CreationStep.WorldHistoryReveal:
                    return IsHistoryAdvanceUnlocked();
                case CreationStep.Birthsign:
                    return !string.IsNullOrWhiteSpace(_selectedBirthsignId);
                case CreationStep.StatRolling:
                    return _rollKept;
                case CreationStep.BuildSelection:
                    // 10/10 playability fix: allow 1-5 skills (auto-fill puts 5 on class select;
                    // user toggle previously trapped at 4). Class+alignment still required.
                    return !string.IsNullOrWhiteSpace(_selectedClassId)
                        && !string.IsNullOrWhiteSpace(_selectedAlignmentId)
                        && _selectedSkills.Count >= 1
                        && _selectedSkills.Count <= 5;
                case CreationStep.Portrait:
                    return true; // portrait is optional-confirm; reroll is available but not required
                case CreationStep.DossierLaunch:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsHistoryAdvanceUnlocked()
        {
            if (_historySkipped) return true;
            if (_historyTimeline.Count == 0) return true;
            float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _historyRevealStartTime);
            return elapsed >= 8f || VisibleHistoryCharacterCount(elapsed) >= FullHistoryText().Length;
        }

        private int VisibleHistoryCharacterCount(float elapsed)
        {
            if (_historySkipped) return FullHistoryText().Length;
            if (_historyTimeline.Count == 0) return 0;

            const float charsPerSecond = 30f;
            const float lineDelaySeconds = 0.3f;
            int visibleCharacters = 0;
            for (int i = 0; i < _historyTimeline.Count; i++)
            {
                float lineStart = i * lineDelaySeconds;
                if (elapsed <= lineStart) break;
                float timeForLine = Mathf.Max(0f, elapsed - lineStart);
                string line = _historyTimeline[i];
                int lineChars = Mathf.Min(line.Length, Mathf.FloorToInt(timeForLine * charsPerSecond));
                visibleCharacters += lineChars;
                if (lineChars >= line.Length && i < _historyTimeline.Count - 1) visibleCharacters += 2;
            }
            return Mathf.Min(visibleCharacters, FullHistoryText().Length);
        }

        private string FullHistoryText()
        {
            return string.Join("\n\n", _historyTimeline);
        }

        private string VisibleHistoryText()
        {
            string full = FullHistoryText();
            if (_historySkipped) return full;

            float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _historyRevealStartTime);
            int count = VisibleHistoryCharacterCount(elapsed);
            if (count <= 0) return string.Empty;
            if (count >= full.Length) return full;
            return full.Substring(0, count);
        }

        private void BuildHistoryTimeline()
        {
            _historyTimeline.Clear();
            var score = _creationService.ScoreAnswers(_answerChoiceIds);
            string strongestClass = score.OrderByDescending(p => p.Value).FirstOrDefault().Key ?? "wanderer";
            int strongestWeight = score.TryGetValue(strongestClass, out var weight) ? weight : 0;

            int startYear = 118;
            int span = 1200;
            int events = 30;
            int stepYears = span / events;
            for (int i = 0; i < events; i++)
            {
                int year = startYear + (i * stepYears);
                string headline = HistoricalHeadline(i, strongestClass, strongestWeight);
                string summary = HistoricalSummary(i, strongestClass);
                string tags = HistoricalTags(i, strongestClass);
                _historyTimeline.Add("Year " + year + " - " + headline + "\n" + summary + "\n" + tags);
            }
        }

        private static string HistoricalHeadline(int index, string classId, int weight)
        {
            string[] templates =
            {
                "The Ember Court drafts a brittle treaty",
                "A forgotten observatory reopens under stormlight",
                "Three trade roads collapse into one guarded pass",
                "The old river changes course without warning",
                "The frontier bells ring before dawn",
                "A masked council claims the archive vaults",
            };
            return templates[index % templates.Length] + " (" + classId + " " + Mathf.Max(1, weight) + ")";
        }

        private static string HistoricalSummary(int index, string classId)
        {
            string[] moods = { "solemn", "defiant", "hungry", "watchful", "fervent", "fractured" };
            return "Witnesses describe a " + moods[index % moods.Length] + " age where " + classId + " instincts shaped every oath.";
        }

        private static string HistoricalTags(int index, string classId)
        {
            string[] tags = { "[trade]", "[faith]", "[war]", "[arcana]", "[law]", "[survival]" };
            return tags[index % tags.Length] + " [importance:" + ((index % 5) + 1) + "] [facet:" + classId + "]";
        }
    }
}

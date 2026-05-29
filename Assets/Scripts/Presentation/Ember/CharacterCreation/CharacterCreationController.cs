// Why this file is intentionally long: it owns the PRD v2 character-creation state machine and its visible UI projection.
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
using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Ui.Foundation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public sealed partial class CharacterCreationController : MonoBehaviour
    {
        public enum CreationStep
        {
            // Ember-fit Genesis wizard. Names kept stable (existing switch cases + tests reference
            // them by name); new Ember-unique stages inserted: world-genesis (mood/calling/fate),
            // player-picked Birthsign, and a standalone Portrait stage. Class/Alignment/Skills stay
            // the combined BuildSelection screen for this increment.
            CommanderIdentity = 0,    // 1. Name — "What name will they remember?"
            WorldMood = 1,            // 2. "What is the world's mood?"  (worldgen)
            PlayerCalling = 2,        // 3. "What is the player's calling?" (worldgen)
            FateBegins = 3,           // 4. "Where does fate begin?" (worldgen)
            PersonalityQuestions = 4, // 5. 10 moral dilemmas, one per page
            WorldHistoryReveal = 5,   // 6. DF-style worldgen streaming
            Birthsign = 6,            // 7. pick 1 of 12 (player-picked now)
            StatRolling = 7,          // 8. Abilities — roll + assign the six attributes
            BuildSelection = 8,       // 9. Class / Alignment / Skills (combined 3-column)
            Portrait = 9,             // 10. LLM portrait + reroll
            DossierLaunch = 10,       // 11. Dossier review → Begin Adventure
            Complete = 11,
        }

        private static readonly string[] StatOrder = { "MIG", "AGI", "END", "MND", "INS", "PRE" };
        private static readonly string[] AlignmentOrder =
        {
            "lawful_good", "neutral_good", "chaotic_good",
            "lawful_neutral", "true_neutral", "chaotic_neutral",
            "lawful_evil", "neutral_evil", "chaotic_evil",
        };

        private readonly CharacterCreationService _creationService = new CharacterCreationService();
        private readonly List<CreationQuestion> _questions = new List<CreationQuestion>();
        private readonly List<string> _answerChoiceIds = new List<string>();
        private readonly List<string> _historyTimeline = new List<string>();
        private readonly Dictionary<string, int> _activeStats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _savedStats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _assignedStats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _selectedSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _dynamicSlots = new List<string>();
        private readonly List<string> _logLines = new List<string>();

        private IUiPanel _panel;
        private uint _seed;
        private string _llmJson = string.Empty;
        private CreationStep _step = CreationStep.CommanderIdentity;
        private string _commanderName = string.Empty;
        private string _worldSeedInput = string.Empty;
        private string _profileHint = string.Empty;
        private string _adapterId = "fantasy_ember";
        private bool _advancedSettingsVisible;
        private int _questionIndex;
        private float _historyRevealStartTime;
        private bool _historySkipped;
        private bool _rollKept;
        private int _rollSerial;
        private string _selectedClassId = string.Empty;
        private string _selectedAlignmentId = string.Empty;
        private string _selectedBirthsignId = string.Empty;
        private string _selectedBackgroundId = "smuggler";
        // World-genesis answers (stages 2-4). Defaulted so the flow advances out of the box;
        // the player overrides them on the mood/calling/fate screens. These feed worldgen.
        private string _worldMood = "grim";
        private string _playerCalling = "survival";
        private string _fateStart = "crossroads";
        private string _suggestedClassId = "warrior";
        private string _firstSceneName = "SmithingOverworld";
        private int _rerollsRemaining = 3;
        private bool _storyLaunched;
        private Func<uint, string, string> _portraitJsonProvider;

        public IReadOnlyList<string> LogLines => _logLines;
        public CreationStep CurrentStep => _step;
        public string CommanderName => _commanderName;
        public string AdapterId => _adapterId;
        public string PortraitJson { get; private set; } = string.Empty;
        public bool CanRerollPortrait => _rerollsRemaining > 0;
        public bool CanAdvance => ComputeCanAdvance();
        public bool AutoLaunchWorldgen { get; set; } = true;

        public void SetPortraitJsonProvider(Func<uint, string, string> provider)
        {
            _portraitJsonProvider = provider;
        }

        public static CharacterCreationController CreateForTests(uint seed, string llmJson)
        {
            var go = new GameObject("CharacterCreationControllerTest");
            var controller = go.AddComponent<CharacterCreationController>();
            controller.AutoLaunchWorldgen = false;
            controller.Configure(seed, llmJson);
            return controller;
        }

        public void SetStartScene(string sceneName)
        {
            _firstSceneName = string.IsNullOrWhiteSpace(sceneName) ? "SmithingOverworld" : sceneName;
        }

        public void Configure(uint seed, string llmJson)
        {
            _seed = seed;
            _llmJson = llmJson ?? string.Empty;
            _questions.Clear();
            _questions.AddRange(CharacterCreationCatalog.Questions);
            _answerChoiceIds.Clear();
            _historyTimeline.Clear();
            _activeStats.Clear();
            _savedStats.Clear();
            _assignedStats.Clear();
            _selectedSkills.Clear();
            _dynamicSlots.Clear();
            _logLines.Clear();
            _step = CreationStep.CommanderIdentity;
            _commanderName = string.Empty;
            _worldSeedInput = string.Empty;
            _profileHint = string.Empty;
            _adapterId = "fantasy_ember";
            _advancedSettingsVisible = false;
            _questionIndex = 0;
            _historyRevealStartTime = 0f;
            _historySkipped = false;
            _rollKept = false;
            _rollSerial = 0;
            _selectedClassId = string.Empty;
            _selectedAlignmentId = string.Empty;
            _selectedBirthsignId = ResolveBirthsignId(_seed);
            _selectedBackgroundId = "smuggler";
            _suggestedClassId = "warrior";
            _rerollsRemaining = 3;
            _storyLaunched = false;
            PortraitJson = string.Empty;

            _panel = UiSurfaceLocator.Current?.Mount("CharacterCreation");
            if (_panel != null)
            {
                _panel.SetButtonHandler("next", Continue);
                _panel.SetButtonHandler("back", Back);
            }

            Render();
        }

        private void OnDestroy()
        {
            if (_panel == null) return;
            UiSurfaceLocator.Current?.Unmount(_panel);
            _panel = null;
        }

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

        public void Continue()
        {
            if (_step == CreationStep.WorldHistoryReveal && !IsHistoryAdvanceUnlocked())
            {
                SkipHistoryReveal();
                return;
            }

            if (!CanAdvance) return;

            // Final stage launches the story; everything else advances linearly through the
            // Genesis wizard (data-driven next = +1) with per-stage on-enter side effects.
            if (_step == CreationStep.DossierLaunch)
            {
                BeginYourStory();
                _step = CreationStep.Complete;
                Render();
                return;
            }

            EnterStage((CreationStep)((int)_step + 1));
            Render();
        }

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

        public void Back()
        {
            if (_step == CreationStep.Complete)
            {
                _step = CreationStep.DossierLaunch;
                _storyLaunched = false;
                Render();
                return;
            }

            if (_step == CreationStep.CommanderIdentity) return;

            _step = (CreationStep)((int)_step - 1);
            Render();
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
                BuildHistoryTimeline();
                _suggestedClassId = _creationService.SuggestClass(_answerChoiceIds).Id;
                _step = CreationStep.WorldHistoryReveal;
                _historyRevealStartTime = Time.realtimeSinceStartup;
                _historySkipped = false;
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

        public void SkipHistoryReveal()
        {
            _historySkipped = true;
            Render();
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

        public void BeginYourStory()
        {
            if (_step != CreationStep.DossierLaunch && _step != CreationStep.Complete) return;
            _storyLaunched = true;
            AddLog("[launch] You arrive at Ashford with nothing but your wits and a name that means nothing - yet.");
            EmberWorldGenIntent.Pending = new EmberWorldGenIntent(
                _profileHint,
                _selectedClassId,
                _firstSceneName,
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

        public void RerollPortrait()
        {
            if (_rerollsRemaining <= 0) return;
            _rerollsRemaining--;
            GeneratePortrait();
        }

        public void LockPortrait()
        {
            _rerollsRemaining = 0;
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

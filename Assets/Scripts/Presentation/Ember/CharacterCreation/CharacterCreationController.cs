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
        // LEFT-007: handle to the in-flight off-thread portrait-upgrade coroutine, and a serial
        // that invalidates a stale async result when a newer reroll/lock supersedes it.
        private Coroutine _portraitUpgradeRoutine;
        private int _portraitGenSerial;

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
            StopPortraitUpgrade();
            if (_panel == null) return;
            UiSurfaceLocator.Current?.Unmount(_panel);
            _panel = null;
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

    }
}

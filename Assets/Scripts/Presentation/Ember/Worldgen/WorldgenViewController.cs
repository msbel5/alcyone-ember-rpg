using System.Collections.Generic;
using System.IO;
using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Ui.Foundation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EmberCrpg.Presentation.Ember.Worldgen
{
    public sealed class WorldgenViewController : MonoBehaviour
    {
        private readonly List<string> _logLines = new List<string>();
        private readonly List<string> _failureJsonLines = new List<string>();
        private readonly List<WorldgenVisibleEvent> _events = new List<WorldgenVisibleEvent>();
        private IUiPanel _panel;
        private WorldgenVisibleEvent _pendingQuestion;
        private Coroutine _autoAdvanceRoutine;
        private float _autoAdvanceSeconds;
        private int _nextEventIndex;
        private string _startScene;
        private bool _loadStarted;

        public IReadOnlyList<string> LogLines => _logLines;
        public IReadOnlyList<string> FailureJsonLines => _failureJsonLines;
        public bool QuestionOpen => _pendingQuestion != null;
        public WorldgenVisibleEvent CurrentQuestion => _pendingQuestion;
        public bool AutoAdvance { get; set; }
        public bool AutoScroll { get; set; } = true;
        public bool AutoLoadScene { get; set; } = true;
        public string RequestedScene { get; private set; }
        public bool StartSceneRequested => !string.IsNullOrWhiteSpace(RequestedScene);

        public static WorldgenViewController CreateForTests(string startScene)
        {
            var go = new GameObject("WorldgenViewControllerTest");
            var controller = go.AddComponent<WorldgenViewController>();
            controller.AutoLoadScene = false;
            controller.Configure(startScene);
            return controller;
        }

        public void Configure(string startScene)
        {
            _startScene = string.IsNullOrWhiteSpace(startScene) ? "SmithingOverworld" : startScene;
            _panel = UiSurfaceLocator.Current?.Mount("WorldgenView");
            HideQuestionSlots();
        }

        private void Update()
        {
            TickAutoAdvance(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (_panel == null) return;
            UiSurfaceLocator.Current?.Unmount(_panel);
            _panel = null;
        }

        public void PlayFromGeneratedWorld(GeneratedWorld generatedWorld, WorldgenProjectionOptions options = null)
        {
            Play(WorldgenEventProjector.Project(generatedWorld, options));
        }

        public void Play(IReadOnlyList<WorldgenVisibleEvent> events)
        {
            if (events == null) return;
            _events.Clear();
            _events.AddRange(events);
            _nextEventIndex = 0;
            PumpEventsUntilPause();
        }

        public void AnswerQuestion(int index)
        {
            if (_pendingQuestion == null) return;
            var answer = index >= 0 && index < _pendingQuestion.Options.Count ? _pendingQuestion.Options[index] : string.Empty;
            Add("[choice] " + _pendingQuestion.Id + ": " + answer, UiLogSeverity.Info);
            Add("[modal] QuestionClosed " + _pendingQuestion.Id, UiLogSeverity.Info);
            HideQuestionSlots();
            _pendingQuestion = null;
            _autoAdvanceSeconds = 0f;
            _autoAdvanceRoutine = null;
            PumpEventsUntilPause();
        }

        public void TickAutoAdvance(float deltaSeconds)
        {
            if (!AutoAdvance || _pendingQuestion == null) return;
            _autoAdvanceSeconds += deltaSeconds;
            if (_autoAdvanceSeconds >= 1.5f) AnswerQuestion(0);
        }

        private void Handle(WorldgenVisibleEvent ev)
        {
            if (ev == null) return;
            if (ev.Kind == WorldgenVisibleEventKind.QuestionRaised)
            {
                _pendingQuestion = ev;
                _autoAdvanceSeconds = 0f;
                Add("[modal] QuestionRaised " + ev.Id + ": " + ev.Message, UiLogSeverity.Warning);
                _panel?.SetVisible("question", true);
                _panel?.SetText("question", ev.Message);
                for (int i = 0; i < ev.Options.Count; i++)
                {
                    Add("[option] " + (i + 1) + ". " + ev.Options[i], UiLogSeverity.Info);
                    string slot = "answer" + i;
                    int captured = i;
                    _panel?.SetText(slot, ev.Options[i]);
                    _panel?.SetButtonHandler(slot, () => AnswerQuestion(captured));
                    _panel?.SetVisible(slot, true);
                }
                StartAutoAdvanceIfNeeded();
                return;
            }
            if (ev.Kind == WorldgenVisibleEventKind.Failure)
            {
                AppendFailureJsonLine(ev);
                Add(ev.Message, UiLogSeverity.Error);
                return;
            }
            Add(ev.Message, ev.Kind == WorldgenVisibleEventKind.Completed ? UiLogSeverity.Success : UiLogSeverity.Info);
            if (ev.Kind == WorldgenVisibleEventKind.Completed)
            {
                RequestedScene = _startScene;
                Add("[start-scene-request] " + RequestedScene, UiLogSeverity.Success);
                if (AutoLoadScene && Application.isPlaying && !_loadStarted)
                {
                    _loadStarted = true;
                    StartCoroutine(LoadStartSceneAfterPause());
                }
            }
        }

        private void PumpEventsUntilPause()
        {
            while (_nextEventIndex < _events.Count)
            {
                var ev = _events[_nextEventIndex++];
                Handle(ev);
                if (_pendingQuestion != null) return;
            }
        }

        private void StartAutoAdvanceIfNeeded()
        {
            if (!AutoAdvance || !Application.isPlaying || _autoAdvanceRoutine != null) return;
            _autoAdvanceRoutine = StartCoroutine(AutoAdvanceAfterDelay());
        }

        private System.Collections.IEnumerator AutoAdvanceAfterDelay()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (AutoAdvance && _pendingQuestion != null)
                AnswerQuestion(0);
            _autoAdvanceRoutine = null;
        }

        private System.Collections.IEnumerator LoadStartSceneAfterPause()
        {
            yield return new WaitForSecondsRealtime(1f);
            if (!string.IsNullOrWhiteSpace(RequestedScene))
                SceneManager.LoadScene(RequestedScene);
        }

        public string ExportFailureJsonLines()
        {
            return string.Join("\n", _failureJsonLines);
        }

        private void AppendFailureJsonLine(WorldgenVisibleEvent ev)
        {
            var payload = string.IsNullOrWhiteSpace(ev.PayloadJson)
                ? "{\"id\":\"" + EscapeJson(ev.Id) + "\",\"message\":\"" + EscapeJson(ev.Message) + "\",\"continue\":true}"
                : ev.PayloadJson;
            _failureJsonLines.Add(payload);
            AppendFailureJsonLineToDisk(payload);
            Add("[failure-jsonl] " + payload, UiLogSeverity.Error);
        }

        private void Add(string line, UiLogSeverity severity)
        {
            _logLines.Add(line);
            _panel?.LogLine("log", severity, line);
        }

        private void HideQuestionSlots()
        {
            _panel?.SetVisible("question", false);
            for (int i = 0; i < 8; i++)
                _panel?.SetVisible("answer" + i, false);
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void AppendFailureJsonLineToDisk(string payload)
        {
            try
            {
                var parent = Directory.GetParent(Application.dataPath);
                var root = parent != null ? parent.FullName : Application.dataPath;
                var logDir = Path.Combine(root, "Logs");
                Directory.CreateDirectory(logDir);
                File.AppendAllText(Path.Combine(logDir, "generation-failures.json"), payload + "\n");
            }
            catch
            {
                // Visible worldgen must never abort because failure telemetry could not be persisted.
            }
        }
    }
}

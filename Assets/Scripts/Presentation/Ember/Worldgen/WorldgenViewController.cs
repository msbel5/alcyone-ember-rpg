using System.Collections.Generic;
using EmberCrpg.Ui.Foundation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Worldgen
{
    public sealed class WorldgenViewController : MonoBehaviour
    {
        private readonly List<string> _logLines = new List<string>();
        private IUiPanel _panel;
        private WorldgenVisibleEvent _pendingQuestion;
        private float _autoAdvanceSeconds;
        private string _startScene;

        public IReadOnlyList<string> LogLines => _logLines;
        public bool QuestionOpen => _pendingQuestion != null;
        public bool AutoAdvance { get; set; }
        public string RequestedScene { get; private set; }

        public static WorldgenViewController CreateForTests(string startScene)
        {
            var go = new GameObject("WorldgenViewControllerTest");
            var controller = go.AddComponent<WorldgenViewController>();
            controller.Configure(startScene);
            return controller;
        }

        public void Configure(string startScene)
        {
            _startScene = string.IsNullOrWhiteSpace(startScene) ? "SmithingOverworld" : startScene;
            _panel = UiSurfaceLocator.Current?.Mount("WorldgenView");
        }

        public void Play(IReadOnlyList<WorldgenVisibleEvent> events)
        {
            for (int i = 0; i < events.Count; i++) Handle(events[i]);
        }

        public void AnswerQuestion(int index)
        {
            if (_pendingQuestion == null) return;
            var answer = index >= 0 && index < _pendingQuestion.Options.Count ? _pendingQuestion.Options[index] : string.Empty;
            Add("[choice] " + _pendingQuestion.Id + ": " + answer, UiLogSeverity.Info);
            _pendingQuestion = null;
            _autoAdvanceSeconds = 0f;
        }

        public void TickAutoAdvance(float deltaSeconds)
        {
            if (!AutoAdvance || _pendingQuestion == null) return;
            _autoAdvanceSeconds += deltaSeconds;
            if (_autoAdvanceSeconds >= 1.5f) AnswerQuestion(0);
        }

        private void Handle(WorldgenVisibleEvent ev)
        {
            if (ev.Kind == WorldgenVisibleEventKind.QuestionRaised)
            {
                _pendingQuestion = ev;
                Add("[question] " + ev.Id + ": " + ev.Message, UiLogSeverity.Warning);
                return;
            }
            if (ev.Kind == WorldgenVisibleEventKind.Failure)
            {
                Add(ev.Message, UiLogSeverity.Error);
                return;
            }
            Add(ev.Message, ev.Kind == WorldgenVisibleEventKind.Completed ? UiLogSeverity.Success : UiLogSeverity.Info);
            if (ev.Kind == WorldgenVisibleEventKind.Completed) RequestedScene = _startScene;
        }

        private void Add(string line, UiLogSeverity severity)
        {
            _logLines.Add(line);
            _panel?.LogLine("log", severity, line);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Presentation.Ember.Worldgen;
using EmberCrpg.Simulation.Worldgen;
using EmberCrpg.Tests.PlayMode.Support;
using EmberCrpg.Ui.Foundation;
using NUnit.Framework;

namespace EmberCrpg.Tests.PlayMode.Worldgen
{
    public sealed class WorldgenViewVisibleTest
    {
        [SetUp]
        public void SetUp()
        {
            UiSurfaceLocator.Clear();
            UiSurfaceLocator.Register(new TestUiSurface());
        }

        [TearDown]
        public void TearDown() => UiSurfaceLocator.Clear();

        [Test]
        public void ViewLogsEventsModalChoiceFailureAndCompletion()
        {
            var view = WorldgenViewController.CreateForTests("SmithingOverworld");
            view.Play(WorldgenEventProjector.CreateMockEvents(2, 3, 5, includeQuestion: true, includeFailure: true));
            Assert.That(view.LogLines.Count, Is.GreaterThanOrEqualTo(13));
            Assert.That(view.QuestionOpen, Is.True);
            view.AnswerQuestion(1);
            Assert.That(view.LogLines.Any(l => l.StartsWith("[choice]")), Is.True);
            view.AutoAdvance = true;
            view.Play(new List<WorldgenVisibleEvent> { WorldgenVisibleEvent.Question("q2", "Pick", new[] { "a", "b" }) });
            view.TickAutoAdvance(1.6f);
            Assert.That(view.LogLines.Any(l => l.Contains("q2")), Is.True);
            Assert.That(view.FailureJsonLines.Count, Is.EqualTo(1));
            Assert.That(view.FailureJsonLines[0], Does.Contain("\"continue\":true"));
            Assert.That(view.RequestedScene, Is.EqualTo("SmithingOverworld"));
        }

        [Test]
        public void ViewProjectsGeneratedWorldAndAppendsFailureJsonLinesWhileContinuing()
        {
            var view = WorldgenViewController.CreateForTests("SmithingOverworld");
            view.AutoScroll = false;

            var world = WorldgenService.Generate(42u, WorldgenParameters.Default);
            view.PlayFromGeneratedWorld(
                world,
                new WorldgenProjectionOptions(
                    maxRegions: 2,
                    maxSettlements: 2,
                    maxNpcs: 2,
                    maxHistoryEvents: 3,
                    includeQuestionPrompt: false,
                    includeSyntheticFailure: true));

            Assert.That(view.AutoScroll, Is.False);
            Assert.That(view.FailureJsonLines.Count, Is.EqualTo(1));
            Assert.That(view.LogLines.Any(l => l.StartsWith("[failure-jsonl]")), Is.True);
            Assert.That(view.LogLines.Any(l => l.StartsWith("[done]")), Is.True);
            Assert.That(view.StartSceneRequested, Is.True);
            Assert.That(view.RequestedScene, Is.EqualTo("SmithingOverworld"));
        }
    }
}

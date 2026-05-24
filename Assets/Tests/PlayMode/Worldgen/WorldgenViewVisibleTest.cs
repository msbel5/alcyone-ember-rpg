using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Presentation.Ember.Worldgen;
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
            Assert.That(view.RequestedScene, Is.EqualTo("SmithingOverworld"));
        }
    }
}

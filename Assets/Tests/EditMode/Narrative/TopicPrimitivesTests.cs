using EmberCrpg.Domain.Narrative;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Narrative
{
    /// <summary>Pins Faz 9 Atoms 2-3 narrative primitives: TopicId, TopicDef.</summary>
    public sealed class TopicPrimitivesTests
    {
        // ----- TopicId -----

        [Test]
        public void TopicId_RejectsBlank()
        {
            Assert.Throws<System.ArgumentException>(() => new TopicId(""));
            Assert.Throws<System.ArgumentException>(() => new TopicId("   "));
        }

        [Test]
        public void TopicId_NormalizesToLowerInvariant_Trim()
        {
            Assert.That(new TopicId("  Weather  ").Code, Is.EqualTo("weather"));
            Assert.That(new TopicId("Trade").Code, Is.EqualTo("trade"));
        }

        [Test]
        public void TopicId_EqualityByNormalizedCode()
        {
            var a = new TopicId("Weather");
            var b = new TopicId("weather");
            var c = new TopicId("trade");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(TopicId.Empty.IsEmpty, Is.True);
        }

        // ----- TopicDef -----

        [Test]
        public void TopicDef_HappyPath_StoresFields()
        {
            var topic = new TopicDef(
                new TopicId("weather"),
                promptPhrasing: "the weather",
                gatingPredicateId: "always",
                defaultAnswerTemplateId: "default_weather");

            Assert.That(topic.Id.Code, Is.EqualTo("weather"));
            Assert.That(topic.PromptPhrasing, Is.EqualTo("the weather"));
            Assert.That(topic.GatingPredicateId, Is.EqualTo("always"));
            Assert.That(topic.HasGate, Is.True);
            Assert.That(topic.DefaultAnswerTemplateId, Is.EqualTo("default_weather"));
        }

        [Test]
        public void TopicDef_EmptyGate_IsOptional()
        {
            var topic = new TopicDef(new TopicId("trade"), "trade routes", null, "default_trade");

            Assert.That(topic.GatingPredicateId, Is.EqualTo(string.Empty));
            Assert.That(topic.HasGate, Is.False);
        }

        [Test]
        public void TopicDef_RejectsInvalidInputs()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new TopicDef(default, "prompt", null, "template"));
            Assert.Throws<System.ArgumentException>(() =>
                new TopicDef(new TopicId("ok"), "", null, "template"));
            Assert.Throws<System.ArgumentException>(() =>
                new TopicDef(new TopicId("ok"), "prompt", null, ""));
        }

        [Test]
        public void TopicDef_EqualityByIdOnly()
        {
            var a = new TopicDef(new TopicId("weather"), "p1", null, "t1");
            var b = new TopicDef(new TopicId("weather"), "p2", "gate", "t2");
            var c = new TopicDef(new TopicId("trade"), "p1", null, "t1");

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }
    }
}

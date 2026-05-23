using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Simulation.CharacterCreation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.CharacterCreation
{
    public sealed class CharacterCreationServiceTests
    {
        [Test]
        public void SameAnswers_ReturnSameSuggestedClass()
        {
            var service = new CharacterCreationService();
            var answers = new[] { "a", "c", "a", "b", "c", "a", "b", "c", "a", "a" };

            var first = service.SuggestClass(answers);
            var second = service.SuggestClass(answers);

            Assert.That(first.Id, Is.EqualTo(second.Id));
            Assert.That(first.Id, Is.EqualTo("mage"));
        }

        [Test]
        public void PlayerCanOverrideSuggestedClass()
        {
            var service = new CharacterCreationService();
            var suggested = service.SuggestClass(new[] { "a", "c", "a", "b", "c", "a", "b", "c", "a", "a" });
            var chosen = service.ResolveOverride("wanderer", new[] { "a", "c", "a", "b", "c", "a", "b", "c", "a", "a" });

            Assert.That(suggested.Id, Is.EqualTo("mage"));
            Assert.That(chosen.Id, Is.EqualTo("wanderer"));
        }

        [Test]
        public void EachQuestionChoiceContributesItsWeightVector()
        {
            var service = new CharacterCreationService();
            var scores = service.ScoreAnswers(new[] { "a" });

            var firstChoice = CharacterCreationCatalog.Questions[0].FindChoice("a");
            Assert.That(scores["mage"], Is.EqualTo(firstChoice.WeightFor("mage")));
            Assert.That(scores["scholar"], Is.EqualTo(firstChoice.WeightFor("scholar")));
            Assert.That(scores["warrior"], Is.EqualTo(0));
        }

        [Test]
        public void UnknownChoicesDoNotChangeScores()
        {
            var service = new CharacterCreationService();
            var scores = service.ScoreAnswers(new[] { "z", "z", "z" });

            foreach (var klass in CharacterCreationCatalog.Classes)
                Assert.That(scores[klass.Id], Is.EqualTo(0), klass.Id);
        }

        [Test]
        public void CatalogShipsSixClassesAndTenQuestions()
        {
            Assert.That(CharacterCreationCatalog.Classes.Count, Is.EqualTo(6));
            Assert.That(CharacterCreationCatalog.Questions.Count, Is.EqualTo(10));
        }
    }
}

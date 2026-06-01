using EmberCrpg.Domain.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class GeometricPromptTests
    {
        [Test]
        public void ItemDiePositive_UsesGeometricTerms_AndNoTemplateTokens()
        {
            var composer = new DefaultPromptComposer();
            var positive = composer.ComposePositive(AssetKind.Item, "die");

            Assert.That(positive, Does.Contain("cube"));
            Assert.That(positive, Does.Contain("dot"));
            Assert.That(positive, Does.Not.Contain("pips"));
            Assert.That(positive, Does.Not.Contain("{subject}"));
        }

        [Test]
        public void ItemDieNegative_IncludesAntiPatternTerms()
        {
            var composer = new DefaultPromptComposer();
            var negative = composer.ComposeNegative(AssetKind.Item, "die");

            var hasPile = negative.Contains("pile");
            var hasMany = negative.Contains("many");
            Assert.That(hasPile || hasMany, Is.True);
        }

        [Test]
        public void UnknownObject_FallsBack_WithoutThrowing()
        {
            var composer = new DefaultPromptComposer();

            Assert.DoesNotThrow(() =>
            {
                var positive = composer.ComposePositive(AssetKind.Item, "mystery relic");
                var negative = composer.ComposeNegative(AssetKind.Item, "mystery relic");

                Assert.That(positive, Does.Contain("single mystery relic"));
                Assert.That(negative, Is.Not.Empty);
            });
        }
    }
}

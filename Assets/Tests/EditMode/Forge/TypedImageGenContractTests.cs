using EmberCrpg.Domain.Forge;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Forge
{
    public sealed class TypedImageGenContractTests
    {
        [Test]
        public void KindTemplates_AreDefinedForAllAssetKinds()
        {
            foreach (AssetKind kind in System.Enum.GetValues(typeof(AssetKind)))
            {
                Assert.DoesNotThrow(() => ImageGenKindTemplate.For(kind));
            }

            Assert.That(ImageGenKindTemplate.AllKinds, Has.Count.EqualTo(7));
        }

        [Test]
        public void DefaultFactory_BuildsPortraitSpec_WithReplacedSubjectAndSeed()
        {
            var factory = new DefaultImageGenSpecFactory();
            var spec = factory.Create(AssetKind.Portrait, "a weathered blacksmith", 42u);

            Assert.That(spec.Kind, Is.EqualTo(AssetKind.Portrait));
            Assert.That(spec.Width, Is.EqualTo(512));
            Assert.That(spec.Height, Is.EqualTo(512));
            Assert.That(spec.Steps, Is.GreaterThanOrEqualTo(1));
            Assert.That(spec.Prompt, Does.Contain("weathered blacksmith"));
            Assert.That(spec.Prompt, Does.Not.Contain("{subject}"));
            Assert.That(spec.Seed, Is.EqualTo(42u));
        }

        [Test]
        public void ImageGenSpec_HasReference_FollowsReferenceIdPresence()
        {
            var noReference = new ImageGenSpec(AssetKind.Item, 128, 128, 4, 0f, "prompt", "neg", 1u);
            var withReference = new ImageGenSpec(AssetKind.Item, 128, 128, 4, 0f, "prompt", "neg", 1u, "abc");

            Assert.That(noReference.HasReference, Is.False);
            Assert.That(withReference.HasReference, Is.True);
        }
    }
}

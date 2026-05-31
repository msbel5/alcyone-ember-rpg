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
        public void PortraitTemplate_UsesOneCenteredScaffold_AndExpandedNegativeTokens()
        {
            var template = ImageGenKindTemplate.For(AssetKind.Portrait);
            const string expectedScaffold =
                "a single centered head-and-shoulders portrait of {subject}, one person, facing forward, symmetrical, plain dark studio background, dark fantasy, painterly, sharp focus";

            Assert.That(template.PromptScaffold, Is.EqualTo(expectedScaffold));
            Assert.That(template.NegativePrompt, Does.Contain("multiple"));
            Assert.That(template.NegativePrompt, Does.Contain("group"));
            Assert.That(template.NegativePrompt, Does.Contain("collage"));
            Assert.That(template.NegativePrompt, Does.Contain("tiled"));
            Assert.That(template.NegativePrompt, Does.Contain("grid"));
            Assert.That(template.NegativePrompt, Does.Contain("many objects"));
            Assert.That(template.NegativePrompt, Does.Contain("two heads"));
            Assert.That(template.NegativePrompt, Does.Contain("extra limbs"));
            Assert.That(template.NegativePrompt, Does.Contain("scattered"));
            Assert.That(template.NegativePrompt, Does.Contain("border"));
            Assert.That(template.NegativePrompt, Does.Contain("frame"));
            Assert.That(template.NegativePrompt, Does.Contain("text"));
            Assert.That(template.NegativePrompt, Does.Contain("watermark"));
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

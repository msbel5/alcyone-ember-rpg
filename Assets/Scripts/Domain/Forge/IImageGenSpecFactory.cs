using System;

namespace EmberCrpg.Domain.Forge
{
    public interface IImageGenSpecFactory
    {
        ImageGenSpec Create(AssetKind kind, string subject, uint seed, string referenceImageId = null);
    }

    public sealed class DefaultImageGenSpecFactory : IImageGenSpecFactory
    {
        public ImageGenSpec Create(AssetKind kind, string subject, uint seed, string referenceImageId = null)
        {
            var template = ImageGenKindTemplate.For(kind);
            var trimmedSubject = string.IsNullOrWhiteSpace(subject) ? "figure" : subject.Trim();
            var prompt = template.PromptScaffold.Replace("{subject}", trimmedSubject);

            return new ImageGenSpec(
                kind,
                template.Width,
                template.Height,
                template.Steps,
                template.Guidance,
                prompt,
                template.NegativePrompt,
                seed,
                referenceImageId);
        }
    }
}

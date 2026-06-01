namespace EmberCrpg.Domain.Forge
{
    public interface IImageGenSpecFactory
    {
        ImageGenSpec Create(AssetKind kind, string subject, uint seed, string referenceImageId = null);
    }

    public sealed class DefaultImageGenSpecFactory : IImageGenSpecFactory
    {
        private readonly IPromptComposer _promptComposer;

        public DefaultImageGenSpecFactory(IPromptComposer promptComposer = null)
        {
            _promptComposer = promptComposer ?? new DefaultPromptComposer();
        }

        public ImageGenSpec Create(AssetKind kind, string subject, uint seed, string referenceImageId = null)
        {
            var template = ImageGenKindTemplate.For(kind);
            var prompt = _promptComposer.ComposePositive(kind, subject);
            var negativePrompt = _promptComposer.ComposeNegative(kind, subject);

            return new ImageGenSpec(
                kind,
                template.Width,
                template.Height,
                template.Steps,
                template.Guidance,
                prompt,
                negativePrompt,
                seed,
                referenceImageId);
        }
    }
}

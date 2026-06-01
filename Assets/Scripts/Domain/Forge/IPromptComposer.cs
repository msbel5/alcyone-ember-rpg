using System;

namespace EmberCrpg.Domain.Forge
{
    public interface IPromptComposer
    {
        string ComposePositive(AssetKind kind, string subject);
        string ComposeNegative(AssetKind kind, string subject);
    }

    public sealed class DefaultPromptComposer : IPromptComposer
    {
        private const string ItemStyleSuffix = ", studio product photograph, sharp focus, hard clean edges, plain dark background, dark fantasy";
        private const string PortraitPositiveScaffold =
            "painted CRPG character portrait, 3/4 view shoulders up, single subject centered, fully clothed with visible gear and armor, dark fantasy oil painting, Gerald Brom + Planescape Torment aesthetic, painterly brushwork, dramatic chiaroscuro lighting, muted tavern backdrop, expressive face, production game portrait, {subject}";

        private readonly GeometricPromptCatalog _catalog;

        public DefaultPromptComposer()
            : this(new GeometricPromptCatalog())
        {
        }

        public DefaultPromptComposer(GeometricPromptCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public string ComposePositive(AssetKind kind, string subject)
        {
            var trimmedSubject = string.IsNullOrWhiteSpace(subject) ? "figure" : subject.Trim();
            switch (kind)
            {
                case AssetKind.Item:
                case AssetKind.InventoryIcon:
                    _catalog.TryGet(trimmedSubject, out var itemPositive, out _);
                    return itemPositive + ItemStyleSuffix;

                case AssetKind.Portrait:
                case AssetKind.NpcBillboard:
                    return PortraitPositiveScaffold.Replace("{subject}", trimmedSubject);

                default:
                    var template = ImageGenKindTemplate.For(kind);
                    return template.PromptScaffold.Replace("{subject}", trimmedSubject);
            }
        }

        public string ComposeNegative(AssetKind kind, string subject)
        {
            var shared = ImageGenKindTemplate.For(kind).NegativePrompt;
            switch (kind)
            {
                case AssetKind.Item:
                case AssetKind.InventoryIcon:
                    _catalog.TryGet(subject, out _, out var itemNegative);
                    if (string.IsNullOrWhiteSpace(itemNegative))
                    {
                        return shared;
                    }

                    if (string.IsNullOrWhiteSpace(shared))
                    {
                        return itemNegative;
                    }

                    return itemNegative + ", " + shared;

                default:
                    return shared;
            }
        }
    }
}

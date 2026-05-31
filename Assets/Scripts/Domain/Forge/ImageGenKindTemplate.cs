using System;
using System.Collections.Generic;

namespace EmberCrpg.Domain.Forge
{
    public sealed class ImageGenKindTemplate
    {
        private const int TurboSteps = 4;
        private const float TurboGuidance = 0f;
        private const string SharedNegativePrompt =
            "blurry, lowres, text, watermark, extra limbs, deformed, multiple, group, collage, tiled, grid, many objects, two heads, scattered, border, frame";

        private static readonly Dictionary<AssetKind, ImageGenKindTemplate> Templates =
            new Dictionary<AssetKind, ImageGenKindTemplate>
            {
                [AssetKind.NpcBillboard] = new ImageGenKindTemplate(
                    AssetKind.NpcBillboard,
                    512,
                    768,
                    TurboSteps,
                    TurboGuidance,
                    "full-body {subject}, flat front view, dark fantasy, plain background",
                    SharedNegativePrompt),
                [AssetKind.Portrait] = new ImageGenKindTemplate(
                    AssetKind.Portrait,
                    512,
                    512,
                    TurboSteps,
                    TurboGuidance,
                    "a single centered head-and-shoulders portrait of {subject}, one person, facing forward, symmetrical, plain dark studio background, dark fantasy, painterly, sharp focus",
                    SharedNegativePrompt),
                [AssetKind.Item] = new ImageGenKindTemplate(
                    AssetKind.Item,
                    384,
                    384,
                    TurboSteps,
                    TurboGuidance,
                    "a single {subject}, one object, centered, isolated on a plain flat background, studio product shot, sharp focus, dark fantasy",
                    SharedNegativePrompt),
                [AssetKind.Furniture] = new ImageGenKindTemplate(
                    AssetKind.Furniture,
                    512,
                    512,
                    TurboSteps,
                    TurboGuidance,
                    "a {subject}, dark fantasy, plain background",
                    SharedNegativePrompt),
                [AssetKind.Logo] = new ImageGenKindTemplate(
                    AssetKind.Logo,
                    256,
                    256,
                    TurboSteps,
                    TurboGuidance,
                    "minimal heraldic emblem of {subject}, flat, gold on dark",
                    SharedNegativePrompt),
                [AssetKind.InventoryIcon] = new ImageGenKindTemplate(
                    AssetKind.InventoryIcon,
                    128,
                    128,
                    TurboSteps,
                    TurboGuidance,
                    "inventory icon of {subject}, top-down, plain dark background",
                    SharedNegativePrompt),
                [AssetKind.EnvironmentProp] = new ImageGenKindTemplate(
                    AssetKind.EnvironmentProp,
                    768,
                    512,
                    TurboSteps,
                    TurboGuidance,
                    "{subject} establishing shot, dark fantasy",
                    SharedNegativePrompt),
            };

        private static readonly IReadOnlyList<AssetKind> Kinds = new[]
        {
            AssetKind.NpcBillboard,
            AssetKind.Portrait,
            AssetKind.Item,
            AssetKind.Furniture,
            AssetKind.Logo,
            AssetKind.InventoryIcon,
            AssetKind.EnvironmentProp,
        };

        public ImageGenKindTemplate(
            AssetKind kind,
            int width,
            int height,
            int steps,
            float guidance,
            string promptScaffold,
            string negativePrompt)
        {
            Kind = kind;
            Width = width;
            Height = height;
            Steps = steps;
            Guidance = guidance;
            PromptScaffold = promptScaffold ?? string.Empty;
            NegativePrompt = negativePrompt ?? string.Empty;
        }

        public AssetKind Kind { get; }
        public int Width { get; }
        public int Height { get; }
        public int Steps { get; }
        public float Guidance { get; }
        public string PromptScaffold { get; }
        public string NegativePrompt { get; }

        public static IReadOnlyList<AssetKind> AllKinds => Kinds;

        public static ImageGenKindTemplate For(AssetKind kind)
        {
            return Templates[kind];
        }
    }
}

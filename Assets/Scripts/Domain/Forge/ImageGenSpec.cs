using System;

namespace EmberCrpg.Domain.Forge
{
    public sealed class ImageGenSpec
    {
        public ImageGenSpec(
            AssetKind kind,
            int width,
            int height,
            int steps,
            float guidance,
            string prompt,
            string negativePrompt,
            uint seed,
            string referenceImageId = null)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (steps < 1) throw new ArgumentOutOfRangeException(nameof(steps));

            Kind = kind;
            Width = width;
            Height = height;
            Steps = steps;
            Guidance = guidance;
            Prompt = prompt ?? string.Empty;
            NegativePrompt = negativePrompt ?? string.Empty;
            Seed = seed;
            ReferenceImageId = referenceImageId;
        }

        public AssetKind Kind { get; }
        public int Width { get; }
        public int Height { get; }
        public int Steps { get; }
        public float Guidance { get; }
        public string Prompt { get; }
        public string NegativePrompt { get; }
        public uint Seed { get; }
        public string ReferenceImageId { get; }
        public bool HasReference => !string.IsNullOrEmpty(ReferenceImageId);
    }
}

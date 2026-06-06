using System;

namespace EmberCrpg.Data.GeneratedAssets
{
    [Serializable]
    public sealed class GeneratedSpriteCropResult
    {
        public GeneratedSpriteCropResult(int width, int height, byte[] rgba)
        {
            Width = width;
            Height = height;
            Rgba = rgba ?? throw new ArgumentNullException(nameof(rgba));
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] Rgba { get; }
    }
}

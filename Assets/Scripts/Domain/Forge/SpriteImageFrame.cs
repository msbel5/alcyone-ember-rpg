using System;

namespace EmberCrpg.Domain.Forge
{
    public sealed class SpriteImageFrame
    {
        public SpriteImageFrame(int width, int height, byte[] rgba)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (rgba == null) throw new ArgumentNullException(nameof(rgba));
            if (rgba.Length != width * height * 4) throw new ArgumentException("RGBA buffer size does not match dimensions.", nameof(rgba));

            Width = width;
            Height = height;
            Rgba = (byte[])rgba.Clone();
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] Rgba { get; }
    }
}

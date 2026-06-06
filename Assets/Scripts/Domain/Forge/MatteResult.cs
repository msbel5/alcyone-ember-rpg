using System;

namespace EmberCrpg.Domain.Forge
{
    public sealed class MatteResult
    {
        public MatteResult(int width, int height, byte[] softAlpha)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (softAlpha == null) throw new ArgumentNullException(nameof(softAlpha));
            if (softAlpha.Length != width * height) throw new ArgumentException("Soft alpha size does not match dimensions.", nameof(softAlpha));

            Width = width;
            Height = height;
            SoftAlpha = (byte[])softAlpha.Clone();
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] SoftAlpha { get; }

        public static MatteResult Opaque(int width, int height)
        {
            var alpha = new byte[width * height];
            for (var i = 0; i < alpha.Length; i++) alpha[i] = 255;
            return new MatteResult(width, height, alpha);
        }
    }
}

using System;

namespace EmberCrpg.Data.GeneratedAssets
{
    public static class GeneratedSpriteCropUtility
    {
        public static GeneratedSpriteCropResult CropRgba(int width, int height, byte[] rgba, PixelRect bounds, int padding)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (rgba == null || rgba.Length != width * height * 4) throw new ArgumentException("RGBA buffer size does not match dimensions.", nameof(rgba));

            var x = Math.Max(0, bounds.x - padding);
            var y = Math.Max(0, bounds.y - padding);
            var w = Math.Min(width - x, bounds.width + (padding * 2));
            var h = Math.Min(height - y, bounds.height + (padding * 2));
            var cropped = new byte[w * h * 4];

            for (var row = 0; row < h; row++)
            {
                var sourceIndex = (((y + row) * width) + x) * 4;
                var targetIndex = row * w * 4;
                Buffer.BlockCopy(rgba, sourceIndex, cropped, targetIndex, w * 4);
            }

            return new GeneratedSpriteCropResult(w, h, cropped);
        }
    }
}

using EmberCrpg.Simulation.Worldgen.Planet;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Worldgen
{
    /// <summary>
    /// Presentation-only bridge from deterministic planet pixels to Unity textures. Unity's UI path expects
    /// the opposite row orientation from PlanetImageSampler, so every planet map surface uses this one flip.
    /// </summary>
    public static class PlanetMapTextureFactory
    {
        public static Texture2D Create(PlanetField field, int width, int height, string textureName)
        {
            var image = PlanetImageSampler.Sample(field, width, height);
            var texture = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = string.IsNullOrWhiteSpace(textureName) ? "PlanetMap" : textureName
            };
            texture.LoadRawTextureData(FlipRows(image.Rgba, image.Width, image.Height));
            texture.Apply(false, true);
            return texture;
        }

        private static byte[] FlipRows(byte[] rgba, int width, int height)
        {
            int stride = width * 4;
            var flipped = new byte[rgba.Length];
            for (int row = 0; row < height; row++)
            {
                int src = row * stride;
                int dst = (height - 1 - row) * stride;
                System.Array.Copy(rgba, src, flipped, dst, stride);
            }
            return flipped;
        }
    }
}

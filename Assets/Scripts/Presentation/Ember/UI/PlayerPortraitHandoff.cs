using System;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>
    /// Facade for the cross-scene player portrait handoff.
    /// Character creation owns the Texture2D; gameplay owns the Sprite. This class keeps the
    /// byte contract in one place so no screen guesses generated sprite keys or duplicates PNG IO.
    /// </summary>
    public static class PlayerPortraitHandoff
    {
        public static bool Publish(Texture2D texture)
        {
            if (texture == null)
                return false;

            try
            {
                var png = texture.EncodeToPNG();
                if (png == null || png.Length == 0)
                    return false;

                EmberWorldGenIntent.PlayerPortraitPng = png;
                if (EmberWorldGenIntent.Pending != null)
                    EmberWorldGenIntent.Pending.PortraitPng = png;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[portrait] player portrait handoff failed: " + ex.Message);
                return false;
            }
        }

        public static Sprite TryCreateSprite()
        {
            var png = EmberWorldGenIntent.PlayerPortraitPng;
            if (png == null || png.Length == 0)
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            if (!texture.LoadImage(png))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }
    }
}

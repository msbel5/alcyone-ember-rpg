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
        private static readonly object Gate = new object();
        private static int _version;

        public static int Version
        {
            get
            {
                lock (Gate)
                    return _version;
            }
        }

        public static bool Publish(Texture2D texture)
        {
            if (texture == null)
                return false;

            try
            {
                var png = texture.EncodeToPNG();
                if (png == null || png.Length == 0)
                    return false;

                return PublishPng(png);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[portrait] player portrait handoff failed: " + ex.Message);
                return false;
            }
        }

        public static bool PublishPng(byte[] png)
        {
            if (png == null || png.Length == 0)
                return false;

            var copy = new byte[png.Length];
            Buffer.BlockCopy(png, 0, copy, 0, png.Length);
            lock (Gate)
            {
                EmberWorldGenIntent.PlayerPortraitPng = copy;
                if (EmberWorldGenIntent.Pending != null)
                    EmberWorldGenIntent.Pending.PortraitPng = copy;
                _version++;
            }
            return true;
        }

        public static bool CopyCurrentToPending()
        {
            lock (Gate)
            {
                if (EmberWorldGenIntent.Pending == null)
                    return false;
                var source = EmberWorldGenIntent.PlayerPortraitPng;
                if (source == null || source.Length == 0)
                    return false;

                var copy = new byte[source.Length];
                Buffer.BlockCopy(source, 0, copy, 0, source.Length);
                EmberWorldGenIntent.Pending.PortraitPng = copy;
                return true;
            }
        }

        public static Sprite TryCreateSprite()
        {
            byte[] png;
            lock (Gate)
            {
                var source = EmberWorldGenIntent.PlayerPortraitPng;
                if (source == null || source.Length == 0)
                    return null;
                png = new byte[source.Length];
                Buffer.BlockCopy(source, 0, png, 0, source.Length);
            }
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

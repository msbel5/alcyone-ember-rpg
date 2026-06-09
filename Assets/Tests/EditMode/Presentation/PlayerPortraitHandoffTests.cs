using EmberCrpg.Presentation.Ember.UI;
using NUnit.Framework;

#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace EmberCrpg.Tests.EditMode.Presentation
{
#if UNITY_5_3_OR_NEWER
    public sealed class PlayerPortraitHandoffTests
    {
        [SetUp]
        public void SetUp()
        {
            EmberWorldGenIntent.Pending = null;
            EmberWorldGenIntent.PlayerPortraitPng = null;
        }

        [TearDown]
        public void TearDown()
        {
            EmberWorldGenIntent.Pending = null;
            EmberWorldGenIntent.PlayerPortraitPng = null;
        }

        [Test]
        public void Publish_StoresSessionPng_AndCreatesReadableSprite()
        {
            var texture = Texture(8, 10, new Color32(91, 42, 17, 255));

            Assert.That(PlayerPortraitHandoff.Publish(texture), Is.True);
            Assert.That(EmberWorldGenIntent.PlayerPortraitPng, Is.Not.Null.And.Not.Empty);

            var sprite = PlayerPortraitHandoff.TryCreateSprite();
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture.width, Is.EqualTo(8));
            Assert.That(sprite.texture.height, Is.EqualTo(10));
        }

        [Test]
        public void Publish_AlsoUpdatesPendingIntent_WhenStoryLaunchHasCreatedOne()
        {
            var texture = Texture(6, 6, new Color32(13, 77, 91, 255));
            EmberWorldGenIntent.Pending = new EmberWorldGenIntent("grim", "survival", "forge");

            Assert.That(PlayerPortraitHandoff.Publish(texture), Is.True);

            Assert.That(EmberWorldGenIntent.Pending.PortraitPng, Is.Not.Null.And.Not.Empty);
            Assert.That(EmberWorldGenIntent.Pending.PortraitPng, Is.EqualTo(EmberWorldGenIntent.PlayerPortraitPng));
        }

        [Test]
        public void PublishPng_IncrementsVersion_AndUpdatesSessionBytes()
        {
            var before = PlayerPortraitHandoff.Version;
            var texture = Texture(4, 5, new Color32(160, 20, 42, 255));
            var png = texture.EncodeToPNG();

            Assert.That(PlayerPortraitHandoff.PublishPng(png), Is.True);

            Assert.That(PlayerPortraitHandoff.Version, Is.GreaterThan(before));
            Assert.That(EmberWorldGenIntent.PlayerPortraitPng, Is.Not.SameAs(png));
            Assert.That(EmberWorldGenIntent.PlayerPortraitPng, Is.EqualTo(png));
        }

        [Test]
        public void CopyCurrentToPending_CarriesLatestBytesWithoutRepublishing()
        {
            var texture = Texture(5, 4, new Color32(40, 200, 180, 255));
            var png = texture.EncodeToPNG();

            Assert.That(PlayerPortraitHandoff.PublishPng(png), Is.True);
            EmberWorldGenIntent.Pending = new EmberWorldGenIntent("grim", "survival", "forge");

            Assert.That(PlayerPortraitHandoff.CopyCurrentToPending(), Is.True);

            Assert.That(EmberWorldGenIntent.Pending.PortraitPng, Is.Not.SameAs(EmberWorldGenIntent.PlayerPortraitPng));
            Assert.That(EmberWorldGenIntent.Pending.PortraitPng, Is.EqualTo(EmberWorldGenIntent.PlayerPortraitPng));
        }

        private static Texture2D Texture(int width, int height, Color32 color)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
#else
    public sealed class PlayerPortraitHandoffTests
    {
        [Test]
        public void PlayerPortraitHandoff_UnityTextureCoverage_IsUnityOnly()
        {
            Assert.Pass("Player portrait handoff texture coverage runs in Unity EditMode.");
        }
    }
#endif
}

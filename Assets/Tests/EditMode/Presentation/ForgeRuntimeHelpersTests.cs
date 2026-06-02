#if UNITY_INCLUDE_TESTS
using EmberCrpg.Presentation.Ember.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace EmberCrpg.Tests.EditMode.Presentation
{
    public sealed class ForgeRuntimeHelpersTests
    {
        [Test]
        public void TryDecodeTexture_ReturnsNullForEmptyPayload()
        {
            Assert.That(ForgeRuntimeHelpers.TryDecodeTexture(null), Is.Null);
            Assert.That(ForgeRuntimeHelpers.TryDecodeTexture(System.Array.Empty<byte>()), Is.Null);
        }

        [Test]
        public void TryDecodeTexture_DecodesValidPngBytes()
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.SetPixel(0, 0, Color.red);
            source.Apply();
            var png = source.EncodeToPNG();
            var decoded = ForgeRuntimeHelpers.TryDecodeTexture(png);
            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.width, Is.EqualTo(2));
            Assert.That(decoded.height, Is.EqualTo(2));
        }
    }
}
#endif

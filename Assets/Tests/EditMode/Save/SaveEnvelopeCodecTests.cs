using EmberCrpg.Presentation.Ember.Save;
using NUnit.Framework;
using UnityEngine;

namespace EmberCrpg.Tests.EditMode.Save
{
    public sealed class SaveEnvelopeCodecTests
    {
        [Test]
        public void EncodeDecode_CurrentEnvelope_RoundTrips()
        {
            var source = new SaveData
            {
                sceneName = "GeneratedWorld",
                playerPosition = new Vector3(3f, 4f, 5f),
                playerYaw = 78.5f,
                tickIndex = 123,
                domainStateJson = "{\"schemaVersion\":7,\"totalMinutes\":915}"
            };

            var encoded = SaveEnvelopeCodec.Encode(source);

            Assert.That(encoded, Does.Contain("\"envelopeVersion\":1"));
            Assert.That(SaveEnvelopeCodec.TryDecode(encoded, out var decoded, out var migratedFromLegacy), Is.True);
            Assert.That(migratedFromLegacy, Is.False);
            Assert.That(decoded.sceneName, Is.EqualTo(source.sceneName));
            Assert.That(decoded.playerPosition, Is.EqualTo(source.playerPosition));
            Assert.That(decoded.playerYaw, Is.EqualTo(source.playerYaw));
            Assert.That(decoded.tickIndex, Is.EqualTo(source.tickIndex));
            Assert.That(decoded.domainStateJson, Is.EqualTo(source.domainStateJson));
        }

        [Test]
        public void TryDecode_LegacyPayload_MigratesToCurrentShape()
        {
            var legacy = JsonUtility.ToJson(new SaveData
            {
                sceneName = "GeneratedWorld",
                playerPosition = new Vector3(1f, 2f, 3f),
                playerYaw = 19f,
                tickIndex = 44,
                domainStateJson = "{\"schemaVersion\":2}"
            }, false);

            Assert.That(SaveEnvelopeCodec.TryDecode(legacy, out var decoded, out var migratedFromLegacy), Is.True);
            Assert.That(migratedFromLegacy, Is.True);
            Assert.That(decoded.sceneName, Is.EqualTo("GeneratedWorld"));
            Assert.That(decoded.tickIndex, Is.EqualTo(44));
        }

        [Test]
        public void TryDecode_InvalidEnvelopeOrLegacyPayload_ReturnsFalse()
        {
            Assert.That(SaveEnvelopeCodec.TryDecode("{", out _, out _), Is.False);
            Assert.That(SaveEnvelopeCodec.TryDecode("{\"envelopeVersion\":1,\"payload\":{\"sceneName\":\"\"}}", out _, out _), Is.False);
        }
    }
}

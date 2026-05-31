using EmberCrpg.Data.Save;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Save
{
    public static class SaveEnvelopeCodec
    {
        public static string Encode(SaveData data)
        {
            if (data == null) return string.Empty;

            var envelope = new SaveEnvelope
            {
                envelopeVersion = SaveEnvelope.CurrentVersion,
                payload = new SaveEnvelopePayload
                {
                    sceneName = data.sceneName ?? string.Empty,
                    playerPositionX = data.playerPosition.x,
                    playerPositionY = data.playerPosition.y,
                    playerPositionZ = data.playerPosition.z,
                    playerYaw = data.playerYaw,
                    tickIndex = data.tickIndex,
                    domainStateJson = data.domainStateJson ?? string.Empty,
                }
            };

            return JsonUtility.ToJson(envelope, false);
        }

        public static bool TryDecode(string rawJson, out SaveData data, out bool migratedFromLegacy)
        {
            data = null;
            migratedFromLegacy = false;

            if (TryDecodeCurrentEnvelope(rawJson, out var envelopeData))
            {
                data = envelopeData;
                return true;
            }

            if (TryDecodeLegacyPayload(rawJson, out var legacyData))
            {
                data = legacyData;
                migratedFromLegacy = true;
                return true;
            }

            return false;
        }

        private static bool TryDecodeCurrentEnvelope(string rawJson, out SaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(rawJson)) return false;

            try
            {
                var envelope = JsonUtility.FromJson<SaveEnvelope>(rawJson);
                if (envelope == null) return false;
                if (envelope.envelopeVersion <= 0 || envelope.envelopeVersion > SaveEnvelope.CurrentVersion) return false;
                if (envelope.payload == null || string.IsNullOrWhiteSpace(envelope.payload.sceneName)) return false;

                data = new SaveData
                {
                    sceneName = envelope.payload.sceneName,
                    playerPosition = new Vector3(envelope.payload.playerPositionX, envelope.payload.playerPositionY, envelope.payload.playerPositionZ),
                    playerYaw = envelope.payload.playerYaw,
                    tickIndex = envelope.payload.tickIndex,
                    domainStateJson = envelope.payload.domainStateJson ?? string.Empty,
                };
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private static bool TryDecodeLegacyPayload(string rawJson, out SaveData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(rawJson)) return false;

            try
            {
                var legacy = JsonUtility.FromJson<SaveData>(rawJson);
                if (legacy == null || string.IsNullOrWhiteSpace(legacy.sceneName)) return false;

                data = new SaveData
                {
                    sceneName = legacy.sceneName,
                    playerPosition = legacy.playerPosition,
                    playerYaw = legacy.playerYaw,
                    tickIndex = legacy.tickIndex,
                    domainStateJson = legacy.domainStateJson ?? string.Empty,
                };
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}

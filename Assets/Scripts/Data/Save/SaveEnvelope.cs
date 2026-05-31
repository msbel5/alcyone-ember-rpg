using System;

namespace EmberCrpg.Data.Save
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public const int CurrentVersion = 1;

        public int envelopeVersion;
        public SaveEnvelopePayload payload;
    }

    [Serializable]
    public sealed class SaveEnvelopePayload
    {
        public string sceneName;
        public float playerPositionX;
        public float playerPositionY;
        public float playerPositionZ;
        public float playerYaw;
        public int tickIndex;
        public string domainStateJson;
    }
}

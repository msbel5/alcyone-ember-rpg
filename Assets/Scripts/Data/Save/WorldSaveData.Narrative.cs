using System;

// REF-f (LEFT/48-LOC): Narrative DTOs split out of WorldSaveData.cs (same namespace, zero behaviour change).
namespace EmberCrpg.Data.Save
{
    [Serializable]
    public sealed class WorldEventSaveData
    {
        public long tickMinutes;
        public int kind;
        public long actorId;
        public long siteId;
        public string reason;
        public string[] reasonTrace;
    }

    [Serializable]
    public sealed class ToolCallTraceSaveData
    {
        public long tickMinutes;
        public long siteId;
        public string surfaceCode;
        public string toolCode;
        public ToolCallParameterSaveData[] parameters;
        public bool accepted;
        public string payload;
        public string rejectionReason;
    }

    [Serializable]
    public sealed class ToolCallParameterSaveData
    {
        public string name;
        public string value;
    }

    [Serializable]
    public sealed class LlmProposalLogSaveData
    {
        public long tickMinutes;
        public string providerCode;
        public string conversationId;
        public string responseText;
        public ToolCallTraceSaveData[] acceptedToolCalls;
        public LlmRejectedToolCallSaveData[] rejectedToolCalls;
    }

    [Serializable]
    public sealed class LlmRejectedToolCallSaveData
    {
        public ToolCallTraceSaveData request;
        public string reason;
    }
}

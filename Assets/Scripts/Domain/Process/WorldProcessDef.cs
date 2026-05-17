using System;

namespace EmberCrpg.Domain.Process
{
    /// <summary>Data row for a slow world process such as plant growth or caravan travel.</summary>
    public sealed class WorldProcessDef
    {
        public WorldProcessDef(WorldProcessId id, string displayName, int durationDays, string outputEventReason)
        {
            if (id.IsEmpty)
                throw new ArgumentException("World process id cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("World process display name is required.", nameof(displayName));
            if (durationDays <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationDays), "World process duration must be positive.");
            if (string.IsNullOrWhiteSpace(outputEventReason))
                throw new ArgumentException("World process output event reason is required.", nameof(outputEventReason));

            Id = id;
            DisplayName = displayName.Trim();
            DurationDays = durationDays;
            OutputEventReason = outputEventReason.Trim();
        }

        public WorldProcessId Id { get; }
        public string DisplayName { get; }
        public int DurationDays { get; }
        public string OutputEventReason { get; }
    }
}

using EmberCrpg.Domain.Actors;

namespace EmberCrpg.Presentation.Ember.UI
{
    /// <summary>Read-model for optional navigation help; it reports real world targets, never starts quests.</summary>
    public readonly struct QuestGuidanceRow
    {
        public QuestGuidanceRow(bool hasTarget, string title, string line, string targetName, int distanceTiles, string direction, string unit = "m")
        {
            HasTarget = hasTarget;
            Title = title ?? string.Empty;
            Line = line ?? string.Empty;
            TargetName = targetName ?? string.Empty;
            DistanceTiles = distanceTiles;
            Direction = direction ?? string.Empty;
            Unit = string.IsNullOrEmpty(unit) ? "m" : unit;
        }

        /// <summary>"m" when the target shares the player's settlement (local metres); "tiles" when the
        /// guidance speaks overland — domain site placement is compact, so cross-settlement metre values
        /// would be meaningless.</summary>
        public string Unit { get; }

        public bool HasTarget { get; }
        public string Title { get; }
        public string Line { get; }
        public string TargetName { get; }
        public int DistanceTiles { get; }
        public string Direction { get; }

        public static QuestGuidanceRow None => new QuestGuidanceRow(false, string.Empty, string.Empty, string.Empty, 0, string.Empty);
    }

    public interface IQuestGuidanceSource
    {
        QuestGuidanceRow ReadQuestGuidance();
    }

    public interface IQuestGuidanceTracker
    {
        void UpdateQuestGuidancePlayerLocalPosition(GridPosition localPosition);
    }
}

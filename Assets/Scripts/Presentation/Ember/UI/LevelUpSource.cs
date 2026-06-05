using System.Collections.Generic;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class LevelUpStatRow
    {
        public LevelUpStatRow(string id, string label, int value)
        {
            Id = id ?? string.Empty;
            Label = label ?? string.Empty;
            Value = value;
        }

        public string Id { get; }
        public string Label { get; }
        public int Value { get; }
    }

    public sealed class LevelUpSpellRow
    {
        public LevelUpSpellRow(string templateId, string name, string school, int manaCost, string summary)
        {
            TemplateId = templateId ?? string.Empty;
            Name = name ?? string.Empty;
            School = school ?? string.Empty;
            ManaCost = manaCost;
            Summary = summary ?? string.Empty;
        }

        public string TemplateId { get; }
        public string Name { get; }
        public string School { get; }
        public int ManaCost { get; }
        public string Summary { get; }
    }

    public sealed class LevelUpScreenState
    {
        public LevelUpScreenState(
            string actorName,
            int currentLevel,
            int pointsAvailable,
            IReadOnlyList<LevelUpStatRow> stats,
            IReadOnlyList<LevelUpSpellRow> spellChoices)
        {
            ActorName = actorName ?? string.Empty;
            CurrentLevel = currentLevel < 1 ? 1 : currentLevel;
            PointsAvailable = pointsAvailable < 0 ? 0 : pointsAvailable;
            Stats = stats ?? System.Array.Empty<LevelUpStatRow>();
            SpellChoices = spellChoices ?? System.Array.Empty<LevelUpSpellRow>();
        }

        public string ActorName { get; }
        public int CurrentLevel { get; }
        public int PointsAvailable { get; }
        public IReadOnlyList<LevelUpStatRow> Stats { get; }
        public IReadOnlyList<LevelUpSpellRow> SpellChoices { get; }
    }

    public readonly struct LevelUpSelection
    {
        public LevelUpSelection(int migDelta, int agiDelta, int endDelta, int mndDelta, int insDelta, int preDelta, string selectedSpellId)
        {
            MigDelta = migDelta;
            AgiDelta = agiDelta;
            EndDelta = endDelta;
            MndDelta = mndDelta;
            InsDelta = insDelta;
            PreDelta = preDelta;
            SelectedSpellId = selectedSpellId ?? string.Empty;
        }

        public int MigDelta { get; }
        public int AgiDelta { get; }
        public int EndDelta { get; }
        public int MndDelta { get; }
        public int InsDelta { get; }
        public int PreDelta { get; }
        public string SelectedSpellId { get; }
        public int TotalPoints => MigDelta + AgiDelta + EndDelta + MndDelta + InsDelta + PreDelta;
    }

    public sealed class LevelUpActionResult
    {
        public LevelUpActionResult(bool success, string message)
        {
            Success = success;
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public string Message { get; }
    }

    public interface ILevelUpSource
    {
        LevelUpScreenState ReadLevelUpState();
    }

    public interface ILevelUpCommandSink
    {
        LevelUpActionResult ApplyLevelUp(LevelUpSelection selection);
    }
}

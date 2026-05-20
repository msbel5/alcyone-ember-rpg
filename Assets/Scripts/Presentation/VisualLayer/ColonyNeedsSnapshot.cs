using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Presentation.VisualLayer
{
    /// <summary>
    /// Read-only snapshot of actor needs and mood for Unity debug HUD surfaces.
    /// Pure C#: no UnityEngine, no mutation. Faz 11 Atom 2.
    /// </summary>
    public sealed class ColonyNeedsSnapshot
    {
        private readonly IReadOnlyList<ColonyNeedsRow> _rows;

        public ColonyNeedsSnapshot(IReadOnlyList<ColonyNeedsRow> rows)
        {
            _rows = rows ?? new ColonyNeedsRow[0];
        }

        public IReadOnlyList<ColonyNeedsRow> Rows => _rows;

        public static ColonyNeedsSnapshot FromActors(ActorStore actors)
        {
            var rows = new List<ColonyNeedsRow>();
            if (actors == null)
                return new ColonyNeedsSnapshot(rows);

            foreach (var actor in actors.Records)
            {
                rows.Add(new ColonyNeedsRow(
                    actor.Id,
                    actor.Name ?? string.Empty,
                    actor.Needs.Hunger.Value,
                    actor.Needs.Fatigue.Value,
                    actor.Needs.Thirst.Value,
                    actor.Mood.Value));
            }
            return new ColonyNeedsSnapshot(rows);
        }
    }

    /// <summary>One actor's current need and mood values for HUD display.</summary>
    public readonly struct ColonyNeedsRow
    {
        public ColonyNeedsRow(ActorId actorId, string actorName, int hunger, int fatigue, int thirst, int mood)
        {
            ActorId = actorId;
            ActorName = actorName ?? string.Empty;
            Hunger = hunger;
            Fatigue = fatigue;
            Thirst = thirst;
            Mood = mood;
        }

        public ActorId ActorId { get; }
        public string ActorName { get; }
        public int Hunger { get; }
        public int Fatigue { get; }
        public int Thirst { get; }
        public int Mood { get; }
    }
}

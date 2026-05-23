using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Actors;

namespace EmberCrpg.Domain.CharacterCreation
{
    public sealed class CharacterClass
    {
        public CharacterClass(string id, string name, EmberStatBlock primaryStats, IEnumerable<string> minorSkills, IEnumerable<string> startingEquipment)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Class id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Class name is required.", nameof(name));
            Id = id;
            Name = name;
            PrimaryStats = primaryStats;
            MinorSkills = Wrap(minorSkills);
            StartingEquipment = Wrap(startingEquipment);
        }

        public string Id { get; }
        public string Name { get; }
        public EmberStatBlock PrimaryStats { get; }
        public IReadOnlyList<string> MinorSkills { get; }
        public IReadOnlyList<string> StartingEquipment { get; }

        private static IReadOnlyList<string> Wrap(IEnumerable<string> values)
        {
            var list = new List<string>();
            foreach (var value in values ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(value))
                    list.Add(value);
            }
            return new ReadOnlyCollection<string>(list);
        }
    }

    public sealed class Birthsign
    {
        public Birthsign(string id, string name, EmberAttribute attribute, int delta)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Birthsign id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Birthsign name is required.", nameof(name));
            if (delta == 0) throw new ArgumentOutOfRangeException(nameof(delta), delta, "Birthsign delta cannot be zero.");
            Id = id;
            Name = name;
            Attribute = attribute;
            Delta = delta;
        }

        public string Id { get; }
        public string Name { get; }
        public EmberAttribute Attribute { get; }
        public int Delta { get; }
        public string PassiveBonus => Attribute + (Delta > 0 ? "+" : string.Empty) + Delta;

        public EmberStatBlock ApplyTo(EmberStatBlock stats)
        {
            return new EmberStatBlock(
                Apply(stats.Mig, EmberAttribute.Mig),
                Apply(stats.Agi, EmberAttribute.Agi),
                Apply(stats.End, EmberAttribute.End),
                Apply(stats.Mnd, EmberAttribute.Mnd),
                Apply(stats.Ins, EmberAttribute.Ins),
                Apply(stats.Pre, EmberAttribute.Pre));
        }

        private int Apply(int value, EmberAttribute attribute)
        {
            if (attribute != Attribute) return value;
            int next = value + Delta;
            if (next < EmberStatBlock.MinValue) return EmberStatBlock.MinValue;
            if (next > EmberStatBlock.MaxValue) return EmberStatBlock.MaxValue;
            return next;
        }
    }

    public sealed class CreationChoice
    {
        private readonly IReadOnlyDictionary<string, int> _classWeights;

        public CreationChoice(string id, string text, IReadOnlyDictionary<string, int> classWeights)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Choice id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Choice text is required.", nameof(text));
            if (classWeights == null || classWeights.Count == 0) throw new ArgumentException("Choice weights are required.", nameof(classWeights));
            Id = id;
            Text = text;
            _classWeights = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(classWeights, StringComparer.OrdinalIgnoreCase));
        }

        public string Id { get; }
        public string Text { get; }
        public IReadOnlyDictionary<string, int> ClassWeights => _classWeights;

        public int WeightFor(string classId)
        {
            return !string.IsNullOrWhiteSpace(classId) && _classWeights.TryGetValue(classId, out var value) ? value : 0;
        }
    }

    public sealed class CreationQuestion
    {
        public CreationQuestion(string id, string prompt, IEnumerable<CreationChoice> choices)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Question id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(prompt)) throw new ArgumentException("Question prompt is required.", nameof(prompt));
            var list = new List<CreationChoice>();
            foreach (var choice in choices ?? Array.Empty<CreationChoice>())
                list.Add(choice ?? throw new ArgumentException("Question choices cannot contain null.", nameof(choices)));
            if (list.Count == 0) throw new ArgumentException("Question choices are required.", nameof(choices));
            Id = id;
            Prompt = prompt;
            Choices = new ReadOnlyCollection<CreationChoice>(list);
        }

        public string Id { get; }
        public string Prompt { get; }
        public IReadOnlyList<CreationChoice> Choices { get; }

        public CreationChoice FindChoice(string choiceId)
        {
            foreach (var choice in Choices)
            {
                if (string.Equals(choice.Id, choiceId, StringComparison.OrdinalIgnoreCase))
                    return choice;
            }
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Simulation.CharacterCreation;

namespace EmberCrpg.Presentation.Ember.Adapters
{
    public sealed class CharacterCreationViewModel
    {
        private readonly CharacterCreationService _service = new CharacterCreationService();

        public IReadOnlyList<CharacterCreationBirthsignRow> Birthsigns { get; } =
            CharacterCreationCatalog.Birthsigns
                .Select(sign => new CharacterCreationBirthsignRow(sign.Id, sign.Name, sign.PassiveBonus))
                .ToArray();

        public IReadOnlyList<CharacterCreationQuestionRow> Questions { get; } =
            CharacterCreationCatalog.Questions
                .Select(question => new CharacterCreationQuestionRow(
                    question.Id,
                    question.Prompt,
                    question.Choices
                        .Select(choice => new CharacterCreationChoiceRow(choice.Id, choice.Text))
                        .ToArray()))
                .ToArray();

        public IReadOnlyList<CharacterCreationClassRow> Classes { get; } =
            CharacterCreationCatalog.Classes
                .Select(klass => new CharacterCreationClassRow(klass.Id, klass.Name))
                .ToArray();

        public CharacterCreationClassRow SuggestClass(IReadOnlyList<string> answers)
        {
            var suggested = _service.SuggestClass((answers ?? Array.Empty<string>()).ToArray());
            return new CharacterCreationClassRow(suggested.Id, suggested.Name);
        }
    }

    public readonly struct CharacterCreationBirthsignRow
    {
        public CharacterCreationBirthsignRow(string id, string name, string passiveBonus)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            PassiveBonus = passiveBonus ?? string.Empty;
        }

        public string Id { get; }
        public string Name { get; }
        public string PassiveBonus { get; }
    }

    public readonly struct CharacterCreationQuestionRow
    {
        public CharacterCreationQuestionRow(string id, string prompt, IReadOnlyList<CharacterCreationChoiceRow> choices)
        {
            Id = id ?? string.Empty;
            Prompt = prompt ?? string.Empty;
            Choices = choices ?? Array.Empty<CharacterCreationChoiceRow>();
        }

        public string Id { get; }
        public string Prompt { get; }
        public IReadOnlyList<CharacterCreationChoiceRow> Choices { get; }
    }

    public readonly struct CharacterCreationChoiceRow
    {
        public CharacterCreationChoiceRow(string id, string text)
        {
            Id = id ?? string.Empty;
            Text = text ?? string.Empty;
        }

        public string Id { get; }
        public string Text { get; }
    }

    public readonly struct CharacterCreationClassRow
    {
        public CharacterCreationClassRow(string id, string name)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
        }

        public string Id { get; }
        public string Name { get; }
    }
}

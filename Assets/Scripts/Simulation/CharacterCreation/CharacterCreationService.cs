using System;
using System.Collections.Generic;
using EmberCrpg.Domain.CharacterCreation;

namespace EmberCrpg.Simulation.CharacterCreation
{
    public sealed class CharacterCreationService
    {
        private readonly IReadOnlyList<CharacterClass> _classes;
        private readonly IReadOnlyList<CreationQuestion> _questions;

        public CharacterCreationService(
            IReadOnlyList<CharacterClass> classes = null,
            IReadOnlyList<CreationQuestion> questions = null)
        {
            _classes = classes ?? CharacterCreationCatalog.Classes;
            _questions = questions ?? CharacterCreationCatalog.Questions;
        }

        public CharacterClass SuggestClass(IReadOnlyList<string> answerChoiceIds)
        {
            var scores = ScoreAnswers(answerChoiceIds);
            CharacterClass best = _classes[0];
            int bestScore = int.MinValue;
            foreach (var candidate in _classes)
            {
                int score = scores.TryGetValue(candidate.Id, out var value) ? value : 0;
                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }
            return best;
        }

        public CharacterClass ResolveOverride(string classId, IReadOnlyList<string> answerChoiceIds)
        {
            if (!string.IsNullOrWhiteSpace(classId))
            {
                foreach (var candidate in _classes)
                {
                    if (string.Equals(candidate.Id, classId, StringComparison.OrdinalIgnoreCase))
                        return candidate;
                }
            }
            return SuggestClass(answerChoiceIds);
        }

        public IReadOnlyDictionary<string, int> ScoreAnswers(IReadOnlyList<string> answerChoiceIds)
        {
            var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in _classes)
                scores[c.Id] = 0;

            if (answerChoiceIds == null) return scores;
            int count = Math.Min(answerChoiceIds.Count, _questions.Count);
            for (int i = 0; i < count; i++)
            {
                var choice = _questions[i].FindChoice(answerChoiceIds[i]);
                if (choice == null) continue;
                foreach (var klass in _classes)
                    scores[klass.Id] += choice.WeightFor(klass.Id);
            }

            return scores;
        }
    }
}

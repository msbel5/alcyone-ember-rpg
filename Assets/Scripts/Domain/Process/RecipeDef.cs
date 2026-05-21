using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

// Design note:
// RecipeDef composes the pure PROCESS/MATTER rows landed earlier in Faz 2
// into a deterministic recipe definition. It still does not mutate inventory,
// tick work, allocate item ids, or emit EventLog lines; RecipeSystem owns that
// runtime behavior in the next visible slice.
// Atom-map ref: docs/sprint-faz-2-atom-map.md Recipe definitions sub-area.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Pure recipe definition describing required inputs, produced outputs, worksite, skill, and duration.
    /// </summary>
    public sealed class RecipeDef
    {
        private readonly ReadOnlyCollection<RecipeIngredient> _inputs;
        private readonly ReadOnlyCollection<RecipeOutput> _outputs;

        public RecipeDef(
            RecipeId id,
            string worksiteKind,
            string skillTag,
            int durationTicks,
            IEnumerable<RecipeIngredient> inputs,
            IEnumerable<RecipeOutput> outputs)
        {
            if (id.IsEmpty)
                throw new ArgumentException("Recipe definition id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(worksiteKind))
                throw new ArgumentException("Recipe definition worksite kind is required.", nameof(worksiteKind));
            if (string.IsNullOrWhiteSpace(skillTag))
                throw new ArgumentException("Recipe definition skill tag is required.", nameof(skillTag));
            if (durationTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationTicks), durationTicks, "Recipe definition duration must be positive.");

            Id = id;
            WorksiteKind = worksiteKind.Trim();
            SkillTag = skillTag.Trim();
            DurationTicks = durationTicks;
            _inputs = CopyRequiredRows(inputs, nameof(inputs), "Recipe definition requires at least one input row.");
            _outputs = CopyRequiredRows(outputs, nameof(outputs), "Recipe definition requires at least one output row.");
        }

        /// <summary>
        /// Stable recipe handle used by registries and save/load code.
        /// </summary>
        public RecipeId Id { get; }

        /// <summary>
        /// Deterministic worksite key required by the recipe, for example "furnace".
        /// </summary>
        public string WorksiteKind { get; }

        /// <summary>
        /// Deterministic skill key used by RecipeSystem when quality/progress rules arrive.
        /// </summary>
        public string SkillTag { get; }

        /// <summary>
        /// Positive number of deterministic ticks required to complete one recipe run.
        /// </summary>
        public int DurationTicks { get; }

        /// <summary>
        /// Required input rows. The collection is a defensive, read-only copy of constructor input.
        /// </summary>
        public IReadOnlyList<RecipeIngredient> Inputs
        {
            get { return _inputs; }
        }

        /// <summary>
        /// Produced output rows. The collection is a defensive, read-only copy of constructor input.
        /// </summary>
        public IReadOnlyList<RecipeOutput> Outputs
        {
            get { return _outputs; }
        }

        private static ReadOnlyCollection<T> CopyRequiredRows<T>(IEnumerable<T> rows, string paramName, string emptyMessage) where T : class
        {
            if (rows == null)
                throw new ArgumentNullException(paramName);

            var copy = rows.ToList();
            if (copy.Count == 0)
                throw new ArgumentException(emptyMessage, paramName);
            if (copy.Any(row => row == null))
                throw new ArgumentException("Recipe definition rows cannot contain null entries.", paramName);

            return copy.AsReadOnly();
        }
    }
}

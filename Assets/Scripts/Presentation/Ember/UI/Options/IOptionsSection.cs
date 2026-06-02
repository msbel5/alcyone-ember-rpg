using UnityEngine;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    /// <summary>Pattern: registry/reflection discovery contract. Why: new tabs land as one class, not a host edit.</summary>
    public interface IOptionsSection
    {
        /// <summary>Implementers must have a public parameterless constructor and be marked [Preserve].</summary>
        string Title { get; }

        int Order { get; }

        /// <summary>Build section UI under the supplied mount; the host clears prior content before calling this.</summary>
        void Build(Transform contentMount);
    }
}

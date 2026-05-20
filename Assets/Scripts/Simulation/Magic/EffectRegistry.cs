using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;

namespace EmberCrpg.Simulation.Magic
{
    /// <summary>
    /// Deterministic registry of EffectDefinition rows. Faz 8 Atom 4.
    /// Replaces the Sprint 5 SpellEffectCode switch matrix.
    /// </summary>
    public sealed class EffectRegistry
    {
        private readonly Dictionary<EffectId, EffectDefinition> _byId = new Dictionary<EffectId, EffectDefinition>();
        private readonly List<EffectId> _order = new List<EffectId>();

        public int Count => _byId.Count;

        public void Register(EffectDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (_byId.ContainsKey(definition.Id))
                throw new InvalidOperationException($"EffectRegistry already contains {definition.Id}.");
            _byId.Add(definition.Id, definition);
            _order.Add(definition.Id);
        }

        public bool TryGet(EffectId id, out EffectDefinition definition)
        {
            if (id.IsEmpty) { definition = null; return false; }
            return _byId.TryGetValue(id, out definition);
        }

        public bool Contains(EffectId id) => !id.IsEmpty && _byId.ContainsKey(id);

        public IEnumerable<EffectDefinition> Definitions
        {
            get { foreach (var id in _order) yield return _byId[id]; }
        }
    }
}

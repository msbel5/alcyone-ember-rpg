using System;
using System.Collections.Generic;

// Design note:
// ComponentStore is a narrow deterministic registry for Phase 5 world-process
// components. It mirrors ActorStore/ItemStore insertion-order semantics but
// stays generic so each component type still owns its domain invariants.
namespace EmberCrpg.Domain.World
{
    /// <summary>Dictionary-backed component registry with deterministic insertion-order enumeration.</summary>
    public sealed class ComponentStore<T>
    {
        private readonly Dictionary<WorldComponentId, T> _byId = new Dictionary<WorldComponentId, T>();
        private readonly List<WorldComponentId> _order = new List<WorldComponentId>();

        public int Count { get { return _byId.Count; } }

        public void Add(WorldComponentId id, T component)
        {
            if (id.IsEmpty)
                throw new ArgumentException("WorldComponentId.Empty cannot be stored.", nameof(id));
            if (component == null)
                throw new ArgumentNullException(nameof(component));
            if (_byId.ContainsKey(id))
                throw new InvalidOperationException($"ComponentStore already contains {id}.");

            _byId.Add(id, component);
            _order.Add(id);
        }

        public T Get(WorldComponentId id)
        {
            if (id.IsEmpty)
                throw new ArgumentException("WorldComponentId.Empty cannot be queried.", nameof(id));
            if (!_byId.TryGetValue(id, out var component))
                throw new KeyNotFoundException($"ComponentStore has no component for {id}.");
            return component;
        }

        public bool TryGet(WorldComponentId id, out T component)
        {
            if (id.IsEmpty)
            {
                component = default;
                return false;
            }

            return _byId.TryGetValue(id, out component);
        }

        public bool Contains(WorldComponentId id)
        {
            return !id.IsEmpty && _byId.ContainsKey(id);
        }

        public bool Replace(WorldComponentId id, T component)
        {
            if (id.IsEmpty || component == null || !_byId.ContainsKey(id))
                return false;

            _byId[id] = component;
            return true;
        }

        public bool Remove(WorldComponentId id)
        {
            if (id.IsEmpty)
                return false;
            if (!_byId.Remove(id))
                return false;

            _order.Remove(id);
            return true;
        }

        public IEnumerable<KeyValuePair<WorldComponentId, T>> Rows
        {
            get
            {
                foreach (var id in _order)
                    yield return new KeyValuePair<WorldComponentId, T>(id, _byId[id]);
            }
        }
    }
}

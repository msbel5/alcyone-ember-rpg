using System;
using System.Collections.Generic;
using EmberCrpg.Domain.AiDm;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>
    /// Loads <see cref="ToolDescriptor"/> rows and exposes deterministic lookup
    /// by (surface, id). Pure in-memory; no Unity types. Faz 10 Atom 5.
    /// </summary>
    public sealed class ToolRegistry
    {
        private readonly Dictionary<RegistryKey, ToolDescriptor> _descriptors = new Dictionary<RegistryKey, ToolDescriptor>();
        private readonly List<ToolDescriptor> _order = new List<ToolDescriptor>();

        public int Count => _descriptors.Count;

        /// <summary>Registers a descriptor. Throws on duplicates (same surface + id).</summary>
        public void Register(ToolDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            var key = new RegistryKey(descriptor.Surface, descriptor.Id);
            if (_descriptors.ContainsKey(key))
                throw new InvalidOperationException($"ToolRegistry already contains {descriptor.Surface.Code}/{descriptor.Id.Code}.");
            _descriptors.Add(key, descriptor);
            _order.Add(descriptor);
        }

        /// <summary>Tries to look up a descriptor by surface + id.</summary>
        public bool TryGet(ToolSurfaceKind surface, ToolId id, out ToolDescriptor descriptor)
        {
            if (surface.IsEmpty || id.IsEmpty)
            {
                descriptor = null;
                return false;
            }
            return _descriptors.TryGetValue(new RegistryKey(surface, id), out descriptor);
        }

        /// <summary>True when a descriptor exists for the (surface, id) pair.</summary>
        public bool Contains(ToolSurfaceKind surface, ToolId id)
        {
            return !surface.IsEmpty && !id.IsEmpty && _descriptors.ContainsKey(new RegistryKey(surface, id));
        }

        /// <summary>Descriptors in registration order; deterministic enumeration.</summary>
        public IEnumerable<ToolDescriptor> Descriptors
        {
            get { foreach (var d in _order) yield return d; }
        }

        /// <summary>Returns the descriptors registered for a single surface, in registration order.</summary>
        public IEnumerable<ToolDescriptor> DescriptorsFor(ToolSurfaceKind surface)
        {
            if (surface.IsEmpty) yield break;
            foreach (var d in _order)
            {
                if (d.Surface.Equals(surface))
                    yield return d;
            }
        }

        /// <summary>Composite key (surface, id) for the registry dictionary.</summary>
        private readonly struct RegistryKey : IEquatable<RegistryKey>
        {
            public RegistryKey(ToolSurfaceKind surface, ToolId id)
            {
                Surface = surface;
                Id = id;
            }

            public ToolSurfaceKind Surface { get; }
            public ToolId Id { get; }

            public bool Equals(RegistryKey other) => Surface.Equals(other.Surface) && Id.Equals(other.Id);
            public override bool Equals(object obj) => obj is RegistryKey other && Equals(other);
            public override int GetHashCode() => unchecked((Surface.GetHashCode() * 397) ^ Id.GetHashCode());
        }
    }
}

using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;

namespace EmberCrpg.Simulation.Magic
{
    public delegate int EffectOperationHandler(EffectOperation operation);

    /// <summary>
    /// Dispatch table from EffectOperationKind to a deterministic handler.
    /// Replaces the Sprint 5 SpellEffectKind switch. Faz 8 Atom 5.
    /// </summary>
    public sealed class EffectOperationHandlers
    {
        private readonly Dictionary<string, EffectOperationHandler> _handlers = new Dictionary<string, EffectOperationHandler>();

        public void Register(EffectOperationKind kind, EffectOperationHandler handler)
        {
            if (kind.IsEmpty) throw new ArgumentException("Kind must be non-empty.", nameof(kind));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[kind.Code] = handler;
        }

        public bool TryHandle(EffectOperation operation, out int magnitudeApplied)
        {
            if (_handlers.TryGetValue(operation.Kind.Code, out var handler))
            {
                magnitudeApplied = handler(operation);
                return true;
            }
            magnitudeApplied = 0;
            return false;
        }

        public bool HasHandler(EffectOperationKind kind) => !kind.IsEmpty && _handlers.ContainsKey(kind.Code);
    }
}

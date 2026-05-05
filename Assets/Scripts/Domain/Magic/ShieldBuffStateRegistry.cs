using System;
using System.Collections.Generic;

// Design note:
// ShieldBuffStateRegistry is the deterministic Sprint 5 actor-keyed registry of per-actor
// ShieldBuffState bags. Inputs: stable actor ids that the wider simulation already uses.
// Outputs: a mutable pure-Domain container that lazily owns one ShieldBuffState per actor,
// so the next slice (actor-keyed shield wiring) and the future damage-absorption slice can
// look up the right buff bag for an actor without ShieldBuffState itself growing an actor
// concept. This is the foundation slice: it stores and exposes per-actor state but does not
// apply, tick, save, or resolve buffs against combat. ShieldBuffService and the JSON save
// layer continue to consume a single ShieldBuffState directly until later slices.
// Bible reference: EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §15 Magic effects.
namespace EmberCrpg.Domain.Magic
{
    /// <summary>Mutable per-actor registry of timed shield-buff state, keyed by stable actor ids.</summary>
    public sealed class ShieldBuffStateRegistry
    {
        private readonly Dictionary<string, ShieldBuffState> _shieldBuffStateByActorId =
            new Dictionary<string, ShieldBuffState>(StringComparer.Ordinal);

        public bool HasState(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
                return false;

            return _shieldBuffStateByActorId.ContainsKey(actorId);
        }

        public ShieldBuffState GetOrCreate(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
                throw new ArgumentException("Actor id must be a non-empty stable id.", nameof(actorId));

            if (!_shieldBuffStateByActorId.TryGetValue(actorId, out var shieldBuffState))
            {
                shieldBuffState = new ShieldBuffState();
                _shieldBuffStateByActorId.Add(actorId, shieldBuffState);
            }

            return shieldBuffState;
        }

        public ShieldBuffState GetOrNull(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
                return null;

            return _shieldBuffStateByActorId.TryGetValue(actorId, out var shieldBuffState)
                ? shieldBuffState
                : null;
        }

        public IReadOnlyList<string> GetTrackedActorIds()
        {
            var trackedActorIds = new List<string>(_shieldBuffStateByActorId.Count);
            foreach (var actorId in _shieldBuffStateByActorId.Keys)
            {
                trackedActorIds.Add(actorId);
            }

            return trackedActorIds;
        }

        public void Remove(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
                return;

            _shieldBuffStateByActorId.Remove(actorId);
        }
    }
}

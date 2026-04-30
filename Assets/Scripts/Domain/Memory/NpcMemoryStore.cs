using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;

// Design note:
// NpcMemoryStore owns the slice's per-NPC memory map without leaking dictionary/engine details.
// Inputs: NPC ids and memory snapshots.
// Outputs: stable ActorMemory lookup for simulation services, save/load, and DM queries.
// Bible reference: ARCHITECTURE.md §1.4 ActorMemory, §4 save model.
namespace EmberCrpg.Domain.Memory
{
    /// <summary>Pure collection of persistent NPC memories.</summary>
    public sealed class NpcMemoryStore
    {
        private readonly List<ActorMemory> _entries = new List<ActorMemory>();

        public NpcMemoryStore()
        {
        }

        public NpcMemoryStore(IEnumerable<ActorId> ownerIds)
        {
            RegisterRange(ownerIds);
        }

        public IReadOnlyList<ActorMemory> Entries => _entries;

        public ActorMemory GetOrCreate(ActorId ownerId)
        {
            ActorMemory memory;
            if (TryGet(ownerId, out memory))
                return memory;

            memory = new ActorMemory(ownerId);
            _entries.Add(memory);
            return memory;
        }

        public bool TryGet(ActorId ownerId, out ActorMemory memory)
        {
            memory = _entries.FirstOrDefault(entry => entry.OwnerId == ownerId);
            return memory != null;
        }

        public void RegisterRange(IEnumerable<ActorId> ownerIds)
        {
            if (ownerIds == null)
                return;

            foreach (var ownerId in ownerIds)
                GetOrCreate(ownerId);
        }

        public void Replace(IEnumerable<ActorMemory> memories)
        {
            _entries.Clear();
            if (memories == null)
                return;

            foreach (var memory in memories)
            {
                var copy = new ActorMemory(memory.OwnerId);
                copy.Replace(memory.Events, memory.DialogueSeen);
                _entries.Add(copy);
            }
        }
    }
}

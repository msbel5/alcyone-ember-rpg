using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Core;

// Design note:
// NpcMemoryStore owns the deterministic collection of per-NPC ActorMemory objects.
// Inputs: stable NPC actor ids and memory replacement during save/load.
// Outputs: sorted saveable memory records and lazy lookup for simulation services.
// Bible reference: ARCHITECTURE.md GetNpcMemory(npc) and ActorMemory serialization.
namespace EmberCrpg.Domain.Memory
{
    /// <summary>World-level lookup for persistent NPC memory.</summary>
    public sealed class NpcMemoryStore
    {
        private readonly Dictionary<ulong, ActorMemory> _memories = new Dictionary<ulong, ActorMemory>();

        public IReadOnlyCollection<ActorMemory> Memories => _memories.Values;

        public ActorMemory GetOrCreate(ActorId actorId)
        {
            if (!_memories.TryGetValue(actorId.Value, out var memory))
            {
                memory = new ActorMemory(actorId);
                _memories.Add(actorId.Value, memory);
            }

            return memory;
        }

        public bool TryGet(ActorId actorId, out ActorMemory memory)
        {
            return _memories.TryGetValue(actorId.Value, out memory);
        }

        public IReadOnlyList<ActorMemory> GetAllSorted()
        {
            return _memories.Values.OrderBy(memory => memory.ActorId.Value).ToArray();
        }

        public void ReplaceAll(IEnumerable<ActorMemory> memories)
        {
            _memories.Clear();
            if (memories == null)
                return;

            foreach (var memory in memories)
            {
                if (memory != null)
                    _memories[memory.ActorId.Value] = memory;
            }
        }
    }
}

using System;

// Design note:
// ItemInstanceSequence deterministically mints unique item ids inside one world snapshot.
// Inputs: one world seed/scope plus the next sequence cursor.
// Outputs: stable ItemId values that survive save/load when the cursor is persisted.
// Bible reference: ARCHITECTURE.md Part 2 UID-keyed items, Sprint 3 identity hardening.
namespace EmberCrpg.Domain.Core
{
    /// <summary>Deterministic item-id cursor for the vertical slice.</summary>
    public sealed class ItemInstanceSequence
    {
        private readonly uint _scope;
        private uint _nextValue;

        public ItemInstanceSequence(int scopeSeed, uint nextValue = 1)
        {
            _scope = unchecked((uint)scopeSeed);
            _nextValue = nextValue == 0 ? 1u : nextValue;
        }

        public int ScopeSeed => unchecked((int)_scope);
        public uint NextValue => _nextValue;

        public ItemId TakeNext()
        {
            var id = new ItemId(((ulong)_scope << 32) | _nextValue);
            _nextValue += 1u;
            return id;
        }

        public ItemInstanceSequence Clone()
        {
            return new ItemInstanceSequence(ScopeSeed, _nextValue);
        }
    }
}

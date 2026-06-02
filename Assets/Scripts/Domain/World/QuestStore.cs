using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Quest;

namespace EmberCrpg.Domain.World
{
    /// <summary>Dictionary-backed registry of active quest runtime states keyed by <see cref="QuestId"/>.</summary>
    public sealed class QuestStore
    {
        private readonly Dictionary<QuestId, QuestState> _byId = new Dictionary<QuestId, QuestState>();
        private readonly List<QuestId> _order = new List<QuestId>();

        public int Count => _byId.Count;

        public void Add(QuestId id, QuestState state)
        {
            if (id.IsEmpty)
                throw new ArgumentException("QuestId.Empty cannot be stored.", nameof(id));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (_byId.ContainsKey(id))
                throw new InvalidOperationException($"QuestStore already contains {id}.");

            _byId.Add(id, state);
            _order.Add(id);
        }

        public bool Contains(QuestId id)
        {
            return !id.IsEmpty && _byId.ContainsKey(id);
        }

        public bool TryGet(QuestId id, out QuestState state)
        {
            if (id.IsEmpty)
            {
                state = null;
                return false;
            }

            return _byId.TryGetValue(id, out state);
        }

        public void Clear()
        {
            _byId.Clear();
            _order.Clear();
        }

        public IEnumerable<KeyValuePair<QuestId, QuestState>> Active
        {
            get
            {
                foreach (var id in _order)
                    yield return new KeyValuePair<QuestId, QuestState>(id, _byId[id]);
            }
        }
    }
}

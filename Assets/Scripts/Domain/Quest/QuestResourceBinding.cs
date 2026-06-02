using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Core;

// Design note:
// QuestResourceBinding keeps DFU-style symbol resources mapped onto Ember ids without leaking store mutation.
// Pattern: Resource binding map with a tagged union payload.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>Immutable symbol map over actor/site/item quest resources.</summary>
    public sealed class QuestResourceBinding
    {
        private readonly Dictionary<string, QuestResourceValue> _bindings = new Dictionary<string, QuestResourceValue>(StringComparer.Ordinal);
        private readonly ReadOnlyDictionary<string, QuestResourceValue> _bindingsView;

        public QuestResourceBinding(IEnumerable<KeyValuePair<string, QuestResourceValue>> bindings)
        {
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));

            foreach (var pair in bindings)
            {
                var symbol = NormalizeSymbol(pair.Key);
                if (_bindings.ContainsKey(symbol))
                    throw new ArgumentException($"Duplicate quest resource symbol '{symbol}'.", nameof(bindings));
                _bindings.Add(symbol, pair.Value);
            }

            _bindingsView = new ReadOnlyDictionary<string, QuestResourceValue>(_bindings);
        }

        /// <summary>Read-only symbol map in deterministic ordinal-key form.</summary>
        public IReadOnlyDictionary<string, QuestResourceValue> Bindings
        {
            get { return _bindingsView; }
        }

        /// <summary>Tries to resolve an actor resource by quest symbol.</summary>
        public bool TryGetActor(string symbol, out ActorId actorId)
        {
            if (TryGetValue(symbol, QuestResourceKind.Person, out var value))
            {
                actorId = value.ActorId;
                return true;
            }

            actorId = default;
            return false;
        }

        /// <summary>Tries to resolve a site resource by quest symbol.</summary>
        public bool TryGetSite(string symbol, out SiteId siteId)
        {
            if (TryGetValue(symbol, QuestResourceKind.Place, out var value))
            {
                siteId = value.SiteId;
                return true;
            }

            siteId = default;
            return false;
        }

        /// <summary>Tries to resolve an item resource by quest symbol.</summary>
        public bool TryGetItem(string symbol, out ItemId itemId)
        {
            if (TryGetValue(symbol, QuestResourceKind.Item, out var value))
            {
                itemId = value.ItemId;
                return true;
            }

            itemId = default;
            return false;
        }

        private bool TryGetValue(string symbol, QuestResourceKind expectedKind, out QuestResourceValue value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(symbol))
                return false;
            if (!_bindings.TryGetValue(symbol.Trim(), out value))
                return false;
            return value.Kind == expectedKind;
        }

        private static string NormalizeSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Quest resource symbol is required.", nameof(symbol));

            return symbol.Trim();
        }
    }

    /// <summary>Tagged union over the valid deterministic quest resource id types.</summary>
    public readonly struct QuestResourceValue : IEquatable<QuestResourceValue>
    {
        private QuestResourceValue(QuestResourceKind kind, ActorId actorId, SiteId siteId, ItemId itemId)
        {
            Kind = kind;
            ActorId = actorId;
            SiteId = siteId;
            ItemId = itemId;
        }

        /// <summary>Kind discriminator for the bound resource id.</summary>
        public QuestResourceKind Kind { get; }
        /// <summary>Actor resource id when <see cref="Kind"/> is <see cref="QuestResourceKind.Person"/>.</summary>
        public ActorId ActorId { get; }
        /// <summary>Site resource id when <see cref="Kind"/> is <see cref="QuestResourceKind.Place"/>.</summary>
        public SiteId SiteId { get; }
        /// <summary>Item resource id when <see cref="Kind"/> is <see cref="QuestResourceKind.Item"/>.</summary>
        public ItemId ItemId { get; }

        /// <summary>Creates a person binding payload.</summary>
        public static QuestResourceValue Person(ActorId actorId)
        {
            if (actorId.IsEmpty)
                throw new ArgumentException("ActorId.Empty cannot back a quest person binding.", nameof(actorId));
            return new QuestResourceValue(QuestResourceKind.Person, actorId, default, default);
        }

        /// <summary>Creates a place binding payload.</summary>
        public static QuestResourceValue Place(SiteId siteId)
        {
            if (siteId.IsEmpty)
                throw new ArgumentException("SiteId.Empty cannot back a quest place binding.", nameof(siteId));
            return new QuestResourceValue(QuestResourceKind.Place, default, siteId, default);
        }

        /// <summary>Creates an item binding payload.</summary>
        public static QuestResourceValue Item(ItemId itemId)
        {
            if (itemId.IsEmpty)
                throw new ArgumentException("ItemId.Empty cannot back a quest item binding.", nameof(itemId));
            return new QuestResourceValue(QuestResourceKind.Item, default, default, itemId);
        }

        /// <summary>Returns true when both tagged values carry the same kind and underlying id.</summary>
        public bool Equals(QuestResourceValue other)
        {
            return Kind == other.Kind
                && ActorId == other.ActorId
                && SiteId == other.SiteId
                && ItemId == other.ItemId;
        }

        /// <summary>Returns true when the object is a tagged quest resource value with the same contents.</summary>
        public override bool Equals(object obj)
        {
            return obj is QuestResourceValue other && Equals(other);
        }

        /// <summary>Returns a hash code derived only from the kind discriminator and underlying ids.</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;
                hash = (hash * 397) ^ ActorId.GetHashCode();
                hash = (hash * 397) ^ SiteId.GetHashCode();
                hash = (hash * 397) ^ ItemId.GetHashCode();
                return hash;
            }
        }
    }

    /// <summary>Supported deterministic quest resource categories.</summary>
    public enum QuestResourceKind
    {
        Person = 1,
        Place = 2,
        Item = 3,
    }
}

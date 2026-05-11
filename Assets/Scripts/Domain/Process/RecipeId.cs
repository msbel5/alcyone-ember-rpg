using System;

// Design note:
// RecipeId is the smallest stable PROCESS-box recipe handle. It is a pure
// Domain value with no registry lookup, allocation, logging, serialization, or
// Unity dependency. Zero is reserved as the empty sentinel so RecipeDef and
// RecipeSystem can reject missing recipes deterministically in later atoms.
// Atom-map ref: DOCS/sprint-faz-2-atom-map.md Recipe definitions sub-area.
namespace EmberCrpg.Domain.Process
{
    /// <summary>
    /// Stable handle to a recipe definition. Value type; default value means no recipe.
    /// </summary>
    public readonly struct RecipeId : IEquatable<RecipeId>
    {
        private readonly ulong _value;

        /// <summary>
        /// Creates a recipe handle from its raw stable identifier.
        /// </summary>
        public RecipeId(ulong value)
        {
            _value = value;
        }

        /// <summary>
        /// Raw stable identifier carried by this recipe handle.
        /// </summary>
        public ulong Value
        {
            get { return _value; }
        }

        /// <summary>
        /// True when this handle is the empty no-recipe sentinel.
        /// </summary>
        public bool IsEmpty
        {
            get { return _value == 0UL; }
        }

        /// <summary>
        /// Returns true when both recipe handles carry the same raw identifier.
        /// </summary>
        public bool Equals(RecipeId other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Returns true when the object is a recipe handle with the same raw identifier.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is RecipeId other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code derived only from the raw stable identifier.
        /// </summary>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Returns a compact debug label for this recipe handle.
        /// </summary>
        public override string ToString()
        {
            return IsEmpty ? "RecipeId.Empty" : $"RecipeId({_value})";
        }

        /// <summary>
        /// Returns true when both recipe handles carry the same raw identifier.
        /// </summary>
        public static bool operator ==(RecipeId left, RecipeId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns true when recipe handles carry different raw identifiers.
        /// </summary>
        public static bool operator !=(RecipeId left, RecipeId right)
        {
            return !left.Equals(right);
        }
    }
}

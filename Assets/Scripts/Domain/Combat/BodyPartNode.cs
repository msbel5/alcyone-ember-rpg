using System;

// Design note:
// BodyPartNode describes the hierarchy and mechanical modifiers for one targetable body zone.
// Inputs: weighted hit selection and collider-reported body-part hits.
// Outputs: parent link, selection weight, armor-class modifier, and damage multiplier.
// Bible reference: MASTER_MECHANICS_BIBLE.md §10 body-part targeting, Sprint 4 Phase 2 hit abstraction.
namespace EmberCrpg.Domain.Combat
{
    /// <summary>Pure descriptor for a hit-location node in the humanoid body hierarchy.</summary>
    public readonly struct BodyPartNode
    {
        public BodyPartNode(BodyPart part, BodyPart? parent, int selectionWeight, int armorClassModifier, int damageMultiplierPercent)
        {
            if (selectionWeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(selectionWeight), selectionWeight, "Body-part weights must be positive.");
            if (damageMultiplierPercent <= 0)
                throw new ArgumentOutOfRangeException(nameof(damageMultiplierPercent), damageMultiplierPercent, "Damage multiplier must be positive.");

            Part = part;
            Parent = parent;
            SelectionWeight = selectionWeight;
            ArmorClassModifier = armorClassModifier;
            DamageMultiplierPercent = damageMultiplierPercent;
        }

        public BodyPart Part { get; }
        public BodyPart? Parent { get; }
        public int SelectionWeight { get; }
        public int ArmorClassModifier { get; }
        public int DamageMultiplierPercent { get; }
    }
}

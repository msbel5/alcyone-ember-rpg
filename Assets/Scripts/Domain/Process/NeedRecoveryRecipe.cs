using System;
using EmberCrpg.Domain.Actors;

// Design note:
// NeedRecoveryRecipe is Phase 4's pure PROCESS/LIVING recovery definition for
// concrete eat/sleep actions. It names one need pressure to reduce and carries
// the optional inventory item consumed by the action. Runtime mutation and
// EventLog output belong to NeedRecoverySystem.
// Atom-map ref: docs/sprint-phase-4-atom-map.md Eat / sleep recovery rail.
namespace EmberCrpg.Domain.Process
{
    /// <summary>Pure definition for one deterministic need-recovery action.</summary>
    public sealed class NeedRecoveryRecipe
    {
        public NeedRecoveryRecipe(string id, string actionKind, NeedKind needKind, int recoveryAmount, string consumedItemTemplateId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Need recovery recipe id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(actionKind))
                throw new ArgumentException("Need recovery action kind is required.", nameof(actionKind));
            if (needKind == NeedKind.None)
                throw new ArgumentException("Need recovery requires a concrete need kind.", nameof(needKind));
            if (recoveryAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(recoveryAmount), recoveryAmount, "Need recovery amount must be positive.");
            if (consumedItemTemplateId != null && string.IsNullOrWhiteSpace(consumedItemTemplateId))
                throw new ArgumentException("Consumed item template id cannot be blank.", nameof(consumedItemTemplateId));

            Id = id.Trim();
            ActionKind = actionKind.Trim();
            NeedKind = needKind;
            RecoveryAmount = recoveryAmount;
            ConsumedItemTemplateId = consumedItemTemplateId == null ? null : consumedItemTemplateId.Trim();
        }

        public string Id { get; }
        public string ActionKind { get; }
        public NeedKind NeedKind { get; }
        public int RecoveryAmount { get; }
        public string ConsumedItemTemplateId { get; }

        public bool RequiresInventoryItem
        {
            get { return !string.IsNullOrEmpty(ConsumedItemTemplateId); }
        }
    }
}

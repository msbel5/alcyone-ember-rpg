using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Magic;

// Design note:
// ShieldBuffService is the deterministic Sprint 5 shield-buff tick-down seam.
// Inputs: a ShieldBuffState container (filled by SpellEffectResolutionService.ApplyShieldBuffs)
// and elapsed simulation ticks. Outputs: in-place tick-down that expires entries when their
// remaining ticks reach zero, preserving each entry's magnitude until expiry.
// This slice is decay-only. It does not key buffs to specific actors, does not reduce shield
// magnitude per absorbed damage, and does not call into combat resolution. Bible reference:
// EMBER_VISION_BIBLE.md §3 Layer 3, MASTER_MECHANICS_BIBLE.md §15 Magic effects, mirroring the
// SpellCooldownService.AdvanceTicks pattern from the cooldown foundation slice.
namespace EmberCrpg.Simulation.Magic
{
    /// <summary>Pure deterministic tick-down service for active shield buff state.</summary>
    public sealed partial class ShieldBuffService
    {
        public void AdvanceTicks(ShieldBuffState shieldBuffState, int elapsedTicks)
        {
            if (shieldBuffState == null)
                throw new ArgumentNullException(nameof(shieldBuffState));
            if (elapsedTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedTicks), elapsedTicks, "Elapsed ticks must be zero or positive.");
            if (elapsedTicks == 0)
                return;

            foreach (var spellTemplateId in shieldBuffState.GetTrackedSpellTemplateIds())
            {
                var remainingTicks = shieldBuffState.GetRemainingTicks(spellTemplateId);
                if (remainingTicks <= 0)
                    continue;

                var magnitude = shieldBuffState.GetMagnitude(spellTemplateId);
                var updatedTicks = remainingTicks - elapsedTicks;
                shieldBuffState.SetActiveBuff(spellTemplateId, updatedTicks > 0 ? updatedTicks : 0, magnitude);
            }
        }

        // Actor-keyed sweep seam: forwards each tracked actor's bag to the single-bag AdvanceTicks
        // so a future combat/world tick loop can advance every actor's shield buffs through one call
        // without itself enumerating ShieldBuffStateRegistry. Pure delegation — no new decay rules,
        // no application/save/absorption changes, parity per actor with single-bag AdvanceTicks.
        public void AdvanceTicksForAllActors(ShieldBuffStateRegistry registry, int elapsedTicks)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (elapsedTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedTicks), elapsedTicks, "Elapsed ticks must be zero or positive.");
            if (elapsedTicks == 0)
                return;

            foreach (var actorId in registry.GetTrackedActorIds())
            {
                var shieldBuffState = registry.GetOrNull(actorId);
                if (shieldBuffState == null)
                    continue;

                AdvanceTicks(shieldBuffState, elapsedTicks);
            }
        }

        // Damage absorption seam: consumes magnitude across active shield buffs in a single
        // ShieldBuffState in deterministic ascending ordinal order of spell template id, returning
        // the absorbed and remaining damage totals plus the per-spell consume/expire trace. Buffs
        // whose magnitude reaches zero are removed entirely even when their remaining ticks have
        // not yet expired; otherwise the buff's remaining ticks are preserved unchanged. Buffs
        // with zero magnitude are skipped without being marked consumed. Pure Simulation: no tick
        // mutation, no save coupling, no actor-keyed dispatch (registry sweep is a future slice).
        public ShieldBuffAbsorptionResult AbsorbDamage(ShieldBuffState shieldBuffState, int incomingDamage)
        {
            if (shieldBuffState == null)
                throw new ArgumentNullException(nameof(shieldBuffState));
            if (incomingDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(incomingDamage), incomingDamage, "Incoming damage must be zero or positive.");

            if (incomingDamage == 0)
            {
                return ShieldBuffAbsorptionResult.Create(
                    incomingDamage: 0,
                    absorbedDamage: 0,
                    remainingDamage: 0,
                    consumedSpellTemplateIds: Array.Empty<string>(),
                    expiredSpellTemplateIds: Array.Empty<string>());
            }

            var consumed = new List<string>();
            var expired = new List<string>();
            var remainingDamage = incomingDamage;
            var absorbedDamage = 0;

            var trackedSpellIds = shieldBuffState.GetTrackedSpellTemplateIds();
            var orderedSpellIds = new List<string>(trackedSpellIds);
            orderedSpellIds.Sort(StringComparer.Ordinal);

            foreach (var spellTemplateId in orderedSpellIds)
            {
                if (remainingDamage <= 0)
                    break;

                var remainingTicks = shieldBuffState.GetRemainingTicks(spellTemplateId);
                if (remainingTicks <= 0)
                    continue;

                var magnitude = shieldBuffState.GetMagnitude(spellTemplateId);
                if (magnitude <= 0)
                    continue;

                var consumeAmount = magnitude < remainingDamage ? magnitude : remainingDamage;
                absorbedDamage += consumeAmount;
                remainingDamage -= consumeAmount;
                var newMagnitude = magnitude - consumeAmount;

                consumed.Add(spellTemplateId);

                if (newMagnitude == 0)
                {
                    shieldBuffState.Clear(spellTemplateId);
                    expired.Add(spellTemplateId);
                }
                else
                {
                    shieldBuffState.SetActiveBuff(spellTemplateId, remainingTicks, newMagnitude);
                }
            }

            return ShieldBuffAbsorptionResult.Create(
                incomingDamage: incomingDamage,
                absorbedDamage: absorbedDamage,
                remainingDamage: remainingDamage,
                consumedSpellTemplateIds: consumed,
                expiredSpellTemplateIds: expired);
        }

        // Actor-keyed absorption seam: routes incoming damage through one actor's shield bag in
        // ShieldBuffStateRegistry by delegating to the single-bag AbsorbDamage. A future combat
        // damage pipeline can call this once per damaged actor without enumerating the registry
        // itself. Pure delegation — same per-buff consume order, same magnitude-exhaust expiry,
        // same trace contract. The registry is read-only here: an untracked actor returns full
        // RemainingDamage with an empty trace and the registry is not mutated, mirroring how
        // AdvanceTicksForAllActors does not lazily create per-actor bags.
        public ShieldBuffAbsorptionResult AbsorbDamageForActor(
            ShieldBuffStateRegistry registry,
            string actorId,
            int incomingDamage)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (string.IsNullOrWhiteSpace(actorId))
                throw new ArgumentException("Actor id must be a non-empty stable id.", nameof(actorId));
            if (incomingDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(incomingDamage), incomingDamage, "Incoming damage must be zero or positive.");

            if (incomingDamage == 0)
            {
                return ShieldBuffAbsorptionResult.Create(
                    incomingDamage: 0,
                    absorbedDamage: 0,
                    remainingDamage: 0,
                    consumedSpellTemplateIds: Array.Empty<string>(),
                    expiredSpellTemplateIds: Array.Empty<string>());
            }

            var shieldBuffState = registry.GetOrNull(actorId);
            if (shieldBuffState == null)
            {
                return ShieldBuffAbsorptionResult.Create(
                    incomingDamage: incomingDamage,
                    absorbedDamage: 0,
                    remainingDamage: incomingDamage,
                    consumedSpellTemplateIds: Array.Empty<string>(),
                    expiredSpellTemplateIds: Array.Empty<string>());
            }

            return AbsorbDamage(shieldBuffState, incomingDamage);
        }

        // Batch actor-keyed absorption seam: routes a per-actor incoming-damage map through the
        // single-actor AbsorbDamageForActor seam so a future combat damage-resolution pass can
        // absorb shield buffs across multiple damaged actors in one deterministic call without
        // itself enumerating ShieldBuffStateRegistry. Pure delegation — same per-buff consume
        // order, same magnitude-exhaust expiry, same trace contract per actor. The registry is
        // read-only here: an untracked actor in the input map yields a result with full
        // RemainingDamage and an empty trace, and the actor is NOT lazily added to the registry.
        // Result keys mirror the input keys exactly (including untracked actors), so callers can
        // reason about the result map purely from their own input rather than registry contents.
        public IReadOnlyDictionary<string, ShieldBuffAbsorptionResult> AbsorbDamageForActors(
            ShieldBuffStateRegistry registry,
            IReadOnlyDictionary<string, int> incomingDamageByActorId)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));
            if (incomingDamageByActorId == null)
                throw new ArgumentNullException(nameof(incomingDamageByActorId));

            var results = new Dictionary<string, ShieldBuffAbsorptionResult>(
                incomingDamageByActorId.Count,
                StringComparer.Ordinal);

            // PR#37 bot review fix: validate the ENTIRE input map before mutating
            // any shield state. Previously validation and AbsorbDamageForActor
            // dispatch were interleaved, so a malformed entry midway through the
            // dictionary left earlier actors partially absorbed while the later
            // ones threw and never ran — leaking partial state into the registry.
            foreach (var pair in incomingDamageByActorId)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new ArgumentException("Actor id keys must be non-empty stable ids.", nameof(incomingDamageByActorId));
                if (pair.Value < 0)
                    throw new ArgumentOutOfRangeException(nameof(incomingDamageByActorId), pair.Value, "Incoming damage must be zero or positive.");
            }

            foreach (var pair in incomingDamageByActorId)
            {
                results[pair.Key] = AbsorbDamageForActor(registry, pair.Key, pair.Value);
            }

            return results;
        }

        // Batch totals seam: deterministically aggregates the per-actor result map returned by
        // AbsorbDamageForActors into a single ShieldBuffAbsorptionBatchTotals snapshot. A future
        // combat damage-resolution pass or telemetry/UI surface can summarize a batch absorption
    }
}

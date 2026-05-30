using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Magic
{
    /// <summary>
    /// Orchestrates effect execution: validate cost, dispatch each operation,
    /// emit SpellResolved. Phase 8 Atom 6.
    /// </summary>
    public sealed class SpellResolver
    {
        private readonly EffectOperationHandlers _handlers;

        public SpellResolver(EffectOperationHandlers handlers)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

        public SpellResolutionResult Resolve(
            EffectDefinition definition,
            int casterMana,
            GameTime now,
            SiteId siteContext,
            WorldEventLog events,
            SpellResolverContext context = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (events == null) throw new ArgumentNullException(nameof(events));

            if (casterMana < definition.Cost)
                return SpellResolutionResult.Failed("insufficient_mana");

            // Codex audit Batch 2 / Finding 2: PR#176's original fix DID emit a Failed
            // result when any op lacked a handler, but it did so AFTER the loop had
            // already mutated state via ApplyOperationToContext for every handled op
            // ahead of the unhandled one. That meant a partially-mutated world AND a
            // `Failed` telemetry message — the worst of both. Pre-validate every
            // operation against the handler registry first; if any are missing, fail
            // before touching context state.
            int operationsUnhandled = 0;
            foreach (var operation in definition.Operations)
            {
                if (!_handlers.HasHandler(operation.Kind))
                    operationsUnhandled++;
            }
            if (operationsUnhandled > 0)
            {
                if (!siteContext.IsEmpty)
                {
                    events.Append(new WorldEvent(
                        now,
                        WorldEventKind.SpellResolved,
                        default,
                        siteContext,
                        $"spell_resolved id:{definition.Id} ops:0 unhandled:{operationsUnhandled} status:failed"));
                }
                return SpellResolutionResult.Failed($"unhandled_operations:{operationsUnhandled}");
            }

            int totalMagnitude = 0;
            int operationsApplied = 0;
            // Codex audit (fifth pass A-P1): the previous loop incremented
            // operationsApplied BEFORE ApplyOperationToContext ran, so a
            // terrain-apply op that early-returned (missing required tag,
            // missing terrain stockpile while present in context) still
            // counted as "applied" — the result reported success for a
            // no-op. Distinguish:
            //   * context == null: pure cost-gating call; count handler
            //     successes (the caller doesn't have a mutable world yet).
            //   * context != null: count only when ApplyOperationToContext
            //     returns true (confirmed mutation).
            foreach (var operation in definition.Operations)
            {
                if (!_handlers.TryHandle(operation, out var magnitude)) continue;
                bool mutated = ApplyOperationToContext(operation, context);
                if (context != null && !mutated) continue;
                totalMagnitude += magnitude;
                operationsApplied++;
            }

            if (!siteContext.IsEmpty)
            {
                events.Append(new WorldEvent(
                    now,
                    WorldEventKind.SpellResolved,
                    default,
                    siteContext,
                    $"spell_resolved id:{definition.Id} ops:{operationsApplied} total:{totalMagnitude}"));
            }

            return SpellResolutionResult.Success(totalMagnitude, operationsApplied);
        }

        /// <summary>
        /// Codex audit (fifth pass A-P1): returns true only when the
        /// context was actually mutated. Callers count an operation as
        /// "applied" only when this returns true so the result's
        /// OperationsApplied / TotalMagnitude reflect real world changes,
        /// not the number of times the loop visited an operation row.
        /// </summary>
        private static bool ApplyOperationToContext(EffectOperation operation, SpellResolverContext context)
        {
            if (context == null)
                return false;

            if (operation.Kind.Equals(EffectOperationKind.DirectDamage) && context.TargetActor != null)
            {
                context.TargetActor.ApplyVitals(context.TargetActor.Vitals.WithHealth(context.TargetActor.Vitals.Health.Damage(operation.Magnitude)));
                return true;
            }

            if (operation.Kind.Equals(EffectOperationKind.DirectRestore) && context.TargetActor != null)
            {
                context.TargetActor.ApplyVitals(context.TargetActor.Vitals.WithHealth(context.TargetActor.Vitals.Health.Restore(operation.Magnitude)));
                return true;
            }

            if (operation.Kind.Equals(EffectOperationKind.TerrainApply) && context.TerrainStockpile != null)
            {
                var requiredTag = string.IsNullOrWhiteSpace(operation.TargetRule) ? context.RequiredTerrainTag : operation.TargetRule;
                if (!string.IsNullOrWhiteSpace(requiredTag) && !context.TerrainStockpile.Contains(requiredTag))
                    return false;

                if (!string.IsNullOrWhiteSpace(requiredTag))
                    context.TerrainStockpile.Remove(requiredTag, 1);
                context.TerrainStockpile.Add(context.ResultTerrainTag, 1);
                if (context.TargetActor != null)
                    context.TargetActor.ApplyVitals(context.TargetActor.Vitals.WithHealth(context.TargetActor.Vitals.Health.Damage(operation.Magnitude)));
                return true;
            }

            return false;
        }
    }

    public sealed class SpellResolverContext
    {
        public SpellResolverContext(ActorRecord targetActor, StockpileComponent terrainStockpile, string requiredTerrainTag, string resultTerrainTag)
        {
            TargetActor = targetActor;
            TerrainStockpile = terrainStockpile;
            RequiredTerrainTag = requiredTerrainTag ?? string.Empty;
            ResultTerrainTag = string.IsNullOrWhiteSpace(resultTerrainTag) ? "terrain_effect" : resultTerrainTag.Trim();
        }

        public ActorRecord TargetActor { get; }
        public StockpileComponent TerrainStockpile { get; }
        public string RequiredTerrainTag { get; }
        public string ResultTerrainTag { get; }
    }

    public sealed class SpellResolutionResult
    {
        private SpellResolutionResult(bool ok, int totalMagnitude, int operationsApplied, string reason)
        {
            Resolved = ok;
            TotalMagnitude = totalMagnitude;
            OperationsApplied = operationsApplied;
            FailureReason = reason ?? string.Empty;
        }
        public bool Resolved { get; }
        public int TotalMagnitude { get; }
        public int OperationsApplied { get; }
        public string FailureReason { get; }
        public static SpellResolutionResult Success(int mag, int ops) => new SpellResolutionResult(true, mag, ops, null);
        public static SpellResolutionResult Failed(string reason) => new SpellResolutionResult(false, 0, 0, reason);
    }
}

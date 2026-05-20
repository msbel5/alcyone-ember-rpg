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
    /// emit SpellResolved. Faz 8 Atom 6.
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

            int totalMagnitude = 0;
            int operationsApplied = 0;
            int operationsUnhandled = 0;
            foreach (var operation in definition.Operations)
            {
                if (_handlers.TryHandle(operation, out var magnitude))
                {
                    totalMagnitude += magnitude;
                    operationsApplied++;
                    ApplyOperationToContext(operation, context);
                }
                else
                {
                    operationsUnhandled++;
                }
            }

            // PR#176 bot review fix: when any operation row has no registered handler
            // the cast partially mutates state but the loop used to fall through and
            // emit SpellResolved as success. Fail fast with a stable reason so callers
            // and telemetry agree the spell did not fully execute.
            if (operationsUnhandled > 0)
            {
                if (!siteContext.IsEmpty)
                {
                    events.Append(new WorldEvent(
                        now,
                        WorldEventKind.SpellResolved,
                        default,
                        siteContext,
                        $"spell_resolved id:{definition.Id} ops:{operationsApplied} unhandled:{operationsUnhandled} status:failed"));
                }
                return SpellResolutionResult.Failed($"unhandled_operations:{operationsUnhandled}");
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

        private static void ApplyOperationToContext(EffectOperation operation, SpellResolverContext context)
        {
            if (context == null)
                return;

            if (operation.Kind.Equals(EffectOperationKind.DirectDamage) && context.TargetActor != null)
            {
                context.TargetActor.ApplyVitals(context.TargetActor.Vitals.WithHealth(context.TargetActor.Vitals.Health.Damage(operation.Magnitude)));
                return;
            }

            if (operation.Kind.Equals(EffectOperationKind.DirectRestore) && context.TargetActor != null)
            {
                context.TargetActor.ApplyVitals(context.TargetActor.Vitals.WithHealth(context.TargetActor.Vitals.Health.Restore(operation.Magnitude)));
                return;
            }

            if (operation.Kind.Equals(EffectOperationKind.TerrainApply) && context.TerrainStockpile != null)
            {
                var requiredTag = string.IsNullOrWhiteSpace(operation.TargetRule) ? context.RequiredTerrainTag : operation.TargetRule;
                if (!string.IsNullOrWhiteSpace(requiredTag) && !context.TerrainStockpile.Contains(requiredTag))
                    return;

                if (!string.IsNullOrWhiteSpace(requiredTag))
                    context.TerrainStockpile.Remove(requiredTag, 1);
                context.TerrainStockpile.Add(context.ResultTerrainTag, 1);
                if (context.TargetActor != null)
                    context.TargetActor.ApplyVitals(context.TargetActor.Vitals.WithHealth(context.TargetActor.Vitals.Health.Damage(operation.Magnitude)));
            }
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

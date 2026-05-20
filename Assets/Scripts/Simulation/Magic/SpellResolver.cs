using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Magic;
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
            WorldEventLog events)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (events == null) throw new ArgumentNullException(nameof(events));

            if (casterMana < definition.Cost)
                return SpellResolutionResult.Failed("insufficient_mana");

            int totalMagnitude = 0;
            int operationsApplied = 0;
            foreach (var operation in definition.Operations)
            {
                if (_handlers.TryHandle(operation, out var magnitude))
                {
                    totalMagnitude += magnitude;
                    operationsApplied++;
                }
            }

            var site = siteContext.IsEmpty ? new SiteId(1UL) : siteContext;
            events.Append(new WorldEvent(
                now,
                WorldEventKind.SpellResolved,
                default,
                site,
                $"spell_resolved id:{definition.Id} ops:{operationsApplied} total:{totalMagnitude}"));

            return SpellResolutionResult.Success(totalMagnitude, operationsApplied);
        }
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

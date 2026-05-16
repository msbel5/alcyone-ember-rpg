using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// NeedRecoverySystem is Faz 4's concrete recovery consumer. EatMeal mutates
// inventory only after proving hunger can change; Sleep never touches inventory.
// Both paths recompute mood and emit a NeedChanged trace for playable proof.
// Atom-map ref: DOCS/sprint-faz-4-atom-map.md Eat / sleep recovery rail.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Applies deterministic need recovery actions to actors.</summary>
    public sealed class NeedRecoverySystem
    {
        public const string EatMealAction = "eat_meal";
        public const string SleepAction = "sleep";

        private readonly NeedMoodEvaluator _moodEvaluator;

        public NeedRecoverySystem()
            : this(new NeedMoodEvaluator())
        {
        }

        public NeedRecoverySystem(NeedMoodEvaluator moodEvaluator)
        {
            _moodEvaluator = moodEvaluator ?? throw new ArgumentNullException(nameof(moodEvaluator));
        }

        public bool EatMeal(
            ActorRecord actor,
            InventoryState inventory,
            NeedRecoveryRecipe recipe,
            WorldEventLog eventLog,
            GameTime now)
        {
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));

            ValidateRecipe(recipe, EatMealAction, NeedKind.Hunger, requiresInventoryItem: true);
            if (!CanRecover(actor, recipe))
                return false;
            if (!inventory.TryRemoveStackable(recipe.ConsumedItemTemplateId, 1))
                return false;

            ApplyRecovery(actor, recipe, eventLog, now, $"item:{recipe.ConsumedItemTemplateId}");
            return true;
        }

        public bool Sleep(
            ActorRecord actor,
            NeedRecoveryRecipe recipe,
            WorldEventLog eventLog,
            GameTime now)
        {
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));

            ValidateRecipe(recipe, SleepAction, NeedKind.Fatigue, requiresInventoryItem: false);
            if (!CanRecover(actor, recipe))
                return false;

            ApplyRecovery(actor, recipe, eventLog, now, "rest:sleep");
            return true;
        }

        private static bool CanRecover(ActorRecord actor, NeedRecoveryRecipe recipe)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            return actor.Needs.Get(recipe.NeedKind).Value > NeedValue.Min;
        }

        private void ApplyRecovery(
            ActorRecord actor,
            NeedRecoveryRecipe recipe,
            WorldEventLog eventLog,
            GameTime now,
            string sourceCause)
        {
            var previousNeeds = actor.Needs;
            var previousValue = previousNeeds.Get(recipe.NeedKind);
            var nextValue = previousValue.Decrease(recipe.RecoveryAmount);
            var nextNeeds = previousNeeds.With(recipe.NeedKind, nextValue);
            actor.ApplyNeeds(nextNeeds);
            var mood = _moodEvaluator.Evaluate(nextNeeds);
            actor.ApplyMood(mood);

            eventLog.Append(new WorldEvent(
                now,
                WorldEventKind.NeedChanged,
                actor.Id,
                default,
                $"need_recovered:{actor.Id.Value}",
                new ReasonTrace(new[]
                {
                    "need_recovery",
                    $"action:{recipe.ActionKind}",
                    $"recipe:{recipe.Id}",
                    $"actor:{actor.Id.Value}",
                    sourceCause,
                    $"{NeedLabel(recipe.NeedKind)}:{previousValue.Value}->{nextValue.Value}",
                    $"mood:{mood.Value}",
                })));
        }

        private static void ValidateRecipe(NeedRecoveryRecipe recipe, string actionKind, NeedKind needKind, bool requiresInventoryItem)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));
            if (!string.Equals(recipe.ActionKind, actionKind, StringComparison.Ordinal))
                throw new ArgumentException($"Need recovery recipe must use action kind {actionKind}.", nameof(recipe));
            if (recipe.NeedKind != needKind)
                throw new ArgumentException($"Need recovery recipe must target {needKind}.", nameof(recipe));
            if (recipe.RequiresInventoryItem != requiresInventoryItem)
                throw new ArgumentException("Need recovery recipe inventory requirement does not match the action.", nameof(recipe));
        }

        private static string NeedLabel(NeedKind kind)
        {
            switch (kind)
            {
                case NeedKind.Hunger:
                    return "hunger";
                case NeedKind.Fatigue:
                    return "fatigue";
                case NeedKind.Thirst:
                    return "thirst";
                default:
                    throw new ArgumentException("Need recovery requires a concrete need kind.", nameof(kind));
            }
        }
    }
}

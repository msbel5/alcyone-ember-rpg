using System;
using System.Collections.Generic;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;

// Design note:
// RecipeSystem is the first visible Phase 2 PROCESS slice. It consumes the pure
// RecipeDef + WorksiteStore atoms, mutates only InventoryState, advances a
// small deterministic work order, and emits a WorldEventLog line when the
// recipe completes. It does not own save/load, job assignment, skill scaling,
// Unity presentation, or generic factory registries; those are later atoms in
// docs/sprint-phase-2-atom-map.md.
namespace EmberCrpg.Simulation.Process
{
    /// <summary>
    /// Pure deterministic recipe execution for one active worksite and one inventory.
    /// </summary>
    public sealed class RecipeSystem
    {
        /// <summary>
        /// Starts a recipe if the active worksite matches and all inputs can be consumed.
        /// Inputs are consumed once at start so repeated ticks cannot double-spend stock.
        /// </summary>
        public bool TryStart(
            RecipeDef recipe,
            WorksiteStore worksites,
            SiteId siteId,
            GridPosition position,
            InventoryState inventory,
            ActorId actorId,
            out RecipeWorkOrder order)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));
            if (worksites == null)
                throw new ArgumentNullException(nameof(worksites));
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            order = null;

            if (!worksites.TryGet(siteId, position, out var worksite))
                return false;
            if (!worksite.IsActive)
                return false;
            if (!MatchesWorksiteKind(recipe.WorksiteKind, worksite.Kind))
                return false;
            if (!HasInputs(recipe, inventory))
                return false;

            ConsumeInputs(recipe, inventory);
            order = new RecipeWorkOrder(recipe, siteId, position, actorId);
            return true;
        }

        /// <summary>
        /// W33 (B06): tag-count IO overload — village production runs on the worksite's real
        /// container (site stockpile) instead of the player's bag. Same gates, same
        /// consume-at-start contract; the InventoryState path above stays byte-identical.
        /// </summary>
        public bool TryStart(
            RecipeDef recipe,
            WorksiteStore worksites,
            SiteId siteId,
            GridPosition position,
            IRecipeInventory io,
            ActorId actorId,
            out RecipeWorkOrder order)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));
            if (worksites == null)
                throw new ArgumentNullException(nameof(worksites));
            if (io == null)
                throw new ArgumentNullException(nameof(io));

            order = null;

            if (!worksites.TryGet(siteId, position, out var worksite))
                return false;
            if (!worksite.IsActive)
                return false;
            if (!MatchesWorksiteKind(recipe.WorksiteKind, worksite.Kind))
                return false;
            foreach (var input in recipe.Inputs)
                if (io.CountOf(input.ItemTag) < input.Quantity)
                    return false;
            foreach (var input in recipe.Inputs)
                if (!io.TryConsume(input.ItemTag, input.Quantity))
                    throw new InvalidOperationException(
                        $"Recipe input {input.ItemTag} passed availability check but could not be consumed.");

            order = new RecipeWorkOrder(recipe, siteId, position, actorId);
            return true;
        }

        /// <summary>
        /// W34 WORK slice (docs/ruh/w34/02 §5.2): funds ONE execution from the bench-side IO —
        /// the TryStart io-overload's CountOf-then-TryConsume grammar reused for the Domain row
        /// path. False = insufficient inputs, NOTHING consumed (the caller pauses SourceDrained).
        /// CONSTRAINT (funding invariant): the caller MUST land the first counter hit in the
        /// SAME PerformWork step this returns true, or the row lies about its inputs.
        /// </summary>
        public static bool TryFund(RecipeDef recipe, IRecipeInventory io)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));
            if (io == null)
                throw new ArgumentNullException(nameof(io));

            foreach (var input in recipe.Inputs)
                if (io.CountOf(input.ItemTag) < input.Quantity)
                    return false;
            foreach (var input in recipe.Inputs)
                if (!io.TryConsume(input.ItemTag, input.Quantity))
                    throw new InvalidOperationException(
                        $"Recipe input {input.ItemTag} passed availability check but could not be consumed.");
            return true;
        }

        /// <summary>
        /// W34 WORK slice (docs/ruh/w34/02 §5.2): advances a Domain WorkOrderRecord row by one
        /// bench stroke. Same body as the io Tick below, but (a) the counter lives on the saved
        /// row, (b) RecipeCompleted carries the REAL boundary stamp — the GameTime(ProgressTicks)
        /// fossil dies only on this new strip (the legacy overloads and their goldens stay put),
        /// and (c) the event's actor is the FINISHER, not the starter (§13.4).
        /// </summary>
        public bool Tick(
            EmberCrpg.Domain.Process.WorkOrderRecord row,
            RecipeDef recipe,
            IRecipeInventory io,
            WorldEventLog eventLog,
            GameTime stamp,
            ActorId actorId)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));
            if (io == null)
                throw new ArgumentNullException(nameof(io));
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));

            if (row.ProgressTicks + 1 < recipe.DurationTicks)
            {
                row.ProgressTicks++;
                return false;
            }

            var probe = io.CloneForPreflight();
            foreach (var output in recipe.Outputs)
                if (!probe.TryAccept(output.ItemTag, output.Quantity))
                    throw new InvalidOperationException($"Recipe IO cannot accept output {output.ItemTag}.");
            foreach (var output in recipe.Outputs)
                if (!io.TryAccept(output.ItemTag, output.Quantity))
                    throw new InvalidOperationException($"Recipe IO rejected output {output.ItemTag} after preflight.");

            row.ProgressTicks++;
            eventLog.Append(new WorldEvent(
                stamp,
                WorldEventKind.RecipeCompleted,
                actorId,
                new SiteId(row.SiteId),
                $"recipe_completed:{recipe.Id.Value}",
                new ReasonTrace(new[]
                {
                    $"recipe:{recipe.Id.Value}",
                    $"worksite:{recipe.WorksiteKind}",
                    $"duration_ticks:{recipe.DurationTicks}",
                })));
            return true;
        }

        /// <summary>
        /// W33 (B06): tag-count Tick — outputs land as counts (no ItemId mint). A preflight
        /// clone proves every output is acceptable BEFORE the first real accept, preserving
        /// the all-or-nothing output contract of the InventoryState path.
        /// </summary>
        public bool Tick(RecipeWorkOrder order, IRecipeInventory io, WorldEventLog eventLog)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));
            if (io == null)
                throw new ArgumentNullException(nameof(io));
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));
            if (order.IsComplete)
                return false;

            if (order.ProgressTicks + 1 < order.Recipe.DurationTicks)
            {
                order.AdvanceOneTick();
                return false;
            }

            var probe = io.CloneForPreflight();
            foreach (var output in order.Recipe.Outputs)
                if (!probe.TryAccept(output.ItemTag, output.Quantity))
                    throw new InvalidOperationException($"Recipe IO cannot accept output {output.ItemTag}.");
            foreach (var output in order.Recipe.Outputs)
                if (!io.TryAccept(output.ItemTag, output.Quantity))
                    throw new InvalidOperationException($"Recipe IO rejected output {output.ItemTag} after preflight.");

            order.AdvanceOneTick();
            eventLog.Append(new WorldEvent(
                new GameTime(order.ProgressTicks),
                WorldEventKind.RecipeCompleted,
                order.ActorId,
                order.SiteId,
                $"recipe_completed:{order.Recipe.Id.Value}",
                new ReasonTrace(new[]
                {
                    $"recipe:{order.Recipe.Id.Value}",
                    $"worksite:{order.Recipe.WorksiteKind}",
                    $"duration_ticks:{order.Recipe.DurationTicks}",
                })));
            return true;
        }

        /// <summary>
        /// Advances an active work order by one deterministic tick. On the completion tick,
        /// outputs are added and exactly one RecipeCompleted event is appended.
        /// The output factory must instantiate one item unit per call; RecipeOutput.Quantity
        /// controls how many unit items this system adds.
        /// </summary>
        public bool Tick(
            RecipeWorkOrder order,
            InventoryState inventory,
            WorldEventLog eventLog,
            Func<RecipeOutput, InventoryItem> createOutput)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));
            if (eventLog == null)
                throw new ArgumentNullException(nameof(eventLog));
            if (createOutput == null)
                throw new ArgumentNullException(nameof(createOutput));
            if (order.IsComplete)
                return false;

            if (order.ProgressTicks + 1 < order.Recipe.DurationTicks)
            {
                order.AdvanceOneTick();
                return false;
            }

            var outputItems = CreateOutputItems(order, createOutput);
            PreflightOutputs(inventory, outputItems);
            foreach (var item in outputItems)
            {
                if (!inventory.TryAdd(item))
                    throw new InvalidOperationException($"Inventory rejected recipe output {item.TemplateId}.");
            }

            order.AdvanceOneTick();
            eventLog.Append(new WorldEvent(
                new GameTime(order.ProgressTicks),
                WorldEventKind.RecipeCompleted,
                order.ActorId,
                order.SiteId,
                $"recipe_completed:{order.Recipe.Id.Value}",
                new ReasonTrace(new[]
                {
                    $"recipe:{order.Recipe.Id.Value}",
                    $"worksite:{order.Recipe.WorksiteKind}",
                    $"duration_ticks:{order.Recipe.DurationTicks}",
                })));
            return true;
        }

        private static IReadOnlyList<InventoryItem> CreateOutputItems(RecipeWorkOrder order, Func<RecipeOutput, InventoryItem> createOutput)
        {
            var outputItems = new List<InventoryItem>();
            foreach (var output in order.Recipe.Outputs)
            {
                for (var i = 0; i < output.Quantity; i++)
                {
                    var item = createOutput(output);
                    if (item == null)
                        throw new InvalidOperationException("Recipe output factory cannot return null.");
                    if (item.Quantity != 1)
                        throw new InvalidOperationException("Recipe output factory must create exactly one item unit per call.");
                    if (!string.Equals(item.TemplateId, output.ItemTag, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Recipe output factory returned {item.TemplateId} for {output.ItemTag}.");

                    outputItems.Add(item);
                }
            }

            return outputItems;
        }

        private static void PreflightOutputs(InventoryState inventory, IReadOnlyList<InventoryItem> outputItems)
        {
            var projected = inventory.Clone();
            foreach (var item in outputItems)
            {
                if (!projected.TryAdd(item))
                    throw new InvalidOperationException($"Inventory cannot accept recipe output {item.TemplateId}.");
            }
        }

        private static bool HasInputs(RecipeDef recipe, InventoryState inventory)
        {
            foreach (var input in recipe.Inputs)
            {
                var available = 0;
                foreach (var item in inventory.Items)
                {
                    if (!item.IsEquipment && string.Equals(item.TemplateId, input.ItemTag, StringComparison.Ordinal))
                        available += item.Quantity;
                }

                if (available < input.Quantity)
                    return false;
            }

            return true;
        }

        private static void ConsumeInputs(RecipeDef recipe, InventoryState inventory)
        {
            foreach (var input in recipe.Inputs)
            {
                if (!inventory.TryRemoveStackable(input.ItemTag, input.Quantity))
                    throw new InvalidOperationException($"Recipe input {input.ItemTag} passed availability check but could not be consumed.");
            }
        }

        private static bool MatchesWorksiteKind(string recipeKind, WorksiteKind worksiteKind)
        {
            return string.Equals(recipeKind, worksiteKind.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Narrow runtime state for one recipe execution. Save/load mapping is a later Phase 2 atom.
    /// </summary>
    public sealed class RecipeWorkOrder
    {
        internal RecipeWorkOrder(RecipeDef recipe, SiteId siteId, GridPosition position, ActorId actorId)
            : this(recipe, siteId, position, actorId, progressTicks: 0)
        {
        }

        private RecipeWorkOrder(RecipeDef recipe, SiteId siteId, GridPosition position, ActorId actorId, int progressTicks)
        {
            Recipe = recipe ?? throw new ArgumentNullException(nameof(recipe));
            if (progressTicks < 0 || progressTicks > recipe.DurationTicks)
                throw new ArgumentOutOfRangeException(nameof(progressTicks), progressTicks, "Recipe work order progress must fit inside the recipe duration.");

            SiteId = siteId;
            Position = position;
            ActorId = actorId;
            ProgressTicks = progressTicks;
        }

        /// <summary>Rehydrates a saved work order without consuming inputs or emitting events.</summary>
        public static RecipeWorkOrder Resume(RecipeDef recipe, SiteId siteId, GridPosition position, ActorId actorId, int progressTicks)
        {
            return new RecipeWorkOrder(recipe, siteId, position, actorId, progressTicks);
        }

        public RecipeDef Recipe { get; }
        public SiteId SiteId { get; }
        public GridPosition Position { get; }
        public ActorId ActorId { get; }
        public int ProgressTicks { get; private set; }
        public bool IsComplete { get { return ProgressTicks >= Recipe.DurationTicks; } }

        internal void AdvanceOneTick()
        {
            if (!IsComplete)
                ProgressTicks++;
        }
    }
}

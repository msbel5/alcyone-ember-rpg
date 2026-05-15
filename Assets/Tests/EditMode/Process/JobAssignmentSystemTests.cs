using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

// Design note:
// These tests pin Faz 3's assignment-system pass: actor preference rows
// claim pending jobs, write ActorScheduleState, and start RecipeSystem work
// orders for claimed jobs, then tick active recipe work until completion
// removes the board row and clears actor schedule state. Job-specific EventLog
// rows and save/load are intentionally later atoms.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies deterministic actor-to-job claiming.</summary>
    public sealed class JobAssignmentSystemTests
    {
        private static readonly SiteId Site = new SiteId(30UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);
        private static readonly ActorId Requester = new ActorId(99UL);

        [Test]
        public void TryAssignNext_AssignsTwoSmithsDeterministically()
        {
            var actors = new ActorStore();
            var first = CreateActor(1UL, "Ada", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            var second = CreateActor(2UL, "Borin", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(first);
            actors.Add(second);
            var board = new JobBoard();
            var firstJob = MakeRequest(10UL, priority: 1);
            var secondJob = MakeRequest(11UL, priority: 1);
            board.Add(firstJob);
            board.Add(secondJob);
            var worksites = ActiveFurnaceStore();
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, worksites, out var firstResult), Is.True);
            Assert.That(system.TryAssignNext(actors, board, worksites, out var secondResult), Is.True);

            Assert.That(firstResult, Is.EqualTo(new JobAssignmentResult(first.Id, firstJob.Id, Site, FurnacePosition)));
            Assert.That(secondResult, Is.EqualTo(new JobAssignmentResult(second.Id, secondJob.Id, Site, FurnacePosition)));
            Assert.That(board.GetClaimedBy(firstJob.Id), Is.EqualTo(first.Id));
            Assert.That(board.GetClaimedBy(secondJob.Id), Is.EqualTo(second.Id));
            Assert.That(first.ScheduleState, Is.EqualTo(ActorScheduleState.Assigned(firstJob.Id, Site, FurnacePosition)));
            Assert.That(second.ScheduleState, Is.EqualTo(ActorScheduleState.Assigned(secondJob.Id, Site, FurnacePosition)));
        }

        [Test]
        public void TryAssignNext_UsesActorPriorityBeforeActorOrder()
        {
            var actors = new ActorStore();
            var lowPreference = CreateActor(1UL, "Low", new ActorJobPreference(JobKind.Smith, JobPriority.Active(3)));
            var highPreference = CreateActor(2UL, "High", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(lowPreference);
            actors.Add(highPreference);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1);
            board.Add(job);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, ActiveFurnaceStore(), out var result), Is.True);

            Assert.That(result.ActorId, Is.EqualTo(highPreference.Id));
            Assert.That(board.GetClaimedBy(job.Id), Is.EqualTo(highPreference.Id));
            Assert.That(lowPreference.ScheduleState.IsIdle, Is.True);
        }


        [Test]
        public void TryAssignNext_SkipsIdleActorsThatAlreadyHoldPendingClaims()
        {
            var actors = new ActorStore();
            var alreadyClaimed = CreateActor(1UL, "Claimed", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            var available = CreateActor(2UL, "Available", new ActorJobPreference(JobKind.Smith, JobPriority.Active(2)));
            actors.Add(alreadyClaimed);
            actors.Add(available);
            var board = new JobBoard();
            var firstJob = MakeRequest(10UL, priority: 1);
            var secondJob = MakeRequest(11UL, priority: 1);
            board.Add(firstJob);
            board.Add(secondJob);
            Assert.That(board.TryClaim(firstJob.Id, alreadyClaimed.Id, out _), Is.True);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, ActiveFurnaceStore(), out var result), Is.True);

            Assert.That(result.ActorId, Is.EqualTo(available.Id));
            Assert.That(result.JobId, Is.EqualTo(secondJob.Id));
            Assert.That(board.GetClaimedBy(firstJob.Id), Is.EqualTo(alreadyClaimed.Id));
            Assert.That(board.GetClaimedBy(secondJob.Id), Is.EqualTo(available.Id));
            Assert.That(alreadyClaimed.ScheduleState.IsIdle, Is.True);
            Assert.That(available.ScheduleState, Is.EqualTo(ActorScheduleState.Assigned(secondJob.Id, Site, FurnacePosition)));
        }

        [Test]
        public void TryAssignNext_IgnoresDisabledOrUnavailableActors()
        {
            var actors = new ActorStore();
            var disabled = CreateActor(1UL, "Disabled", ActorJobPreference.Disabled(JobKind.Smith));
            var noPreference = CreateActor(2UL, "No Preference");
            actors.Add(disabled);
            actors.Add(noPreference);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1);
            board.Add(job);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, ActiveFurnaceStore(), out var result), Is.False);

            Assert.That(result, Is.EqualTo(default(JobAssignmentResult)));
            Assert.That(board.IsClaimed(job.Id), Is.False);
            Assert.That(disabled.ScheduleState.IsIdle, Is.True);
            Assert.That(noPreference.ScheduleState.IsIdle, Is.True);
        }

        [Test]
        public void CanActorWorkJob_RequiresAliveIdlePreferenceAndActiveWorksite()
        {
            var system = new JobAssignmentSystem();
            var job = MakeRequest(10UL, priority: 1);
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));

            Assert.That(system.CanActorWorkJob(actor, job, ActiveFurnaceStore()), Is.True);

            var busy = CreateActor(2UL, "Busy", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            busy.ApplyScheduleState(ActorScheduleState.Assigned(new JobId(77UL), Site, FurnacePosition));
            Assert.That(system.CanActorWorkJob(busy, job, ActiveFurnaceStore()), Is.False);

            var dead = CreateActor(3UL, "Dead", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)), alive: false);
            Assert.That(system.CanActorWorkJob(dead, job, ActiveFurnaceStore()), Is.False);
            Assert.That(system.CanActorWorkJob(actor, job, InactiveFurnaceStore()), Is.False);
        }

        [Test]
        public void CanActorWorkJob_WithRecipeInputs_ReturnsTrueWithoutMutatingInventory()
        {
            var system = new JobAssignmentSystem();
            var job = MakeRequest(10UL, priority: 1);
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            var inventory = InventoryWithInputs(ore: 2, fuel: 1);
            var recipe = SmeltIronRecipe(job.RecipeId);

            Assert.That(system.CanActorWorkJob(actor, job, ActiveFurnaceStore(), recipe, inventory), Is.True);

            Assert.That(CountTemplate(inventory, "iron_ore"), Is.EqualTo(2));
            Assert.That(CountTemplate(inventory, "fuel"), Is.EqualTo(1));
        }

        [Test]
        public void CanActorWorkJob_WithMissingRecipeInputs_ReturnsFalseWithoutMutatingInventory()
        {
            var system = new JobAssignmentSystem();
            var job = MakeRequest(10UL, priority: 1);
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            var inventory = InventoryWithInputs(ore: 2, fuel: 0);
            var recipe = SmeltIronRecipe(job.RecipeId);

            Assert.That(system.CanActorWorkJob(actor, job, ActiveFurnaceStore(), recipe, inventory), Is.False);

            Assert.That(CountTemplate(inventory, "iron_ore"), Is.EqualTo(2));
            Assert.That(CountTemplate(inventory, "fuel"), Is.EqualTo(0));
        }

        [Test]
        public void CanActorWorkJob_WithBatchRecipeInputs_RequiresInputsForEveryExecution()
        {
            var system = new JobAssignmentSystem();
            var job = MakeRequest(10UL, priority: 1, quantity: 2);
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            var inventory = InventoryWithInputs(ore: 2, fuel: 1);
            var recipe = SmeltIronRecipe(job.RecipeId);

            Assert.That(system.CanActorWorkJob(actor, job, ActiveFurnaceStore(), recipe, inventory), Is.False);

            Assert.That(CountTemplate(inventory, "iron_ore"), Is.EqualTo(2));
            Assert.That(CountTemplate(inventory, "fuel"), Is.EqualTo(1));
        }

        [Test]
        public void CanActorWorkJob_WithBatchRecipeInputs_ReturnsTrueWhenStockCoversEveryExecution()
        {
            var system = new JobAssignmentSystem();
            var job = MakeRequest(10UL, priority: 1, quantity: 2);
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            var inventory = InventoryWithInputs(ore: 4, fuel: 2);
            var recipe = SmeltIronRecipe(job.RecipeId);

            Assert.That(system.CanActorWorkJob(actor, job, ActiveFurnaceStore(), recipe, inventory), Is.True);

            Assert.That(CountTemplate(inventory, "iron_ore"), Is.EqualTo(4));
            Assert.That(CountTemplate(inventory, "fuel"), Is.EqualTo(2));
        }

        [Test]
        public void StartRecipeForClaim_ConsumesInputsAndStoresActiveWorkOrder()
        {
            var actors = new ActorStore();
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(actor);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1);
            board.Add(job);
            var worksites = ActiveFurnaceStore();
            var inventory = InventoryWithInputs(ore: 2, fuel: 1);
            var recipe = SmeltIronRecipe(job.RecipeId);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, worksites, out _), Is.True);
            Assert.That(system.StartRecipeForClaim(actors, board, worksites, recipe, inventory, job.Id, out var result), Is.True);

            Assert.That(result.ActorId, Is.EqualTo(actor.Id));
            Assert.That(result.JobId, Is.EqualTo(job.Id));
            Assert.That(result.SiteId, Is.EqualTo(Site));
            Assert.That(result.WorksitePosition, Is.EqualTo(FurnacePosition));
            Assert.That(result.WorkOrder.Recipe, Is.SameAs(recipe));
            Assert.That(result.WorkOrder.ActorId, Is.EqualTo(actor.Id));
            Assert.That(result.WorkOrder.SiteId, Is.EqualTo(Site));
            Assert.That(result.WorkOrder.Position, Is.EqualTo(FurnacePosition));
            Assert.That(result.WorkOrder.ProgressTicks, Is.EqualTo(0));
            Assert.That(system.TryGetActiveWorkOrder(job.Id, out var activeOrder), Is.True);
            Assert.That(activeOrder, Is.SameAs(result.WorkOrder));
            Assert.That(CountTemplate(inventory, "iron_ore"), Is.EqualTo(0));
            Assert.That(CountTemplate(inventory, "fuel"), Is.EqualTo(0));
            Assert.That(board.GetClaimedBy(job.Id), Is.EqualTo(actor.Id));
            Assert.That(actor.ScheduleState, Is.EqualTo(ActorScheduleState.Assigned(job.Id, Site, FurnacePosition)));
        }

        [Test]
        public void StartRecipeForClaim_WithMissingInputs_DoesNotStartOrMutateInventory()
        {
            var actors = new ActorStore();
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(actor);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1);
            board.Add(job);
            var worksites = ActiveFurnaceStore();
            var inventory = InventoryWithInputs(ore: 2, fuel: 0);
            var recipe = SmeltIronRecipe(job.RecipeId);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, worksites, out _), Is.True);
            Assert.That(system.StartRecipeForClaim(actors, board, worksites, recipe, inventory, job.Id, out var result), Is.False);

            Assert.That(result, Is.EqualTo(default(JobRecipeStartResult)));
            Assert.That(system.TryGetActiveWorkOrder(job.Id, out _), Is.False);
            Assert.That(CountTemplate(inventory, "iron_ore"), Is.EqualTo(2));
            Assert.That(CountTemplate(inventory, "fuel"), Is.EqualTo(0));
            Assert.That(board.GetClaimedBy(job.Id), Is.EqualTo(actor.Id));
        }

        [Test]
        public void StartRecipeForClaim_WithBatchMissingInputs_DoesNotStartOrMutateInventory()
        {
            var actors = new ActorStore();
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(actor);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1, quantity: 2);
            board.Add(job);
            var worksites = ActiveFurnaceStore();
            var inventory = InventoryWithInputs(ore: 2, fuel: 1);
            var recipe = SmeltIronRecipe(job.RecipeId);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, worksites, out _), Is.True);
            Assert.That(system.StartRecipeForClaim(actors, board, worksites, recipe, inventory, job.Id, out var result), Is.False);

            Assert.That(result, Is.EqualTo(default(JobRecipeStartResult)));
            Assert.That(system.TryGetActiveWorkOrder(job.Id, out _), Is.False);
            Assert.That(CountTemplate(inventory, "iron_ore"), Is.EqualTo(2));
            Assert.That(CountTemplate(inventory, "fuel"), Is.EqualTo(1));
            Assert.That(board.GetClaimedBy(job.Id), Is.EqualTo(actor.Id));
        }

        [Test]
        public void StartRecipeForClaim_WithBatchInputs_PreflightsFullQuantityThenStartsOneWorkOrder()
        {
            var actors = new ActorStore();
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(actor);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1, quantity: 2);
            board.Add(job);
            var worksites = ActiveFurnaceStore();
            var inventory = InventoryWithInputs(ore: 4, fuel: 2);
            var recipe = SmeltIronRecipe(job.RecipeId);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, worksites, out _), Is.True);
            Assert.That(system.StartRecipeForClaim(actors, board, worksites, recipe, inventory, job.Id, out var result), Is.True);

            Assert.That(result.JobId, Is.EqualTo(job.Id));
            Assert.That(system.TryGetActiveWorkOrder(job.Id, out var activeOrder), Is.True);
            Assert.That(activeOrder, Is.SameAs(result.WorkOrder));
            Assert.That(CountTemplate(inventory, "iron_ore"), Is.EqualTo(2));
            Assert.That(CountTemplate(inventory, "fuel"), Is.EqualTo(1));
        }

        [Test]
        public void StartRecipeForClaim_RequiresClaimWithoutMutatingInventory()
        {
            var actors = new ActorStore();
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(actor);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1);
            board.Add(job);
            var inventory = InventoryWithInputs(ore: 2, fuel: 1);
            var system = new JobAssignmentSystem();

            Assert.That(
                system.StartRecipeForClaim(actors, board, ActiveFurnaceStore(), SmeltIronRecipe(job.RecipeId), inventory, job.Id, out var result),
                Is.False);

            Assert.That(result, Is.EqualTo(default(JobRecipeStartResult)));
            Assert.That(system.TryGetActiveWorkOrder(job.Id, out _), Is.False);
            Assert.That(CountTemplate(inventory, "iron_ore"), Is.EqualTo(2));
            Assert.That(CountTemplate(inventory, "fuel"), Is.EqualTo(1));
        }

        [Test]
        public void StartRecipeForClaim_RejectsDuplicateStartsWithoutDoubleConsumingInputs()
        {
            var actors = new ActorStore();
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(actor);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1);
            board.Add(job);
            var worksites = ActiveFurnaceStore();
            var inventory = InventoryWithInputs(ore: 4, fuel: 2);
            var recipe = SmeltIronRecipe(job.RecipeId);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, worksites, out _), Is.True);
            Assert.That(system.StartRecipeForClaim(actors, board, worksites, recipe, inventory, job.Id, out var first), Is.True);
            Assert.That(system.StartRecipeForClaim(actors, board, worksites, recipe, inventory, job.Id, out var second), Is.False);

            Assert.That(second, Is.EqualTo(default(JobRecipeStartResult)));
            Assert.That(system.TryGetActiveWorkOrder(job.Id, out var activeOrder), Is.True);
            Assert.That(activeOrder, Is.SameAs(first.WorkOrder));
            Assert.That(CountTemplate(inventory, "iron_ore"), Is.EqualTo(2));
            Assert.That(CountTemplate(inventory, "fuel"), Is.EqualTo(1));
        }

        [Test]
        public void TickAssignedJobs_AdvancesActiveWorkOrderWithoutCompletingEarly()
        {
            var actors = new ActorStore();
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(actor);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1);
            board.Add(job);
            var worksites = ActiveFurnaceStore();
            var inventory = InventoryWithInputs(ore: 2, fuel: 1);
            var eventLog = new WorldEventLog();
            var recipe = SmeltIronRecipe(job.RecipeId);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, worksites, out _), Is.True);
            Assert.That(system.StartRecipeForClaim(actors, board, worksites, recipe, inventory, job.Id, out _), Is.True);

            Assert.That(system.TickAssignedJobs(actors, board, inventory, eventLog, CreateOutput), Is.EqualTo(0));

            Assert.That(system.TryGetActiveWorkOrder(job.Id, out var activeOrder), Is.True);
            Assert.That(activeOrder.ProgressTicks, Is.EqualTo(1));
            Assert.That(board.Contains(job.Id), Is.True);
            Assert.That(board.GetClaimedBy(job.Id), Is.EqualTo(actor.Id));
            Assert.That(actor.ScheduleState, Is.EqualTo(ActorScheduleState.Assigned(job.Id, Site, FurnacePosition)));
            Assert.That(eventLog.IsEmpty, Is.True);
            Assert.That(CountTemplate(inventory, "iron_ingot"), Is.EqualTo(0));
        }

        [Test]
        public void TickAssignedJobs_CompletesBoardEntryAndClearsActorScheduleWhenRecipeCompletes()
        {
            var actors = new ActorStore();
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(actor);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1);
            board.Add(job);
            var worksites = ActiveFurnaceStore();
            var inventory = InventoryWithInputs(ore: 2, fuel: 1);
            var eventLog = new WorldEventLog();
            var recipe = SmeltIronRecipe(job.RecipeId);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, worksites, out _), Is.True);
            Assert.That(system.StartRecipeForClaim(actors, board, worksites, recipe, inventory, job.Id, out _), Is.True);
            Assert.That(system.TickAssignedJobs(actors, board, inventory, eventLog, CreateOutput), Is.EqualTo(0));

            Assert.That(system.TickAssignedJobs(actors, board, inventory, eventLog, CreateOutput), Is.EqualTo(1));

            Assert.That(system.TryGetActiveWorkOrder(job.Id, out _), Is.False);
            Assert.That(board.Contains(job.Id), Is.False);
            Assert.That(actor.ScheduleState, Is.EqualTo(ActorScheduleState.Idle));
            Assert.That(CountTemplate(inventory, "iron_ingot"), Is.EqualTo(1));
            Assert.That(eventLog.Count, Is.EqualTo(1));
            Assert.That(eventLog.Events[0].Kind, Is.EqualTo(WorldEventKind.RecipeCompleted));
            Assert.That(eventLog.Events[0].ActorId, Is.EqualTo(actor.Id));
            Assert.That(eventLog.Events[0].SiteId, Is.EqualTo(Site));
        }

        [Test]
        public void CanActorWorkJob_WithRecipeMismatch_ReturnsFalseWithoutMutatingInventory()
        {
            var system = new JobAssignmentSystem();
            var job = MakeRequest(10UL, priority: 1);
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            var inventory = InventoryWithInputs(ore: 2, fuel: 1);

            Assert.That(
                system.CanActorWorkJob(actor, job, ActiveFurnaceStore(), SmeltIronRecipe(new RecipeId(999UL)), inventory),
                Is.False);
            Assert.That(
                system.CanActorWorkJob(actor, job, ActiveFurnaceStore(), SmeltIronRecipe(job.RecipeId, worksiteKind: "oven"), inventory),
                Is.False);

            Assert.That(CountTemplate(inventory, "iron_ore"), Is.EqualTo(2));
            Assert.That(CountTemplate(inventory, "fuel"), Is.EqualTo(1));
        }

        private static JobRequest MakeRequest(ulong jobId, int priority, int quantity = 1)
        {
            return new JobRequest(
                new JobId(jobId),
                new RecipeId(200UL + jobId),
                Site,
                FurnacePosition,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(priority),
                quantity: quantity,
                requesterId: Requester);
        }

        private static WorksiteStore ActiveFurnaceStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(Site, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            return store;
        }

        private static WorksiteStore InactiveFurnaceStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(Site, FurnacePosition, WorksiteKind.Furnace, isActive: false));
            return store;
        }

        private static RecipeDef SmeltIronRecipe(RecipeId recipeId, string worksiteKind = "furnace")
        {
            return new RecipeDef(
                recipeId,
                worksiteKind,
                "smithing",
                durationTicks: 2,
                new[] { new RecipeIngredient("iron_ore", 2), new RecipeIngredient("fuel", 1) },
                new[] { new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1) });
        }

        private static InventoryState InventoryWithInputs(int ore, int fuel)
        {
            var inventory = new InventoryState(8);
            if (ore > 0)
                inventory.TryAdd(new InventoryItem(new ItemId(501UL), "iron_ore", "Iron Ore", ore));
            if (fuel > 0)
                inventory.TryAdd(new InventoryItem(new ItemId(502UL), "fuel", "Fuel", fuel));
            return inventory;
        }

        private static InventoryItem CreateOutput(RecipeOutput output)
        {
            return new InventoryItem(new ItemId(900UL), output.ItemTag, output.ItemTag, 1);
        }

        private static int CountTemplate(InventoryState inventory, string templateId)
        {
            var count = 0;
            foreach (var item in inventory.Items)
            {
                if (item.TemplateId == templateId)
                    count += item.Quantity;
            }

            return count;
        }

        private static ActorRecord CreateActor(ulong id, string name, ActorJobPreference preference = default, bool alive = true)
        {
            ActorJobPreference[] preferences = preference.Kind == JobKind.None ? null : new[] { preference };
            return new ActorRecord(
                new ActorId(id),
                name,
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(alive ? 12 : 0, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 5,
                armor: 1,
                baseDamage: 3,
                jobPreferences: preferences);
        }
    }
}

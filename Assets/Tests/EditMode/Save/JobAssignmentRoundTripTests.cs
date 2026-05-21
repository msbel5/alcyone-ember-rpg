using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Save;
using EmberCrpg.Simulation.Process;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

// Design note:
// Pins the Faz 3 job-save-proof rail without adding new SliceWorldState fields:
// JsonSliceSaveService carries JobBoard state like the existing Worksite rail,
// while actor records carry their schedule target through ActorSaveData.
namespace EmberCrpg.Tests.EditMode.Save
{
    public sealed class JobAssignmentRoundTripTests
    {
        private static readonly JobId Job = new JobId(701UL);
        private static readonly RecipeId Recipe = new RecipeId(1001UL);
        private static readonly SiteId FurnaceSite = new SiteId(77UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);

        [Test]
        public void JsonDto_RoundTripsClaimedJobBoardAndActorScheduleState()
        {
            var world = new SliceWorldFactory().Create(303);
            var actor = world.Actors.FirstByRole(ActorRole.Player);
            actor.ApplyJobPreferences(new[] { new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)) });
            actor.ApplyScheduleState(ActorScheduleState.Assigned(Job, FurnaceSite, FurnacePosition));

            var board = new JobBoard();
            var request = new JobRequest(
                Job,
                Recipe,
                FurnaceSite,
                FurnacePosition,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(2),
                quantity: 2,
                requesterId: actor.Id);
            board.Add(request);
            Assert.That(board.TryClaim(Job, actor.Id, out _), Is.True);

            var activeOrder = RecipeWorkOrder.Resume(CreateSmeltIronIngotRecipe(), FurnaceSite, FurnacePosition, actor.Id, progressTicks: 1);
            var service = new JsonSliceSaveService(ResolveRecipe)
            {
                Jobs = board,
                Worksites = CreateActiveFurnaceStore(),
            };
            service.ReplaceRecipeWorkOrders(new[] { activeOrder });

            var json = service.SaveToJson(world);
            Assert.That(json, Does.Contain("jobs"));
            Assert.That(json, Does.Contain("currentJobId"));

            var loaded = service.LoadFromJson(json);
            var loadedActor = loaded.Actors.Get(actor.Id);
            var loadedJob = service.Jobs.Requests.Single();

            Assert.That(loadedActor.JobPreferences.Single().Kind, Is.EqualTo(JobKind.Smith));
            Assert.That(loadedActor.JobPreferences.Single().Priority, Is.EqualTo(JobPriority.Active(1)));
            Assert.That(loadedActor.ScheduleState.CurrentJobId, Is.EqualTo(Job));
            Assert.That(loadedActor.ScheduleState.TargetSiteId, Is.EqualTo(FurnaceSite));
            Assert.That(loadedActor.ScheduleState.TargetWorksitePosition, Is.EqualTo(FurnacePosition));

            Assert.That(loadedJob.Id, Is.EqualTo(Job));
            Assert.That(loadedJob.RecipeId, Is.EqualTo(Recipe));
            Assert.That(loadedJob.Quantity, Is.EqualTo(2));
            Assert.That(service.Jobs.GetClaimedBy(Job), Is.EqualTo(actor.Id));
            Assert.That(service.RecipeWorkOrders.Single().ProgressTicks, Is.EqualTo(1));
            Assert.That(service.Worksites.Get(FurnaceSite, FurnacePosition).IsActive, Is.True);
        }

        private static RecipeDef ResolveRecipe(RecipeId recipeId)
        {
            var recipe = CreateSmeltIronIngotRecipe();
            return recipe.Id.Equals(recipeId) ? recipe : null;
        }

        private static RecipeDef CreateSmeltIronIngotRecipe()
        {
            return new RecipeDef(
                Recipe,
                "furnace",
                "smelting",
                durationTicks: 2,
                new[] { new RecipeIngredient("iron_ore", 2), new RecipeIngredient("fuel", 1) },
                new[] { new RecipeOutput("iron_ingot", ItemMaterial.Iron, ItemQuality.Common, 1) });
        }

        private static WorksiteStore CreateActiveFurnaceStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(FurnaceSite, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            return store;
        }
    }
}

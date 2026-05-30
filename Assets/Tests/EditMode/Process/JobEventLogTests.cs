using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Process;
using NUnit.Framework;

// Design note:
// JobEventLogTests pins the Phase 3 player-visible job assignment chronicle.
// Assignment and completion now emit WorldEventLog rows with reason traces so
// the Unity/debug layer has a deterministic "player can watch jobs progress"
// signal before save/load mapping lands.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies job-assignment world-event emission.</summary>
    public sealed class JobEventLogTests
    {
        private static readonly SiteId Site = new SiteId(30UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);
        private static readonly ActorId Requester = new ActorId(99UL);

        [Test]
        public void TryAssignNext_WithEventLog_AppendsJobAssignedReasonTrace()
        {
            var actors = new ActorStore();
            var actor = CreateActor(1UL, "Smith", new ActorJobPreference(JobKind.Smith, JobPriority.Active(1)));
            actors.Add(actor);
            var board = new JobBoard();
            var job = MakeRequest(10UL, priority: 1);
            board.Add(job);
            var log = new WorldEventLog();
            var system = new JobAssignmentSystem();
            var now = new GameTime(120L);

            Assert.That(system.TryAssignNext(actors, board, ActiveFurnaceStore(), log, now, out var result), Is.True);

            Assert.That(result.ActorId, Is.EqualTo(actor.Id));
            Assert.That(log.Events, Has.Count.EqualTo(1));
            var evt = log.Events.Single();
            Assert.That(evt.Kind, Is.EqualTo(WorldEventKind.JobAssigned));
            Assert.That(evt.ActorId, Is.EqualTo(actor.Id));
            Assert.That(evt.SiteId, Is.EqualTo(Site));
            Assert.That(evt.Tick, Is.EqualTo(now));
            Assert.That(evt.Reason, Is.EqualTo("job_assigned:10"));
            Assert.That(evt.ReasonTrace.Causes, Is.EqualTo(new[] { "job:10", "actor:1", "site:30", "worksite:4,5" }));
        }

        [Test]
        public void TickAssignedJobs_WithGameTime_AppendsJobCompletedAfterRecipeCompleted()
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
            var recipe = RecipeFixtureCatalog.SmeltIronIngot(job.RecipeId);
            var system = new JobAssignmentSystem();

            Assert.That(system.TryAssignNext(actors, board, worksites, out _), Is.True);
            Assert.That(system.StartRecipeForClaim(actors, board, worksites, recipe, inventory, job.Id, out _), Is.True);
            Assert.That(system.TickAssignedJobs(actors, board, worksites, inventory, eventLog, new GameTime(200L), CreateOutput), Is.EqualTo(0));

            Assert.That(system.TickAssignedJobs(actors, board, worksites, inventory, eventLog, new GameTime(201L), CreateOutput), Is.EqualTo(1));

            Assert.That(eventLog.Events, Has.Count.EqualTo(2));
            Assert.That(eventLog.Events[0].Kind, Is.EqualTo(WorldEventKind.RecipeCompleted));
            var evt = eventLog.Events[1];
            Assert.That(evt.Kind, Is.EqualTo(WorldEventKind.JobCompleted));
            Assert.That(evt.ActorId, Is.EqualTo(actor.Id));
            Assert.That(evt.SiteId, Is.EqualTo(Site));
            Assert.That(evt.Tick, Is.EqualTo(new GameTime(201L)));
            Assert.That(evt.Reason, Is.EqualTo("job_completed:10"));
            Assert.That(evt.ReasonTrace.Causes, Is.EqualTo(new[] { "job:10", "recipe:1001", "quantity:1", "worksite:Furnace" }));
            Assert.That(board.Contains(job.Id), Is.False);
            Assert.That(actor.ScheduleState, Is.EqualTo(ActorScheduleState.Idle));
        }

        private static JobRequest MakeRequest(ulong jobId, int priority)
        {
            return new JobRequest(
                new JobId(jobId),
                new RecipeId(1001UL),
                Site,
                FurnacePosition,
                WorksiteKind.Furnace,
                JobKind.Smith,
                JobPriority.Active(priority),
                quantity: 1,
                requesterId: Requester);
        }

        private static WorksiteStore ActiveFurnaceStore()
        {
            var store = new WorksiteStore();
            store.Add(new WorksiteRecord(Site, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            return store;
        }

        private static InventoryState InventoryWithInputs(int ore, int fuel)
        {
            var inventory = new InventoryState(12);
            if (ore > 0)
                inventory.TryAdd(new InventoryItem(new ItemId(1UL), "iron_ore", "Iron Ore", ore));
            if (fuel > 0)
                inventory.TryAdd(new InventoryItem(new ItemId(2UL), "fuel", "Fuel", fuel));
            return inventory;
        }

        private static InventoryItem CreateOutput(RecipeOutput output)
        {
            return new InventoryItem(new ItemId(9001UL), output.ItemTag, output.ItemTag, 1);
        }

        private static ActorRecord CreateActor(ulong id, string name, ActorJobPreference preference)
        {
            return new ActorRecord(
                new ActorId(id),
                name,
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(12, 12), new VitalStat(8, 8), new VitalStat(6, 6)),
                new GridPosition(0, 0),
                accuracy: 10,
                dodge: 5,
                armor: 1,
                baseDamage: 3,
                jobPreferences: new[] { preference });
        }
    }
}

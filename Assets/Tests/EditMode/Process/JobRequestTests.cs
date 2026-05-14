using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using NUnit.Framework;

// Design note:
// These tests pin the pure JobRequest row before assignment, recipe ticking,
// save/load, or EventLog integration. A request is only a validated piece of
// pending PROCESS data that JobBoard can order and claim.
namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies the minimal immutable job request contract.</summary>
    public sealed class JobRequestTests
    {
        private static readonly JobId Job = new JobId(1UL);
        private static readonly RecipeId Recipe = new RecipeId(2UL);
        private static readonly SiteId Site = new SiteId(3UL);
        private static readonly GridPosition Worksite = new GridPosition(4, 5);
        private static readonly ActorId Requester = new ActorId(6UL);

        private static JobRequest MakeRequest(
            JobId? id = null,
            RecipeId? recipeId = null,
            SiteId? siteId = null,
            WorksiteKind worksiteKind = WorksiteKind.Furnace,
            JobKind kind = JobKind.Smith,
            JobPriority? priority = null,
            int quantity = 1,
            ActorId? requesterId = null)
        {
            return new JobRequest(
                id ?? Job,
                recipeId ?? Recipe,
                siteId ?? Site,
                Worksite,
                worksiteKind,
                kind,
                priority ?? JobPriority.Active(1),
                quantity,
                requesterId ?? Requester);
        }

        /// <summary>The constructor stores every validated identity and routing field.</summary>
        [Test]
        public void Constructor_StoresFields()
        {
            var request = MakeRequest(quantity: 3, priority: JobPriority.Active(2));

            Assert.That(request.Id, Is.EqualTo(Job));
            Assert.That(request.RecipeId, Is.EqualTo(Recipe));
            Assert.That(request.SiteId, Is.EqualTo(Site));
            Assert.That(request.WorksitePosition, Is.EqualTo(Worksite));
            Assert.That(request.WorksiteKind, Is.EqualTo(WorksiteKind.Furnace));
            Assert.That(request.Kind, Is.EqualTo(JobKind.Smith));
            Assert.That(request.Priority, Is.EqualTo(JobPriority.Active(2)));
            Assert.That(request.Quantity, Is.EqualTo(3));
            Assert.That(request.RequesterId, Is.EqualTo(Requester));
        }

        /// <summary>Stable handles must all be concrete before a job can enter the board.</summary>
        [Test]
        public void Constructor_RejectsEmptyIds()
        {
            Assert.Throws<ArgumentException>(() => MakeRequest(id: default(JobId)));
            Assert.Throws<ArgumentException>(() => MakeRequest(recipeId: default(RecipeId)));
            Assert.Throws<ArgumentException>(() => MakeRequest(siteId: default(SiteId)));
            Assert.Throws<ArgumentException>(() => MakeRequest(requesterId: default(ActorId)));
        }

        /// <summary>A request must target a concrete worksite kind.</summary>
        [Test]
        public void Constructor_RejectsMissingWorksite()
        {
            Assert.Throws<ArgumentException>(() => MakeRequest(worksiteKind: WorksiteKind.None));
        }

        /// <summary>JobKind.None is only a sentinel and cannot be queued.</summary>
        [Test]
        public void Constructor_RejectsNoneJobKind()
        {
            Assert.Throws<ArgumentException>(() => MakeRequest(kind: JobKind.None));
        }

        /// <summary>Requests need active priority and a positive quantity.</summary>
        [Test]
        public void Constructor_RejectsInactivePriorityOrQuantity()
        {
            Assert.Throws<ArgumentException>(() => MakeRequest(priority: JobPriority.Disabled));
            Assert.Throws<ArgumentOutOfRangeException>(() => MakeRequest(quantity: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => MakeRequest(quantity: -1));
        }
    }
}

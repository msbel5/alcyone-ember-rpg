using System.Linq;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Presentation.Ember.Save;
using EmberCrpg.Simulation.World;
using NUnit.Framework;
using UnityEngine;

// Design note:
// Pins the process-state save rail. W34 WORK slice migration (docs/ruh/w34/02 §5.2): the
// service's RecipeWorkOrder park list is RETIRED — the recipeWorkOrders DTO array is fed from
// the pure-Domain WorldState.WorkOrders ledger now, jobId-bound. This pins the NEW contract:
// a mid-progress bench row survives the JSON boundary field-for-field (progress, batch
// counter, attribution), legacy jobId-less rows drop, and the worksite rail is unchanged.
namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>Verifies work-order + worksite save DTO round-trips over the W34 ledger rail.</summary>
    public sealed class RecipeWorksiteRoundTripTests
    {
        private static readonly SiteId FurnaceSite = new SiteId(77UL);
        private static readonly GridPosition FurnacePosition = new GridPosition(4, 5);

        [Test]
        public void JsonDto_RoundTripsActiveWorksiteAndMidProgressWorkOrderRow()
        {
            var world = new WorldFactory().Create(2026);
            world.Worksites.Add(new WorksiteRecord(FurnaceSite, FurnacePosition, WorksiteKind.Furnace, isActive: true));
            world.WorkOrders.Add(new WorkOrderRecord
            {
                JobId = 701UL,
                RecipeId = 1001UL,
                SiteId = FurnaceSite.Value,
                PositionX = FurnacePosition.X,
                PositionY = FurnacePosition.Y,
                StartedByActorId = 12UL,
                ProgressTicks = 1,
                CompletedExecutions = 1,
            });

            var service = new JsonSliceSaveService();
            var json = service.SaveToJson(world);
            Assert.That(json, Does.Contain("worksites"));
            Assert.That(json, Does.Contain("recipeWorkOrders"));

            var loaded = service.LoadFromJson(json);

            Assert.That(loaded.Worksites.Get(FurnaceSite, FurnacePosition).IsActive, Is.True);
            var row = loaded.WorkOrders.Rows.Single();
            Assert.That(row.JobId, Is.EqualTo(701UL), "the jobId rebind key survives the boundary");
            Assert.That(row.RecipeId, Is.EqualTo(1001UL));
            Assert.That(row.SiteId, Is.EqualTo(FurnaceSite.Value));
            Assert.That(row.PositionX, Is.EqualTo(FurnacePosition.X));
            Assert.That(row.PositionY, Is.EqualTo(FurnacePosition.Y));
            Assert.That(row.StartedByActorId, Is.EqualTo(12UL), "attribution is the STARTER, kept verbatim");
            Assert.That(row.ProgressTicks, Is.EqualTo(1), "the bench counter resumes where it froze");
            Assert.That(row.CompletedExecutions, Is.EqualTo(1), "the batch counter is saved truth now");
            Assert.That(loaded.WorkOrders.TryGetByJob(701UL, out _), Is.True,
                "the derived job index is rebuilt on load");
        }

        [Test]
        public void LegacyRows_WithoutAJobBinding_AreDroppedOnLoad()
        {
            // Legacy park-list rows carried no jobId (the DTO field did not exist; JsonUtility
            // reads the missing field as 0). They were never restored to the world before W34
            // either — the drop IS the status quo, not a regression (docs/ruh/w34/02 §5.2).
            var world = new WorldFactory().Create(2027);
            var service = new JsonSliceSaveService();
            var data = JsonUtility.FromJson<WorldSaveData>(service.SaveToJson(world));
            data.recipeWorkOrders = new[]
            {
                new RecipeWorkOrderSaveData
                {
                    recipeId = 1001L, siteId = 77L, positionX = 4, positionY = 5,
                    actorId = 12L, progressTicks = 17, // jobId stays 0: the legacy shape
                },
            };

            var loaded = service.LoadFromJson(JsonUtility.ToJson(data, true));
            Assert.That(loaded.WorkOrders.Rows, Is.Empty,
                "a jobId-less legacy row cannot rebind to a claim and is dropped, not half-loaded");
        }
    }
}

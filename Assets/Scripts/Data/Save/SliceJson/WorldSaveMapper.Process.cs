// WorldSaveMapper partial — process: worksites, jobs, soil, plants (split from the 961-line monolith, NAME/LOC-split).
using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Data.Save
{
    public static partial class WorldSaveMapper
    {
        public static WorksiteSaveData[] ToWorksiteData(WorksiteStore store)
        {
            return (store?.Records ?? Array.Empty<WorksiteRecord>()).Select(ToWorksiteData).ToArray();
        }

        public static WorksiteStore ToWorksiteStore(WorksiteSaveData[] data)
        {
            var store = new WorksiteStore();
            foreach (var record in data ?? Array.Empty<WorksiteSaveData>())
            {
                if (record != null)
                    store.Add(ToWorksiteRecord(record));
            }

            return store;
        }

        // Codex audit (seventh pass B-P1 #9, #10): the previous methods
        // `ToRecipeWorkOrderData(RecipeWorkOrder)` and
        // `ToRecipeWorkOrder(RecipeWorkOrderSaveData, ...)` took/returned a
        // Simulation.Process type, forcing this Data asmdef to reference
        // EmberCrpg.Simulation. Moved to
        // EmberCrpg.Simulation.Process.WorldSaveRehydration so the Data
        // asmdef no longer leaks the Simulation namespace.

        // W34 WORK slice (docs/ruh/w34/02 §5.2): the pure-Domain WorkOrderLedger rides the SAME
        // recipeWorkOrders DTO array (append-only: + jobId + completedExecutions). The Presentation
        // save bridge may still overwrite the array from its legacy Simulation park list (its rows
        // carry jobId 0), so the load side drops jobId == 0 rows — legacy rows were never restored
        // to the world before either; status quo, no regression.
        public static RecipeWorkOrderSaveData[] ToRecipeWorkOrderData(WorkOrderLedger ledger)
        {
            return (ledger?.Rows ?? new List<WorkOrderRecord>())
                .Where(row => row != null)
                .Select(row => new RecipeWorkOrderSaveData
                {
                    recipeId = (long)row.RecipeId,
                    siteId = (long)row.SiteId,
                    positionX = row.PositionX,
                    positionY = row.PositionY,
                    actorId = (long)row.StartedByActorId,
                    progressTicks = row.ProgressTicks,
                    jobId = (long)row.JobId,
                    completedExecutions = row.CompletedExecutions,
                })
                .ToArray();
        }

        public static WorkOrderLedger ToWorkOrderLedger(RecipeWorkOrderSaveData[] data)
        {
            var ledger = new WorkOrderLedger();
            foreach (var row in data ?? Array.Empty<RecipeWorkOrderSaveData>())
            {
                if (row == null || row.jobId == 0L)
                    continue; // legacy / park-list row: never world-restored before W34 — keep dropping
                ledger.Add(new WorkOrderRecord
                {
                    JobId = (ulong)row.jobId,
                    RecipeId = (ulong)row.recipeId,
                    SiteId = (ulong)row.siteId,
                    PositionX = row.positionX,
                    PositionY = row.positionY,
                    StartedByActorId = (ulong)row.actorId,
                    ProgressTicks = row.progressTicks,
                    CompletedExecutions = row.completedExecutions,
                });
            }

            ledger.RebuildIndexes();
            return ledger;
        }

        public static JobRequestSaveData[] ToJobBoardData(JobBoard board)
        {
            return (board?.Requests ?? Array.Empty<JobRequest>()).Select(request => ToJobRequestData(request, board)).ToArray();
        }

        public static JobBoard ToJobBoard(JobRequestSaveData[] data)
        {
            var board = new JobBoard();
            // PR#138 bot review fix: add jobs in insertion order first, then restore
            // claims in original claim-sequence order using the explicit accessor so
            // GetQueueIndex returns the same value after a roundtrip.
            var sorted = (data ?? Array.Empty<JobRequestSaveData>())
                .Where(d => d != null)
                .ToArray();

            foreach (var saved in sorted)
            {
                var request = new JobRequest(
                    new JobId((ulong)saved.id),
                    new RecipeId((ulong)saved.recipeId),
                    new SiteId((ulong)saved.siteId),
                    new GridPosition(saved.positionX, saved.positionY),
                    (WorksiteKind)saved.worksiteKind,
                    (JobKind)saved.kind,
                    JobPriority.Active(saved.priority),
                    saved.quantity,
                    new ActorId((ulong)saved.requesterId));
                board.Add(request);
            }

            foreach (var saved in sorted.OrderBy(d => d.claimSequence))
            {
                var claimedBy = new ActorId((ulong)saved.claimedByActorId);
                if (claimedBy.IsEmpty) continue;

                var restored = saved.claimSequence > 0
                    ? board.TryRestoreClaim(new JobId((ulong)saved.id), claimedBy, saved.claimSequence)
                    : board.TryClaim(new JobId((ulong)saved.id), claimedBy, out _);
                if (!restored)
                    throw new InvalidOperationException($"JobBoard save data could not restore claim for {saved.id}.");
            }

            return board;
        }

        public static SoilComponentSaveData[] ToSoilComponentData(ComponentStore<SoilComponent> soils)
        {
            return (soils?.Rows ?? Array.Empty<System.Collections.Generic.KeyValuePair<WorldComponentId, SoilComponent>>())
                .Select(row => ToSoilComponentData(row.Value))
                .ToArray();
        }

        public static ComponentStore<SoilComponent> ToSoilComponentStore(SoilComponentSaveData[] data)
        {
            var store = new ComponentStore<SoilComponent>();
            foreach (var soil in data ?? Array.Empty<SoilComponentSaveData>())
            {
                if (soil == null)
                    continue;

                var component = new SoilComponent(
                    new WorldComponentId((ulong)soil.id),
                    new SiteId((ulong)soil.siteId),
                    new GridPosition(soil.positionX, soil.positionY),
                    soil.fertility,
                    soil.moisture,
                    new WorldComponentId((ulong)soil.plantId));
                store.Add(component.Id, component);
            }

            return store;
        }

        public static PlantComponentSaveData[] ToPlantComponentData(ComponentStore<PlantComponent> plants)
        {
            return (plants?.Rows ?? Array.Empty<System.Collections.Generic.KeyValuePair<WorldComponentId, PlantComponent>>())
                .Select(row => ToPlantComponentData(row.Value))
                .ToArray();
        }

        public static ComponentStore<PlantComponent> ToPlantComponentStore(PlantComponentSaveData[] data)
        {
            var store = new ComponentStore<PlantComponent>();
            foreach (var plant in data ?? Array.Empty<PlantComponentSaveData>())
            {
                if (plant == null)
                    continue;

                var component = new PlantComponent(
                    new WorldComponentId((ulong)plant.id),
                    new SiteId((ulong)plant.siteId),
                    new GridPosition(plant.positionX, plant.positionY),
                    plant.speciesId,
                    new PlantStageId(plant.stageId),
                    plant.daysInStage);
                store.Add(component.Id, component);
            }

            return store;
        }

        private static SoilComponentSaveData ToSoilComponentData(SoilComponent soil)
        {
            return new SoilComponentSaveData
            {
                id = (long)soil.Id.Value,
                siteId = (long)soil.SiteId.Value,
                positionX = soil.Position.X,
                positionY = soil.Position.Y,
                fertility = soil.Fertility,
                moisture = soil.Moisture,
                plantId = (long)soil.PlantId.Value,
            };
        }

        private static PlantComponentSaveData ToPlantComponentData(PlantComponent plant)
        {
            return new PlantComponentSaveData
            {
                id = (long)plant.Id.Value,
                siteId = (long)plant.SiteId.Value,
                positionX = plant.Position.X,
                positionY = plant.Position.Y,
                speciesId = plant.SpeciesId,
                stageId = plant.StageId.Value,
                daysInStage = plant.DaysInStage,
            };
        }

        private static JobRequestSaveData ToJobRequestData(JobRequest request, JobBoard board)
        {
            return new JobRequestSaveData
            {
                id = (long)request.Id.Value,
                recipeId = (long)request.RecipeId.Value,
                siteId = (long)request.SiteId.Value,
                positionX = request.WorksitePosition.X,
                positionY = request.WorksitePosition.Y,
                worksiteKind = (int)request.WorksiteKind,
                kind = (int)request.Kind,
                priority = request.Priority.Value,
                quantity = request.Quantity,
                requesterId = (long)request.RequesterId.Value,
                claimedByActorId = (long)board.GetClaimedBy(request.Id).Value,
                claimSequence = board.GetClaimSequence(request.Id),
            };
        }
    }
}

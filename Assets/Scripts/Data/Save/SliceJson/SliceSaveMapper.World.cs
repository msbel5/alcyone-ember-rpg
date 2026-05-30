// SliceSaveMapper partial — world stores: actors, items, sites, factions (split from the 961-line monolith, NAME/LOC-split).
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
    public static partial class SliceSaveMapper
    {
        private static ActorSaveData[] ToActorStoreData(ActorStore store)
        {
            return (store?.Records ?? Array.Empty<ActorRecord>()).Select(ActorSaveMapper.ToData).ToArray();
        }

        private static ActorStore ToActorStore(ActorSaveData[] data)
        {
            var store = new ActorStore();
            foreach (var actor in data ?? Array.Empty<ActorSaveData>())
            {
                if (actor != null)
                    store.Add(ActorSaveMapper.ToActor(actor));
            }
            return store;
        }

        private static ItemRecordSaveData[] ToItemStoreData(ItemStore store)
        {
            return (store?.Records ?? Array.Empty<ItemRecord>()).Select(ToItemRecordData).ToArray();
        }

        private static ItemRecordSaveData ToItemRecordData(ItemRecord record)
        {
            return new ItemRecordSaveData
            {
                id = (long)record.Id.Value,
                material = (int)record.Material,
                quality = (int)record.Quality,
                slot = (int)record.Slot,
                slotCode = record.Slot.Code,
            };
        }

        private static ItemStore ToItemStore(ItemRecordSaveData[] data)
        {
            var store = new ItemStore();
            foreach (var record in data ?? Array.Empty<ItemRecordSaveData>())
            {
                if (record != null)
                    store.Add(new ItemRecord(new ItemId((ulong)record.id), (ItemMaterial)record.material, (ItemQuality)record.quality, ToEquipmentSlot(record.slotCode, record.slot)));
            }
            return store;
        }

        private static SiteRecordSaveData[] ToSiteStoreData(SiteStore store)
        {
            return (store?.Records ?? Array.Empty<SiteRecord>()).Select(ToSiteRecordData).ToArray();
        }

        private static SiteRecordSaveData ToSiteRecordData(SiteRecord record)
        {
            return new SiteRecordSaveData
            {
                id = (long)record.Id.Value,
                kind = (int)record.Kind,
                name = record.Name,
                minX = record.MinBound.X,
                minY = record.MinBound.Y,
                maxX = record.MaxBound.X,
                maxY = record.MaxBound.Y,
            };
        }

        private static SiteStore ToSiteStore(SiteRecordSaveData[] data)
        {
            var store = new SiteStore();
            foreach (var record in data ?? Array.Empty<SiteRecordSaveData>())
            {
                if (record != null)
                {
                    store.Add(new SiteRecord(
                        new SiteId((ulong)record.id),
                        (SiteKind)record.kind,
                        record.name,
                        new GridPosition(record.minX, record.minY),
                        new GridPosition(record.maxX, record.maxY)));
                }
            }
            return store;
        }

        private static WorksiteSaveData ToWorksiteData(WorksiteRecord record)
        {
            return new WorksiteSaveData
            {
                siteId = (long)record.SiteId.Value,
                positionX = record.Position.X,
                positionY = record.Position.Y,
                kind = (int)record.Kind,
                isActive = record.IsActive,
            };
        }

        private static WorksiteRecord ToWorksiteRecord(WorksiteSaveData data)
        {
            return new WorksiteRecord(
                new SiteId((ulong)data.siteId),
                new GridPosition(data.positionX, data.positionY),
                (WorksiteKind)data.kind,
                data.isActive);
        }

        private static FactionRecordSaveData[] ToFactionStoreData(FactionStore store)
        {
            return (store?.Records ?? Array.Empty<FactionRecord>()).Select(ToFactionRecordData).ToArray();
        }

        private static FactionRecordSaveData ToFactionRecordData(FactionRecord record)
        {
            return new FactionRecordSaveData
            {
                id = (long)record.Id.Value,
                name = record.Name,
                tags = record.Tags.ToArray(),
            };
        }

        private static FactionStore ToFactionStore(FactionRecordSaveData[] data)
        {
            var store = new FactionStore();
            foreach (var record in data ?? Array.Empty<FactionRecordSaveData>())
            {
                if (record != null)
                    store.Add(new FactionRecord(new FactionId((ulong)record.id), record.name, record.tags ?? Array.Empty<string>()));
            }
            return store;
        }

        private static FactionReputationSaveData[] ToFactionReputationData(FactionStore store)
        {
            return (store?.ReputationRows ?? Array.Empty<FactionReputationRow>())
                .Select(row => new FactionReputationSaveData
                {
                    a = (long)row.A.Value,
                    b = (long)row.B.Value,
                    reputation = row.Reputation.Value,
                })
                .ToArray();
        }

        private static void ApplyFactionReputations(FactionStore store, FactionReputationSaveData[] data)
        {
            if (store == null)
                return;

            foreach (var row in data ?? Array.Empty<FactionReputationSaveData>())
            {
                if (row == null || row.a == 0L || row.b == 0L || row.a == row.b)
                    continue;
                store.WithReputation(new FactionId((ulong)row.a), new FactionId((ulong)row.b), new FactionReputation(row.reputation));
            }
        }
    }
}

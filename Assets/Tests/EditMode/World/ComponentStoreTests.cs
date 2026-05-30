using System;
using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Verifies deterministic ComponentStore behavior with the Phase 5 soil consumer.</summary>
    public sealed class ComponentStoreTests
    {
        [Test]
        public void AddGetAndRows_PreserveInsertionOrderForSoilComponents()
        {
            var store = new ComponentStore<SoilComponent>();
            var first = CreateSoil(1, 1);
            var second = CreateSoil(2, 2);

            store.Add(first.Id, first);
            store.Add(second.Id, second);

            Assert.That(store.Get(first.Id), Is.SameAs(first));
            Assert.That(store.Rows.Select(row => row.Key).ToArray(), Is.EqualTo(new[]
            {
                first.Id,
                second.Id,
            }));
        }

        [Test]
        public void Replace_UpdatesComponentWithoutChangingOrder()
        {
            var store = new ComponentStore<SoilComponent>();
            var soil = CreateSoil(1, 1);
            store.Add(soil.Id, soil);

            var replaced = soil.WithMoisture(80);

            Assert.That(store.Replace(soil.Id, replaced), Is.True);
            Assert.That(store.Get(soil.Id).Moisture, Is.EqualTo(80));
            Assert.That(store.Rows.Single().Key, Is.EqualTo(soil.Id));
        }

        [Test]
        public void RejectsInvalidRows()
        {
            var store = new ComponentStore<SoilComponent>();
            var soil = CreateSoil(1, 1);

            Assert.Throws<ArgumentException>(() => store.Add(default, soil));
            Assert.Throws<ArgumentNullException>(() => store.Add(new WorldComponentId(9), null));
            store.Add(soil.Id, soil);
            Assert.Throws<InvalidOperationException>(() => store.Add(soil.Id, soil));
            Assert.Throws<KeyNotFoundException>(() => store.Get(new WorldComponentId(99)));
        }

        private static SoilComponent CreateSoil(ulong id, int x)
        {
            return new SoilComponent(
                new WorldComponentId(id),
                new SiteId(3),
                new GridPosition(x, 4),
                50,
                25,
                default);
        }
    }
}

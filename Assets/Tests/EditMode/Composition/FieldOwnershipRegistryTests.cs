using System.Linq;
using EmberCrpg.Simulation.Composition;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    /// <summary>REFORM #2 lint: every declared writer must be a REAL registered system, and
    /// the core mutable fields must have a declared ownership row at all.</summary>
    public sealed class FieldOwnershipRegistryTests
    {
        [Test]
        public void EveryDeclaredWriter_ExistsInTheTickRegistry()
        {
            var knownIds = new[]
            {
                "core.time", "core.magic", "living.schedule", "living.companion_follow",
                "living.eatOnArrival", "econ.jobs", "quest.tick", "living.needs",
                "living.consumption", "living.predation", "living.companion_guard",
                "living.witness", "living.ambient", "living.rumors",
                "world.growth", "world.harvest", "econ.prices", "econ.trade",
                "world.shortage", "world.history", "econ.caravan", "faction.decay",
            };
            var missing = FieldOwnershipRegistry.Writers
                .SelectMany(kv => kv.Value)
                .Select(w => w.Split('@')[0])
                .Distinct()
                .Where(id => !knownIds.Contains(id))
                .ToList();
            Assert.That(missing, Is.Empty,
                "ownership ledger names writers that are not registered systems: " + string.Join(", ", missing));
        }

        [Test]
        public void CoreMutableFields_HaveDeclaredOwnership()
        {
            foreach (var field in new[]
                { "Actor.Position", "Actor.Needs", "Actor.Vitals", "World.Stockpiles", "World.GuardPursuits" })
                Assert.That(FieldOwnershipRegistry.Writers.ContainsKey(field), Is.True,
                    field + " has no declared ownership - undeclared writers breed cadence conflicts");
        }
    }
}

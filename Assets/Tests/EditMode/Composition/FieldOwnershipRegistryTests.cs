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
        public void EveryDeclaredWriter_IsARealRegisteredSystem_AtItsDeclaredSlot()
        {
            // B03: the known-id set is DERIVED from the composition root — a hand-typed list
            // rotted into six ghosts and blessed the econ.trade@Daily:28 phantom writer.
            // Linting the FULL "id@Cadence:Order" triple is simultaneously the reverse lint:
            // a declared writer with no real system at that exact slot (or one whose cadence/
            // order drifted without a ledger update) fails here.
            var registered = DefaultRegistryFixture.CreateDefault().Ordered
                .Select(s => $"{s.Id}@{s.Cadence}:{s.Order}")
                .ToHashSet();
            var ghosts = FieldOwnershipRegistry.Writers
                .SelectMany(kv => kv.Value)
                .Distinct()
                .Where(w => !registered.Contains(w))
                .ToList();
            Assert.That(ghosts, Is.Empty,
                "ownership ledger declares writers with no real registered system at that slot: "
                + string.Join(", ", ghosts));
        }

        [Test]
        public void CoreMutableFields_HaveDeclaredOwnership()
        {
            foreach (var field in new[]
                { "Actor.Position", "Actor.Needs", "Actor.Vitals", "World.Stockpiles", "World.GuardPursuits",
                  "Actor.ActionState", "World.Reservations" }) // W32: the new single-writer rows are pinned
                Assert.That(FieldOwnershipRegistry.Writers.ContainsKey(field), Is.True,
                    field + " has no declared ownership - undeclared writers breed cadence conflicts");
        }
    }
}

using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Visual;
using NUnit.Framework;

// Pattern: Specification checks — pin the pure HUD-interest rule and filtered tail ordering.
namespace EmberCrpg.Tests.EditMode.Visual
{
    public sealed class WorldEventInterestTests
    {
        [TestCase("NeedChanged", false)]
        [TestCase("ActorStepped", false)]
        [TestCase("RecipeCompleted", true)]
        [TestCase("JobCompleted", true)]
        [TestCase("JobAssigned", true)]
        [TestCase("CombatResolved", true)]
        public void IsHudWorthy_ReturnsExpectedDecision(string kindCode, bool expected)
        {
            Assert.That(WorldEventInterest.IsHudWorthy(kindCode), Is.EqualTo(expected));
        }

        [Test]
        public void TailSnapshot_FilteredOverload_KeepsMatchingKindsInOrder()
        {
            var log = new WorldEventLog();
            log.Append(new WorldEvent(new GameTime(10), WorldEventKind.NeedChanged, new ActorId(1UL), default, "tick"));
            log.Append(new WorldEvent(new GameTime(20), WorldEventKind.JobAssigned, new ActorId(2UL), default, "assigned"));
            log.Append(new WorldEvent(new GameTime(30), WorldEventKind.ActorStepped, new ActorId(3UL), default, "step"));
            log.Append(new WorldEvent(new GameTime(40), WorldEventKind.CombatResolved, new ActorId(4UL), default, "won"));

            var snapshot = WorldEventTailSnapshot.FromLog(log, 8, WorldEventInterest.IsHudWorthy);

            Assert.That(snapshot.Rows.Count, Is.EqualTo(2));
            Assert.That(snapshot.Rows[0].KindCode, Is.EqualTo("JobAssigned"));
            Assert.That(snapshot.Rows[0].Reason, Is.EqualTo("assigned"));
            Assert.That(snapshot.Rows[1].KindCode, Is.EqualTo("CombatResolved"));
            Assert.That(snapshot.Rows[1].Reason, Is.EqualTo("won"));
        }
    }
}

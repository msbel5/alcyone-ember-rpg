using System;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

// Design note:
// These tests pin the product-visible Phase 4 EventLog trace for need pressure.
// Eat/sleep recovery and job refusal get their own later EventLog proofs.
namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>Verifies NeedChanged EventLog emission.</summary>
    public sealed class NeedsEventLogTests
    {
        [Test]
        public void TickActorNeeds_AppendsNeedChangedEventWithReasonTrace()
        {
            var actor = CreateActor();
            var log = new WorldEventLog();
            var now = new GameTime(GameTime.MinutesPerDay);

            var changed = new NeedsSystem().TickActorNeeds(actor, log, now, ticks: 2);

            Assert.That(changed, Is.True);
            Assert.That(log.Count, Is.EqualTo(1));

            var evt = log.Events.Single();
            Assert.That(evt.Kind, Is.EqualTo(WorldEventKind.NeedChanged));
            Assert.That(evt.ActorId, Is.EqualTo(actor.Id));
            Assert.That(evt.SiteId.IsEmpty, Is.True);
            Assert.That(evt.Tick, Is.EqualTo(now));
            Assert.That(evt.Reason, Is.EqualTo($"need_changed:{actor.Id.Value}"));
            Assert.That(evt.ReasonTrace.Causes, Is.EqualTo(new[]
            {
                "needs_tick",
                $"actor:{actor.Id.Value}",
                "ticks:2",
                $"time:{GameTime.MinutesPerDay}",
                "hunger:0->16",
                "fatigue:0->12",
                // CAN SUYU H2 rates: 2 ticks = +16/+12/+10; mood 50-(16+12+10)/3 = 38.
                "thirst:0->10",
                "mood:38",
            }));
        }

        [Test]
        public void TickActorNeeds_NonPositiveTicksDoNotAppendEvent()
        {
            var actor = CreateActor();
            var log = new WorldEventLog();

            var changed = new NeedsSystem().TickActorNeeds(actor, log, new GameTime(0), ticks: 0);

            Assert.That(changed, Is.False);
            Assert.That(log.IsEmpty, Is.True);
            Assert.That(actor.Needs, Is.EqualTo(ActorNeeds.Comfortable));
            Assert.That(actor.Mood, Is.EqualTo(ActorMood.Neutral));
        }

        [Test]
        public void TickActorNeeds_RejectsNullInputs()
        {
            var system = new NeedsSystem();
            var actor = CreateActor();

            Assert.Throws<ArgumentNullException>(() => system.TickActorNeeds(null, new WorldEventLog(), new GameTime(0)));
            Assert.Throws<ArgumentNullException>(() => system.TickActorNeeds(actor, null, new GameTime(0)));
        }

        private static ActorRecord CreateActor()
        {
            return new ActorRecord(
                new ActorId(22),
                "Needs Event",
                ActorRole.Guard,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(0, 0),
                10,
                5,
                1,
                3);
        }
    }
}

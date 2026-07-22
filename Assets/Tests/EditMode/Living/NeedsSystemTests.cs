using EmberCrpg.Domain.Actors;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

// Design note:
// These tests pin the pure need-pressure tick. Mood, EventLog, recovery,
// save/load, and job refusal are covered by separate Phase 4 rails.
namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>Verifies deterministic need pressure ticking.</summary>
    public sealed class NeedsSystemTests
    {
        [Test]
        public void TickNeeds_AdvancesHungerFatigueAndThirst()
        {
            var needs = new ActorNeeds(new NeedValue(10), new NeedValue(20), new NeedValue(30));

            var ticked = new NeedsSystem().TickNeeds(needs);

            // CAN SUYU H2 rates: +8 hunger / +6 fatigue / +5 thirst per hour (24h-cycle scale).
            Assert.That(ticked.Hunger.Value, Is.EqualTo(18));
            Assert.That(ticked.Fatigue.Value, Is.EqualTo(26));
            Assert.That(ticked.Thirst.Value, Is.EqualTo(35));
        }

        [Test]
        public void TickNeeds_RepeatedTicksClampAtMaximum()
        {
            var needs = new ActorNeeds(new NeedValue(90), new NeedValue(95), new NeedValue(40));

            var ticked = new NeedsSystem().TickNeeds(needs, ticks: 5);

            Assert.That(ticked.Hunger, Is.EqualTo(NeedValue.Critical));   // 90 + 8*5 clamps at 100
            Assert.That(ticked.Fatigue, Is.EqualTo(NeedValue.Critical));  // 95 + 6*5 clamps at 100
            // 40 + (5 * 5) = 65, still below Max=100 — the non-clamp branch for thirst.
            Assert.That(ticked.Thirst.Value, Is.EqualTo(65));
        }

        [Test]
        public void TickNeeds_NonPositiveTicksDoNotChangeNeeds()
        {
            var needs = new ActorNeeds(new NeedValue(10), new NeedValue(20), new NeedValue(30));
            var system = new NeedsSystem();

            Assert.That(system.TickNeeds(needs, ticks: 0), Is.EqualTo(needs));
            Assert.That(system.TickNeeds(needs, ticks: -2), Is.EqualTo(needs));
        }
    }
}

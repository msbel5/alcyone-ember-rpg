using EmberCrpg.Domain.Actors;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

// Design note:
// These tests pin the pure need-pressure tick. Mood, EventLog, recovery,
// save/load, and job refusal are covered by separate Faz 4 rails.
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

            Assert.That(ticked.Hunger.Value, Is.EqualTo(30));
            Assert.That(ticked.Fatigue.Value, Is.EqualTo(35));
            // Codex audit (eighth pass A-P1): Thirst now ticks at
            // ThirstIncreasePerTick=10 instead of staying frozen.
            Assert.That(ticked.Thirst.Value, Is.EqualTo(40));
        }

        [Test]
        public void TickNeeds_RepeatedTicksClampAtMaximum()
        {
            var needs = new ActorNeeds(new NeedValue(90), new NeedValue(95), new NeedValue(40));

            var ticked = new NeedsSystem().TickNeeds(needs, ticks: 5);

            Assert.That(ticked.Hunger, Is.EqualTo(NeedValue.Critical));
            Assert.That(ticked.Fatigue, Is.EqualTo(NeedValue.Critical));
            // Codex audit (eighth pass A-P1): 40 + (10 * 5) = 90, still below
            // Max=100 so this exercises the non-clamp branch for thirst.
            Assert.That(ticked.Thirst.Value, Is.EqualTo(90));
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

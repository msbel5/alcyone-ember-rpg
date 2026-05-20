using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Memory;
using EmberCrpg.Simulation.Narrative;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Narrative
{
    public sealed class TradeRefusalHookTests
    {
        private static readonly ActorId SellerId = new ActorId(10UL);
        private static readonly ActorId BuyerId = new ActorId(20UL);

        [Test]
        public void Refuses_OnFactionWar()
        {
            var factions = new FactionStore();
            factions.Add(new FactionRecord(new FactionId(1UL), "A", new string[0]));
            factions.Add(new FactionRecord(new FactionId(2UL), "B", new string[0]));
            factions.WithReputation(new FactionId(1UL), new FactionId(2UL), new FactionReputation(-90));

            var refuse = new TradeRefusalHook(new MemoryRecallService())
                .ShouldRefuse(null, new FactionId(1UL), new FactionId(2UL), factions, default, default, out var reason);

            Assert.That(refuse, Is.True);
            Assert.That(reason, Is.EqualTo("faction_war"));
        }

        [Test]
        public void Refuses_OnRecentCrimeMemory()
        {
            var memory = new MemoryComponent(SellerId);
            memory.Add(new MemoryFact(SellerId, new TopicId("crime"), BuyerId, new GameTime(200), "stole"));

            var refuse = new TradeRefusalHook(new MemoryRecallService())
                .ShouldRefuse(memory, default, default, null, default, new GameTime(100), out var reason);

            Assert.That(refuse, Is.True);
            Assert.That(reason, Is.EqualTo("memory_recent_crime"));
        }

        [Test]
        public void Allows_WhenNeitherWarNorCrime()
        {
            var memory = new MemoryComponent(SellerId);
            memory.Add(new MemoryFact(SellerId, new TopicId("crime"), BuyerId, new GameTime(10), "old"));

            var refuse = new TradeRefusalHook(new MemoryRecallService())
                .ShouldRefuse(memory, default, default, null, default, new GameTime(100), out var reason);

            Assert.That(refuse, Is.False);
            Assert.That(reason, Is.EqualTo(string.Empty));
        }
    }
}

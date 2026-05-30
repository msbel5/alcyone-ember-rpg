using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    public sealed class NpcMemoryLlmEnvelopeTests
    {
        [Test]
        public void Build_CarriesPersonaAndLastEightMemoryRows()
        {
            var npc = new NpcSeedRecord(new NpcId(9), new SettlementId(2), new FactionId(3), "Brennec", 900, NpcRole.Artisan);
            var world = new WorldState
            {
                WorldProfile = new WorldProfile(WorldStyle.DarkFantasyGrim, WorldGenre.PoliticalIntrigue, 42, 1000000, 47, 12, 100, "grim", "smith", "city"),
            };
            var memory = world.NpcMemory.GetOrCreate(new ActorId(9));
            for (int i = 0; i < 10; i++)
                memory.RecordEvent(new InteractionEvent(new GameTime(i), "Talk", default, "topic_" + i, "", 0, default));

            var request = NpcMemoryLlmEnvelope.Build(npc, world, StarterToolSurfaces.Registry(), 128, 44);

            Assert.That(request.SystemPrompt, Does.Contain("DarkFantasyGrim"));
            Assert.That(request.SystemPrompt, Does.Contain("Brennec"));
            Assert.That(request.RecentTurns.Count, Is.EqualTo(8));
            Assert.That(request.RecentTurns[0], Does.Contain("topic_2"));
            Assert.That(request.AvailableTools.Count, Is.EqualTo(5));
        }

        [Test]
        public void RecordAcceptedBark_AppendsDeterministicMemoryFragment()
        {
            var world = new WorldState();
            NpcMemoryLlmEnvelope.RecordAcceptedBark(world, new NpcId(12), new GameTime(5), "warned about the ash road");

            Assert.That(world.NpcMemory.TryGet(new ActorId(12), out var memory), Is.True);
            Assert.That(memory.Events[0].EventType, Is.EqualTo("AiBark"));
            Assert.That(memory.Events[0].SubjectId, Is.EqualTo("warned about the ash road"));
        }
    }
}

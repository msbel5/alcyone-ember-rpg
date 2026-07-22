using System;
using System.Linq;
using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.World;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.Memory;

namespace EmberCrpg.Simulation.AiDm
{
    public static class NpcMemoryLlmEnvelope
    {
        public static LlmRequest Build(NpcSeedRecord npc, WorldState world, ToolRegistry registry, int maxTokens, ulong seed)
        {
            if (npc == null) throw new ArgumentNullException(nameof(npc));
            if (world == null) throw new ArgumentNullException(nameof(world));
            var profile = world.WorldProfile;
            var system = $"{StyleGuideline(profile)} Persona: {npc.Name}, {npc.Role}, born {npc.BirthYear}.";
            var recent = new string[0];
            if (world.NpcMemory != null && world.NpcMemory.TryGet(new ActorId(npc.Id.Value), out var memory))
                recent = new MemoryRecallService().LastN(memory, 8).ToArray();
            return new LlmRequest(
                "npc_memory",
                "npc:" + npc.Id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                registry == null ? null : registry.Descriptors,
                maxTokens,
                seed,
                system,
                recent);
        }

        /// <summary>The memory the NPC brings to its tongue: last N interaction events as
        /// prompt turns. THE canonical dialogue-context source - the live dialog path and the
        /// Gate9 proof both call this, so memory reaching the prompt is a tested invariant.</summary>
        public static System.Collections.Generic.List<string> RecallLines(WorldState world, ActorId actorId, int count)
        {
            var lines = new System.Collections.Generic.List<string>();
            if (world?.NpcMemory != null && !actorId.IsEmpty
                && world.NpcMemory.TryGet(actorId, out var memory))
                lines.AddRange(new MemoryRecallService().LastN(memory, count));
            return lines;
        }

        public static void RecordAcceptedBark(WorldState world, NpcId npcId, GameTime now, string summary)
        {
            if (world == null || npcId.IsEmpty || string.IsNullOrWhiteSpace(summary))
                return;
            if (world.NpcMemory == null) world.NpcMemory = new NpcMemoryStore();
            world.NpcMemory.GetOrCreate(new ActorId(npcId.Value)).RecordEvent(new InteractionEvent(now, "AiBark", default, summary.Trim(), string.Empty, 0, default));
        }

        private static string StyleGuideline(WorldProfile profile)
        {
            if (profile == null) return "Ground the answer in Ember's dark CRPG tone.";
            return $"World style {profile.Style}; genre {profile.Genre}; mood {profile.MoodKeyword}.";
        }
    }
}

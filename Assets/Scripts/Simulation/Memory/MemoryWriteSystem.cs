using System;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Memory;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.World;

namespace EmberCrpg.Simulation.Memory
{
    /// <summary>
    /// Records memory facts about observed events. Pure: callers translate
    /// world events into Record calls. Faz 9 Atom 10.
    /// </summary>
    public sealed class MemoryWriteSystem
    {
        public void Record(
            MemoryComponent witnessMemory,
            TopicId topic,
            ActorId aboutActor,
            GameTime now,
            string detail)
        {
            if (witnessMemory == null) throw new ArgumentNullException(nameof(witnessMemory));
            if (topic.IsEmpty) throw new ArgumentException("Topic must be non-empty.", nameof(topic));
            witnessMemory.Add(new MemoryFact(witnessMemory.OwnerId, topic, aboutActor, now, detail ?? string.Empty));
        }

        public void RecordCrime(MemoryComponent witnessMemory, ActorId perpetrator, GameTime now, string detail)
        {
            Record(witnessMemory, new TopicId("crime"), perpetrator, now, detail);
        }

        public void RecordTrade(MemoryComponent witnessMemory, ActorId counterparty, GameTime now, string detail)
        {
            Record(witnessMemory, new TopicId("trade"), counterparty, now, detail);
        }

        // Codex audit (second pass A-P2): the original crime detector relied on
        // substring matching ("theft" / "stole") inside the free-form
        // worldEvent.Reason. Structured crime events whose reason starts with
        // "assault", "pickpocket", "vandalism", "arson", or any future crime
        // vocabulary were silently dropped. The stable code prefix set lets
        // the engine emit deterministic crime markers and lets simulation
        // services bind on a known list rather than a substring search.
        internal static readonly System.Collections.Generic.HashSet<string> KnownCrimeCodes =
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "theft", "stole",
                "assault", "battery",
                "pickpocket", "robbery",
                "vandalism", "arson",
                "trespass", "fraud",
                "murder", "manslaughter",
            };

        public bool RecordFromWorldEvent(MemoryComponent witnessMemory, WorldEvent worldEvent)
        {
            if (witnessMemory == null) throw new ArgumentNullException(nameof(witnessMemory));
            if (worldEvent == null) throw new ArgumentNullException(nameof(worldEvent));

            var reason = worldEvent.Reason ?? string.Empty;
            if (IsCrimeReason(reason))
            {
                RecordCrime(witnessMemory, worldEvent.ActorId, worldEvent.Tick, reason);
                return true;
            }

            if (worldEvent.Kind == WorldEventKind.TradeCompleted)
            {
                RecordTrade(witnessMemory, worldEvent.ActorId, worldEvent.Tick, reason);
                return true;
            }

            return false;
        }

        private static bool IsCrimeReason(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return false;
            // Stable lexer: take the leading whitespace/punctuation-delimited
            // token as the structured code and check it against the known set.
            // This lets reason strings shape themselves as
            //   "theft item:coin"
            //   "assault target:guard"
            //   "pickpocket gold:5"
            // and land deterministically in the crime branch.
            var tokens = reason.Split(new[] { ' ', '\t', ':', ',', ';' },
                StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return false;
            return KnownCrimeCodes.Contains(tokens[0]);
        }
    }
}

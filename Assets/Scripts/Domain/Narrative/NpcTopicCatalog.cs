using System.Collections.Generic;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Domain.Narrative
{
    /// <summary>
    /// EMB-045: derives an NPC's "Ask About" topics from that actor's own context — their role,
    /// faction, and home — instead of handing every NPC the same global <c>SliceWorldState.Topics</c>
    /// list. Two NPCs of different roles therefore expose different topics (and different deterministic
    /// fallback answers), so a conversation feels like talking to a person, not browsing a global menu.
    /// Pure + deterministic (no Unity, no time, no RNG); the live LLM enriches the answers at runtime,
    /// but the topic set and the offline fallback copy come from here.
    /// </summary>
    public static class NpcTopicCatalog
    {
        /// <summary>Build the per-actor topic list. <paramref name="sharedWorldTopics"/> (the seeded
        /// world rumours/lore) are appended after the role/faction topics so world lore stays reachable
        /// through any NPC, but the actor-specific topics lead.</summary>
        public static IReadOnlyList<AskAboutTopic> For(
            NpcRole role,
            ulong factionValue,
            IReadOnlyList<AskAboutTopic> sharedWorldTopics = null,
            int sharedCount = 2)
        {
            var topics = new List<AskAboutTopic>();

            foreach (var t in RoleTopics(role))
                topics.Add(t);

            if (factionValue != 0UL)
                topics.Add(new AskAboutTopic(
                    "faction",
                    "their allegiance",
                    "Loyalties are a private thing here — coin and kin both pull at a person."));

            if (sharedWorldTopics != null && sharedCount > 0)
            {
                int added = 0;
                foreach (var t in sharedWorldTopics)
                {
                    if (added >= sharedCount) break;
                    if (t == null) continue;
                    topics.Add(t);
                    added++;
                }
            }

            return topics;
        }

        private static IEnumerable<AskAboutTopic> RoleTopics(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Farmer:
                    yield return new AskAboutTopic("harvest", "the harvest", "The fields have given what they will; we make do with the rest.");
                    yield return new AskAboutTopic("land", "working the land", "Soil remembers every season. Treat it poorly and it answers in kind.");
                    break;
                case NpcRole.Merchant:
                    yield return new AskAboutTopic("trade", "trade and prices", "Prices rise with every rumour of trouble on the roads. Such is commerce.");
                    yield return new AskAboutTopic("roads", "the trade roads", "The roads are only as safe as the last caravan that walked them.");
                    break;
                case NpcRole.Guard:
                    yield return new AskAboutTopic("watch", "the watch", "We keep the peace as best we can with the hands we're given.");
                    yield return new AskAboutTopic("trouble", "recent trouble", "There's always trouble. Lately more than I'd like to admit.");
                    break;
                case NpcRole.Noble:
                    yield return new AskAboutTopic("court", "the court", "Every smile at court hides a ledger of favours owed.");
                    yield return new AskAboutTopic("intrigue", "local intrigue", "Whispers move faster than couriers in these halls.");
                    break;
                case NpcRole.Priest:
                    yield return new AskAboutTopic("faith", "matters of faith", "Faith is a lamp for the dark roads, not a shield against them.");
                    yield return new AskAboutTopic("omens", "recent omens", "The signs have been uneasy of late. Make of that what you will.");
                    break;
                case NpcRole.Scholar:
                    yield return new AskAboutTopic("lore", "old lore", "Most of what's written is wrong, but the errors are instructive.");
                    yield return new AskAboutTopic("studies", "their studies", "I chase a question that keeps unfolding into more questions.");
                    break;
                case NpcRole.Artisan:
                    yield return new AskAboutTopic("craft", "their craft", "Good work takes the time it takes. Hurry it and it shows.");
                    yield return new AskAboutTopic("workshop", "the workshop", "Everything in here earns its place or it goes to the scrap bin.");
                    break;
                case NpcRole.Outlaw:
                    yield return new AskAboutTopic("rumors", "whispered rumours", "I hear things. Whether I share them depends on you.");
                    yield return new AskAboutTopic("contraband", "off-ledger goods", "Some goods don't like daylight. I don't judge — I deliver.");
                    break;
                default:
                    yield return new AskAboutTopic("life", "life hereabouts", "It's a quiet enough life, until it isn't.");
                    break;
            }
        }
    }
}

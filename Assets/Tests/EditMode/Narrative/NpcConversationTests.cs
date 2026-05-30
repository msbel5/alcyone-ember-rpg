using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Narrative;
using EmberCrpg.Domain.Worldgen;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Narrative
{
    /// <summary>EMB-020/045: per-actor topics (not a global menu) + the one conversation-state model.</summary>
    public sealed class NpcConversationTests
    {
        private static List<string> Ids(IReadOnlyList<AskAboutTopic> topics) => topics.Select(t => t.Id).ToList();

        [Test]
        public void DifferentRoles_ExposeDifferentTopics()
        {
            var guard = NpcTopicCatalog.For(NpcRole.Guard, 0UL);
            var merchant = NpcTopicCatalog.For(NpcRole.Merchant, 0UL);

            Assert.That(Ids(guard), Is.EquivalentTo(new[] { "watch", "trouble" }));
            Assert.That(Ids(merchant), Is.EquivalentTo(new[] { "trade", "roads" }));
            // The whole point of EMB-045: two different NPCs do NOT share a topic menu.
            Assert.That(Ids(guard), Is.Not.EqualTo(Ids(merchant)));
        }

        [Test]
        public void DifferentRoles_GiveDifferentFallbackAnswers()
        {
            var priest = NpcTopicCatalog.For(NpcRole.Priest, 0UL).First();
            var outlaw = NpcTopicCatalog.For(NpcRole.Outlaw, 0UL).First();
            Assert.That(priest.Answer, Is.Not.EqualTo(outlaw.Answer));
        }

        [Test]
        public void Faction_AddsAllegianceTopic_OnlyWhenNonZero()
        {
            var noFaction = NpcTopicCatalog.For(NpcRole.Scholar, 0UL);
            var withFaction = NpcTopicCatalog.For(NpcRole.Scholar, 42UL);
            Assert.That(Ids(noFaction), Does.Not.Contain("faction"));
            Assert.That(Ids(withFaction), Does.Contain("faction"));
        }

        [Test]
        public void SharedWorldTopics_AppendedAfterActorTopics_RespectingCount()
        {
            var shared = new List<AskAboutTopic>
            {
                new AskAboutTopic("rumor", "rumours", "..."),
                new AskAboutTopic("fate", "fate", "..."),
                new AskAboutTopic("extra", "extra", "..."),
            };
            var topics = NpcTopicCatalog.For(NpcRole.Farmer, 0UL, shared, sharedCount: 2);
            var ids = Ids(topics);
            // Actor topics lead, then exactly 2 shared.
            Assert.That(ids.Take(2), Is.EquivalentTo(new[] { "harvest", "land" }));
            Assert.That(ids, Does.Contain("rumor"));
            Assert.That(ids, Does.Contain("fate"));
            Assert.That(ids, Does.Not.Contain("extra"));
        }

        [Test]
        public void For_IsDeterministic()
        {
            var a = Ids(NpcTopicCatalog.For(NpcRole.Noble, 7UL));
            var b = Ids(NpcTopicCatalog.For(NpcRole.Noble, 7UL));
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void ConversationState_FindTopic_OnlyAnswersOfferedTopics()
        {
            var topics = NpcTopicCatalog.For(NpcRole.Guard, 0UL);
            var convo = new ConversationState("Sera the Watch", "portrait_guard", topics);

            Assert.That(convo.IsActive, Is.True);
            Assert.That(convo.FindTopic("watch"), Is.Not.Null);
            Assert.That(convo.FindTopic("trade"), Is.Null, "must not answer a topic this actor never offered");
            Assert.That(convo.FindTopic(null), Is.Null);
        }

        [Test]
        public void ConversationState_None_IsInactive()
        {
            Assert.That(ConversationState.None.IsActive, Is.False);
            Assert.That(ConversationState.None.Topics, Is.Empty);
        }
    }
}

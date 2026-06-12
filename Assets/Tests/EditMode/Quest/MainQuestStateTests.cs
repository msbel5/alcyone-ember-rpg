using EmberCrpg.Domain.Quest;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Quest
{
    /// <summary>
    /// F31: the three-act spine is a STATE MACHINE, not a checklist — out-of-order calls refuse,
    /// each delve yields exactly one piece, the requirement adapts to small worlds, and only the
    /// pinned FINAL delve's Warden completes the quest.
    /// </summary>
    public sealed class MainQuestStateTests
    {
        [Test]
        public void ThreeActs_RunEndToEnd_InOrder()
        {
            var quest = new MainQuestState();
            quest.Configure(delveCount: 3, finalDelveId: 90UL);

            Assert.That(quest.TryFindInscription(10UL, out var l1), Is.True);
            Assert.That(l1, Does.Contain("1/3"));
            Assert.That(quest.TryFindInscription(20UL, out _), Is.True);
            Assert.That(quest.TryFindInscription(30UL, out var l3), Is.True);
            Assert.That(l3, Does.Contain("whole"));
            Assert.That(quest.Act, Is.EqualTo(2));

            Assert.That(quest.TryConsultSage(out var sage), Is.True);
            Assert.That(sage, Does.Contain("warden").IgnoreCase);
            Assert.That(quest.Act, Is.EqualTo(3));

            Assert.That(quest.TryFellFinalWarden(90UL, out var fin), Is.True);
            Assert.That(quest.IsComplete, Is.True);
            Assert.That(fin, Does.Contain("ember"));
        }

        [Test]
        public void OutOfOrder_AndWrongTargets_Refuse()
        {
            var quest = new MainQuestState();
            quest.Configure(delveCount: 3, finalDelveId: 90UL);

            Assert.That(quest.TryConsultSage(out _), Is.False, "no sage before the pieces");
            Assert.That(quest.TryFellFinalWarden(90UL, out _), Is.False, "no finale before the sage");

            Assert.That(quest.TryFindInscription(10UL, out _), Is.True);
            Assert.That(quest.TryFindInscription(10UL, out _), Is.False, "one piece per delve");
            Assert.That(quest.InscriptionsFound, Is.EqualTo(1));

            quest.TryFindInscription(20UL, out _);
            quest.TryFindInscription(30UL, out _);
            quest.TryConsultSage(out _);
            Assert.That(quest.TryFellFinalWarden(55UL, out _), Is.False, "only the FINAL delve's Warden counts");
            Assert.That(quest.TryFellFinalWarden(90UL, out _), Is.True);
            Assert.That(quest.TryFindInscription(40UL, out _), Is.False, "a complete spine stays complete");
        }

        [Test]
        public void SmallWorlds_AdaptTheRequirement()
        {
            var quest = new MainQuestState();
            quest.Configure(delveCount: 1, finalDelveId: 7UL);
            Assert.That(quest.RequiredInscriptions, Is.EqualTo(1));
            Assert.That(quest.TryFindInscription(7UL, out var line), Is.True);
            Assert.That(line, Does.Contain("whole"));
            Assert.That(quest.Act, Is.EqualTo(2), "a one-delve world still has a complete spine");

            var zero = new MainQuestState();
            zero.Configure(delveCount: 0, finalDelveId: 0UL);
            Assert.That(zero.RequiredInscriptions, Is.EqualTo(1), "never zero — the invariant floor");
        }

        [Test]
        public void EnsureInvariants_HealsNullsAndClamps()
        {
            var quest = new MainQuestState { Act = 9, ClaimedDelveIds = null, RequiredInscriptions = 0 };
            quest.EnsureInvariants();
            Assert.That(quest.Act, Is.EqualTo(4));
            Assert.That(quest.ClaimedDelveIds, Is.Not.Null);
            Assert.That(quest.RequiredInscriptions, Is.EqualTo(1));
        }
    }
}

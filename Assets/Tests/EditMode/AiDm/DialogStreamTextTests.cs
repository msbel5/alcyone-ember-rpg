using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    /// <summary>REFORM #3 pin: the W28 incident, frozen as adversarial tests. A weak model
    /// parroting the instruction may NEVER reach the screen or the option list again.</summary>
    public sealed class DialogStreamTextTests
    {
        [Test]
        public void Split_InstructionOnlyEcho_YieldsEmptyBody_AndNoParrotFollowups()
        {
            var echoed = "FOLLOWUPS: first question | second question | third question - three short " +
                         "in-character questions the traveller might naturally ask you NEXT.";
            var split = DialogStreamText.SplitFollowups(echoed);
            Assert.That(split.Body, Is.Empty, "an instruction-only reply must NOT reach the screen");
            Assert.That(split.Followups, Is.Null, "template parrots must never become bubbles");
        }

        [Test]
        public void Split_HealthyAnswer_YieldsBodyAndRealQuestions()
        {
            var split = DialogStreamText.SplitFollowups(
                "The gate holds, for now. FOLLOWUPS: Who guards the gate at night? | " +
                "What lies beyond the pass? | Why did the last caravan turn back?");
            Assert.That(split.Body, Is.EqualTo("The gate holds, for now."));
            Assert.That(split.Followups, Has.Count.EqualTo(3));
            Assert.That(split.Followups[0], Does.EndWith("?"));
        }

        [Test]
        public void Split_NoMarker_PassesThroughUntouched()
        {
            var split = DialogStreamText.SplitFollowups("Plain answer, no protocol.");
            Assert.That(split.Body, Is.EqualTo("Plain answer, no protocol."));
            Assert.That(split.Followups, Is.Null);
        }

        [Test]
        public void IsRealFollowup_RejectsParrotsAndFragments_AcceptsQuestions()
        {
            Assert.That(DialogStreamText.IsRealFollowup("first question?"), Is.False);
            Assert.That(DialogStreamText.IsRealFollowup("short?"), Is.False);
            Assert.That(DialogStreamText.IsRealFollowup("no question mark here"), Is.False);
            Assert.That(DialogStreamText.IsRealFollowup("Who tends the forge these days?"), Is.True);
        }

        [Test]
        public void NaturalQuestion_TurnsLabelsIntoSpeech()
        {
            Assert.That(DialogStreamText.NaturalQuestion("Ask about Gate"),
                Is.EqualTo("What can you tell me about Gate?"));
            Assert.That(DialogStreamText.NaturalQuestion("companion_join: Travel with me"),
                Is.EqualTo("Will you travel with me?"));
        }
    }
}

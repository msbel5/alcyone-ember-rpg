using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    /// <summary>M3b pin: the voice IS the identity - it must never wander between sessions.</summary>
    public sealed class NpcVoiceSignatureServiceTests
    {
        [Test]
        public void SignatureFor_SameKey_IsStable_AndInRange()
        {
            for (ulong key = 1; key < 200; key++)
            {
                var a = NpcVoiceSignatureService.SignatureFor(key, 3);
                var b = NpcVoiceSignatureService.SignatureFor(key, 3);
                Assert.That(a.VoiceIndex, Is.EqualTo(b.VoiceIndex));
                Assert.That(a.RateOffset, Is.EqualTo(b.RateOffset).And.InRange(-3, 3));
                Assert.That(a.PitchOffset, Is.EqualTo(b.PitchOffset).And.InRange(-9, 9));
                Assert.That(a.VoiceIndex, Is.InRange(0, 2));
            }
        }

        [Test]
        public void SignatureFor_SpreadsAcrossVoicesAndPitches()
        {
            var voices = new System.Collections.Generic.HashSet<int>();
            var pitches = new System.Collections.Generic.HashSet<int>();
            for (ulong key = 1; key < 60; key++)
            {
                var s = NpcVoiceSignatureService.SignatureFor(key, 3);
                voices.Add(s.VoiceIndex);
                pitches.Add(s.PitchOffset);
            }
            Assert.That(voices.Count, Is.GreaterThanOrEqualTo(2), "60 NPCs must not share one voice");
            Assert.That(pitches.Count, Is.GreaterThanOrEqualTo(5), "pitch must actually modulate");
        }

        [Test]
        public void Chunker_DrainsCompleteSentences_KeepsTheFormingTail()
        {
            int cursor = 0;
            var first = SpeechSentenceChunker.Drain("The forge is hot. The road is lo", ref cursor);
            Assert.That(first, Is.EqualTo(new[] { "The forge is hot." }));
            var second = SpeechSentenceChunker.Drain("The forge is hot. The road is long! Beware", ref cursor);
            Assert.That(second, Is.EqualTo(new[] { "The road is long!" }));
            Assert.That(cursor, Is.EqualTo("The forge is hot. The road is long!".Length));
        }
    }
}

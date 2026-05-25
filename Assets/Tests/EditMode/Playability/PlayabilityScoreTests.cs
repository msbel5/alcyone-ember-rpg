using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Playability
{
    public sealed class PlayabilityScoreTests
    {
        [Test]
        public void WeightedScoresClampToZeroToOneHundred()
        {
            Assert.That(PlayabilityScore.ComputeUx(25, 20, 20, 15, 10, 10), Is.EqualTo(100));
            Assert.That(PlayabilityScore.ComputePlayability(25, 20, 20, 15, 10, 10), Is.EqualTo(100));
            Assert.That(PlayabilityScore.ComputeUx(40, 40, 40, 40, 40, 40), Is.EqualTo(100));
            Assert.That(PlayabilityScore.ComputePlayability(-1, -1, -1, -1, -1, -1), Is.EqualTo(0));
        }
    }

    public static class PlayabilityScore
    {
        public static int ComputeUx(int readability, int ui, int art, int feedback, int contrast, int comfort)
        {
            return Clamp(readability + ui + art + feedback + contrast + comfort);
        }

        public static int ComputePlayability(int progression, int camera, int actors, int performance, int transitions, int content)
        {
            return Clamp(progression + camera + actors + performance + transitions + content);
        }

        private static int Clamp(int value)
        {
            if (value < 0) return 0;
            return value > 100 ? 100 : value;
        }
    }
}

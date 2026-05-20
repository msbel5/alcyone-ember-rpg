using EmberCrpg.Domain.AiDm;
using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    public sealed class FlavourBudgetTests
    {
        // ----- FlavourBudget -----
        [Test]
        public void Constructor_RejectsNegativeCap()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new FlavourBudget(-1));
        }

        [Test]
        public void TryReserve_ConsumesUpToCap_ThenFails()
        {
            var b = new FlavourBudget(2);
            Assert.That(b.TryReserve(), Is.True);
            Assert.That(b.TryReserve(), Is.True);
            Assert.That(b.TryReserve(), Is.False);
            Assert.That(b.Spent, Is.EqualTo(2));
            Assert.That(b.Remaining, Is.EqualTo(0));
        }

        [Test]
        public void ResetForTick_RestoresBudget()
        {
            var b = new FlavourBudget(1);
            b.TryReserve();
            b.ResetForTick();
            Assert.That(b.Spent, Is.EqualTo(0));
            Assert.That(b.TryReserve(), Is.True);
        }

        [Test]
        public void UpdateCap_RejectsNegative()
        {
            var b = new FlavourBudget(1);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => b.UpdateCap(-1));
        }

        // ----- ConsultFateOutcomeBucket -----
        [Test]
        public void Bucket_ThreeStableCodes()
        {
            Assert.That(ConsultFateOutcomeBucket.Favourable.Code, Is.EqualTo("favourable"));
            Assert.That(ConsultFateOutcomeBucket.Neutral.Code, Is.EqualTo("neutral"));
            Assert.That(ConsultFateOutcomeBucket.Setback.Code, Is.EqualTo("setback"));
        }

        [Test]
        public void FromRoll_BucketsByRange()
        {
            // PR#173 bot review fix: rolls are 1..100 (XorShiftRng.RollPercent
            // contract), thresholds use inclusive upper bounds <=35 / <=70.
            Assert.That(ConsultFateOutcomeBucket.FromRoll(1), Is.EqualTo(ConsultFateOutcomeBucket.Setback));
            Assert.That(ConsultFateOutcomeBucket.FromRoll(35), Is.EqualTo(ConsultFateOutcomeBucket.Setback));
            Assert.That(ConsultFateOutcomeBucket.FromRoll(36), Is.EqualTo(ConsultFateOutcomeBucket.Neutral));
            Assert.That(ConsultFateOutcomeBucket.FromRoll(70), Is.EqualTo(ConsultFateOutcomeBucket.Neutral));
            Assert.That(ConsultFateOutcomeBucket.FromRoll(71), Is.EqualTo(ConsultFateOutcomeBucket.Favourable));
            Assert.That(ConsultFateOutcomeBucket.FromRoll(100), Is.EqualTo(ConsultFateOutcomeBucket.Favourable));
        }

        [Test]
        public void FromRoll_RejectsOutOfRange()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => ConsultFateOutcomeBucket.FromRoll(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => ConsultFateOutcomeBucket.FromRoll(101));
        }
    }
}

using EmberCrpg.Domain.AiDm;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Combat;
using EmberCrpg.Simulation.Rng;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    /// <summary>
    /// Sixth-pass codex audit — coverage for the 48-finding sweep landed on
    /// branch <c>mami/audit-sixth-pass-48-findings</c>.
    ///
    /// Covers the items that can be validated without the Unity engine
    /// (Domain + Simulation + the Data.SliceJson asmdef bridge). Presentation
    /// classes (DomainSimulationAdapter, EmberWorldHost, etc.) are exercised
    /// indirectly via the deterministic services they now call into.
    /// </summary>
    public sealed class AuditSixthPassCoverageTests
    {
        // ---- A-P3 #11: CombatHitRollService always consumes one rng draw. -----
        [Test]
        public void CombatHitRollService_AlwaysConsumesOneRoll_RegardlessOfChance()
        {
            // Toggling chance to 100 or 0 must NOT skip the rng draw. We compare
            // the RNG sequences after two calls with different chance values —
            // both must advance the RNG by exactly one RollPercent step.
            var hit = new CombatHitRollService();
            var rngA = new XorShiftRng(seed: 42u);
            var rngB = new XorShiftRng(seed: 42u);

            hit.Roll(attackerAccuracy: 200, defenderDodge: 0, rngA); // chance=100 path
            hit.Roll(attackerAccuracy: 0, defenderDodge: 200, rngB); // chance<=0 path

            // Both rngs should be at the same position now (one RollPercent each).
            Assert.That(rngA.NextInt(1000), Is.EqualTo(rngB.NextInt(1000)),
                "RNG sequence must not depend on chance value.");
        }

        // ---- A-P3 #12: CombatDamageService enforces rng contract. -------------
        [Test]
        public void CombatDamageService_ThrowsWhenBandWidthPositiveButRngNull()
        {
            var dmg = new CombatDamageService();
            Assert.That(
                () => dmg.Roll(baseDamage: 5, damageBandWidth: 3, armor: 0, rng: null),
                Throws.TypeOf<System.ArgumentNullException>());
        }

        [Test]
        public void CombatDamageService_AllowsNullRngWhenBandWidthZero()
        {
            var dmg = new CombatDamageService();
            // No throw expected — armor mitigates the deterministic base.
            int rolled = dmg.Roll(baseDamage: 5, damageBandWidth: 0, armor: 2, rng: null);
            Assert.That(rolled, Is.EqualTo(3));
        }

        // ---- A-P3 #13: XorShiftRng rejection samples for large bounds. -------
        [Test]
        public void XorShiftRng_RejectionSamplesForLargeBounds()
        {
            // Bounds above 2^16 take the rejection-sample branch. Verify the
            // value stays within bounds across many draws so we know the
            // branch executes without throwing or returning out-of-range.
            var rng = new XorShiftRng(seed: 12345u);
            int bound = (1 << 16) + 1; // 65537 — forces rejection-sample
            for (int i = 0; i < 5000; i++)
            {
                int sample = rng.NextInt(bound);
                Assert.That(sample, Is.GreaterThanOrEqualTo(0));
                Assert.That(sample, Is.LessThan(bound));
            }
        }

        // ---- A-P2 #10: FactionRelationKind symmetric Neutral band. ------------
        [Test]
        public void FactionRelationKind_SymmetricNeutralBand_NegativeTwentyFiveIsNeutral()
        {
            // Before the fix, -25 mapped to Hostile (asymmetric off-by-one).
            Assert.That(FactionRelationKind.FromReputation(-25), Is.EqualTo(FactionRelationKind.Neutral));
            Assert.That(FactionRelationKind.FromReputation(0), Is.EqualTo(FactionRelationKind.Neutral));
            Assert.That(FactionRelationKind.FromReputation(24), Is.EqualTo(FactionRelationKind.Neutral));
            Assert.That(FactionRelationKind.FromReputation(25), Is.EqualTo(FactionRelationKind.Friendly));
            Assert.That(FactionRelationKind.FromReputation(-26), Is.EqualTo(FactionRelationKind.Hostile));
        }

        // ---- A-P2 #7: ConsultFate distribution canonised by bucket. -----------
        [Test]
        public void ConsultFateOutcomeBucket_DistributionMatches35_35_30()
        {
            // DomainSimulationAdapter routes through this bucket. Verify the boundaries are unchanged:
            // rolls 1..35 → Setback, 36..70 → Neutral, 71..100 → Favourable.
            int setback = 0, neutral = 0, favourable = 0;
            for (int roll = 1; roll <= 100; roll++)
            {
                var bucket = ConsultFateOutcomeBucket.FromRoll(roll);
                if (bucket == ConsultFateOutcomeBucket.Setback) setback++;
                else if (bucket == ConsultFateOutcomeBucket.Neutral) neutral++;
                else if (bucket == ConsultFateOutcomeBucket.Favourable) favourable++;
                else Assert.Fail($"Unknown bucket {bucket}");
            }

            Assert.That(setback, Is.EqualTo(35), "Setback band must be 35 rolls (1..35).");
            Assert.That(neutral, Is.EqualTo(35), "Neutral band must be 35 rolls (36..70).");
            Assert.That(favourable, Is.EqualTo(30), "Favourable band must be 30 rolls (71..100).");
        }

        [Test]
        public void ConsultFateOutcomeBucket_BoundariesAtExpectedRolls()
        {
            Assert.That(ConsultFateOutcomeBucket.FromRoll(35), Is.EqualTo(ConsultFateOutcomeBucket.Setback));
            Assert.That(ConsultFateOutcomeBucket.FromRoll(36), Is.EqualTo(ConsultFateOutcomeBucket.Neutral));
            Assert.That(ConsultFateOutcomeBucket.FromRoll(70), Is.EqualTo(ConsultFateOutcomeBucket.Neutral));
            Assert.That(ConsultFateOutcomeBucket.FromRoll(71), Is.EqualTo(ConsultFateOutcomeBucket.Favourable));
        }

        // ---- A-P3 #13b: XorShiftRng small bounds skip rejection branch. ------
        [Test]
        public void XorShiftRng_SmallBoundsAcceptModulo()
        {
            // Boundary is exclusive: exclusiveMax == 1<<16 still uses modulo.
            var rng = new XorShiftRng(seed: 7u);
            int bound = 1 << 16;
            for (int i = 0; i < 1000; i++)
            {
                int sample = rng.NextInt(bound);
                Assert.That(sample, Is.GreaterThanOrEqualTo(0));
                Assert.That(sample, Is.LessThan(bound));
            }
        }
    }
}

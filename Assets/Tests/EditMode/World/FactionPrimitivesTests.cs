using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>
    /// Pins Faz 6 SOCIETY-rail primitives: FactionRelationKind codes,
    /// FactionReputation clamp/decay/equality, and FactionStore reputation
    /// symmetric WithReputation/GetReputation.
    /// </summary>
    public sealed class FactionPrimitivesTests
    {
        // ----- FactionRelationKind -----

        [Test]
        public void RelationKind_FiveStableCodes()
        {
            Assert.That(FactionRelationKind.Allied.Code, Is.EqualTo("allied"));
            Assert.That(FactionRelationKind.Friendly.Code, Is.EqualTo("friendly"));
            Assert.That(FactionRelationKind.Neutral.Code, Is.EqualTo("neutral"));
            Assert.That(FactionRelationKind.Hostile.Code, Is.EqualTo("hostile"));
            Assert.That(FactionRelationKind.War.Code, Is.EqualTo("war"));
        }

        [Test]
        public void RelationKind_FromReputation_Buckets()
        {
            Assert.That(FactionRelationKind.FromReputation(80), Is.EqualTo(FactionRelationKind.Allied));
            Assert.That(FactionRelationKind.FromReputation(30), Is.EqualTo(FactionRelationKind.Friendly));
            Assert.That(FactionRelationKind.FromReputation(0), Is.EqualTo(FactionRelationKind.Neutral));
            Assert.That(FactionRelationKind.FromReputation(-30), Is.EqualTo(FactionRelationKind.Hostile));
            Assert.That(FactionRelationKind.FromReputation(-90), Is.EqualTo(FactionRelationKind.War));
        }

        [Test]
        public void RelationKind_EqualityByCode()
        {
            Assert.That(FactionRelationKind.Allied, Is.EqualTo(FactionRelationKind.Allied));
            Assert.That(FactionRelationKind.Allied, Is.Not.EqualTo(FactionRelationKind.War));
        }

        // ----- FactionReputation -----

        [Test]
        public void Reputation_ClampsToBounds()
        {
            Assert.That(new FactionReputation(200).Value, Is.EqualTo(100));
            Assert.That(new FactionReputation(-200).Value, Is.EqualTo(-100));
            Assert.That(FactionReputation.Neutral.Value, Is.EqualTo(0));
        }

        [Test]
        public void Reputation_ApplyDelta_RespectsClamp()
        {
            var current = new FactionReputation(60);
            var raised = current.Apply(60);
            Assert.That(raised.Value, Is.EqualTo(100));
            var dropped = current.Apply(-200);
            Assert.That(dropped.Value, Is.EqualTo(-100));
        }

        [Test]
        public void Reputation_DecayTowardsZero()
        {
            Assert.That(new FactionReputation(50).Decay(20).Value, Is.EqualTo(30));
            Assert.That(new FactionReputation(-50).Decay(20).Value, Is.EqualTo(-30));
            Assert.That(new FactionReputation(10).Decay(100).Value, Is.EqualTo(0));
            Assert.That(FactionReputation.Neutral.Decay(50).Value, Is.EqualTo(0));
        }

        [Test]
        public void Reputation_ToRelationKind_ResolvesBucket()
        {
            Assert.That(new FactionReputation(80).ToRelationKind(), Is.EqualTo(FactionRelationKind.Allied));
            Assert.That(new FactionReputation(0).ToRelationKind(), Is.EqualTo(FactionRelationKind.Neutral));
            Assert.That(new FactionReputation(-90).ToRelationKind(), Is.EqualTo(FactionRelationKind.War));
        }

        // ----- FactionStore reputation extension -----

        [Test]
        public void FactionStore_DefaultReputation_IsNeutral()
        {
            var store = new FactionStore();
            var a = new FactionId(1UL);
            var b = new FactionId(2UL);

            Assert.That(store.GetReputation(a, b), Is.EqualTo(FactionReputation.Neutral));
        }

        [Test]
        public void FactionStore_WithReputation_IsSymmetric()
        {
            var store = new FactionStore();
            var a = new FactionId(1UL);
            var b = new FactionId(2UL);
            var rep = new FactionReputation(40);

            store.WithReputation(a, b, rep);

            Assert.That(store.GetReputation(a, b), Is.EqualTo(rep));
            Assert.That(store.GetReputation(b, a), Is.EqualTo(rep));
        }

        [Test]
        public void FactionStore_WithReputation_RejectsEmptyOrSelfPair()
        {
            var store = new FactionStore();
            var a = new FactionId(1UL);
            Assert.Throws<System.ArgumentException>(() => store.WithReputation(default, a, FactionReputation.Neutral));
            Assert.Throws<System.ArgumentException>(() => store.WithReputation(a, default, FactionReputation.Neutral));
            Assert.Throws<System.ArgumentException>(() => store.WithReputation(a, a, FactionReputation.Neutral));
        }

        [Test]
        public void FactionStore_GetReputation_EmptyPair_ReturnsNeutral()
        {
            var store = new FactionStore();
            var a = new FactionId(1UL);
            Assert.That(store.GetReputation(default, a), Is.EqualTo(FactionReputation.Neutral));
            Assert.That(store.GetReputation(a, a), Is.EqualTo(FactionReputation.Neutral));
        }
    }
}

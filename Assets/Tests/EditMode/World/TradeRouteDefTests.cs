using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Pins TradeRouteDef field validation and equality. Faz 6 Atom 7.</summary>
    public sealed class TradeRouteDefTests
    {
        private static readonly TradeRouteId RouteId = new TradeRouteId(101UL);
        private static readonly SiteId Origin = new SiteId(1UL);
        private static readonly SiteId Destination = new SiteId(2UL);

        [Test]
        public void Constructor_HappyPath_StoresFields()
        {
            var route = new TradeRouteDef(RouteId, Origin, Destination, "iron_ingot", 5, 7);

            Assert.That(route.Id, Is.EqualTo(RouteId));
            Assert.That(route.OriginSiteId, Is.EqualTo(Origin));
            Assert.That(route.DestinationSiteId, Is.EqualTo(Destination));
            Assert.That(route.ItemTag, Is.EqualTo("iron_ingot"));
            Assert.That(route.QuantityPerCaravan, Is.EqualTo(5));
            Assert.That(route.CadenceDays, Is.EqualTo(7));
        }

        [Test]
        public void Constructor_RejectsEmptyIdsOrBlankTagOrNonPositiveScalars()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new TradeRouteDef(default, Origin, Destination, "iron_ingot", 5, 7));
            Assert.Throws<System.ArgumentException>(() =>
                new TradeRouteDef(RouteId, default, Destination, "iron_ingot", 5, 7));
            Assert.Throws<System.ArgumentException>(() =>
                new TradeRouteDef(RouteId, Origin, default, "iron_ingot", 5, 7));
            Assert.Throws<System.ArgumentException>(() =>
                new TradeRouteDef(RouteId, Origin, Origin, "iron_ingot", 5, 7));
            Assert.Throws<System.ArgumentException>(() =>
                new TradeRouteDef(RouteId, Origin, Destination, "", 5, 7));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new TradeRouteDef(RouteId, Origin, Destination, "ok", 0, 7));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new TradeRouteDef(RouteId, Origin, Destination, "ok", 5, 0));
        }

        [Test]
        public void Equality_IsByIdOnly()
        {
            var a = new TradeRouteDef(RouteId, Origin, Destination, "iron_ingot", 5, 7);
            var b = new TradeRouteDef(RouteId, Origin, Destination, "bread", 3, 14);
            var c = new TradeRouteDef(new TradeRouteId(102UL), Origin, Destination, "iron_ingot", 5, 7);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void TradeRouteId_EmptyDefault_AndEqualityByValue()
        {
            Assert.That(default(TradeRouteId).IsEmpty, Is.True);
            Assert.That(new TradeRouteId(7UL).IsEmpty, Is.False);
            Assert.That(new TradeRouteId(7UL), Is.EqualTo(new TradeRouteId(7UL)));
            Assert.That(new TradeRouteId(7UL), Is.Not.EqualTo(new TradeRouteId(8UL)));
        }
    }
}

using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>Pins Phase 6 Atom 8 CaravanInstance lifecycle: advance, arrive, unload.</summary>
    public sealed class CaravanInstanceTests
    {
        private static readonly CaravanId Id = new CaravanId(7UL);
        private static readonly TradeRouteId Route = new TradeRouteId(101UL);
        private static readonly SiteId Origin = new SiteId(1UL);
        private static readonly SiteId Destination = new SiteId(2UL);

        [Test]
        public void State_HasFiveStableCodes()
        {
            Assert.That(CaravanState.Loading.Code, Is.EqualTo("loading"));
            Assert.That(CaravanState.EnRoute.Code, Is.EqualTo("en_route"));
            Assert.That(CaravanState.Arrived.Code, Is.EqualTo("arrived"));
            Assert.That(CaravanState.Unloading.Code, Is.EqualTo("unloading"));
            Assert.That(CaravanState.Idle.Code, Is.EqualTo("idle"));
        }

        [Test]
        public void Constructor_RejectsInvalidInputs()
        {
            Assert.Throws<System.ArgumentException>(() =>
                new CaravanInstance(default, Route, Origin, 5, 0, CaravanState.Loading));
            Assert.Throws<System.ArgumentException>(() =>
                new CaravanInstance(Id, default, Origin, 5, 0, CaravanState.Loading));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new CaravanInstance(Id, Route, Origin, -1, 0, CaravanState.Loading));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new CaravanInstance(Id, Route, Origin, 5, -1, CaravanState.Loading));
        }

        [Test]
        public void Constructor_EmptyState_DefaultsToIdle()
        {
            var caravan = new CaravanInstance(Id, Route, Origin, 5, 0, default);
            Assert.That(caravan.State, Is.EqualTo(CaravanState.Idle));
        }

        [Test]
        public void AdvanceStep_IncrementsAndMarksEnRoute()
        {
            var caravan = new CaravanInstance(Id, Route, Origin, 5, 0, CaravanState.Loading);
            caravan.AdvanceStep();
            Assert.That(caravan.StepsSinceDeparture, Is.EqualTo(1));
            Assert.That(caravan.State, Is.EqualTo(CaravanState.EnRoute));
            caravan.AdvanceStep();
            Assert.That(caravan.StepsSinceDeparture, Is.EqualTo(2));
        }

        [Test]
        public void Arrive_UpdatesSiteAndState()
        {
            var caravan = new CaravanInstance(Id, Route, Origin, 5, 3, CaravanState.EnRoute);
            caravan.Arrive(Destination);
            Assert.That(caravan.CurrentSiteId, Is.EqualTo(Destination));
            Assert.That(caravan.State, Is.EqualTo(CaravanState.Arrived));
        }

        [Test]
        public void Arrive_RejectsEmptySite()
        {
            var caravan = new CaravanInstance(Id, Route, Origin, 5, 3, CaravanState.EnRoute);
            Assert.Throws<System.ArgumentException>(() => caravan.Arrive(default));
        }

        [Test]
        public void Unload_PartialThenFull()
        {
            var caravan = new CaravanInstance(Id, Route, Origin, 5, 3, CaravanState.Arrived);

            Assert.That(caravan.Unload(2), Is.EqualTo(2));
            Assert.That(caravan.PayloadRemaining, Is.EqualTo(3));
            Assert.That(caravan.State, Is.EqualTo(CaravanState.Unloading));

            Assert.That(caravan.Unload(100), Is.EqualTo(3));
            Assert.That(caravan.PayloadRemaining, Is.EqualTo(0));
            Assert.That(caravan.State, Is.EqualTo(CaravanState.Idle));
        }

        [Test]
        public void Unload_RejectsNegativeQuantity()
        {
            var caravan = new CaravanInstance(Id, Route, Origin, 5, 0, CaravanState.Arrived);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => caravan.Unload(-1));
        }

        [Test]
        public void CaravanId_EmptySentinel()
        {
            Assert.That(default(CaravanId).IsEmpty, Is.True);
            Assert.That(new CaravanId(1UL).IsEmpty, Is.False);
            Assert.That(new CaravanId(1UL), Is.EqualTo(new CaravanId(1UL)));
        }
    }
}

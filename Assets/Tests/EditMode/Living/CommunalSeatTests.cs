using EmberCrpg.Domain.Actors;
using EmberCrpg.Simulation.Living;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Living
{
    /// <summary>W32: the Gate8 seat-ring pin, moved with the seats from ScheduleSystem to
    /// CommunalSeat (the EAT decision assigns seats at action creation now).</summary>
    public sealed class CommunalSeatTests
    {
        [Test]
        public void For_Ordinals0Through15_DistinctRingSeats_NeverTheTabletop()
        {
            var table = new GridPosition(10, 10);
            var seats = new System.Collections.Generic.HashSet<GridPosition>();
            int worstReach = 0;
            int nearestReach = int.MaxValue;
            for (int ordinal = 0; ordinal < CommunalSeat.SeatCount; ordinal++)
            {
                var seat = CommunalSeat.For(table, ordinal);
                seats.Add(seat);
                int reach = System.Math.Max(System.Math.Abs(seat.X - table.X), System.Math.Abs(seat.Y - table.Y));
                if (reach > worstReach) worstReach = reach;
                if (reach < nearestReach) nearestReach = reach;
            }

            Assert.That(seats.Count, Is.EqualTo(CommunalSeat.SeatCount), "no two ordinals may share a seat");
            Assert.That(worstReach, Is.LessThanOrEqualTo(NeedConsumptionSystem.EatReachCells),
                "every seat must stay within eating reach of the table");
            Assert.That(nearestReach, Is.EqualTo(2),
                "the inner 3x3 belongs to the table and benches - no diner stands ON the furniture");
        }

        [Test]
        public void For_OrdinalWrapsDeterministically()
        {
            var table = new GridPosition(0, 0);
            Assert.That(CommunalSeat.For(table, 17), Is.EqualTo(CommunalSeat.For(table, 1)));
            Assert.That(CommunalSeat.For(table, -1), Is.EqualTo(CommunalSeat.For(table, 15)));
        }
    }
}

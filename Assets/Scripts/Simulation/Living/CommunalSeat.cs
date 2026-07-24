using EmberCrpg.Domain.Actors;

// Design note:
// W32-02 §3.3: the communal-table seat ring, moved verbatim from ScheduleSystem (which lost
// its eat branch). The 16 seats are the Chebyshev RING-2 cells around the food spot.
// LIVE BUG ('masanin uzerine cikip eating'): the inner 3x3 is where the plaza table +
// benches render, so diners sit AROUND the furniture; every ring cell is exactly
// EatReachCells (2) from the centre, so meals succeed from every seat.
namespace EmberCrpg.Simulation.Living
{
    /// <summary>Deterministic ring-2 seat cells around a communal food spot.</summary>
    public static class CommunalSeat
    {
        public const int SeatCount = 16;

        private static readonly (int dx, int dy)[] SeatOffsets =
        {
            (2, 0), (2, 1), (2, 2), (1, 2), (0, 2), (-1, 2), (-2, 2), (-2, 1), (-2, 0),
            (-2, -1), (-2, -2), (-1, -2), (0, -2), (1, -2), (2, -2), (2, -1),
        };

        public static GridPosition For(GridPosition table, int seatOrdinal)
        {
            var (dx, dy) = SeatOffsets[((seatOrdinal % SeatCount) + SeatCount) % SeatCount];
            return new GridPosition(table.X + dx, table.Y + dy);
        }
    }
}

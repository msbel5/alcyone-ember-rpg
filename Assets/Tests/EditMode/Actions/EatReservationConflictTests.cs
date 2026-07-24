using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Actors.Actions;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W32 DOC6 T2: the SAME last food unit can never be promised to two actors. "Ayşe
    /// reserves the last bread; Mehmet finds none and KNOWS it" — the loser stays actionless
    /// (silent by design: an unlogged empty-larder pass, the per-tick spam lesson) instead of
    /// living the old silent empty-scan.
    /// </summary>
    public sealed class EatReservationConflictTests
    {
        [Test]
        public void LastUnit_TwoActors_OneDeterministicWinner_MatterConserved()
        {
            var world = EatSliceWorld.Build(wheat: 1);                // the LAST unit
            world.Actors.Add(EatSliceWorld.Hungry(1, 4, 4));          // A — FIRST in store order
            world.Actors.Add(EatSliceWorld.Hungry(2, 4, 5));          // B — same distance class
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            composer.Advance(world, 1); // decision + reservation land in the same tick band

            Assert.That(world.Reservations.Rows.Count, Is.EqualTo(1), "one unit -> one reservation");
            Assert.That(world.Reservations.Rows.Single().ActorId, Is.EqualTo(1UL),
                "the winner is DETERMINISTIC: store insertion order (order breaks the tie, not a seed)");
            var b = world.Actors.Get(new ActorId(2));
            Assert.That(b.ActionState.CurrentAction, Is.EqualTo(ActorActionType.None),
                "B gets no EatAction — its intent falls through (no_food_available) for replan");

            // Run the episode out: conservation to the chapter's end.
            for (var tick = 2; tick <= 40; tick++)
                composer.Advance(world, tick);
            Assert.That(EatSliceWorld.Meals(world), Is.EqualTo(1), "one unit goes down exactly one throat");
            Assert.That(world.Stockpiles[0].Get("wheat"), Is.Zero);
            Assert.That(world.Reservations.Rows, Is.Empty, "no claim outlives the episode");
        }

        [Test]
        public void ReservedUnit_IsInvisible_ToASecondReserver()
        {
            var world = EatSliceWorld.Build(wheat: 1);
            world.Actors.Add(EatSliceWorld.Hungry(1, 4, 4));
            world.Actors.Add(EatSliceWorld.Hungry(2, 4, 5));
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);
            composer.Advance(world, 1); // A holds the claim now

            Assert.That(world.Reservations.TryReserve(1UL, "wheat", 2UL,
                untilMinutes: 999, pileCount: world.Stockpiles[0].Get("wheat"), out _), Is.False,
                "a reserved unit is invisible — the direct reserve for B is refused");
            for (var i = 0; i < world.ActionLog.Count; i++)
            {
                var entry = world.ActionLog.At(i);
                Assert.That(entry.ActorId == 2UL && entry.Reason == ActionLogReason.ReservationAcquired,
                    Is.False, "the trace holds NO ResourceReserved line for B");
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W32 DOC6 T4: the action persists across ticks with the SAME identity. The episode's
    /// id is its (StartedAtMinutes, ReservationId) pair — constant from the decision to the
    /// meal; the chain only ever moves forward (MoveToFood -> TakeFood -> ConsumeFood) and no
    /// in-episode tick leaves the actor actionless.
    /// </summary>
    public sealed class EatActionContinuityTests
    {
        [Test]
        public void OneEpisode_OneIdentity_MonotonePhases_NoGaps()
        {
            var world = EatSliceWorld.Build();
            world.Actors.Add(EatSliceWorld.Hungry(7, 20, 20)); // walk >= 13 ticks: MoveToFood lives long
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            ActorRecord A() => world.Actors.Get(new ActorId(7));
            bool MealDone() => world.Events.Events.Any(e => e.Kind == WorldEventKind.ActionCompleted);

            var episode = new List<(long startedAt, ulong reservation, ActorActionType action)>();
            int gapTicks = 0;
            for (var tick = 1; tick <= 200 && !MealDone(); tick++)
            {
                composer.Advance(world, tick);
                var state = A().ActionState;
                if (state.CurrentAction != ActorActionType.None)
                    episode.Add((state.StartedAtMinutes, state.ReservationId.Value, state.CurrentAction));
                else if (episode.Count > 0 && !MealDone())
                    gapTicks++;
            }

            Assert.That(episode.Select(e => (e.startedAt, e.reservation)).Distinct().Count(), Is.EqualTo(1),
                "ONE identity from IntentChosen to MealConsumed — the episode id persists across ticks");
            Assert.That(episode.Zip(episode.Skip(1), (a, b) => (int)b.action >= (int)a.action).All(x => x),
                Is.True, "phase order MoveToFood->TakeFood->ConsumeFood: never rewinds, never skips back");
            Assert.That(episode.Count, Is.GreaterThanOrEqualTo(13), "the episode really lived multi-tick");
            Assert.That(gapTicks, Is.Zero, "no in-episode tick left the actor 'actionless'");
        }
    }
}

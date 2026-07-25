using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Actions
{
    /// <summary>
    /// W34 DOC4 S4: output authorship — an ingot is BORN of a completed PerformWork commit,
    /// nothing else. The rest of matter conservation: no RecipeCompleted event without a
    /// PerformWork/Running->/Succeeded transition, no JobCompleted without an actor at the
    /// bench, and every tick where iron rises is a tick where a smith stood at the bench.
    /// </summary>
    public sealed class WorkOutputAuthorshipTests
    {
        [Test]
        public void EveryIronGained_HasABodyAtTheBench_AndACompletedPerformWork()
        {
            var world = WorkSliceWorld.Build(ore: 6, fuel: 3);
            var smith = WorkSliceWorld.Smith(7, 6, 5);
            world.Actors.Add(smith);
            WorkSliceWorld.PostSmeltJob(world, quantity: 3);
            var composer = new WorldTickComposer();
            composer.Advance(world, 0);

            var pile = WorkSliceWorld.Pile(world);
            int previousIngots = 0;
            int commitsWitnessed = 0;
            // Run long enough to finish all three executions; each ingot must be authored.
            for (var tick = 1; tick <= 6 * 60
                 && pile.Get(WorkSliceWorld.IngotTag) < 3; tick++)
            {
                composer.Advance(world, tick);
                var ingots = pile.Get(WorkSliceWorld.IngotTag);
                if (ingots > previousIngots)
                {
                    // The ingot-mint tick is a PerformWork commit tick — the smith is at
                    // the bench and the RecipeCompleted event carries THIS tick's stamp.
                    Assert.That(smith.ActionState.CurrentAction, Is.EqualTo(ActorActionType.PerformWork),
                        $"tick {tick}: ingots rose but the smith was not on the PerformWork step");
                    var distance = System.Math.Max(
                        System.Math.Abs(smith.Position.X - WorkSliceWorld.Bench.X),
                        System.Math.Abs(smith.Position.Y - WorkSliceWorld.Bench.Y));
                    Assert.That(distance, Is.LessThanOrEqualTo(1),
                        $"tick {tick}: an ingot appeared while the smith was {distance} away — remote labour lives");
                    Assert.That(world.Events.Events.Any(e => e.Kind == WorldEventKind.RecipeCompleted
                        && e.Tick.TotalMinutes == world.Time.TotalMinutes), Is.True,
                        $"tick {tick}: an ingot without a RecipeCompleted event — the commit grammar is broken");
                    previousIngots = ingots;
                    commitsWitnessed++;
                }
            }
            Assert.That(commitsWitnessed, Is.EqualTo(3),
                "vacuous guard: three ingots minted, three commit witnesses — the horizon was enough");

            // Matter conservation: iron in the world equals what was minted (inputs consumed).
            Assert.That(pile.Get(WorkSliceWorld.IngotTag), Is.EqualTo(3));
            Assert.That(pile.Get(WorkSliceWorld.OreTag), Is.EqualTo(0));
            Assert.That(pile.Get(WorkSliceWorld.FuelTag), Is.EqualTo(0));

            // One JobCompleted total (quantity closes the job).
            var jobDone = world.Events.Events.Where(e => e.Kind == WorldEventKind.JobCompleted).ToList();
            Assert.That(jobDone.Count, Is.EqualTo(1), "one job, one JobCompleted — no ghosts");
            Assert.That(jobDone[0].ActorId.Value, Is.EqualTo(7UL),
                "attribution went to the smith at the finishing stroke (§13.4)");
        }
    }
}

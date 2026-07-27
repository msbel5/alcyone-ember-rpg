#if UNITY_EDITOR
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Presentation.Ember.Adapters;
using EmberCrpg.Tests.EditMode.Actions.Support;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Diagnostics
{
    /// <summary>
    /// B26 (Doc 03 §6.2/§6.3): the ProofLivingCensus snapshot alone caught nothing between
    /// action chunks — a marathon happily reported meals=6195 alongside eating=0 because
    /// the accumulating event log and the instant snapshot live on DIFFERENT clocks. These
    /// tests pin both the wound (T-CENSUS-1) and the peaks fix that closes it (T-CENSUS-2),
    /// plus the driver-side PASS gate the peaks feed (T-CENSUS-3).
    /// </summary>
    public sealed class ProofLivingCensusPeaksTests
    {
        // Small parser: pulls "field=int" out of the census string. Tests don't reproduce
        // the entire format — the value is what we're checking, not the printer.
        private static int Field(string census, string name)
        {
            int i = census.IndexOf(name + "=", System.StringComparison.Ordinal);
            Assert.That(i, Is.GreaterThanOrEqualTo(0), $"census missing field '{name}': {census}");
            int start = i + name.Length + 1;
            int end = start;
            while (end < census.Length && (char.IsDigit(census[end]) || census[end] == '-')) end++;
            return int.Parse(census.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static WorldState BuildWorldWithMealEvents(int meals)
        {
            var world = EatSliceWorld.Build(wheat: 10);
            for (int k = 0; k < meals; k++)
            {
                // The exact prefix ProofLivingCensus counts (see ConsumeFoodAdvancer):
                // "meal_eaten item:{tag} hunger:{value}".
                world.Events.Append(new WorldEvent(
                    world.Time, WorldEventKind.NeedChanged,
                    new ActorId(1UL), new SiteId(1),
                    $"meal_eaten item:wheat hunger:{50 - k}"));
            }
            return world;
        }

        /// <summary>
        /// T-CENSUS-1: N meals happened (proven by the event log), but no actor is mid-eat
        /// at the moment of the snapshot. The old census printed `eating=0` alongside the
        /// meals count — pure evidence of the wound. This test DOCUMENTS that split-clock
        /// behaviour and pins the `*Now` rename so future confusion re-introduces a red test.
        /// </summary>
        [Test]
        public void SnapshotZero_EventsMany_ExposesTheWound()
        {
            var world = BuildWorldWithMealEvents(meals: 7);
            var eater = EatSliceWorld.Hungry(1UL, 5, 5);
            world.Actors.Add(eater);
            // Actor lives, but CurrentAction is None between chunks — the split-clock case.
            Assert.That(eater.ActionState.CurrentAction, Is.EqualTo(ActorActionType.None),
                "fixture precondition: no live slice at snapshot time");
            var adapter = new DomainSimulationAdapter(world);

            string census = adapter.ProofLivingCensus();

            Assert.That(Field(census, "meals"), Is.EqualTo(7),
                "events log carries every meal ever eaten");
            Assert.That(Field(census, "eatingNow"), Is.EqualTo(0),
                "snapshot fields honestly report NOW — no actor is mid-eat at report time");
        }

        /// <summary>
        /// T-CENSUS-2: the direct positive. Two samples — one taken while an actor holds
        /// ConsumeFood, one taken after the actor returns to None. The final census must
        /// report `eatingPeak > 0` while `eatingNow == 0`. This is the exact property the
        /// peaks accumulator guarantees: evidence a slice ever ran survives the actor
        /// returning to Idle between the slice and the census read.
        /// </summary>
        [Test]
        public void PeaksAccumulateAcrossSamples_EvenWhenSnapshotZero()
        {
            var world = BuildWorldWithMealEvents(meals: 1);
            var eater = EatSliceWorld.Hungry(1UL, 5, 5);
            world.Actors.Add(eater);
            var adapter = new DomainSimulationAdapter(world);
            adapter.ProofResetLivingPeaks();
            var zeroed = adapter.ProofLivingPeaks();
            Assert.That(zeroed.sleeping + zeroed.working + zeroed.eating + zeroed.farming + zeroed.samples,
                Is.EqualTo(0), "reset zeros every peak and the sample counter (peaks would otherwise leak between runs)");

            // Sample #1: actor is mid-eat — the slice is LIVE.
            eater.ApplyActionState(ActorActionState.ForIntent(ActorIntent.Eat).Start(
                ActorActionType.ConsumeFood, new SiteId(1), ItemId.Empty,
                ReservationId.Empty, startedAtMinutes: 60L, ActionInterruptPolicy.Interruptible));
            adapter.ProofSampleLivingPeaks();

            // Sample #2: actor is back to Idle — the slice has ended.
            eater.ApplyActionState(ActorActionState.Idle);
            adapter.ProofSampleLivingPeaks();

            string census = adapter.ProofLivingCensus();
            Assert.That(Field(census, "eatingNow"), Is.EqualTo(0),
                "no actor is mid-eat at the moment of the census read");
            Assert.That(Field(census, "eatingPeak"), Is.GreaterThan(0),
                "the peak survives the actor returning to Idle — the whole point of the fix");
            Assert.That(Field(census, "peakSamples"), Is.GreaterThanOrEqualTo(2),
                "every explicit sample counts (plus the one folded by ProofLivingCensus itself)");
        }

        /// <summary>
        /// T-CENSUS-4 (W39): the sampling GAP that broke the marathon — an outer sample every
        /// N seconds misses actions that start AND end inside one AdvanceTick jump. The fix
        /// arms per-tick sampling INSIDE AdvanceTick so a five-tick jump folds five peaks
        /// (one per composer sub-step), not one. Unarmed AdvanceTick folds none — production
        /// paths keep paying zero. Together the two asserts pin the actual mechanism the
        /// marathon censusOk gate now depends on.
        /// </summary>
        [Test]
        public void ArmedAdvanceTick_FoldsOnePeakPerComposerSubStep()
        {
            var world = EatSliceWorld.Build(wheat: 10);
            world.Actors.Add(EatSliceWorld.Hungry(1UL, 5, 5));
            var adapter = new DomainSimulationAdapter(world);

            // Anchor the composer so subsequent jumps produce a real delta (first-ever
            // Advance is an anchor-only no-op per WorldTickComposer's contract).
            adapter.AdvanceTick(adapter.TickIndex);

            adapter.ProofResetLivingPeaks();
            adapter.ProofArmPeakSampling(true);
            int before = adapter.ProofLivingPeaks().samples;

            // Jump five ticks in ONE call. The armed path must sample after every sub-step
            // so a slice born and buried inside the jump still lights the peak.
            adapter.AdvanceTick(adapter.TickIndex + 5);

            int afterArmed = adapter.ProofLivingPeaks().samples;
            Assert.That(afterArmed - before, Is.EqualTo(5),
                "armed AdvanceTick folds ONE peak per composer sub-step (5 ticks -> 5 samples), which is why an in-flight Sleep between two outer samples still lands in the peaks");

            // Symmetric disarm: production paths must pay zero per-tick sampler cost.
            adapter.ProofArmPeakSampling(false);
            int mark = adapter.ProofLivingPeaks().samples;
            adapter.AdvanceTick(adapter.TickIndex + 5);
            Assert.That(adapter.ProofLivingPeaks().samples, Is.EqualTo(mark),
                "unarmed AdvanceTick folds NO samples — the marathon's proof-only hook does not leak into normal play");
        }

    }
}
#endif

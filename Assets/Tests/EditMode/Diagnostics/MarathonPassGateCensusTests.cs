using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Diagnostics
{
    /// <summary>
    /// B26 (Doc 03 §6.3): T-CENSUS-3. Extracted from ProofLivingCensusPeaksTests so this
    /// pure-boolean gate is visible to the engine-free fallback harness — the adapter-touching
    /// T-CENSUS-1/2 stay Editor-only (they need DomainSimulationAdapter which is Unity-tied).
    /// The boolean is MIRRORED verbatim from EmberProofScreenshotDriver.RunMarathon; extracting
    /// a helper class would touch more Presentation code than the wound justifies. If the driver
    /// changes the clause without touching this test, the comment drift becomes the review flag.
    /// </summary>
    public sealed class MarathonPassGateCensusTests
    {
        // Verbatim clause from the driver:
        //   bool censusOk = gameHours < 24 || (peaks.sleeping > 0 && peaks.working > 0 && peaks.eating > 0);
        private static bool CensusOk(long gameHours, int sleep, int work, int eat)
            => gameHours < 24 || (sleep > 0 && work > 0 && eat > 0);

        [Test]
        public void FullDay_ZeroSleepPeak_Rejects()
        {
            // "A soak that lived through a full day-night cycle in which NOBODY slept
            //  is a broken world wearing a green badge." — Doc 03 §6.3.
            Assert.That(CensusOk(gameHours: 48, sleep: 0, work: 1000, eat: 1000), Is.False,
                "eating events prove nothing about sleep — a full day with zero sleep peak is Potemkin PASS");
        }

        [Test]
        public void FullDay_AllPeaksPositive_Passes()
        {
            Assert.That(CensusOk(gameHours: 48, sleep: 3, work: 5, eat: 12), Is.True,
                "every cardinal slice witnessed at least once — the world lived");
        }

        [Test]
        public void ShortSmoke_AllZeroPeaks_Passes_AdvisoryOnly()
        {
            // Under a game-day the peaks are advisory — short smoke soaks would flake
            // on clock phase (they might not straddle any sleep/work window).
            Assert.That(CensusOk(gameHours: 6, sleep: 0, work: 0, eat: 0), Is.True,
                "under 24 game-hours the gate must not require peaks");
            Assert.That(CensusOk(gameHours: 23, sleep: 0, work: 0, eat: 0), Is.True,
                "the carveout holds right up to the 24-hour boundary");
        }

        [Test]
        public void FullDay_ZeroWorkPeak_Rejects()
        {
            Assert.That(CensusOk(gameHours: 24, sleep: 5, work: 0, eat: 5), Is.False,
                "a day where nobody worked is a dead colony");
        }

        [Test]
        public void FullDay_ZeroEatPeak_Rejects()
        {
            // meals-as-events proves eating happened; a zero eat PEAK means no eat slice was
            // ever seen live in the census sample stream — evidence must overlap the gate.
            Assert.That(CensusOk(gameHours: 24, sleep: 3, work: 3, eat: 0), Is.False,
                "zero eat peak is unfinished evidence even when meal events accumulated");
        }

        [Test]
        public void ExactlyDayBoundary_RequiresPositivePeaks()
        {
            // gameHours == 24 is the FIRST value that triggers the strict half of the clause
            // (< 24 short-circuits). Pin the boundary so a future off-by-one is caught.
            Assert.That(CensusOk(gameHours: 24, sleep: 0, work: 0, eat: 0), Is.False,
                "the strict half activates AT 24 hours, not after");
            Assert.That(CensusOk(gameHours: 24, sleep: 1, work: 1, eat: 1), Is.True,
                "and one witness of each slice is enough to pass it");
        }
    }
}

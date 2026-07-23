using EmberCrpg.Domain.Actors;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.World
{
    /// <summary>PLAYTEST pin: the wait/rest keys ride on these two pure functions.</summary>
    public sealed class PlayerRestServiceTests
    {
        [Test]
        public void HoursUntilDawn_At2300_IsSeven()
            => Assert.That(PlayerRestService.HoursUntilDawn(23 * 60), Is.EqualTo(7));

        [Test]
        public void HoursUntilDawn_ExactlyAtDawn_IsAFullDay()
            => Assert.That(PlayerRestService.HoursUntilDawn(6 * 60), Is.EqualTo(24));

        [Test]
        public void RestedVitals_RefillsFatigueAndMana_AndKnitsHealthGradually()
        {
            var tired = new ActorVitals(new VitalStat(10, 60), new VitalStat(2, 40), new VitalStat(0, 20));
            var rested = PlayerRestService.RestedVitals(tired, 8);
            Assert.That(rested.Fatigue.Current, Is.EqualTo(40), "sleep refills fatigue");
            Assert.That(rested.Mana.Current, Is.EqualTo(20), "sleep refills mana");
            Assert.That(rested.Health.Current, Is.EqualTo(50), "8h knits 8/12 of max (40) onto 10");
            Assert.That(PlayerRestService.RestedVitals(rested, 24).Health.Current, Is.EqualTo(60),
                "healing clamps at max");
        }
    }
}

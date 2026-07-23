using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>
    /// PLAYTEST pin: the mapper used to DROP Home/DayAnchor (the ctor re-defaulted both to the
    /// saved position), so one night save collapsed every villager's Home onto the sleeping
    /// pile at the town centre — forever. The roundtrip must keep both anchors.
    /// </summary>
    public sealed class ActorSaveMapperTests
    {
        [Test]
        public void Roundtrip_PreservesHomeAndDayAnchor()
        {
            var actor = new ActorRecord(
                new ActorId(7), "Villager", ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(21, 22),
                accuracy: 10, dodge: 5, armor: 1, baseDamage: 2,
                home: new GridPosition(3, 4), dayAnchor: new GridPosition(8, 9));

            var back = ActorSaveMapper.ToActor(ActorSaveMapper.ToData(actor));

            Assert.That(back.Home, Is.EqualTo(new GridPosition(3, 4)), "Home must survive the save");
            Assert.That(back.DayAnchor, Is.EqualTo(new GridPosition(8, 9)), "DayAnchor must survive the save");
            Assert.That(back.Position, Is.EqualTo(new GridPosition(21, 22)), "Position stays independent of the anchors");
        }
    }
}

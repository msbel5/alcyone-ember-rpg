using EmberCrpg.Simulation.CharacterCreation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.CharacterCreation
{
    public sealed class DiceRollDeterminismTest
    {
        [Test]
        public void Roll4d6DropLowest_IsDeterministic()
        {
            var a = AttributeRoller.Roll4d6DropLowest(42u, "STR");
            var b = AttributeRoller.Roll4d6DropLowest(42u, "STR");
            Assert.That(a.Dice, Is.EqualTo(b.Dice));
            Assert.That(a.Total, Is.EqualTo(b.Total));
            Assert.That(a.Total, Is.EqualTo(a.Dice[0] + a.Dice[1] + a.Dice[2] + a.Dice[3] - a.DroppedValue));
        }
    }
}

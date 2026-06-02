using EmberCrpg.Domain.Core;
using EmberCrpg.Presentation.Visual;
using NUnit.Framework;

// Pattern: Golden-master formatter checks — lock deterministic narrator output to exact strings.
namespace EmberCrpg.Tests.EditMode.Visual
{
    public sealed class WorldEventNarratorTests
    {
        [Test]
        public void RecipeCompleted_WithActorSubject_FormatsExactLine()
        {
            var narrator = new WorldEventNarrator();
            var row = new WorldEventRow(
                new GameTime(400),
                "RecipeCompleted",
                new ActorId(42UL),
                default,
                "smelted IronIngot at Furnace");

            var line = narrator.ToLine(row);

            Assert.That(line, Is.EqualTo("[d1 06:40] 42 · crafted · smelted IronIngot at Furnace"));
        }

        [Test]
        public void SiteEntered_UsesSiteAsSubject_WhenActorIsEmpty()
        {
            var narrator = new WorldEventNarrator();
            var row = new WorldEventRow(
                new GameTime(75),
                "SiteEntered",
                default,
                new SiteId(9UL),
                "entered market gate");

            var line = narrator.ToLine(row);

            Assert.That(line, Is.EqualTo("[d1 01:15] 9 · entered · entered market gate"));
        }

        [Test]
        public void UnmappedKind_FallsBackToKindCode_AndIsDeterministic()
        {
            var narrator = new WorldEventNarrator();
            var row = new WorldEventRow(
                new GameTime(1440),
                "CustomKind",
                default,
                default,
                "custom reason");

            var first = narrator.ToLine(row);
            var second = narrator.ToLine(row);

            Assert.That(first, Is.EqualTo("[d2 00:00] world · CustomKind · custom reason"));
            Assert.That(second, Is.EqualTo(first));
        }
    }
}

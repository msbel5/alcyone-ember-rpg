using System.Linq;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Magic;
using EmberCrpg.Simulation.Process;
using EmberCrpg.Simulation.Time;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Composition
{
    /// <summary>
    /// B27 wound-close story test. The bug was in the composer wire, not in the domain gate:
    /// <c>DefaultTickSystems.PlantGrowthStep.Run</c> hardcoded <c>isSnowing: false</c>, so
    /// species with <c>BlockedBySnow=true</c> kept advancing through winter. The fix collapses
    /// snow-day to <c>season == Season.Winter</c>. These tests fail when the wire regresses:
    /// (a) a BlockedBySnow=true crop stays frozen on a winter day, while
    /// (b) a winter-tolerant crop still ticks; and (c) both crops advance on a spring day.
    /// </summary>
    public sealed class PlantGrowthSnowGateWireTests
    {
        private const string FrostShyId = "b27_frostshy";
        private const string WinterHardyId = "b27_winterhardy";

        [Test]
        public void PlantGrowthStep_WinterDay_FreezesBlockedBySnow_TicksTolerantSpecies()
        {
            var world = new WorldState();
            SeedTwoCrops(world);
            var step = ResolvePlantGrowthStep();

            // DayOfYear = 280 falls inside Winter (271-360) in the canonical calendar.
            var winter = new GameTime(279L * GameTime.MinutesPerDay);
            Assume.That(new SeasonCalendar(WinterCalendarDefs()).GetSeason(winter), Is.EqualTo(Season.Winter));

            step.Run(new TickContext(world, winter, 1));

            Assert.That(world.Plants.Get(new WorldComponentId(1)).DaysInStage, Is.EqualTo(0),
                "BlockedBySnow=true crop must NOT tick on a winter day (B27 wire).");
            Assert.That(world.Plants.Get(new WorldComponentId(2)).DaysInStage, Is.EqualTo(1),
                "BlockedBySnow=false crop must still tick on a winter day.");
        }

        [Test]
        public void PlantGrowthStep_SpringDay_AdvancesBothSpecies()
        {
            var world = new WorldState();
            SeedTwoCrops(world);
            var step = ResolvePlantGrowthStep();

            var spring = new GameTime(4L * GameTime.MinutesPerDay); // DayOfYear=5, Spring.
            Assume.That(new SeasonCalendar(WinterCalendarDefs()).GetSeason(spring), Is.EqualTo(Season.Spring));

            step.Run(new TickContext(world, spring, 1));

            Assert.That(world.Plants.Get(new WorldComponentId(1)).DaysInStage, Is.EqualTo(1),
                "BlockedBySnow=true crop must tick outside winter.");
            Assert.That(world.Plants.Get(new WorldComponentId(2)).DaysInStage, Is.EqualTo(1),
                "BlockedBySnow=false crop must tick outside winter.");
        }

        private static IWorldTickSystem ResolvePlantGrowthStep()
        {
            var registry = DefaultTickSystems.Create(
                new GameTimeAdvanceSystem(new SeasonCalendar(WinterCalendarDefs())),
                new NeedsSystem(),
                new MagicTickDriver(new SpellCooldownService(), new ShieldBuffService()),
                new CaravanSystem(),
                new PlantGrowthSystem(),
                new JobAssignmentSystem(),
                new PriceUpdateSystem(),
                new ScheduleSystem(),
                new FactionReputationDecaySystem(),
                FactionDecayConfig.Default,
                new SeasonCalendar(WinterCalendarDefs()),
                TwoSpecies());

            // The actual step id in DefaultTickSystems.cs PlantGrowthStep is "econ.plantgrowth".
            return registry.Ordered.Single(s => s.Id == "econ.plantgrowth");
        }

        private static void SeedTwoCrops(WorldState world)
        {
            world.Plants.Add(new WorldComponentId(1), new PlantComponent(
                new WorldComponentId(1), new SiteId(1), new GridPosition(0, 0),
                FrostShyId, new PlantStageId("seed"), 0));
            world.Plants.Add(new WorldComponentId(2), new PlantComponent(
                new WorldComponentId(2), new SiteId(1), new GridPosition(1, 0),
                WinterHardyId, new PlantStageId("seed"), 0));
        }

        private static PlantSpeciesDef[] TwoSpecies()
        {
            var stages = new[]
            {
                new PlantGrowthStageDef(new PlantStageId("seed"), "Seed", 5, false),
                new PlantGrowthStageDef(new PlantStageId("ripe"), "Ripe", 0, true),
            };
            return new[]
            {
                new PlantSpeciesDef(FrostShyId, FrostShyId + "_seed", FrostShyId,
                    stages,
                    // Any season allowed, but snow blocks — under Slice 1 "snow == winter".
                    new[] { new PlantGrowthRule(Season.None, true, true) }),
                new PlantSpeciesDef(WinterHardyId, WinterHardyId + "_seed", WinterHardyId,
                    stages,
                    new[] { new PlantGrowthRule(Season.None, true, false) }),
            };
        }

        private static SeasonDefinition[] WinterCalendarDefs()
        {
            return new[]
            {
                new SeasonDefinition(Season.Spring, 1, 90),
                new SeasonDefinition(Season.Summer, 91, 180),
                new SeasonDefinition(Season.Autumn, 181, 270),
                new SeasonDefinition(Season.Winter, 271, 360),
            };
        }
    }
}

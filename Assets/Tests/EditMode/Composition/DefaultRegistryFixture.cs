using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Time;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.Composition;
using EmberCrpg.Simulation.Living;
using EmberCrpg.Simulation.Magic;
using EmberCrpg.Simulation.Process;
using EmberCrpg.Simulation.Time;
using EmberCrpg.Simulation.World;

namespace EmberCrpg.Tests.EditMode.Composition
{
    /// <summary>
    /// B03 (W33-05 fix 1): the ONE test-side construction of the default tick registry.
    /// CONSTRAINT: "known system ids" must be DERIVED from the composition root
    /// (DefaultTickSystems.Create), never hand-typed — a hand-typed list rotted into six
    /// ghost ids and blessed the econ.trade@Daily:28 phantom writer. Calling Create and
    /// reading registry.Ordered IS "reading every base(...) id" at runtime, immune to
    /// future registrations.
    /// </summary>
    internal static class DefaultRegistryFixture
    {
        internal static WorldTickRegistry CreateDefault()
        {
            return DefaultTickSystems.Create(
                new GameTimeAdvanceSystem(DefaultCalendar()),
                new NeedsSystem(),
                new MagicTickDriver(new SpellCooldownService(), new ShieldBuffService()),
                new CaravanSystem(),
                new PlantGrowthSystem(),
                new JobAssignmentSystem(),
                new PriceUpdateSystem(),
                new ScheduleSystem(),
                new FactionReputationDecaySystem(),
                FactionDecayConfig.Default,
                DefaultCalendar(),
                DefaultPlantSpecies());
        }

        internal static SeasonCalendar DefaultCalendar()
        {
            return new SeasonCalendar(new[]
            {
                new SeasonDefinition(Season.Spring, 1, 90),
                new SeasonDefinition(Season.Summer, 91, 180),
                new SeasonDefinition(Season.Autumn, 181, 270),
                new SeasonDefinition(Season.Winter, 271, 360),
            });
        }

        internal static PlantSpeciesDef[] DefaultPlantSpecies()
        {
            return new[]
            {
                new PlantSpeciesDef(
                    "wheat",
                    "wheat_seed",
                    "wheat_grain",
                    new[]
                    {
                        new PlantGrowthStageDef(new PlantStageId("seed"), "Seed", 1, false),
                        new PlantGrowthStageDef(new PlantStageId("sprout"), "Sprout", 1, false),
                        new PlantGrowthStageDef(new PlantStageId("ripe"), "Ripe", 0, true),
                    },
                    new[]
                    {
                        new PlantGrowthRule(Season.None, true, false),
                    }),
            };
        }
    }
}

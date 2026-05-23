using System;
using System.Globalization;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Simulation.CharacterCreation;
using EmberCrpg.Simulation.Worldgen;

namespace EmberCrpg.Validation.Sample
{
    internal static class Program
    {
        private static int Main()
        {
            var style = WorldStyle.DarkFantasyGrim;
            var genre = WorldGenre.PoliticalIntrigue;
            var world = WorldgenService.Generate(42u, WorldgenParameters.For(style, genre));
            var answers = new[] { "a", "c", "a", "b", "c", "a", "b", "c", "a", "a" };
            var suggested = new CharacterCreationService().SuggestClass(answers);
            var birthsign = CharacterCreationCatalog.GetBirthsign("the_lover");

            Console.WriteLine(
                $"Style={style}, Genre={genre}, {world.Regions.Count} regions, {world.Settlements.Count} settlements, " +
                $"{world.TotalPopulation.ToString("N0", CultureInfo.InvariantCulture)} total population, {world.Npcs.Count} NPCs, {world.History.Count} history events, " +
                $"suggested class for {{a, c, a, b...}} answers = {suggested.Name} with {birthsign.Name.Replace(" ", string.Empty)} birthsign");
            return 0;
        }
    }
}

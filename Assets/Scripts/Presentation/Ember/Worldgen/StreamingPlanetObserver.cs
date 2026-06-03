using System.Collections.Concurrent;
using EmberCrpg.Simulation.Worldgen.Planet;

namespace EmberCrpg.Presentation.Ember.Worldgen
{
    /// <summary>
    /// Collects planet-generation stage reports (fired on the worker thread while the planet is built off the
    /// main thread) into a thread-safe queue, so the char-creation "World Awakens" reveal coroutine can drain
    /// them on the main thread and STREAM the world forming, stage by stage. Pure presentation glue — it only
    /// turns each stage into a line of flavour text + its one-line summary.
    /// </summary>
    public sealed class StreamingPlanetObserver : IPlanetGenerationObserver
    {
        private readonly ConcurrentQueue<string> _lines = new ConcurrentQueue<string>();

        public void OnStageCompleted(PlanetStageReport report)
        {
            _lines.Enqueue(PhraseFor(report));
        }

        public bool TryDequeue(out string line) => _lines.TryDequeue(out line);

        private static string PhraseFor(PlanetStageReport report)
        {
            string flavor;
            switch (report.StageName)
            {
                case "Icosphere": flavor = "The planet's ember cools; a crust hardens over the sphere"; break;
                case "Plates": flavor = "The crust cracks into great drifting plates"; break;
                case "Boundaries": flavor = "The plates grind and collide along their margins"; break;
                case "TectonicElevation": flavor = "Mountains rise where plates converge; trenches yawn where they part"; break;
                case "ElevationNoise": flavor = "Ages of weathering break the land into hills and ragged coasts"; break;
                case "Climate": flavor = "Winds wheel into belts; rains fall, and shadows gather behind the peaks"; break;
                case "Hydrology": flavor = "Waters run together into rivers and lakes, carving toward the sea"; break;
                case "Erosion": flavor = "Rivers cut their valleys; the highlands slowly wear down"; break;
                case "Resources": flavor = "Metals settle into deep veins; coal and oil bed down in old basins"; break;
                case "Settlements": flavor = "The first peoples take root on fertile shores and river plains"; break;
                default: flavor = report.StageName; break;
            }

            return string.IsNullOrEmpty(report.Summary) ? flavor : flavor + "  (" + report.Summary + ")";
        }
    }
}

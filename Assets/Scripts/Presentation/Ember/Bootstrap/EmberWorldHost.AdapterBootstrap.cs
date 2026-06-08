using EmberCrpg.Domain.Configuration;
using EmberCrpg.Presentation.Ember.Adapters;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Bootstrap
{
    public sealed partial class EmberWorldHost
    {
        private static IDomainSimulationAdapter CreateFallbackAdapter()
        {
            Debug.LogError("EmberWorldHost: BROKEN no real domain adapter is active; using unavailable adapter with no fabricated gameplay rows.");
            return new EmberCrpg.Presentation.Ember.Adapters.UnavailableSimulationAdapter();
        }

        /// <summary>
        /// Codex audit (third pass A-P1): try to bootstrap a real
        /// <see cref="DomainSimulationAdapter"/> over a fresh
        /// <see cref="EmberCrpg.Domain.World.WorldState"/>. Returns
        /// <c>null</c> if WorldFactory throws or if the construction
        /// path is otherwise unavailable; the caller falls through to an
        /// honest disabled adapter. Wrapped in try/catch so a missing
        /// Simulation-side dependency never crashes scene bootstrap.
        /// </summary>
        private static IDomainSimulationAdapter TryCreateDomainAdapter()
        {
            try
            {
                var world = new EmberCrpg.Simulation.World.WorldFactory().Create(roomSeed: 1);
                // LIVE-3: standalone scenes (not entered through the worldgen wizard) had no WorldProfile,
                // so their HUD top-bar showed only "Tick/Day" while worldgen-entered scenes showed the full
                // "<Style> / <Genre>  Pop <n>" line — inconsistent across the 10 scenes. Seed a default
                // profile so the top-bar reads IDENTICALLY everywhere; the worldgen path overwrites it with
                // the player's real choices when they come through character creation.
                if (world.WorldProfile == null)
                {
                    var fallback = EmberRuntimeOptionsProvider.Current.WorldHost;
                    world.WorldProfile = new EmberCrpg.Domain.Worldgen.WorldProfile(
                        EmberCrpg.Domain.Worldgen.WorldStyle.LowFantasy,
                        EmberCrpg.Domain.Worldgen.WorldGenre.Survival,
                        seed: fallback.FallbackWorldSeed,
                        targetPopulation: fallback.FallbackTargetPopulation,
                        regionCount: fallback.FallbackRegionCount,
                        factionCount: fallback.FallbackFactionCount,
                        historyYears: fallback.FallbackHistoryYears,
                        moodKeyword: fallback.FallbackMood,
                        playerCallingKeyword: fallback.FallbackCalling,
                        startLocationKeyword: fallback.FallbackStart);
                }
                return new EmberCrpg.Presentation.Ember.Adapters.DomainSimulationAdapter(world);
            }
            catch (System.Exception ex)
            {
                // Codex audit (seventh pass A-P2 #8): the previous catch was
                // silent — a real backend bootstrap failure produced no log
                // line, the host quietly fell through to the placeholder,
                // and Mami saw an empty HUD with no clue why. Surface the
                // exception so the failure is visible in the Editor console
                // and player.log. We still return null so the caller's honest
                // unavailable fallback runs (game stays bootable), but the
                // operator can now investigate the root cause.
                Debug.LogError("EmberWorldHost: domain adapter bootstrap failed; falling back to unavailable adapter. " + ex);
                return null;
            }
        }    }
}

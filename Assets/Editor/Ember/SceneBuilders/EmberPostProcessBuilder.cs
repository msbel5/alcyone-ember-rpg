// Why: AAA scenes need a global URP Volume with Bloom + ColorAdjustments +
// Vignette per mood. Recipes call AddVolume once with a profile preset; this
// builder hides the volume-component plumbing behind a small enum.
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Per-scene post-process mood presets matched to the AAA uplift PRD §2
    /// vision (warm forge, cold dungeon, candle tavern, amber shrine).
    /// </summary>
    public enum EmberMoodPreset
    {
        SmithingWarmGlow,
        TavernCandle,
        DungeonCold,
        ShrineAmber,
        FarmDay,
        MarketDay,
        NeutralIndoor,
        OracleVision,
    }

    /// <summary>
    /// Adds a global URP Volume with a small profile that pushes Bloom +
    /// ColorAdjustments + Vignette toward the requested mood. Designer-tunable
    /// via the volume's profile asset once the scene exists.
    /// </summary>
    public static class EmberPostProcessBuilder
    {
        public static GameObject AddVolume(string name, EmberMoodPreset preset)
        {
            var go = new GameObject(name);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = name + "_Profile";
            ConfigureProfile(profile, preset);
            volume.sharedProfile = profile;
            return go;
        }

        private static void ConfigureProfile(VolumeProfile profile, EmberMoodPreset preset)
        {
#if UNITY_RENDER_PIPELINE_UNIVERSAL
            var bloom = profile.Add<Bloom>(true);
            var color = profile.Add<ColorAdjustments>(true);
            var vignette = profile.Add<Vignette>(true);

            switch (preset)
            {
                case EmberMoodPreset.SmithingWarmGlow:
                    bloom.intensity.Override(0.85f);
                    bloom.threshold.Override(0.95f);
                    color.colorFilter.Override(new Color(1.05f, 0.92f, 0.78f));
                    color.saturation.Override(8f);
                    color.contrast.Override(6f);
                    vignette.intensity.Override(0.28f);
                    vignette.color.Override(new Color(0.10f, 0.05f, 0.02f));
                    break;
                case EmberMoodPreset.TavernCandle:
                    bloom.intensity.Override(0.65f);
                    bloom.threshold.Override(0.90f);
                    color.colorFilter.Override(new Color(1.06f, 0.96f, 0.82f));
                    color.saturation.Override(6f);
                    color.contrast.Override(4f);
                    vignette.intensity.Override(0.32f);
                    vignette.color.Override(new Color(0.05f, 0.02f, 0f));
                    break;
                case EmberMoodPreset.DungeonCold:
                    bloom.intensity.Override(0.35f);
                    bloom.threshold.Override(1.10f);
                    color.colorFilter.Override(new Color(0.78f, 0.90f, 1.05f));
                    color.saturation.Override(-12f);
                    color.contrast.Override(10f);
                    vignette.intensity.Override(0.45f);
                    vignette.color.Override(new Color(0f, 0.03f, 0.07f));
                    break;
                case EmberMoodPreset.ShrineAmber:
                    bloom.intensity.Override(1.15f);
                    bloom.threshold.Override(0.80f);
                    color.colorFilter.Override(new Color(1.10f, 0.95f, 0.70f));
                    color.saturation.Override(4f);
                    color.contrast.Override(2f);
                    vignette.intensity.Override(0.20f);
                    vignette.color.Override(new Color(0.10f, 0.06f, 0.02f));
                    break;
                case EmberMoodPreset.FarmDay:
                    bloom.intensity.Override(0.45f);
                    bloom.threshold.Override(1.05f);
                    color.colorFilter.Override(new Color(1.00f, 1.02f, 0.96f));
                    color.saturation.Override(12f);
                    color.contrast.Override(0f);
                    vignette.intensity.Override(0.12f);
                    break;
                case EmberMoodPreset.MarketDay:
                    bloom.intensity.Override(0.50f);
                    bloom.threshold.Override(1.00f);
                    color.colorFilter.Override(new Color(1.02f, 1.00f, 0.94f));
                    color.saturation.Override(8f);
                    color.contrast.Override(2f);
                    vignette.intensity.Override(0.18f);
                    break;
                case EmberMoodPreset.OracleVision:
                    bloom.intensity.Override(1.40f);
                    bloom.threshold.Override(0.65f);
                    color.colorFilter.Override(new Color(0.95f, 0.92f, 1.10f));
                    color.saturation.Override(-4f);
                    color.contrast.Override(8f);
                    vignette.intensity.Override(0.35f);
                    vignette.color.Override(new Color(0.05f, 0.02f, 0.10f));
                    break;
                default:
                    bloom.intensity.Override(0.40f);
                    bloom.threshold.Override(1.00f);
                    color.saturation.Override(0f);
                    vignette.intensity.Override(0.15f);
                    break;
            }
#endif
        }
    }
}

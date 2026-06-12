using UnityEngine;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    /// <summary>
    /// F32: app-level player settings — PlayerPrefs-backed (NOT world-save: volumes and mouse
    /// feel belong to the machine, not the save). Consumers read the statics live every frame
    /// or poll; the options screen writes + saves.
    /// </summary>
    public static class RuntimePlayerSettings
    {
        private const string MusicKey = "ember_music_vol";
        private const string SfxKey = "ember_sfx_vol";
        private const string SensitivityKey = "ember_mouse_sens";

        public static float MusicVolume = PlayerPrefs.GetFloat(MusicKey, 1f);
        public static float SfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
        /// <summary>Multiplier over the controller's authored sensitivity (0.2-3.0).</summary>
        public static float MouseSensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1f);

        public static void Save()
        {
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
            PlayerPrefs.SetFloat(SensitivityKey, MouseSensitivity);
            PlayerPrefs.Save();
        }
    }
}

namespace EmberCrpg.Presentation.Ember.WorldDirector
{
    /// <summary>
    /// Kind-aware NPC billboard density, set by WorldSceneDirector at realize time — an Inn must not draw a
    /// city crowd ("her yer aynı" playtest fix, population identity half). A static channel because the
    /// host creates the spawner AFTER the director runs; 0 means unset → the spawner keeps its own default.
    /// </summary>
    public static class RuntimeNpcDensity
    {
        public static int Cap;

        public static int CapOrDefault(int fallback) => Cap > 0 ? Cap : fallback;
    }
}

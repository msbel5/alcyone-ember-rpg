namespace EmberCrpg.Presentation.Ember.Worldgen
{
    /// <summary>Two-escape JSON writer for worldgen telemetry — kept minimal because the payloads
    /// are single-line diagnostic breadcrumbs, not full JSON documents. Three siblings used to
    /// hand-roll the same Replace pair; ONE arithmetic home lives here.</summary>
    internal static class WorldgenJson
    {
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EmberCrpg.Domain.Generation
{
    public sealed class ArchetypeEntry
    {
        public ArchetypeEntry(string archetypeId, string silhouettePath, int huePaletteMin, int huePaletteMax, float saturationMin, float saturationMax, float lightnessMin, float lightnessMax, string notes, bool requiresGeneration)
        {
            if (string.IsNullOrWhiteSpace(archetypeId)) throw new ArgumentException("Archetype id is required.", nameof(archetypeId));
            ArchetypeId = archetypeId.Trim();
            SilhouettePath = silhouettePath == null ? string.Empty : silhouettePath.Trim().Replace('\\', '/');
            HuePaletteMin = huePaletteMin;
            HuePaletteMax = huePaletteMax;
            SaturationMin = saturationMin;
            SaturationMax = saturationMax;
            LightnessMin = lightnessMin;
            LightnessMax = lightnessMax;
            Notes = notes ?? string.Empty;
            RequiresGeneration = requiresGeneration;
        }

        public string ArchetypeId { get; }
        public string SilhouettePath { get; }
        public int HuePaletteMin { get; }
        public int HuePaletteMax { get; }
        public float SaturationMin { get; }
        public float SaturationMax { get; }
        public float LightnessMin { get; }
        public float LightnessMax { get; }
        public string Notes { get; }
        public bool RequiresGeneration { get; }
    }

    public sealed class GenericNpcBaseManifest
    {
        public GenericNpcBaseManifest(IEnumerable<ArchetypeEntry> archetypes)
        {
            Archetypes = new ReadOnlyCollection<ArchetypeEntry>(new List<ArchetypeEntry>(archetypes ?? throw new ArgumentNullException(nameof(archetypes))));
        }

        public IReadOnlyList<ArchetypeEntry> Archetypes { get; }

        public bool Contains(string archetypeId)
        {
            for (int i = 0; i < Archetypes.Count; i++)
                if (string.Equals(Archetypes[i].ArchetypeId, archetypeId, StringComparison.Ordinal)) return true;
            return false;
        }

        public static GenericNpcBaseManifest CreateDefault()
        {
            return new GenericNpcBaseManifest(new[]
            {
                Row("humanoid_male", "Assets/Art/BodySilhouettes/humanoid_male.png", 20, 48, "male humanoid silhouette"),
                Row("humanoid_female", "Assets/Art/BodySilhouettes/humanoid_female.png", 20, 48, "female humanoid silhouette"),
                Row("beast_quadruped", "Assets/Art/BodySilhouettes/beast_quadruped.png", 55, 110, "quadruped beast silhouette"),
                Row("undead_humanoid", "Assets/Art/BodySilhouettes/undead_humanoid.png", 180, 230, "undead humanoid silhouette"),
                Row("construct", "Assets/Art/BodySilhouettes/construct.png", 25, 60, "construct silhouette"),
                Row("aberration", "Assets/Art/BodySilhouettes/aberration.png", 260, 315, "aberration silhouette"),
            });
        }

        private static ArchetypeEntry Row(string id, string path, int minHue, int maxHue, string notes)
        {
            return new ArchetypeEntry(id, path, minHue, maxHue, 0.35f, 0.75f, 0.25f, 0.70f, notes, false);
        }
    }
}

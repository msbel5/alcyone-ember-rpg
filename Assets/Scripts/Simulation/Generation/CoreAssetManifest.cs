using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Generation;

namespace EmberCrpg.Simulation.Generation
{
    public sealed class CoreAssetManifest
    {
        public CoreAssetManifest(IEnumerable<ManifestEntry> entries)
        {
            Entries = new ReadOnlyCollection<ManifestEntry>(new List<ManifestEntry>(entries ?? throw new ArgumentNullException(nameof(entries))));
        }

        public IReadOnlyList<ManifestEntry> Entries { get; }

        public static CoreAssetManifest CreateDefault()
        {
            var entries = new List<ManifestEntry>();
            // Critical visual assets first so the menu/loading screens get backdrops even if later
            // generation stalls or hits the timeout. Order: splash, logos, then UI icons, items, spells.
            // SD 1.5 LCM requires width/height divisible by 64. 720 was not; ONNX inference threw.
            // 768x512 (3:2) generates reliably and ScaleAndCrop fills 16:9 backdrops without artefacts.
            entries.Add(new ManifestEntry("splash_background", "splash", "Assets/Generated/Core/splash_background.png", "splash_background", 768, 512, true, 300, "sd15-lcm"));
            entries.Add(new ManifestEntry("logo_full", "logo", "Assets/Generated/Core/logo_full.png", "logo_full", 256, 128, true, 300, "sd15-lcm"));
            entries.Add(new ManifestEntry("logo_compact", "logo", "Assets/Generated/Core/logo_compact.png", "logo_compact", 128, 128, true, 300, "sd15-lcm"));
            AddMany(entries, "ui", 64, 64, true, "new_game", "settings", "dice", "skill", "attack", "defend", "equip", "drop", "inventory", "map", "journal", "magic", "rest", "continue", "error");
            entries.Add(new ManifestEntry("font_body", "font", "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", "", 1, 1, false));
            entries.Add(new ManifestEntry("font_heading", "font", "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", "", 1, 1, false));
            entries.Add(new ManifestEntry("font_mono", "font", "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", "", 1, 1, false));
            AddSilhouette(entries, "humanoid_male");
            AddSilhouette(entries, "humanoid_female");
            AddSilhouette(entries, "beast_quadruped");
            AddSilhouette(entries, "undead_humanoid");
            AddSilhouette(entries, "construct");
            AddSilhouette(entries, "aberration");
            AddMany(entries, "item", 128, 128, true, "sword", "bow", "staff", "potion", "scroll", "key", "ring", "helm", "boots", "shield");
            AddMany(entries, "spell", 96, 96, true, "sleep", "heal", "fire", "ice", "shield", "lightning");
            AddSound(entries, "ui_click"); AddSound(entries, "ui_hover"); AddSound(entries, "dice_roll"); AddSound(entries, "level_up"); AddSound(entries, "error");
            // logos + splash moved to top of list (above) so menus get visuals before later stalls.
            // splash uses sd15-lcm @ 1280x720 to skip the cuDNN 9 dependency; re-up to sdxl-turbo
            // when cudnn64_9.dll is part of the standard machine setup.
            return new CoreAssetManifest(entries);
        }

        private static void AddMany(List<ManifestEntry> entries, string category, int width, int height, bool generated, params string[] ids)
        {
            foreach (var id in ids)
            {
                var entryId = category == "ui" ? id : category + "_" + id;
                var path = "Assets/Generated/Core/" + entryId + ".png";
                entries.Add(new ManifestEntry(entryId, category, path, entryId, width, height, generated, 300, "sd15-lcm"));
            }
        }

        private static void AddSilhouette(List<ManifestEntry> entries, string id)
        {
            entries.Add(new ManifestEntry("silhouette_" + id, "silhouette", "Assets/Art/BodySilhouettes/" + id + ".png", "", 256, 512, false));
        }

        private static void AddSound(List<ManifestEntry> entries, string id)
        {
            entries.Add(new ManifestEntry("sfx_" + id, "sound", "Assets/Audio/Placeholders/" + id + ".wav", "", 1, 1, false));
        }
    }
}

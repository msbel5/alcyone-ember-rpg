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
            // Keep logo assets on 512 while UI/item/spell icon entries now target SDXL-Turbo at 1024².
            // The display layer downscales to slot size; larger generation preserves usable detail.
            entries.Add(new ManifestEntry("logo_full", "logo", "Assets/Generated/Core/logo_full.png", "logo_full", 512, 512, true, 300, "sd15-lcm"));
            entries.Add(new ManifestEntry("logo_compact", "logo", "Assets/Generated/Core/logo_compact.png", "logo_compact", 512, 512, true, 300, "sd15-lcm"));
            AddMany(entries, "ui", 1024, 1024, true, "sdxl-turbo", "new_game", "settings", "dice", "skill", "attack", "defend", "equip", "drop", "inventory", "map", "journal", "magic", "rest", "continue", "error");
            entries.Add(new ManifestEntry("font_body", "font", "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", "", 1, 1, false));
            entries.Add(new ManifestEntry("font_heading", "font", "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", "", 1, 1, false));
            entries.Add(new ManifestEntry("font_mono", "font", "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", "", 1, 1, false));
            AddSilhouette(entries, "humanoid_male");
            AddSilhouette(entries, "humanoid_female");
            AddSilhouette(entries, "beast_quadruped");
            AddSilhouette(entries, "undead_humanoid");
            AddSilhouette(entries, "construct");
            AddSilhouette(entries, "aberration");
            AddMany(entries, "item", 1024, 1024, true, "sdxl-turbo", "sword", "bow", "staff", "potion", "scroll", "key", "ring", "helm", "boots", "shield");
            AddMany(entries, "spell", 1024, 1024, true, "sdxl-turbo", "sleep", "heal", "fire", "ice", "shield", "lightning");
            AddSound(entries, "ui_click"); AddSound(entries, "ui_hover"); AddSound(entries, "dice_roll"); AddSound(entries, "level_up"); AddSound(entries, "error");
            // logos + splash moved to top of list (above) so menus get visuals before later stalls.
            // splash uses sd15-lcm @ 1280x720 to skip the cuDNN 9 dependency; re-up to sdxl-turbo
            // when cudnn64_9.dll is part of the standard machine setup.
            // Environment floor textures (one per scene whose terrain still uses the fallback).
            // Generated during loading, painted onto scene terrain by SceneEnvironmentDresser.
            entries.Add(new ManifestEntry("env_colonyneeds", "environment", "Assets/Generated/Core/env_colonyneeds.png", "env_colonyneeds", 512, 512, true, 300, "sd15-lcm"));
            entries.Add(new ManifestEntry("env_combatdungeon", "environment", "Assets/Generated/Core/env_combatdungeon.png", "env_combatdungeon", 512, 512, true, 300, "sd15-lcm"));
            entries.Add(new ManifestEntry("env_oracleshrine", "environment", "Assets/Generated/Core/env_oracleshrine.png", "env_oracleshrine", 512, 512, true, 300, "sd15-lcm"));
            entries.Add(new ManifestEntry("env_ritualhall", "environment", "Assets/Generated/Core/env_ritualhall.png", "env_ritualhall", 512, 512, true, 300, "sd15-lcm"));
            entries.Add(new ManifestEntry("env_seasonfarm", "environment", "Assets/Generated/Core/env_seasonfarm.png", "env_seasonfarm", 512, 512, true, 300, "sd15-lcm"));
            entries.Add(new ManifestEntry("env_trademarket", "environment", "Assets/Generated/Core/env_trademarket.png", "env_trademarket", 512, 512, true, 300, "sd15-lcm"));
            entries.Add(new ManifestEntry("env_showroomoverview", "environment", "Assets/Generated/Core/env_showroomoverview.png", "env_showroomoverview", 512, 512, true, 300, "sd15-lcm"));
            entries.Add(new ManifestEntry("env_tavernflavour", "environment", "Assets/Generated/Core/env_tavernflavour.png", "env_tavernflavour", 512, 512, true, 300, "sd15-lcm"));

            return new CoreAssetManifest(entries);
        }

        private static void AddMany(List<ManifestEntry> entries, string category, int width, int height, bool generated, string modelHint, params string[] ids)
        {
            foreach (var id in ids)
            {
                var entryId = category == "ui" ? id : category + "_" + id;
                var path = "Assets/Generated/Core/" + entryId + ".png";
                entries.Add(new ManifestEntry(entryId, category, path, entryId, width, height, generated, 300, modelHint));
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

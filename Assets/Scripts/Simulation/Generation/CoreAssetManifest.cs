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
            // SDXL-Turbo is a 512-NATIVE model. Forge-proof (2026-06-01) showed it TILES the subject at
            // 1024 (a single die became a 40-die grid) and even cube-only prompts fill the extra 1024
            // resolution with dot-noise; the clean SINGLE object only appears at 512. So object icons
            // generate at Turbo's native 512² and the display layer downscales to slot size. Logos/splash/
            // env keep their own model+size below.
            entries.Add(new ManifestEntry("logo_full", "logo", "Assets/Generated/Core/logo_full.png", "logo_full", 512, 512, true, 300, "sd15-lcm"));
            entries.Add(new ManifestEntry("logo_compact", "logo", "Assets/Generated/Core/logo_compact.png", "logo_compact", 512, 512, true, 300, "sd15-lcm"));
            // The AI Dungeon Master (the Oracle / narrator) had no face. Give it one, generated right after the
            // logo so the menu shows it early. Its own "portrait" category so the Options Generated-Assets panel
            // lists it as a distinct group, beside (not buried under) the logos.
            entries.Add(new ManifestEntry("dm_portrait", "portrait", "Assets/Generated/Core/dm_portrait.png", "dm_portrait", 512, 512, true, 300, "sd15-lcm"));
            AddMany(entries, "ui", 512, 512, true, "sdxl-turbo", "new_game", "settings", "dice", "skill", "attack", "defend", "equip", "drop", "inventory", "map", "journal", "magic", "rest", "continue", "error");
            entries.Add(new ManifestEntry("font_body", "font", "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", "", 1, 1, false));
            entries.Add(new ManifestEntry("font_heading", "font", "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", "", 1, 1, false));
            entries.Add(new ManifestEntry("font_mono", "font", "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset", "", 1, 1, false));
            AddSilhouette(entries, "humanoid_male");
            AddSilhouette(entries, "humanoid_female");
            AddSilhouette(entries, "beast_quadruped");
            AddSilhouette(entries, "undead_humanoid");
            AddSilhouette(entries, "construct");
            AddSilhouette(entries, "aberration");
            AddMany(entries, "item", 512, 512, true, "sdxl-turbo", "sword", "bow", "staff", "potion", "scroll", "key", "ring", "helm", "boots", "shield");
            AddMany(entries, "spell", 512, 512, true, "sdxl-turbo", "sleep", "heal", "fire", "ice", "shield", "lightning");
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

            // Architectural shell pieces so a scene is dressed with more than just its floor.
            // Walls/roofs are tileable surfaces (sd15-lcm, same as the floors above); doors/windows are
            // centered fixtures (sdxl-turbo at native 512, same as item icons). Each is its own category so
            // the Options "Generated Assets" panel lists wall/roof/door/window as distinct, regenerable groups.
            // AddMany prefixes non-"ui" categories ("wall" + "colonyneeds" => "wall_colonyneeds"), so pass
            // BARE ids here; the StaticPromptCatalog keys (wall_colonyneeds, roof_thatch, ...) match the result.
            AddMany(entries, "wall", 512, 512, true, "sd15-lcm", "colonyneeds", "combatdungeon", "oracleshrine", "ritualhall", "seasonfarm", "trademarket", "showroomoverview", "tavernflavour");
            AddMany(entries, "roof", 512, 512, true, "sd15-lcm", "thatch", "clay_tile", "slate", "timber");
            AddMany(entries, "door", 512, 512, true, "sdxl-turbo", "oak", "iron", "stone_arch", "temple", "cellar");
            AddMany(entries, "window", 512, 512, true, "sdxl-turbo", "shutter", "leaded", "arched", "oculus", "barred");

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

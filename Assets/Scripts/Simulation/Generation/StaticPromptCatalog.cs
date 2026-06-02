using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Simulation.Generation
{
    public sealed class StaticPromptCatalog
    {
        public const string EmberStyleHeader = "dark-fantasy ember-warm palette, painterly low-saturation, transparent background, single subject centered";
        // Floors/walls are tileable surfaces, NOT centered icons: this header replaces the icon
        // "transparent background, single subject centered" with a seamless-fill directive.
        public const string EmberFloorHeader = "dark-fantasy ember-warm palette, painterly low-saturation, seamless tileable texture, top-down orthographic surface filling the entire frame edge to edge, no central subject";
        // Walls are tileable too, but seen straight-on (vertical) rather than top-down.
        public const string EmberWallHeader = "dark-fantasy ember-warm palette, painterly low-saturation, seamless tileable texture, front-facing orthographic wall surface filling the entire frame edge to edge, no central subject";
        public const string EmberNegativeFooter = "no text, no watermark, no border, no UI elements, no signature, no logo";
        public const string EmberGenerationNegative = EmberNegativeFooter + ", no multiple objects, no group, no duplicate subject, no scattered objects, no collage";

        private readonly IReadOnlyDictionary<string, string> _prompts;

        public StaticPromptCatalog(IReadOnlyDictionary<string, string> prompts)
        {
            _prompts = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(prompts ?? throw new ArgumentNullException(nameof(prompts)), StringComparer.Ordinal));
        }

        public bool TryGetPrompt(string key, out string prompt)
        {
            if (string.IsNullOrWhiteSpace(key)) { prompt = string.Empty; return false; }
            return _prompts.TryGetValue(key, out prompt);
        }

        public static StaticPromptCatalog CreateDefault()
        {
            var prompts = new Dictionary<string, string>(StringComparer.Ordinal);
            var geometricCatalog = new GeometricPromptCatalog();
            Add(prompts, "new_game", "a small ember-lit iron door icon, ash dust at the threshold");
            Add(prompts, "settings", "a blackened brass gear icon with soot in the teeth");
            AddGeometric(prompts, geometricCatalog, "dice", "dice");
            Add(prompts, "skill", "a stitched leather skill ledger with a copper clasp");
            Add(prompts, "attack", "a chipped iron axe head angled forward");
            Add(prompts, "defend", "a dark oak shield rimmed with warm iron");
            Add(prompts, "equip", "a gloved hand fastening a worn belt buckle");
            Add(prompts, "drop", "a cracked pouch spilling a single brass coin");
            Add(prompts, "inventory", "a compact travel satchel with labeled pockets");
            Add(prompts, "map", "a folded vellum map with ember-red route ink");
            Add(prompts, "journal", "a soot-stained field journal with a red thread bookmark");
            Add(prompts, "magic", "a small ember sigil hovering over dark slate");
            Add(prompts, "rest", "a bedroll beside a low coal fire");
            Add(prompts, "continue", "a forward-pointing wrought iron arrow");
            Add(prompts, "error", "a cracked warning seal glowing dull red");

            // Environment floor textures: tileable surfaces (EmberFloorHeader), NOT centered icons.
            prompts["env_colonyneeds"] = EmberFloorHeader + ", worn flagstone colony floor, soot-darkened warm stone, ash and cinders in the cracks, torchlit, " + EmberNegativeFooter;
            prompts["env_combatdungeon"] = EmberFloorHeader + ", cold damp dungeon floor, cracked dark flagstones, faint ember glow in the gaps, grim, " + EmberNegativeFooter;
            prompts["env_oracleshrine"] = EmberFloorHeader + ", sacred shrine floor, polished dark marble veined with ember-gold inlay, mystical sheen, " + EmberNegativeFooter;
            prompts["env_ritualhall"] = EmberFloorHeader + ", ritual hall floor, black obsidian slabs etched with faintly glowing ember runes, " + EmberNegativeFooter;
            prompts["env_seasonfarm"] = EmberFloorHeader + ", tilled farm soil and dry stubble grass, warm dusk earth, scattered ash, " + EmberNegativeFooter;
            prompts["env_trademarket"] = EmberFloorHeader + ", market square cobblestone, worn warm-grey pavers, dust and straw, lantern-lit, " + EmberNegativeFooter;
            prompts["env_showroomoverview"] = EmberFloorHeader + ", showroom floor, smooth dark stone with ember-gold seam trim, polished sheen, " + EmberNegativeFooter;
            prompts["env_tavernflavour"] = EmberFloorHeader + ", tavern wooden plank floor, warm aged oak timber, hearth-glow, knot details, " + EmberNegativeFooter;

            // Wall surfaces, one per scene so a scene's wall matches its floor (EmberWallHeader, vertical tileable).
            prompts["wall_colonyneeds"] = EmberWallHeader + ", soot-stained colony stone wall, rough mortared blocks, torch-scorch marks, " + EmberNegativeFooter;
            prompts["wall_combatdungeon"] = EmberWallHeader + ", cold damp dungeon wall, cracked dark stone blocks, moss and faint ember glow in the seams, " + EmberNegativeFooter;
            prompts["wall_oracleshrine"] = EmberWallHeader + ", sacred shrine wall, dark marble panels with ember-gold inlay tracery, " + EmberNegativeFooter;
            prompts["wall_ritualhall"] = EmberWallHeader + ", ritual hall wall, black obsidian panels etched with faintly glowing ember runes, " + EmberNegativeFooter;
            prompts["wall_seasonfarm"] = EmberWallHeader + ", rustic farm wall, weathered timber boards and wattle, dried straw, warm dusk tone, " + EmberNegativeFooter;
            prompts["wall_trademarket"] = EmberWallHeader + ", market plaster wall over stone, warm ochre render, lantern soot, " + EmberNegativeFooter;
            prompts["wall_showroomoverview"] = EmberWallHeader + ", showroom wall, smooth dark stone with ember-gold seam trim, polished sheen, " + EmberNegativeFooter;
            prompts["wall_tavernflavour"] = EmberWallHeader + ", tavern wall, warm aged oak panelling and plaster, hearth-glow, " + EmberNegativeFooter;

            // Roof material swatches (EmberFloorHeader tileable surface, read as an overhead material tile).
            prompts["roof_thatch"] = EmberFloorHeader + ", thatched straw roof bundles, warm dry golden-brown, weathered, " + EmberNegativeFooter;
            prompts["roof_clay_tile"] = EmberFloorHeader + ", overlapping terracotta clay roof tiles, warm rust-red, " + EmberNegativeFooter;
            prompts["roof_slate"] = EmberFloorHeader + ", layered dark slate roof shingles, cool grey with ember-warm edge light, " + EmberNegativeFooter;
            prompts["roof_timber"] = EmberFloorHeader + ", split timber shake roof, aged grey-brown wood, " + EmberNegativeFooter;
            AddGeometric(prompts, geometricCatalog, "item_sword", "sword");
            AddGeometric(prompts, geometricCatalog, "item_bow", "bow");
            AddGeometric(prompts, geometricCatalog, "item_staff", "staff");
            AddGeometric(prompts, geometricCatalog, "item_potion", "potion");
            AddGeometric(prompts, geometricCatalog, "item_scroll", "scroll");
            AddGeometric(prompts, geometricCatalog, "item_key", "key");
            AddGeometric(prompts, geometricCatalog, "item_ring", "ring");
            AddGeometric(prompts, geometricCatalog, "item_helm", "helm");
            AddGeometric(prompts, geometricCatalog, "item_boots", "boots");
            AddGeometric(prompts, geometricCatalog, "item_shield", "shield");
            AddGeometric(prompts, geometricCatalog, "spell_sleep", "sleep");
            AddGeometric(prompts, geometricCatalog, "spell_heal", "heal");
            AddGeometric(prompts, geometricCatalog, "spell_fire", "fire");
            AddGeometric(prompts, geometricCatalog, "spell_ice", "ice");
            AddGeometric(prompts, geometricCatalog, "spell_shield", "shield");
            AddGeometric(prompts, geometricCatalog, "spell_lightning", "lightning");
            Add(prompts, "logo_full", "the word Ember implied by a forged crest and coal-lit crown silhouette");
            Add(prompts, "logo_compact", "a compact ember crown mark cut from blackened brass");
            // The AI Dungeon Master / Oracle — the unseen narrator's face beside the Ember logo. EmberStyleHeader
            // already enforces a centered single subject on a transparent background, so this reads as a bust.
            Add(prompts, "dm_portrait", "a hooded loremaster oracle, the unseen dungeon master, weathered ember-lit face half in shadow beneath a deep cowl, faintly glowing coal-orange eyes, ancient keeper of the world's fate, solemn painterly character portrait bust");

            // Doors + windows: centered fixtures (EmberStyleHeader single subject), like item icons.
            Add(prompts, "door_oak", "a heavy oak plank door bound with iron studs, dark-fantasy fixture, straight-on front view");
            Add(prompts, "door_iron", "a riveted iron-banded dungeon door, cold dark metal, straight-on front view");
            Add(prompts, "door_stone_arch", "an arched stone doorway with a carved keystone, weathered, straight-on front view");
            Add(prompts, "door_temple", "an ornate temple door of blackwood and ember-gold filigree, straight-on front view");
            Add(prompts, "door_cellar", "a slanted timber cellar hatch with an iron ring handle, straight-on front view");
            Add(prompts, "window_shutter", "a wooden shuttered window with warm hearth light behind it, straight-on front view");
            Add(prompts, "window_leaded", "a leaded diamond-pane glass window with a faint ember glow, straight-on front view");
            Add(prompts, "window_arched", "a tall arched stone window with a timber frame, straight-on front view");
            Add(prompts, "window_oculus", "a round oculus window with iron tracery, straight-on front view");
            Add(prompts, "window_barred", "a small barred prison window set in dark stone, straight-on front view");
            Add(prompts, "splash_background", "a first-person dark fantasy road toward a distant ember-lit citadel at dusk");
            return new StaticPromptCatalog(prompts);
        }

        private static void Add(Dictionary<string, string> prompts, string key, string body)
        {
            prompts[key] = EmberStyleHeader + ", " + body + ", " + EmberNegativeFooter;
        }

        private static void AddGeometric(
            Dictionary<string, string> prompts,
            GeometricPromptCatalog geometricCatalog,
            string key,
            string objectName)
        {
            geometricCatalog.TryGet(objectName, out var positive, out _);
            Add(prompts, key, positive);
        }
    }
}

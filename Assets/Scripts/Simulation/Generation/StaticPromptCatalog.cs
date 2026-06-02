using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Simulation.Generation
{
    public sealed class StaticPromptCatalog
    {
        public const string EmberStyleHeader = "dark-fantasy ember-warm palette, painterly low-saturation, transparent background, single subject centered";
        public const string EmberCharacterPortraitHeader = EmberStyleHeader + ", exactly one person, centered character bust, no second person, no crowd";
        public const string EmberNpcSpriteHeader = EmberStyleHeader + ", exactly one person, full-body character sprite, plain transparent or neutral background, consistent ember-lit palette, no second person, no crowd";
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
            // The AI Dungeon Master / Oracle — explicit one-person wording prevents two-face portrait drift.
            AddPortrait(prompts, "dm_portrait", "a hooded loremaster oracle, the unseen dungeon master, weathered ember-lit face half in shadow beneath a deep cowl, faintly glowing coal-orange eyes, ancient keeper of the world's fate, solemn painterly character portrait");
            AddPortrait(prompts, "portrait_npc_blacksmith", "a soot-marked village blacksmith, leather apron, ember-lit face, steady artisan gaze, dark fantasy NPC portrait");
            AddPortrait(prompts, "portrait_npc_merchant", "a shrewd travelling merchant, layered trade cloak, warm lantern light, observant expression, dark fantasy NPC portrait");
            AddPortrait(prompts, "portrait_npc_innkeeper", "a weathered innkeeper, simple tavern clothes, hearth-warm face, welcoming but wary expression, dark fantasy NPC portrait");
            AddPortrait(prompts, "portrait_npc_warrior", "a rugged outlaw warrior, worn cloak and leather armor, scarred ember-lit face, guarded expression, dark fantasy NPC portrait");
            AddPortrait(prompts, "portrait_npc_knight", "a stern city guard knight, dark mail collar, ember glint on cheek and helm rim, disciplined expression, dark fantasy NPC portrait");
            AddPortrait(prompts, "portrait_npc_sage", "a quiet scholar priest, ash-grey robe, old book clasp at the collar, thoughtful ember-lit face, dark fantasy NPC portrait");
            AddNpcRolePrompts(prompts);

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

        private static void AddPortrait(Dictionary<string, string> prompts, string key, string body)
        {
            prompts[key] = EmberCharacterPortraitHeader + ", " + body + ", no duplicate face, no twin, " + EmberNegativeFooter;
        }

        private static void AddNpcRolePrompts(Dictionary<string, string> prompts)
        {
            AddNpcSprite(prompts, "farmer", "a weathered farmer with a seed satchel, rolled sleeves, mud-dark boots, sickle at the belt");
            AddNpcSprite(prompts, "merchant", "a travelling merchant in layered trade cloak, coin pouch, small ledger, alert bargaining stance");
            AddNpcSprite(prompts, "guard", "a town guard in practical mail shirt and kettle helm, spear upright, watchful stance");
            AddNpcSprite(prompts, "noble", "a minor noble in dark velvet court coat, ember-gold trim, signet ring, reserved posture");
            AddNpcSprite(prompts, "priest", "a shrine priest in ash-grey vestments, simple holy charm, hands folded around a coal-lit reliquary");
            AddNpcSprite(prompts, "scholar", "a field scholar in worn robe and satchel, scroll case, ink-stained fingers, observant expression");
            AddNpcSprite(prompts, "artisan", "a village artisan with tool belt, dyed work apron, careful hands, crafted trinkets at the waist");
            AddNpcSprite(prompts, "outlaw", "a hard-eyed outlaw in patched leather armor, hooded cloak, hidden knife, wary stance");
            AddNpcSprite(prompts, "blacksmith", "a soot-marked blacksmith in forge apron, heavy gloves, hammer resting at one side");
            AddNpcSprite(prompts, "innkeeper", "a hearth-warm innkeeper in simple tavern clothes, towel over one shoulder, guarded welcome");
            AddNpcSprite(prompts, "healer", "a village healer with herb satchel, linen wraps, small tonic bottles, calm focused posture");
            AddNpcSprite(prompts, "mage", "a wandering mage in layered ember-trimmed robes, staff crystal glowing low, composed stance");
            AddNpcSprite(prompts, "knight", "a stern knight in dark plate and mail, tabard scorched at the hem, sword held point-down");
            AddNpcSprite(prompts, "bard", "a travelling bard with weathered lute, bright scarf, worn boots, half-smile under lantern light");
            AddNpcSprite(prompts, "sage", "an elderly sage in ash-colored robe, book clasp, long sleeves, quiet knowing gaze");
            AddNpcSprite(prompts, "rogue", "a nimble rogue in fitted dark leather, lockpicks at the belt, cloak pulled close");
            AddNpcSprite(prompts, "beggar", "a hungry beggar in patched rags and frayed cloak, empty bowl, tired guarded posture");
            AddNpcSprite(prompts, "bandit", "a rough bandit in mismatched leathers, scarf mask loose at the neck, notched axe at the hip");
        }

        private static void AddNpcSprite(Dictionary<string, string> prompts, string role, string body)
        {
            prompts["npc_" + role] = EmberNpcSpriteHeader + ", " + body + ", no duplicate face, no twin, " + EmberNegativeFooter;
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

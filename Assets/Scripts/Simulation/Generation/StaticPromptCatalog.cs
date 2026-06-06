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
        // POSITIVE ONLY. SDXL-Turbo here runs at guidance 0 with no negative conditioning, so every word
        // is a positive token. Avoid "sprite"/"game sprite"/"character sheet" vocabulary (those correlate
        // with turnaround/design SHEETS in SDXL's training data) and avoid all "no X" phrases (CLIP reads
        // "no second person" as the tokens "second person"). Pure constructive single-figure framing only.
        public const string EmberNpcSpriteHeader = EmberStyleHeader + ", retro dark-fantasy CRPG, a single lone individual, one person standing alone, full body shown head to boots, facing forward, centered by itself, plain neutral studio backdrop, even soft lighting, consistent ember-lit palette";
        public const string EmberFloorHeader = "dark-fantasy retro fantasy material sample, seamless tileable floor material swatch, albedo only, flat diffuse color, top-down orthographic, evenly lit, no cast shadows, no baked lighting, no central subject";
        public const string EmberWallHeader = "dark-fantasy retro fantasy material sample, seamless tileable wall material swatch, albedo only, flat diffuse color, fronto-parallel orthographic, evenly lit, no cast shadows, no baked lighting, no central subject";
        public const string EmberNegativeFooter = "no text, no watermark, no border, no UI elements, no signature, no logo";
        public const string EmberGenerationNegative = EmberNegativeFooter + ", no multiple objects, no group, no duplicate subject, no scattered objects, no collage, no character sheet, no turnaround, no model sheet, no design sheet, no reference sheet, no multiple views, no triptych, no diptych, no lineup, no duplicate person, no second character, no mirrored character, no twin, no extra body, no extra limbs, no extra head, no border, no frame, no caption";
        private const string EmberNpcSpritePromptTail = "no character sheet, no turnaround, no model sheet, no design sheet, no reference sheet, no multiple views, no triptych, no diptych, no lineup, no duplicate person, no second character, no mirrored character, no twin, no extra body, no extra limbs, no extra head, no floating prop, no flames, no aura, no magic trail, no environment scene";
        private const string EmberTexturePromptTail = "no room, no fireplace, no hearth, no torch, no candle, no window, no door, no furniture, no shadow, no glow, no reflection, no highlight, no specular, no perspective, no horizon, no scene, no corner, no debris pile, no object, no prop, no character";

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
            prompts["env_colonyneeds"] = EmberFloorHeader + ", worn flagstone colony floor material, soot-darkened warm stone, ash and cinders in the cracks, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["env_combatdungeon"] = EmberFloorHeader + ", rough cracked dungeon flagstone floor material, cold dark stone joints, damp mineral staining, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["env_oracleshrine"] = EmberFloorHeader + ", sacred shrine floor material, dark marble slabs with ember-gold inlay veins, restrained albedo contrast, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["env_ritualhall"] = EmberFloorHeader + ", ritual hall floor material, black obsidian slabs with ember-rune carvings painted into the albedo, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["env_seasonfarm"] = EmberFloorHeader + ", farmyard ground material, packed soil with dry stubble and scattered ash, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["env_trademarket"] = EmberFloorHeader + ", market square cobblestone floor material, worn warm-grey pavers with dust in the joints, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["env_showroomoverview"] = EmberFloorHeader + ", showroom stone floor material, smooth dark slabs with ember-gold seam inlay, restrained albedo-only finish, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["env_tavernflavour"] = EmberFloorHeader + ", rough wooden plank floor material, warm aged oak boards, knot grain, subtle wear, no hearth glow, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;

            // Wall surfaces, one per scene so a scene's wall matches its floor (EmberWallHeader, vertical tileable).
            prompts["wall_colonyneeds"] = EmberWallHeader + ", soot-stained colony stone wall material, rough mortared blocks, darkened scorch residue painted into the plaster, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["wall_combatdungeon"] = EmberWallHeader + ", cracked dark dungeon wall material, cold damp stone blocks with moss staining, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["wall_oracleshrine"] = EmberWallHeader + ", sacred shrine wall material, dark marble panels with ember-gold inlay tracery, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["wall_ritualhall"] = EmberWallHeader + ", ritual hall wall material, black obsidian panels etched with ember-rune tracery in the albedo, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["wall_seasonfarm"] = EmberWallHeader + ", rustic farm wall material, weathered timber boards with wattle and dried straw weave, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["wall_trademarket"] = EmberWallHeader + ", market wall material, warm ochre plaster over stone with soot-dark mortar wear, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["wall_showroomoverview"] = EmberWallHeader + ", showroom wall material, smooth dark stone blocks with ember-gold seam trim, restrained albedo-only finish, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["wall_tavernflavour"] = EmberWallHeader + ", old plaster tavern wall material with aged oak panel trim, flat diffuse albedo, no hearth glow, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;

            // Roof material swatches (EmberFloorHeader tileable surface, read as an overhead material tile).
            prompts["roof_thatch"] = EmberFloorHeader + ", thatched straw roof material, warm dry golden-brown bundles, weathered fibre texture, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["roof_clay_tile"] = EmberFloorHeader + ", terracotta roof tile material, overlapping clay courses, warm rust-red albedo, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["roof_slate"] = EmberFloorHeader + ", dark slate roof material, layered cool grey shingles, restrained albedo variation, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
            prompts["roof_timber"] = EmberFloorHeader + ", timber shake roof material, split aged grey-brown wood shingles, " + EmberTexturePromptTail + ", " + EmberNegativeFooter;
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
            // Keep the POSITIVE purely constructive. The suppression terms (EmberNpcSpritePromptTail /
            // EmberNegativeFooter) are "no X" phrases that, baked into this positive string on a guidance-0
            // Turbo pipeline, were INJECTING "character sheet / twin / extra body" and producing the
            // multi-figure sheets. They belong in the negative prompt (EmberGenerationNegative), which
            // CFG-capable pipelines consume and Turbo simply ignores.
            prompts["npc_" + role] = EmberNpcSpriteHeader + ", " + body;
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

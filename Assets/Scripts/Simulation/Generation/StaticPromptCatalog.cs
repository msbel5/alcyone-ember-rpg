using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EmberCrpg.Simulation.Generation
{
    public sealed class StaticPromptCatalog
    {
        public const string EmberStyleHeader = "dark-fantasy ember-warm palette, painterly low-saturation, transparent background, single subject centered";
        // Floors/walls are tileable surfaces, NOT centered icons: this header replaces the icon
        // "transparent background, single subject centered" with a seamless-fill directive.
        public const string EmberFloorHeader = "dark-fantasy ember-warm palette, painterly low-saturation, seamless tileable texture, top-down orthographic surface filling the entire frame edge to edge, no central subject";
        public const string EmberNegativeFooter = "no text, no watermark, no border, no UI elements, no signature, no logo";

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
            Add(prompts, "new_game", "a small ember-lit iron door icon, ash dust at the threshold");
            Add(prompts, "settings", "a blackened brass gear icon with soot in the teeth");
            Add(prompts, "dice", "four bone dice marked with ember pips");
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
            Add(prompts, "item_sword", "a wrought-iron longsword with rune-etched fuller, oxblood leather grip");
            Add(prompts, "item_bow", "a recurved yew bow strung with dark cord and brass nocks");
            Add(prompts, "item_staff", "a charred ashwood staff capped with warm iron");
            Add(prompts, "item_potion", "a squat glass potion vial filled with coal-red liquid");
            Add(prompts, "item_scroll", "a sealed parchment scroll tied with black thread");
            Add(prompts, "item_key", "an old iron key with a crown-shaped bit");
            Add(prompts, "item_ring", "a tarnished ring set with a dull ember stone");
            Add(prompts, "item_helm", "a dented sallet helm with soot along the visor");
            Add(prompts, "item_boots", "travel boots with ash-caked soles and bronze buckles");
            Add(prompts, "item_shield", "a kite shield painted with a fading coal sigil");
            Add(prompts, "spell_sleep", "a blue-gray sleep charm drifting like smoke over a candle");
            Add(prompts, "spell_heal", "a muted gold healing sigil stitched from warm light");
            Add(prompts, "spell_fire", "a controlled coal flame cupped inside a black rune circle");
            Add(prompts, "spell_ice", "a pale ice shard rimmed with soot-dark frost");
            Add(prompts, "spell_shield", "a translucent ward disk hammered from amber light");
            Add(prompts, "spell_lightning", "a fork of dull copper lightning over storm slate");
            Add(prompts, "logo_full", "the word Ember implied by a forged crest and coal-lit crown silhouette");
            Add(prompts, "logo_compact", "a compact ember crown mark cut from blackened brass");
            Add(prompts, "splash_background", "a first-person dark fantasy road toward a distant ember-lit citadel at dusk");
            return new StaticPromptCatalog(prompts);
        }

        private static void Add(Dictionary<string, string> prompts, string key, string body)
        {
            prompts[key] = EmberStyleHeader + ", " + body + ", " + EmberNegativeFooter;
        }
    }
}

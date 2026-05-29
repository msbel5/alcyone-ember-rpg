using System.Collections.Generic;
using System.Collections.ObjectModel;
using EmberCrpg.Domain.Actors;

namespace EmberCrpg.Domain.CharacterCreation
{
    public static class CharacterCreationCatalog
    {
        public static IReadOnlyList<CharacterClass> Classes { get; } = new ReadOnlyCollection<CharacterClass>(new[]
        {
            new CharacterClass("warrior", "Warrior", new EmberStatBlock(70, 45, 65, 30, 40, 45),
                new[] { "athletics", "intimidation", "survival" }, new[] { "iron_sword", "round_shield", "mail_shirt" }),
            new CharacterClass("mage", "Mage", new EmberStatBlock(25, 40, 35, 75, 65, 45),
                new[] { "arcana", "history", "investigation" }, new[] { "apprentice_staff", "ember_ward_scroll", "ink_and_chalk" }),
            new CharacterClass("rogue", "Rogue", new EmberStatBlock(35, 75, 40, 45, 65, 55),
                new[] { "stealth", "sleight_of_hand", "deception", "investigation" }, new[] { "dagger", "lockpicks", "hooded_cloak" }),
            new CharacterClass("scholar", "Scholar", new EmberStatBlock(25, 35, 40, 70, 75, 50),
                new[] { "history", "medicine", "insight", "religion" }, new[] { "field_journal", "lens", "healer_kit" }),
            new CharacterClass("diplomat", "Diplomat", new EmberStatBlock(35, 45, 45, 55, 60, 75),
                new[] { "persuasion", "insight", "deception", "history" }, new[] { "court_letter", "fine_cloak", "signet" }),
            new CharacterClass("wanderer", "Wanderer", new EmberStatBlock(50, 60, 55, 40, 65, 40),
                new[] { "survival", "perception", "athletics", "medicine" }, new[] { "travel_bow", "bedroll", "flint" }),
        });

        public static IReadOnlyList<Birthsign> Birthsigns { get; } = new ReadOnlyCollection<Birthsign>(new[]
        {
            // Ember-original birthsigns — forge/ember/ash motifs, NOT the Elder Scrolls signs.
            // Each marks the newborn's blood with one attribute gift. Balanced 2 per attribute.
            new Birthsign("the_anvil", "The Anvil", EmberAttribute.Mig, 5),
            new Birthsign("the_hammer", "The Hammer", EmberAttribute.Mig, 3),
            new Birthsign("the_kiln", "The Kiln", EmberAttribute.End, 5),
            new Birthsign("the_ash", "The Ash", EmberAttribute.End, 4),
            new Birthsign("the_ember", "The Ember", EmberAttribute.Mnd, 5),
            new Birthsign("the_forgefire", "The Forge-Fire", EmberAttribute.Mnd, 6),
            new Birthsign("the_spark", "The Spark", EmberAttribute.Agi, 5),
            new Birthsign("the_wisp", "The Wisp", EmberAttribute.Agi, 3),
            new Birthsign("the_beacon", "The Beacon", EmberAttribute.Pre, 5),
            new Birthsign("the_pyre", "The Pyre", EmberAttribute.Pre, 3),
            new Birthsign("the_cinder", "The Cinder", EmberAttribute.Ins, 4),
            new Birthsign("the_smoke", "The Smoke", EmberAttribute.Ins, 4),
        });

        public static IReadOnlyList<CreationQuestion> Questions { get; } = new ReadOnlyCollection<CreationQuestion>(new[]
        {
            Question("robbed_merchant", "A merchant is robbed in front of you. You...",
                Choice("a", "Mark the thief with a quick ward and follow the trace.", W("mage", 3, "scholar", 1)),
                Choice("b", "Rally the crowd and protect the victim.", W("diplomat", 3, "warrior", 1)),
                Choice("c", "Slip through the alley before anyone notices.", W("rogue", 3, "wanderer", 1))),
            Question("ruined_shrine", "You find a sealed shrine beneath old stone. You...",
                Choice("a", "Force the door before night falls.", W("warrior", 3, "wanderer", 1)),
                Choice("b", "Read the carved warnings first.", W("scholar", 3, "mage", 2)),
                Choice("c", "Listen for the spell binding the lock.", W("mage", 3, "scholar", 1))),
            Question("border_dispute", "Two villages claim the same spring. You...",
                Choice("a", "Bind both elders to a written accord.", W("diplomat", 3, "scholar", 1)),
                Choice("b", "Survey the land and find a second route.", W("wanderer", 3, "scholar", 1)),
                Choice("c", "Pressure the stronger side to stand down.", W("warrior", 2, "diplomat", 1))),
            Question("haunted_road", "A road is haunted after sunset. You...",
                Choice("a", "Prepare charms and test the pattern.", W("mage", 3, "scholar", 1)),
                Choice("b", "Escort the next caravan through.", W("warrior", 2, "wanderer", 2)),
                Choice("c", "Find who profits from the fear.", W("rogue", 2, "diplomat", 2))),
            Question("forbidden_book", "A forbidden book is offered to you. You...",
                Choice("a", "Sell it before the owner returns.", W("rogue", 3, "diplomat", 1)),
                Choice("b", "Seal it and report the danger.", W("scholar", 2, "diplomat", 2)),
                Choice("c", "Study one page for the hidden rule.", W("mage", 4))),
            Question("winter_hunger", "Winter stores run thin. You...",
                Choice("a", "Organize a ration ledger.", W("diplomat", 2, "scholar", 2)),
                Choice("b", "Hunt beyond the safe road.", W("wanderer", 4)),
                Choice("c", "Take supplies from hoarders at night.", W("rogue", 3, "warrior", 1))),
            Question("duel_challenge", "A proud captain challenges you. You...",
                Choice("a", "Accept and end it cleanly.", W("warrior", 4)),
                Choice("b", "Name a clever proxy battlefield.", W("mage", 3, "diplomat", 1)),
                Choice("c", "Expose why the captain needs the duel.", W("diplomat", 3, "rogue", 1))),
            Question("lost_child", "A child vanishes near a marsh. You...",
                Choice("a", "Track reeds and mud by lantern.", W("wanderer", 3, "scholar", 1)),
                Choice("b", "Question every witness separately.", W("diplomat", 2, "rogue", 2)),
                Choice("c", "Cast for echoes of fear.", W("mage", 3, "scholar", 1))),
            Question("guild_offer", "A guild offers protection for obedience. You...",
                Choice("a", "Join and learn its secret structure.", W("mage", 2, "scholar", 2)),
                Choice("b", "Negotiate better terms.", W("diplomat", 4)),
                Choice("c", "Break into the guildhall for leverage.", W("rogue", 4))),
            Question("ancient_map", "An ancient map disagrees with every road. You...",
                Choice("a", "Trust the older stars.", W("mage", 2, "wanderer", 2)),
                Choice("b", "Compare it with tax and temple records.", W("scholar", 4)),
                Choice("c", "Sell copies to every rival faction.", W("rogue", 2, "diplomat", 2))),
        });

        public static CharacterClass GetClass(string id) => Find(Classes, id) ?? Classes[0];
        public static Birthsign GetBirthsign(string id) => Find(Birthsigns, id) ?? Birthsigns[0];

        private static T Find<T>(IReadOnlyList<T> rows, string id) where T : class
        {
            foreach (var row in rows)
            {
                var value = row is CharacterClass c ? c.Id : ((Birthsign)(object)row).Id;
                if (string.Equals(value, id, System.StringComparison.OrdinalIgnoreCase))
                    return row;
            }
            return null;
        }

        private static CreationQuestion Question(string id, string prompt, params CreationChoice[] choices)
        {
            return new CreationQuestion(id, prompt, choices);
        }

        private static CreationChoice Choice(string id, string text, IReadOnlyDictionary<string, int> weights)
        {
            return new CreationChoice(id, text, weights);
        }

        private static IReadOnlyDictionary<string, int> W(params object[] values)
        {
            var dict = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i + 1 < values.Length; i += 2)
                dict[(string)values[i]] = (int)values[i + 1];
            return dict;
        }
    }
}

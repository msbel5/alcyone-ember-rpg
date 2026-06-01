using System;
using System.Collections.Generic;
using System.Text;

namespace EmberCrpg.Domain.Forge
{
    public sealed class GeometricPromptCatalog
    {
        private const string DefaultNegative = "pile, many, cluster, stack, scattered, multiple, ornate, filigree";

        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>
        {
            ["die"] = new Entry(
                "a single six-sided game die, one solid ivory cube with smooth slightly rounded corners, neat round black dot markings on each face, exactly ONE die resting on a dark surface",
                DefaultNegative),
            ["dice"] = new Entry(
                "a single six-sided game die, one solid ivory cube with smooth slightly rounded corners, neat round black dot markings on each face, exactly ONE die resting on a dark surface",
                DefaultNegative),
            ["sword"] = new Entry(
                "a single straight longsword, one rigid steel blade with a centered fuller groove, a simple crossguard, a wrapped leather grip, and a round metal pommel",
                DefaultNegative),
            ["bow"] = new Entry(
                "a single recurve bow, one continuous wood-and-horn arc with taut string, slim limbs, and a centered wrapped grip",
                DefaultNegative),
            ["staff"] = new Entry(
                "a single wooden staff, one long cylindrical polished hardwood shaft with subtle grain and a capped metal tip",
                DefaultNegative),
            ["potion"] = new Entry(
                "a single potion bottle, one small clear glass vial with a narrow neck, rounded body, visible liquid fill, and sealed cork stopper",
                DefaultNegative),
            ["scroll"] = new Entry(
                "a single parchment scroll, one rolled sheet with visible fibers, slightly frayed edges, and a simple tying cord at the center",
                DefaultNegative),
            ["key"] = new Entry(
                "a single metal key, one solid shaft with a circular bow, defined shoulder, and a toothed bit profile",
                DefaultNegative),
            ["ring"] = new Entry(
                "a single metal ring, one smooth circular band with consistent thickness and polished reflective surface",
                DefaultNegative),
            ["helm"] = new Entry(
                "a single medieval helm, one forged steel dome with a nasal guard, cheek coverage, and a narrow eye opening",
                DefaultNegative),
            ["boots"] = new Entry(
                "a single leather boot, one sturdy ankle-high form with reinforced toe box, layered stitched panels, and a thick sole",
                DefaultNegative),
            ["shield"] = new Entry(
                "a single kite shield, one convex hardwood core faced with metal rim, central boss, and visible arm straps on the back",
                DefaultNegative),
            ["sleep"] = new Entry(
                "a single sleep spell token, one flat circular obsidian medallion etched with concentric crescent arcs and a central closed-eye sigil",
                DefaultNegative),
            ["heal"] = new Entry(
                "a single heal spell token, one flat circular brass medallion etched with a symmetric four-lobed cross and a central radiant core",
                DefaultNegative),
            ["fire"] = new Entry(
                "a single fire spell token, one flat circular iron medallion etched with three rising flame tongues around a bright center",
                DefaultNegative),
            ["ice"] = new Entry(
                "a single ice spell token, one flat circular silver medallion etched with a six-point crystalline star and radial fracture lines",
                DefaultNegative),
            ["lightning"] = new Entry(
                "a single lightning spell token, one flat circular steel medallion etched with a jagged branching bolt crossing a split ring",
                DefaultNegative),
        };

        public static string NormalizeObjectName(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return string.Empty;
            }

            var trimmed = objectName.Trim();
            var normalized = new StringBuilder(trimmed.Length);
            var lastWasSpace = false;
            for (var i = 0; i < trimmed.Length; i++)
            {
                var c = char.ToLowerInvariant(trimmed[i]);
                if (char.IsLetterOrDigit(c))
                {
                    normalized.Append(c);
                    lastWasSpace = false;
                    continue;
                }

                if (lastWasSpace)
                {
                    continue;
                }

                normalized.Append(' ');
                lastWasSpace = true;
            }

            var key = normalized.ToString().Trim();
            if (key.StartsWith("spell ", StringComparison.Ordinal))
            {
                key = key.Substring(6).Trim();
            }

            return key;
        }

        public bool TryGet(string objectName, out string positive, out string negative)
        {
            var key = NormalizeObjectName(objectName);
            if (Entries.TryGetValue(key, out var entry))
            {
                positive = entry.Positive;
                negative = entry.Negative;
                return true;
            }

            var fallbackName = string.IsNullOrWhiteSpace(key) ? "object" : key;
            positive = "a single " + fallbackName + ", one solid object, centered, isolated";
            negative = DefaultNegative;
            return false;
        }

        private readonly struct Entry
        {
            public Entry(string positive, string negative)
            {
                Positive = positive ?? string.Empty;
                Negative = negative ?? string.Empty;
            }

            public string Positive { get; }
            public string Negative { get; }
        }
    }
}

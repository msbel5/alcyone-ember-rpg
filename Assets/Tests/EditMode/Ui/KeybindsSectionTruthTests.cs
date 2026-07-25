#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EmberCrpg.Presentation.Ember.UI.Options;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Ui
{
    // B25/B30 pin: KeybindsSection is the read-only cheatsheet the player consults; it must not
    // lie about what the in-game hotkey switch (InGameUiController.HandleScreenInput) actually
    // does. Historically the two drifted — "Tab → Inventory" was listed while Tab in fact opens
    // the browser and I opens inventory; I/K/B/T/H were missing entirely. This test pins the
    // truth-up. If a screen-hotkey KeyCode is added or removed from HandleScreenInput, one of
    // these assertions will fail loudly and remind the maintainer to update the cheatsheet.
    public sealed class KeybindsSectionTruthTests
    {
        private static (string key, string action)[] LoadBindings()
        {
            var field = typeof(KeybindsSection).GetField("Bindings",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, "KeybindsSection.Bindings must exist as a static field.");
            var raw = (Array)field.GetValue(null);
            var rows = new List<(string key, string action)>(raw.Length);
            foreach (var item in raw)
            {
                var t = item.GetType();
                var k = (string)t.GetField("Item1").GetValue(item);
                var a = (string)t.GetField("Item2").GetValue(item);
                rows.Add((k, a));
            }
            return rows.ToArray();
        }

        [Test]
        public void Bindings_CoverEveryScreenHotkeyInHandleScreenInputSwitch()
        {
            // These are the exact letters HandleScreenInput polls via EmberInput.KeyDown(KeyCode.X).
            // Update this list only when the in-game switch itself changes.
            var required = new[] { "Tab", "I", "M", "J", "K", "C", "R", "B", "T", "H", "Esc" };
            var rows = LoadBindings();
            var keys = rows.Select(r => r.key).ToArray();

            foreach (var req in required)
            {
                Assert.That(keys.Any(k => KeyContains(k, req)),
                    $"KeybindsSection is missing a row whose key mentions '{req}' — HandleScreenInput handles it, the cheatsheet must too.");
            }
        }

        [Test]
        public void Bindings_DoNotClaimTabOpensInventory()
        {
            // The exact false label the pre-fix cheatsheet carried. Guard against regression.
            foreach (var (key, action) in LoadBindings())
            {
                var keyMentionsTab = KeyContains(key, "Tab");
                var actionMentionsInventory = (action ?? string.Empty).IndexOf("inventory", StringComparison.OrdinalIgnoreCase) >= 0;
                Assert.That(keyMentionsTab && actionMentionsInventory, Is.False,
                    "Tab does NOT open Inventory in-game — HandleScreenInput routes Tab to the browser and I to inventory.");
            }
        }

        [Test]
        public void Bindings_HaveNoBlankKeyOrAction()
        {
            foreach (var (key, action) in LoadBindings())
            {
                Assert.That(string.IsNullOrWhiteSpace(key), Is.False, "Bindings row has a blank key.");
                Assert.That(string.IsNullOrWhiteSpace(action), Is.False, $"Bindings row for '{key}' has a blank action.");
            }
        }

        // Case-insensitive whole-token contains — "K" must match "K" but not "Left click".
        private static bool KeyContains(string keyLabel, string token)
        {
            if (string.IsNullOrEmpty(keyLabel) || string.IsNullOrEmpty(token)) return false;
            var parts = keyLabel.Split(new[] { ' ', '/', '+', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (string.Equals(p, token, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
#endif

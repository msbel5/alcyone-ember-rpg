#if UNITY_INCLUDE_TESTS
using EmberCrpg.Presentation.Ember.UI;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Ui
{
    public sealed class DialogPortraitKeyTests
    {
        [TestCase(null, "blacksmith")]
        [TestCase("", "blacksmith")]
        [TestCase("portrait_npc_placeholder", "blacksmith")]
        [TestCase("portrait_player_placeholder", "blacksmith")]
        [TestCase("portrait_guard_placeholder", "knight")]
        [TestCase("portrait_priest_placeholder", "sage")]
        [TestCase("Assets/Generated/NpcPortraits/merchant.png", "merchant")]
        [TestCase("C:/tmp/portraits/blacksmith.png", "blacksmith")]
        public void Normalize_ReturnsCanonicalPortraitKeys(string raw, string expected)
        {
            Assert.That(DialogPortraitKey.Normalize(raw), Is.EqualTo(expected));
        }
    }
}
#endif

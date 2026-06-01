#if UNITY_INCLUDE_TESTS
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Adapters;
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

        [Test]
        public void FromSource_UsesDefault_WhenSourceHasNoPortraitInterface()
        {
            Assert.That(DialogPortraitKey.FromSource(new PlainSource()), Is.EqualTo(DialogPortraitKey.Default));
        }

        [Test]
        public void FromSource_NormalizesPortraitName_WhenPortraitSourceProvided()
        {
            Assert.That(DialogPortraitKey.FromSource(new PortraitSource("portrait_guard_placeholder")), Is.EqualTo("knight"));
        }

        private sealed class PlainSource : IDialogSource
        {
            public string GetCurrentLine() => string.Empty;
            public System.Collections.Generic.IReadOnlyList<string> GetTopics() => System.Array.Empty<string>();
            public void SelectTopic(string topicId) { }
        }

        private sealed class PortraitSource : IDialogSourcePortrait
        {
            private readonly string _portraitName;
            public PortraitSource(string portraitName) { _portraitName = portraitName; }
            public string GetCurrentLine() => string.Empty;
            public System.Collections.Generic.IReadOnlyList<string> GetTopics() => System.Array.Empty<string>();
            public void SelectTopic(string topicId) { }
            public string GetPortraitName() => _portraitName;
        }
    }
}
#endif

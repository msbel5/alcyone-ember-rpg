#if UNITY_EDITOR
using System;
using EmberCrpg.Presentation.Ember.UI.InGame.Screens;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace EmberCrpg.Tests.EditMode.UI.InGame
{
    public sealed class InGameEmptyStateViewTests
    {
        [SetUp]
        public void SetUp()
        {
            IgMockData.SpellSchools = Array.Empty<SpellSchoolData>();
            IgMockData.SpellBar = Array.Empty<SpellBarSlotData>();
            IgMockData.ColonyNpcs = Array.Empty<ColonyNpcData>();
        }

        [TearDown]
        public void TearDown()
        {
            IgMockData.SpellSchools = IgMockData.DefaultSpellSchools;
            IgMockData.SpellBar = IgMockData.DefaultSpellBar;
            IgMockData.ColonyNpcs = IgMockData.DefaultColonyNpcs;
        }

        [Test]
        public void SpellbookView_WithNoLiveSpells_RendersEmptyState()
        {
            var root = new VisualElement();

            Assert.DoesNotThrow(() => new SpellbookView(root, null));
            Assert.That(ContainsLabel(root, "No known spells are available."), Is.True);
        }

        [Test]
        public void ColonyView_WithNoLiveColonists_RendersEmptyState()
        {
            var root = new VisualElement();

            Assert.DoesNotThrow(() => new ColonyView(root, null));
            Assert.That(ContainsLabel(root, "No live colonists are available."), Is.True);
        }

        private static bool ContainsLabel(VisualElement root, string text)
        {
            if (root == null) return false;
            foreach (var label in root.Query<Label>().ToList())
            {
                if (string.Equals(label.text, text, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
#endif

using System.Collections;
using System.Linq;
using EmberCrpg.Presentation.Ember.CharacterCreation;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Worldgen;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmberCrpg.Tests.PlayMode.Playability
{
    public sealed class RouteAndWorldgenRuntimeTest
    {
        [UnityTest]
        public IEnumerator WorldgenView_AutoAdvanceUpdate_ReachesStartSceneRequest()
        {
            var view = WorldgenViewController.CreateForTests("SmithingOverworld");
            view.AutoAdvance = true;
            view.Play(new[]
            {
                WorldgenVisibleEvent.Region("ashford"),
                WorldgenVisibleEvent.Question("gate", "Choose a road.", new[] { "north", "south" }),
                WorldgenVisibleEvent.Completed("World built. Regions: 1, Settlements: 0, NPCs: 0. 0 failures.")
            });

            Assert.That(view.QuestionOpen, Is.True);
            view.TickAutoAdvance(1.7f);
            yield return null;
            Assert.That(view.QuestionOpen, Is.False);
            Assert.That(view.RequestedScene, Is.EqualTo("SmithingOverworld"));

            Object.DestroyImmediate(view.gameObject);
        }

        [Test]
        public void CharacterCreation_BeginStoryPersistsFullDossierIntent()
        {
            EmberWorldGenIntent.Pending = null;
            var controller = CharacterCreationController.CreateForTests(77u, string.Empty);
            controller.SetCommanderIdentity("Cinder Vey", "77", "ember-smith");
            controller.Continue();
            for (int i = 0; i < 10; i++) controller.SelectAnswerByIndex(i % 3);
            controller.SkipHistoryReveal();
            controller.Continue();
            controller.RollAllAttributes();
            controller.KeepThisRoll();
            controller.Continue();
            controller.SelectClass("mage");
            controller.SelectAlignment("neutral_good");
            foreach (var skill in new[] { "insight", "religion" })
                controller.ToggleSkill(skill);
            controller.ChooseBackground("smuggler");
            controller.Continue();
            controller.BeginYourStory();

            var intent = EmberWorldGenIntent.Pending;
            Assert.That(intent, Is.Not.Null);
            Assert.That(intent.PlayerName, Is.EqualTo("Cinder Vey"));
            Assert.That(intent.CharacterClassId, Is.EqualTo("mage"));
            Assert.That(intent.BirthsignId, Is.Not.Empty);
            Assert.That(intent.AlignmentId, Is.EqualTo("neutral_good"));
            Assert.That(intent.BackgroundId, Is.EqualTo("smuggler"));
            Assert.That(intent.SkillIds.OrderBy(v => v), Is.EquivalentTo(new[] { "arcana", "history", "insight", "investigation", "religion" }));
            Assert.That(intent.AttributeRolls.Length, Is.EqualTo(6));
            Assert.That(intent.PortraitSeed, Is.EqualTo(77u));
            Assert.That(intent.Start, Is.EqualTo("SmithingOverworld"));

            Object.DestroyImmediate(controller.gameObject);
            EmberWorldGenIntent.Pending = null;
        }
    }
}

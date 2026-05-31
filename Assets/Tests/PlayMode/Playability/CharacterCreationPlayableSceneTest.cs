using System.Collections;
using EmberCrpg.Presentation.Ember.CharacterCreation;
using EmberCrpg.Presentation.Ember.Loading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmberCrpg.Tests.PlayMode.Playability
{
    public sealed class CharacterCreationPlayableSceneTest
    {
        [UnityTest]
        public IEnumerator CharacterCreationScene_HasCameraAndCanReachVisibleWorldgen()
        {
            SceneManager.LoadScene("CharacterCreation");
            yield return null;
            yield return null;

            Assert.That(Camera.main, Is.Not.Null, "CharacterCreation must render a real camera.");
            var controller = Object.FindFirstObjectByType<CharacterCreationController>();
            Assert.That(controller, Is.Not.Null);

            DriveToDossier(controller);
            controller.Continue();
            yield return null;
            yield return null;

            Assert.That(LoadingScreen.IsVisibleLoading(), Is.True);
            Assert.That(Object.FindFirstObjectByType<EmberCrpg.Presentation.Ember.Worldgen.WorldgenViewController>(), Is.Not.Null);
        }

        [Test]
        public void PortraitJson_UsesInjectedLlmBeforeDeterministicFallback()
        {
            var controller = CharacterCreationController.CreateForTests(42u, string.Empty);
            int calls = 0;
            controller.SetPortraitJsonProvider((seed, correction) =>
            {
                calls++;
                return "{\"archetype_id\":\"humanoid_male\",\"primary_hue_degrees\":28,\"secondary_hue_degrees\":215,\"mood_keywords\":[\"wary\"],\"distinctive_features\":[\"iron earring\"],\"clothing_style\":\"leather jerkin\",\"accessory\":\"talisman pendant\",\"world_style_anchor\":\"ember-warm\"}";
            });

            DriveToDossier(controller);

            Assert.That(calls, Is.GreaterThan(0));
            Assert.That(controller.PortraitJson, Does.Contain("iron earring"));
        }

        private static void DriveToDossier(CharacterCreationController controller)
        {
            controller.SetCommanderIdentity("Aria", "42", "ember");
            controller.Continue();
            controller.SetWorldMood("grim");
            controller.SetPlayerCalling("survival");
            controller.SetFateBegins("crossroads");
            for (int i = 0; i < 10; i++) controller.SelectAnswerByIndex(i % 3);
            controller.SkipHistoryReveal();
            controller.Continue();
            controller.SelectBirthsign("the_ember");
            controller.Continue();
            controller.RollAllAttributes();
            controller.KeepThisRoll();
            controller.Continue();
            controller.SelectClass("mage");
            controller.SelectAlignment("neutral_good");
            controller.ToggleSkill("insight");
            controller.ToggleSkill("deception");
            controller.Continue();
            controller.Continue();
            Assert.That(controller.CurrentStep, Is.EqualTo(CharacterCreationController.CreationStep.DossierLaunch));
        }
    }
}

using System.Collections;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Tests.PlayMode.Support;
using EmberCrpg.Ui.Foundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EmberCrpg.Tests.PlayMode.Loading
{
    public sealed class LoadingScreenApiContractTest
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            UiSurfaceLocator.Clear();
            UiSurfaceLocator.Register(new TestUiSurface());
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            LoadingScreen.Hide();
            UiSurfaceLocator.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShowUpdateThumbnailHide_RoundTripsAndReusesController()
        {
            LoadingScreen.Show("Loading", "World");
            LoadingScreen.SetProgress(0.3f, "Step A");
            LoadingScreen.LogLine(UiLogSeverity.Info, "x");
            LoadingScreen.ShowThumbnail(new Texture2D(2, 2), "y");
            Assert.That(Object.FindObjectsByType<LoadingScreenController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            LoadingScreen.Hide();
            LoadingScreen.Show("Loading", "Again");
            Assert.That(Object.FindObjectsByType<LoadingScreenController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            SceneManager.CreateScene("LoadingScreenApiContractTest");
            yield return null;
        }
    }
}

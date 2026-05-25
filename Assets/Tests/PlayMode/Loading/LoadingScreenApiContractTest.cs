using System.Collections;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Tests.PlayMode.Support;
using EmberCrpg.Ui.Foundation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
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
            LoadingScreen.ShowForContext(new LoadingScreenContext("ar1000", "Candlekeep", "load"));
            LoadingScreen.SetProgress(0.3f, "Step A");
            LoadingScreen.LogLine(UiLogSeverity.Info, "x");
            LoadingScreen.ShowThumbnail(new Texture2D(2, 2), "y");
            LoadingScreen.SetInputBlocking(true);
            LoadingScreen.StartTipRotation();
            LoadingScreen.TickEllipsisAnimation(0.5f);
            LoadingScreen.SetLoadingType("save");
            Assert.That(LoadingScreen.BuildLoadingLabelText(), Does.StartWith("Saving"));
            var context = LoadingScreen.GetLoadingContext();
            Assert.That(context.AreaId, Is.EqualTo("ar1000"));
            Assert.That(context.AreaName, Is.EqualTo("Candlekeep"));
            Assert.That(LoadingScreen.IsVisibleLoading(), Is.True);
            Assert.That(Object.FindObjectsByType<LoadingScreenController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            LoadingScreen.Dismiss();
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.That(LoadingScreen.IsVisibleLoading(), Is.False);
            LoadingScreen.Show("Loading", "Again");
            Assert.That(Object.FindObjectsByType<LoadingScreenController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            if (Application.isPlaying) SceneManager.CreateScene("LoadingScreenApiContractTest");
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LoadingLabel_EllipsisCycles()
        {
            LoadingScreen.ShowForContext(new LoadingScreenContext("boot", "Boot", "load"));
            var first = LoadingScreen.BuildLoadingLabelText();
            LoadingScreen.TickEllipsisAnimation(0.5f);
            var second = LoadingScreen.BuildLoadingLabelText();
            LoadingScreen.TickEllipsisAnimation(0.5f);
            var third = LoadingScreen.BuildLoadingLabelText();
            Assert.That(first, Does.StartWith("Loading"));
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(third, Is.Not.EqualTo(second));
            LoadingScreen.Dismiss();
            yield return new WaitForSecondsRealtime(0.35f);
        }
    }
}

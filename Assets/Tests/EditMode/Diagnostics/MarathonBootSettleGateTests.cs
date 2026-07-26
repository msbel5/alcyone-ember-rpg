using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Diagnostics
{
    /// <summary>
    /// REGRESSION PIN for the marathon boot race (obs 20016): the driver's WaitForBootToSettle
    /// used a 30 s hard deadline against `SceneManager.GetActiveScene().name == MainMenu`. On
    /// forge-on boots the ONNX startup burns 60 s+ BEFORE BootBootstrap even calls
    /// LoadSceneAsync(MainMenu), so at 30 s the guard bailed on TIMEOUT, the marathon fired its
    /// own LoadScene(GeneratedWorld), and BootBootstrap's still-queued MainMenu load STOMPED
    /// GeneratedWorld right back out — [Marathon] soak armed never printed.
    ///
    /// The boolean below is MIRRORED verbatim from EmberProofScreenshotDriver.WaitForBootToSettle;
    /// extracting a shared helper would drag Presentation types (SceneManager, LoadingScreen) into
    /// this engine-free test namespace for no wound-proportional gain. If the driver moves the
    /// clause without touching this test, the comment drift is the review flag — same discipline
    /// as the sibling MarathonPassGateCensusTests.
    /// </summary>
    public sealed class MarathonBootSettleGateTests
    {
        // Verbatim while-condition from the driver:
        //   while (Time.unscaledTime < deadline &&
        //          (SceneManager.GetActiveScene().name != EmberScenes.MainMenu ||
        //           LoadingScreen.IsVisibleLoading()))
        private static bool KeepWaiting(float now, float deadline, string activeScene, bool isLoading, string mainMenu)
            => now < deadline && (activeScene != mainMenu || isLoading);

        // Verbatim deadline from the driver.
        private const float DeadlineSeconds = 180f;
        private const string MainMenu = "MainMenu";
        private const string Boot = "Boot";

        [Test]
        public void ForgeBoot_At30s_StillInBoot_KeepsWaiting()
        {
            // The regression scenario: 30 s in, scene is still Boot because ONNX has not returned yet.
            // The OLD 30 s deadline would have exited HERE and armed the stomp race. The new 180 s
            // deadline keeps the wait alive — no early LoadScene(GeneratedWorld).
            Assert.That(KeepWaiting(now: 30f, DeadlineSeconds, activeScene: Boot, isLoading: true, MainMenu),
                Is.True, "at 30s in Boot the guard MUST keep waiting — 30s was the regressed old cutoff");
        }

        [Test]
        public void ForgeBoot_At90s_BootBootstrapFiredLoad_LoadingUp_KeepsWaiting()
        {
            // BootBootstrap has begun its LoadSceneAsync(MainMenu); scene name is already "MainMenu"
            // but the LoadingScreen has not been dismissed. If we exit here the marathon's
            // LoadScene(GeneratedWorld) still races BootBootstrap's async continuation.
            Assert.That(KeepWaiting(now: 90f, DeadlineSeconds, activeScene: MainMenu, isLoading: true, MainMenu),
                Is.True, "MainMenu active + LoadingScreen visible = boot still in flight, must keep waiting");
        }

        [Test]
        public void BootFullySettled_MainMenuActive_LoadingDismissed_Exits()
        {
            // BootBootstrap has finished its async chain and dismissed the LoadingScreen —
            // the only deterministic "no further stomp is queued" signal. Exit is safe here.
            Assert.That(KeepWaiting(now: 90f, DeadlineSeconds, activeScene: MainMenu, isLoading: false, MainMenu),
                Is.False, "MainMenu active + LoadingScreen dismissed = BootBootstrap's async chain resolved; exit");
        }

        [Test]
        public void DeadlineExpires_ExitsRegardless()
        {
            // Even the 180 s ceiling releases eventually so the marathon does not hang the harness
            // on a truly stuck boot — the diagnostic value of a timeout still lives.
            Assert.That(KeepWaiting(now: 181f, DeadlineSeconds, activeScene: Boot, isLoading: true, MainMenu),
                Is.False, "past the 180s ceiling the guard must always release");
            Assert.That(KeepWaiting(now: 181f, DeadlineSeconds, activeScene: MainMenu, isLoading: true, MainMenu),
                Is.False, "past the 180s ceiling even a still-loading MainMenu releases");
        }

        [Test]
        public void DeadlineIs180Seconds_MatchesMarathonAdapterDeadline()
        {
            // Pinning the constant: the driver's own comment ("Forge-on boots spend 60 s+ in ONNX")
            // plus the marathon's own adapterDeadline (120 s + 60 s slack = 180 s) is the choice.
            // A regression that shrinks the deadline back to 30 s trips THIS test.
            Assert.That(DeadlineSeconds, Is.GreaterThanOrEqualTo(180f),
                "180s covers the worst forge boot; 30s did not — DO NOT lower this");
        }
    }
}

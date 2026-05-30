using UnityEngine;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Ui.Foundation;

namespace EmberCrpg.Presentation.Ember
{
    /// <summary>
    /// EMB-017: hidden global state (ForgeLocator / UiSurfaceLocator service singletons + the
    /// EmberWorldGenIntent.Pending hand-off) used to survive across Enter-Play-Mode sessions when
    /// Unity's domain reload is disabled, and across EditMode test runs — causing scene/test bleed
    /// (a stale forge or UI surface from a previous run leaking into the next). This central reset
    /// runs at the very start of every play session, BEFORE any scene loads or any service registers,
    /// guaranteeing a clean slate. The locators stay as compatibility wrappers; a full scene-scoped
    /// composition root is a larger follow-up, but this closes the actual bleed.
    /// </summary>
    public static class EmberLocators
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetOnPlayStart()
        {
            ResetAll();
        }

        /// <summary>Clear every cross-scene static service/hand-off. Safe to call any time a clean
        /// slate is wanted (play start, test setup/teardown).</summary>
        public static void ResetAll()
        {
            ForgeLocator.Clear();
            UiSurfaceLocator.Clear();
            EmberCrpg.Presentation.Ember.UI.EmberWorldGenIntent.Pending = null;
        }
    }
}

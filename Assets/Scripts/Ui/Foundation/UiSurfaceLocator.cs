// PRD: docs/prds/visible-generation-and-consistent-ui.md (Phase 1)
//
// UiSurfaceLocator is the only place game code obtains an IUiSurface.
// Backends register themselves on Awake (UI Toolkit by default, UGUI or
// Web/CSS as fallbacks) and every screen looks the surface up here.
// Mirrors the existing ForgeLocator pattern used by the Forge subsystem.

using System;

namespace EmberCrpg.Ui.Foundation
{
    /// <summary>
    /// Process-wide registry for the active <see cref="IUiSurface"/>.
    /// </summary>
    public static class UiSurfaceLocator
    {
        private static IUiSurface _current;
        private static readonly object _lock = new object();

        /// <summary>
        /// The currently active surface. Returns null when no backend has
        /// registered yet (e.g. very early in Application.Start, or in
        /// tests that have not stood up a fake).
        /// </summary>
        public static IUiSurface Current
        {
            get { lock (_lock) return _current; }
        }

        /// <summary>
        /// Backends call this once on Awake to publish themselves. Calling
        /// twice without an intervening <see cref="Clear"/> throws so two
        /// backends do not silently fight over the surface.
        /// </summary>
        public static void Register(IUiSurface surface)
        {
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            lock (_lock)
            {
                if (_current != null && !ReferenceEquals(_current, surface))
                    throw new InvalidOperationException(
                        $"UiSurfaceLocator already holds {_current.GetType().Name}; " +
                        $"cannot also register {surface.GetType().Name}. Call Clear() first.");
                _current = surface;
            }
        }

        /// <summary>
        /// Drop the currently registered surface. Called from OnDestroy on
        /// the registering backend, and at the start of tests.
        /// </summary>
        public static void Clear()
        {
            lock (_lock) _current = null;
        }
    }
}

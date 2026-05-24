// PRD: docs/prds/visible-generation-and-consistent-ui.md (Phase 1)
//
// IUiSurface is the project-wide UI rendering abstraction. Every screen in
// the Visible Generation pipeline (Boot, Loading, Worldgen) is built against
// this interface — never against UI Toolkit / UGUI types directly — so the
// renderer can be swapped without rewriting any screen.
//
// Decision D1 in the PRD: ship UI Toolkit as the default backend, keep UGUI
// as a fallback if a future Unity LTS breaks UI Toolkit, and leave room for a
// Web/CSS backend (Noesis / ReactUnity) in Phase 6+.
//
// Surfaces are obtained via UiSurfaceLocator (see UiSurfaceLocator.cs) so
// game code never news up a concrete backend.

using System;

namespace EmberCrpg.Ui.Foundation
{
    /// <summary>
    /// Top-level UI rendering host. A surface owns the root container that
    /// all panels mount into and supplies the tokens that style them.
    /// </summary>
    public interface IUiSurface : IDisposable
    {
        /// <summary>
        /// The design tokens (colors, spacing, typography) this surface
        /// will style every panel and prefab with. Never null after the
        /// surface is created.
        /// </summary>
        UiTokens Tokens { get; }

        /// <summary>
        /// Mounts a new panel on top of any existing panels. The returned
        /// panel handle is the only thing callers should use to drive UI;
        /// disposing it removes the panel from the surface.
        /// </summary>
        /// <param name="kind">
        /// Identifies the panel template the backend should instantiate
        /// (e.g. "BootScreen", "LoadingScreen", "WorldgenQuestion"). The
        /// backend resolves <c>kind</c> against its registered prefab /
        /// UXML / web-component library.
        /// </param>
        IUiPanel Mount(string kind);

        /// <summary>
        /// Removes every mounted panel and returns the surface to an empty
        /// state. Equivalent to disposing every <see cref="IUiPanel"/> in
        /// reverse mount order.
        /// </summary>
        void Clear();
    }
}

// PRD: docs/prds/visible-generation-and-consistent-ui.md (Phase 1)
//
// IUiPanel is the second half of the UI abstraction. Every screen the
// Visible Generation pipeline shows (Boot, Loading, Worldgen) talks to a
// panel through this interface and never reaches for UI Toolkit / UGUI
// concrete types.
//
// The contract is intentionally narrow: anything a "log + progress +
// thumbnail" surface needs to update its visible state, and nothing else.
// Layout decisions live in the backend's template (UXML / prefab / web
// component) — IUiPanel only mutates labeled slots inside that template.

using System;
using UnityEngine;

namespace EmberCrpg.Ui.Foundation
{
    /// <summary>
    /// A handle on one panel currently mounted on an <see cref="IUiSurface"/>.
    /// Dispose to unmount.
    /// </summary>
    public interface IUiPanel : IDisposable
    {
        /// <summary>
        /// The kind string that was passed to <see cref="IUiSurface.Mount"/>
        /// when this panel was created. Useful for diagnostics and tests.
        /// </summary>
        string Kind { get; }

        /// <summary>
        /// Update the text content of a labeled slot in the panel template.
        /// Slots that the template does not declare are silently ignored so
        /// the same screen code can run against multiple backends with
        /// slightly different markup.
        /// </summary>
        void SetText(string slot, string value);

        /// <summary>
        /// Update a 0..1 progress bar slot. Values outside the range are
        /// clamped by the backend.
        /// </summary>
        void SetProgress(string slot, float normalized);

        /// <summary>
        /// Update an image slot with raw RGBA / PNG bytes. The backend
        /// owns the lifetime of the resulting GPU texture.
        /// </summary>
        void SetImage(string slot, Texture2D texture);

        /// <summary>
        /// Append one line to a scrollable log slot. Severity is used for
        /// styling (info / warning / error colors come from the surface's
        /// <see cref="UiTokens"/>).
        /// </summary>
        void AppendLog(string slot, UiLogSeverity severity, string line);
    }

    /// <summary>
    /// Log severity levels recognised by panel log slots. Backends map
    /// these to the appropriate color tokens.
    /// </summary>
    public enum UiLogSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Success = 3,
    }
}

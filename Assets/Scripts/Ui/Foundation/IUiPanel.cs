// IUiPanel is the second half of the UI abstraction. Every screen the Visible
// Generation pipeline shows (Boot, Loading, Worldgen, CharacterCreation overlay)
// talks to a panel through this interface and never reaches for UI Toolkit /
// UGUI concrete types.
//
// Contract is intentionally narrow: anything a "log + progress + thumbnail"
// surface needs to update its visible state, and nothing else. Layout decisions
// live in the backend's template (UXML / prefab / web component) — IUiPanel
// only mutates labeled slots inside that template. Slots the template does not
// declare are silently ignored so the same screen code can run against
// multiple backends with slightly different markup.

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
        /// The panel identifier passed to <see cref="IUiSurface.Mount"/> when
        /// this panel was created. Useful for diagnostics and tests.
        /// </summary>
        string Id { get; }

        /// <summary>Update the text content of a labeled slot.</summary>
        void SetText(string slot, string text);

        /// <summary>Update a 0..1 progress bar slot. Out-of-range values are clamped by the backend.</summary>
        void SetProgress(string slot, float normalized);

        /// <summary>
        /// Append one line to a scrollable log slot. Severity is used for
        /// styling (info / warning / error / success colors come from the
        /// surface's <see cref="UiTokens"/>).
        /// </summary>
        void LogLine(string slot, UiLogSeverity severity, string line);

        /// <summary>Update an image / thumbnail slot. Pass <c>null</c> to clear.</summary>
        void SetThumbnail(string slot, Texture2D texture);

        /// <summary>Show or hide a slot.</summary>
        void SetVisible(string slot, bool visible);

        /// <summary>Wire (or unwire, by passing <c>null</c>) a button slot's click handler.</summary>
        void SetButtonHandler(string slot, Action onClick);
    }

    /// <summary>
    /// Log severity levels recognised by panel log slots. Backends map these
    /// to the appropriate color tokens via <see cref="UiTokens.SeverityColor"/>.
    /// </summary>
    public enum UiLogSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Success = 3,
    }
}

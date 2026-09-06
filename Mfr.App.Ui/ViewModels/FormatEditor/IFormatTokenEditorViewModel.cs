namespace Mfr.App.Ui.ViewModels.FormatEditor
{
    /// <summary>
    /// View-model contract for a format-token parameter dialog body.
    /// </summary>
    public interface IFormatTokenEditorViewModel
    {
        /// <summary>
        /// Gets the dialog window title.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets the canonical token name (for example <c>counter</c>).
        /// </summary>
        string CanonicalName { get; }

        /// <summary>
        /// Gets the full preview including angle brackets (for example <c>&lt;counter:initial=1,…&gt;</c>).
        /// </summary>
        string ResultingFormatString { get; }

        /// <summary>
        /// Builds the inner token text without angle brackets (<c>name</c> or <c>name:args</c>).
        /// </summary>
        /// <returns>Inner text suitable for wrapping in <c>&lt;…&gt;</c>.</returns>
        string BuildInnerText();
    }
}

using Mfr.App.Ui.ViewModels.LogDialog;

namespace Mfr.App.Ui.Views
{
    /// <summary>
    /// Optional overrides for MFR → Log (headless tests; leave null in production).
    /// </summary>
    internal sealed class RenameLogDialogHooks
    {
        /// <summary>
        /// When set, replaces showing the Rename Log dialog.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Return <see langword="true"/> when the user chose Undo (host should apply
        /// <see cref="RenameLogDialogViewModel.LogToUndo"/>).
        /// </para>
        /// </remarks>
        public Func<RenameLogDialogViewModel, Task<bool?>>? Show { get; init; }
    }
}

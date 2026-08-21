using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.Services.FileList;

namespace Mfr.App.Ui.ViewModels.FileList
{
    /// <summary>
    /// Draft state for the Exclude Masks dialog (MFR 7 layout).
    /// </summary>
    public sealed partial class ExcludeMasksDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes the dialog from the File List's current exclude-mask settings.
        /// </summary>
        /// <param name="enabled">Whether exclude masks are currently applied.</param>
        /// <param name="joinedMasks">Persisted <c>;</c>-delimited masks.</param>
        public ExcludeMasksDialogViewModel(bool enabled, string? joinedMasks)
        {
            IsEnabled = enabled;
            MasksText = WildcardMask.FormatForEditor(joinedMasks);
        }

        /// <summary>
        /// Whether matching file names should be excluded when OK is pressed.
        /// </summary>
        [ObservableProperty]
        private bool _isEnabled;

        /// <summary>
        /// Exclude patterns, one per line (also accepts <c>;</c> / <c>:</c> / <c>|</c> on a line).
        /// </summary>
        [ObservableProperty]
        private string _masksText = string.Empty;
    }
}

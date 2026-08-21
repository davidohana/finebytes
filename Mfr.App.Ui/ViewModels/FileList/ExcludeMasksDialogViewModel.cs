using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.App.Ui.Services.FileList;

namespace Mfr.App.Ui.ViewModels.FileList
{
    /// <summary>
    /// Draft state for the Exclude Masks dialog.
    /// </summary>
    public sealed partial class ExcludeMasksDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes the dialog from the File List's current exclude-mask settings.
        /// </summary>
        /// <param name="enabled">Whether exclude masks are currently applied.</param>
        /// <param name="masks">Persisted exclude mask list.</param>
        public ExcludeMasksDialogViewModel(bool enabled, IReadOnlyList<string>? masks)
        {
            IsEnabled = enabled;
            MasksText = WildcardMask.FormatForEditor(masks);
        }

        /// <summary>
        /// Whether matching file names should be excluded when OK is pressed.
        /// </summary>
        [ObservableProperty]
        private bool _isEnabled;

        /// <summary>
        /// Exclude patterns, one per line.
        /// </summary>
        [ObservableProperty]
        private string _masksText = string.Empty;
    }
}

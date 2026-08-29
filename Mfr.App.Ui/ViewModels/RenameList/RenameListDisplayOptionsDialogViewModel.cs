using CommunityToolkit.Mvvm.ComponentModel;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Draft state for the Rename List Display Options dialog.
    /// </summary>
    public sealed partial class RenameListDisplayOptionsDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes the dialog from the current Rename List display settings.
        /// </summary>
        /// <param name="useFixedWidthFont">Current fixed-width font flag.</param>
        public RenameListDisplayOptionsDialogViewModel(bool useFixedWidthFont)
        {
            UseFixedWidthFont = useFixedWidthFont;
        }

        /// <summary>
        /// Draft fixed-width font choice applied when OK is pressed.
        /// </summary>
        [ObservableProperty]
        private bool _useFixedWidthFont;
    }
}

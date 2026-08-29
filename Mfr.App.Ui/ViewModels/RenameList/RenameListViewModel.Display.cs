using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Models.Config;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Rename List display preferences for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Raised when the view should open the Display Options dialog.
        /// </summary>
        public event EventHandler? DisplayOptionsRequested;

        /// <summary>
        /// Gets whether the Rename List grid uses a fixed-width font.
        /// </summary>
        [ObservableProperty]
        private bool _useFixedWidthFont = ConfigStore.Config.Ui.RenameListUseFixedWidthFont;

        /// <summary>
        /// Updates the live fixed-width font flag.
        /// </summary>
        /// <param name="value">New value.</param>
        public void SetUseFixedWidthFont(bool value)
        {
            if (UseFixedWidthFont == value)
            {
                return;
            }

            UseFixedWidthFont = value;
        }

        /// <summary>
        /// Commits display options from the dialog and persists them to <c>config.json</c>.
        /// </summary>
        /// <param name="useFixedWidthFont">Draft fixed-width font choice.</param>
        public void CommitDisplayOptions(bool useFixedWidthFont)
        {
            SetUseFixedWidthFont(useFixedWidthFont);
            ConfigStore.Config.Ui.RenameListUseFixedWidthFont = useFixedWidthFont;
            ConfigStore.Save();
        }

        /// <summary>
        /// Opens the Display Options dialog (context menu).
        /// </summary>
        [RelayCommand]
        public void OpenDisplayOptions()
        {
            DisplayOptionsRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

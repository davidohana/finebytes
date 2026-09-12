using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Color legend panel visibility for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Gets whether the Rename List color legend panel is visible (MFR7 ColorLegend).
        /// </summary>
        [ObservableProperty]
        private bool _isLegendVisible;

        /// <summary>
        /// Shows or hides the color legend panel.
        /// </summary>
        [RelayCommand]
        public void ToggleLegend()
        {
            IsLegendVisible = !IsLegendVisible;
        }
    }
}

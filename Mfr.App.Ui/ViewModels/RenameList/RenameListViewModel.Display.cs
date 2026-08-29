using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Rename List display preferences for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Gets whether the Rename List grid uses a fixed-width font.
        /// </summary>
        [ObservableProperty]
        private bool _useFixedWidthFont;

        /// <summary>
        /// Toggles fixed-width font (context and main menus).
        /// </summary>
        [RelayCommand]
        public void ToggleUseFixedWidthFont()
        {
            UseFixedWidthFont = !UseFixedWidthFont;
        }
    }
}

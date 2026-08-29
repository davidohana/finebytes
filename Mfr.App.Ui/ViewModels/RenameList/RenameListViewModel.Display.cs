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
        /// Gets whether the Rename List grid uses a fixed-width font.
        /// </summary>
        [ObservableProperty]
        private bool _useFixedWidthFont = SessionStore.Current.RenameList?.UseFixedWidthFont ?? false;

        /// <summary>
        /// Updates the fixed-width font flag and persists it to <c>session.json</c>.
        /// </summary>
        /// <param name="value">New value.</param>
        public void SetUseFixedWidthFont(bool value)
        {
            if (UseFixedWidthFont == value)
            {
                return;
            }

            UseFixedWidthFont = value;
            SessionStore.Current.EnsureRenameList().UseFixedWidthFont = value;
            SessionStore.SaveCurrentPreferences();
        }

        /// <summary>
        /// Toggles fixed-width font (context and main menus).
        /// </summary>
        [RelayCommand]
        public void ToggleUseFixedWidthFont()
        {
            SetUseFixedWidthFont(!UseFixedWidthFont);
        }
    }
}

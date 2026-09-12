using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Models.Config;

namespace Mfr.App.Ui.ViewModels.Options
{
    /// <summary>
    /// Draft state for the Options dialog (app-wide prefs).
    /// </summary>
    public sealed partial class OptionsDialogViewModel : ViewModelBase
    {
        private readonly SessionState _session;

        /// <summary>
        /// Initializes the dialog from the live session and <see cref="ConfigStore.Config"/>.
        /// </summary>
        /// <param name="session">Live session document whose remember flags are edited.</param>
        public OptionsDialogViewModel(SessionState session)
        {
            ArgumentNullException.ThrowIfNull(session);

            _session = session;
            var mainWindow = session.MainWindow ?? new SessionStateMainWindow();
            var fileList = session.FileList ?? new SessionStateFileList();
            RememberLastFolder = fileList.RememberLastFolder;
            RememberWindowState = mainWindow.RememberWindowState;
            ConfirmReplaceAppliedFiltersOnLoad = ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad;
        }

        /// <summary>
        /// When <see langword="true"/>, restore and save the File List last folder across launches.
        /// </summary>
        [ObservableProperty]
        private bool _rememberLastFolder;

        /// <summary>
        /// When <see langword="true"/>, restore and save main-window size, position, and splitters.
        /// </summary>
        [ObservableProperty]
        private bool _rememberWindowState;

        /// <summary>
        /// When <see langword="true"/>, confirm before replacing a non-empty Applied Filters chain on preset load.
        /// </summary>
        [ObservableProperty]
        private bool _confirmReplaceAppliedFiltersOnLoad;

        /// <summary>
        /// Writes draft values into the live session and <see cref="ConfigStore.Config"/>.
        /// <para>Does not write <c>config.json</c> or <c>session.json</c>; the host calls
        /// <see cref="ConfigStore.Save"/> and session flags ride close-save.</para>
        /// </summary>
        public void Commit()
        {
            _session.EnsureFileList().RememberLastFolder = RememberLastFolder;
            _session.EnsureMainWindow().RememberWindowState = RememberWindowState;
            ConfigStore.Config.Ui.Presets.ConfirmReplaceAppliedFiltersOnLoad = ConfirmReplaceAppliedFiltersOnLoad;
        }
    }
}

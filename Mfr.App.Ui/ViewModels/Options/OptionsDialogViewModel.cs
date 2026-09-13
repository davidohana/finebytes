using CommunityToolkit.Mvvm.ComponentModel;
using Mfr.Models.Config;

namespace Mfr.App.Ui.ViewModels.Options
{
    /// <summary>
    /// Draft state for the Options dialog (app-wide prefs).
    /// </summary>
    public sealed partial class OptionsDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes the dialog from live <see cref="ConfigStore"/> sections.
        /// </summary>
        public OptionsDialogViewModel()
        {
            RememberLastFolder = ConfigStore.FileList?.RememberLastFolder ?? true;
            RememberWindowState = ConfigStore.MainWindow?.RememberWindowState ?? true;
            ConfirmationPrompts = ConfigStore.Ui.ConfirmationPrompts;
            DoubleClickAddsToRenameList = ConfigStore.FileList?.DoubleClickAddsToRenameList ?? false;
            AddMode = ConfigStore.RenameList?.AddMode ?? RenameListAddMode.Files;
            AddFolderContents = ConfigStore.RenameList?.AddFolderContents ?? true;
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
        /// Draft confirmation-prompts level for gated UI confirms.
        /// </summary>
        [ObservableProperty]
        private ConfirmationPrompts _confirmationPrompts;

        /// <summary>
        /// When <see langword="true"/>, double-click in the File List adds the selection to the Rename List.
        /// </summary>
        [ObservableProperty]
        private bool _doubleClickAddsToRenameList;

        /// <summary>
        /// Draft path kinds that become Rename List rows when adding from the File List.
        /// </summary>
        [ObservableProperty]
        private RenameListAddMode _addMode;

        /// <summary>
        /// When <see langword="true"/>, folder sources recurse into subfolders when adding.
        /// </summary>
        [ObservableProperty]
        private bool _addFolderContents;

        /// <summary>
        /// Writes draft values into live <see cref="ConfigStore"/> sections.
        /// <para>Does not write <c>config.json</c>; the host calls <see cref="ConfigStore.Save"/>
        /// (whole prefs document, including the mutated sections).</para>
        /// </summary>
        public void Commit()
        {
            var fileList = ConfigStore.EnsureFileList();
            fileList.RememberLastFolder = RememberLastFolder;
            fileList.DoubleClickAddsToRenameList = DoubleClickAddsToRenameList;
            ConfigStore.EnsureMainWindow().RememberWindowState = RememberWindowState;
            ConfigStore.Ui.ConfirmationPrompts = ConfirmationPrompts;
            var renameList = ConfigStore.EnsureRenameList();
            renameList.AddMode = AddMode;
            renameList.AddFolderContents = AddFolderContents;
        }
    }
}

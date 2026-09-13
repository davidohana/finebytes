using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Models.Config;

namespace Mfr.App.Ui.ViewModels.Options
{
    /// <summary>
    /// Draft state for the Options dialog (app-wide prefs).
    /// </summary>
    public sealed partial class OptionsDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Default limited retention count (matches <see cref="RenameLogConfig.Limit"/> default).
        /// </summary>
        public const int DefaultLimitedCount = 10;

        /// <summary>
        /// Minimum value for the Limited spinner (MFR7 parity).
        /// </summary>
        public const decimal MinLimitedCount = 1;

        /// <summary>
        /// Maximum value for the Limited spinner (MFR7 parity).
        /// </summary>
        public const decimal MaxLimitedCount = 1000;

        private bool _suppressLimitedCountModeSelect;

        /// <summary>
        /// Initializes the dialog from live <see cref="ConfigStore"/> sections.
        /// </summary>
        public OptionsDialogViewModel()
        {
            RememberLastFolder = ConfigStore.FileList?.RememberLastFolder ?? true;
            RememberWindowState = ConfigStore.MainWindow?.RememberWindowState ?? true;
            SuppressedConfirmations = [.. ConfigStore.Ui.SuppressedConfirmations];
            DoubleClickAddsToRenameList = ConfigStore.FileList?.DoubleClickAddsToRenameList ?? false;
            AddMode = ConfigStore.RenameList?.AddMode ?? RenameListAddMode.Files;
            AddFolderContents = ConfigStore.RenameList?.AddFolderContents ?? true;
            _LoadRenameLogRetention(ConfigStore.RenameLog.Limit);
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
        /// Draft list of confirmation kinds the user chose not to see again.
        /// </summary>
        [ObservableProperty]
        private List<ConfirmationKind> _suppressedConfirmations = [];

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
        /// Draft rename-log retention mode (maps to <c>renameLog.limit</c>).
        /// </summary>
        [ObservableProperty]
        private RenameLogRetentionMode _renameLogRetentionMode;

        /// <summary>
        /// Draft Limited count when <see cref="RenameLogRetentionMode"/> is <see cref="RenameLogRetentionMode.Limited"/>.
        /// </summary>
        [ObservableProperty]
        private decimal _renameLogLimitedCount = DefaultLimitedCount;

        /// <summary>
        /// Clears the draft suppress list so all confirmation dialogs show again after OK.
        /// <para>
        /// Does not mutate <see cref="ConfigStore"/>; <see cref="Commit"/> writes the empty list.
        /// </para>
        /// </summary>
        [RelayCommand]
        public void ResetConfirmations()
        {
            SuppressedConfirmations = [];
        }

        /// <summary>
        /// Writes draft values into live <see cref="ConfigStore"/> sections.
        /// <para>
        /// Does not write <c>config.json</c>; the host calls <see cref="ConfigStore.Save"/>
        /// (whole prefs document, including the mutated sections) and may prune on-disk
        /// <c>.mfrlog</c> files to the new <c>renameLog.limit</c>.
        /// </para>
        /// </summary>
        public void Commit()
        {
            var fileList = ConfigStore.EnsureFileList();
            fileList.RememberLastFolder = RememberLastFolder;
            fileList.DoubleClickAddsToRenameList = DoubleClickAddsToRenameList;
            ConfigStore.EnsureMainWindow().RememberWindowState = RememberWindowState;
            ConfigStore.Ui.SuppressedConfirmations = [.. SuppressedConfirmations];
            var renameList = ConfigStore.EnsureRenameList();
            renameList.AddMode = AddMode;
            renameList.AddFolderContents = AddFolderContents;
            ConfigStore.RenameLog.Limit = _LimitFromDraft();
        }

        /// <summary>
        /// Selects Limited when the user edits the spinner (MFR7 parity).
        /// </summary>
        /// <param name="value">New spinner value (unused; mode switch only).</param>
        partial void OnRenameLogLimitedCountChanged(decimal value)
        {
            if (_suppressLimitedCountModeSelect)
            {
                return;
            }

            if (RenameLogRetentionMode != RenameLogRetentionMode.Limited)
            {
                RenameLogRetentionMode = RenameLogRetentionMode.Limited;
            }
        }

        /// <summary>
        /// Maps persisted <c>renameLog.limit</c> onto draft mode + spinner without selecting Limited.
        /// </summary>
        /// <param name="limit">Stored retention limit.</param>
        private void _LoadRenameLogRetention(int limit)
        {
            _suppressLimitedCountModeSelect = true;
            try
            {
                if (limit <= 0)
                {
                    RenameLogRetentionMode = RenameLogRetentionMode.Disabled;
                    RenameLogLimitedCount = DefaultLimitedCount;
                    return;
                }

                if (limit == int.MaxValue)
                {
                    RenameLogRetentionMode = RenameLogRetentionMode.Unlimited;
                    RenameLogLimitedCount = DefaultLimitedCount;
                    return;
                }

                RenameLogRetentionMode = RenameLogRetentionMode.Limited;
                RenameLogLimitedCount = Math.Clamp(limit, (int)MinLimitedCount, (int)MaxLimitedCount);
            }
            finally
            {
                _suppressLimitedCountModeSelect = false;
            }
        }

        /// <summary>
        /// Maps draft mode + spinner to the integer written as <c>renameLog.limit</c>.
        /// </summary>
        /// <returns>0, N in 1..1000, or <see cref="int.MaxValue"/>.</returns>
        private int _LimitFromDraft()
        {
            if (RenameLogRetentionMode == RenameLogRetentionMode.Disabled)
            {
                return 0;
            }

            if (RenameLogRetentionMode == RenameLogRetentionMode.Unlimited)
            {
                return int.MaxValue;
            }

            var count = (int)Math.Round(RenameLogLimitedCount, MidpointRounding.AwayFromZero);
            return Math.Clamp(count, (int)MinLimitedCount, (int)MaxLimitedCount);
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services.Help;
using Mfr.Engine.Config;
using Mfr.Models.Config;

namespace Mfr.App.Ui.ViewModels.Options
{
    /// <summary>
    /// Draft state for the Options dialog (app-wide prefs).
    /// </summary>
    public sealed partial class OptionsDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Default limited retention count (matches <see cref="RenameLogConfig.DefaultLimit"/>).
        /// </summary>
        public const int DefaultLimitedCount = RenameLogConfig.DefaultLimit;

        /// <summary>
        /// Minimum value for the Limited spinner (MFR7 parity).
        /// </summary>
        public const decimal MinLimitedCount = 1;

        /// <summary>
        /// Maximum value for the Limited spinner (MFR7 parity).
        /// </summary>
        public const decimal MaxLimitedCount = 1000;

        private readonly HelpHost _helpHost;
        private readonly Action<string>? _helpMissing;
        private bool _suppressLimitedCountModeSelect;

        /// <summary>
        /// Initializes the dialog from live <see cref="ConfigStore"/> sections.
        /// </summary>
        /// <param name="helpHost">Optional Help opener for the GeoNames username instructions link.</param>
        /// <param name="onHelpMissing">Optional callback when the GeoNames help file cannot be opened.</param>
        public OptionsDialogViewModel(HelpHost? helpHost = null, Action<string>? onHelpMissing = null)
        {
            _helpHost = helpHost ?? new HelpHost();
            _helpMissing = onHelpMissing;
            var options = ConfigStore.Options;
            RememberLastFolder = options.RememberLastFolder;
            RememberWindowState = options.RememberWindowState;
            SuppressedConfirmations = [.. options.SuppressedConfirmations];
            DoubleClickAddsToRenameList = options.DoubleClickAddsToRenameList;
            AddMode = options.AddMode;
            AddFolderContents = options.AddFolderContents;
            IncludeHidden = options.IncludeHidden;
            RememberColumnWidths = options.RememberColumnWidths;
            GeoNamesUsername = options.GeoNamesUsername ?? string.Empty;
            _LoadRenameLogRetention(ConfigStore.RenameLog.Limit);
        }

        /// <summary>
        /// When <see langword="true"/>, restore and save the File List last folder across launches.
        /// </summary>
        [ObservableProperty]
        private bool _rememberLastFolder;

        /// <summary>
        /// When <see langword="true"/>, restore and save main-window and dialog size/position across launches.
        /// </summary>
        [ObservableProperty]
        private bool _rememberWindowState;

        /// <summary>
        /// Draft list of confirmation kinds the user chose not to see again.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SuppressedConfirmationsSummary))]
        private List<ConfirmationKind> _suppressedConfirmations = [];

        /// <summary>
        /// Status line for how many confirmation kinds are suppressed in the draft.
        /// </summary>
        public string SuppressedConfirmationsSummary
        {
            get
            {
                var count = SuppressedConfirmations.Count;
                if (count == 0)
                {
                    return "No confirmations are currently suppressed.";
                }

                if (count == 1)
                {
                    return "1 confirmation is currently suppressed.";
                }

                return $"{count} confirmations are currently suppressed.";
            }
        }

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
        /// When <see langword="true"/>, show Hidden|System in the File List and include them when adding.
        /// </summary>
        [ObservableProperty]
        private bool _includeHidden;

        /// <summary>
        /// When <see langword="true"/>, reuse last resized Rename List column widths when a column returns.
        /// </summary>
        [ObservableProperty]
        private bool _rememberColumnWidths;

        /// <summary>
        /// Draft GeoNames username override (blank = bundled FineBytes account).
        /// </summary>
        [ObservableProperty]
        private string _geoNamesUsername = string.Empty;

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
        /// Opens the in-app GeoNames username help page.
        /// </summary>
        [RelayCommand]
        public void OpenGeoNamesUsernameHelp()
        {
            const string helpFileName = "geonames-username.html";
            if (_helpHost.TryOpen(helpFileName, out _))
            {
                return;
            }

            _helpMissing?.Invoke(helpFileName);
        }

        /// <summary>
        /// Writes draft values into live <see cref="ConfigStore"/> sections.
        /// <para>
        /// Does not write <c>config.json</c>; the host calls <see cref="ConfigStore.Save"/>
        /// (whole prefs document, including the mutated sections) and may prune on-disk
        /// <c>.mfrlog</c> files to the new <c>renameLog.limit</c>. Changing the GeoNames
        /// username clears the process GeoNames cache and circuit breaker (not the disk cache).
        /// </para>
        /// </summary>
        public void Commit()
        {
            var options = ConfigStore.Options;
            var previousUsername = options.GeoNamesUsername ?? string.Empty;
            var nextUsername = GeoNamesUsername ?? string.Empty;
            options.RememberLastFolder = RememberLastFolder;
            options.DoubleClickAddsToRenameList = DoubleClickAddsToRenameList;
            options.RememberWindowState = RememberWindowState;
            options.SuppressedConfirmations = [.. SuppressedConfirmations];
            options.AddMode = AddMode;
            options.AddFolderContents = AddFolderContents;
            options.IncludeHidden = IncludeHidden;
            options.RememberColumnWidths = RememberColumnWidths;
            options.GeoNamesUsername = nextUsername;
            ConfigStore.RenameLog.Limit = _LimitFromDraft();

            if (!string.Equals(previousUsername.Trim(), nextUsername.Trim(), StringComparison.Ordinal))
            {
                ConfigStore.ClearGeoNamesProcessCache();
            }
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

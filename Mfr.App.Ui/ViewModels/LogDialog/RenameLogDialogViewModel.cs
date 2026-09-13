using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mfr.Engine.RenameLog;
using Mfr.Models.Rename;
using Mfr.Utils;

namespace Mfr.App.Ui.ViewModels.LogDialog
{
    /// <summary>
    /// One row in the Rename Log list (in-memory last op or a disk <c>.mfrlog</c>).
    /// </summary>
    public sealed class RenameLogListItem
    {
        /// <summary>
        /// Initializes a last-operation row (in-memory only; Erase does not clear the store).
        /// </summary>
        /// <param name="log">In-memory last GO log.</param>
        public RenameLogListItem(RenameLog log)
        {
            ArgumentNullException.ThrowIfNull(log);
            Title = RenameLogStore.FormatListTitle(log.CommittedAt);
            IsLastOperation = true;
            FilePath = null;
            _cachedLog = log;
        }

        /// <summary>
        /// Initializes a disk log row.
        /// </summary>
        /// <param name="filePath">Absolute path to the <c>.mfrlog</c> file.</param>
        /// <param name="cachedLog">
        /// Optional already-loaded log (e.g. in-memory <see cref="RenameLogStore.LastOperation"/> for the
        /// matching disk file) so the dialog can show details without re-reading a large file.
        /// </param>
        public RenameLogListItem(string filePath, RenameLog? cachedLog = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            Title = RenameLogStore.FormatDiskListTitle(filePath);
            IsLastOperation = false;
            FilePath = filePath;
            _cachedLog = cachedLog;
        }

        /// <summary>
        /// Gets the list display text.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Whether this row is the in-memory last operation (not a disk file).
        /// </summary>
        public bool IsLastOperation { get; }

        /// <summary>
        /// Gets the on-disk path, or <see langword="null"/> for last operation.
        /// </summary>
        public string? FilePath { get; }

        private RenameLog? _cachedLog;
        private string? _loadError;

        /// <summary>
        /// Loads the log for details / Undo (cached after first success).
        /// </summary>
        /// <param name="errorMessage">Set when load fails; otherwise <see langword="null"/>.</param>
        /// <returns>The log, or <see langword="null"/> on failure.</returns>
        public RenameLog? TryGetLog(out string? errorMessage)
        {
            if (_cachedLog is not null)
            {
                errorMessage = null;
                return _cachedLog;
            }

            if (_loadError is not null)
            {
                errorMessage = _loadError;
                return null;
            }

            if (FilePath.IsBlank())
            {
                errorMessage = "Failed to load log.";
                return null;
            }

            var loaded = RenameLogStore.TryLoadFile(FilePath);
            if (loaded is null)
            {
                _loadError = "Failed to load log.";
                errorMessage = _loadError;
                return null;
            }

            _cachedLog = loaded;
            errorMessage = null;
            return loaded;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Title;
        }
    }

    /// <summary>
    /// View model for the Rename Log dialog (list, details, Undo, Erase).
    /// </summary>
    public sealed partial class RenameLogDialogViewModel : ViewModelBase
    {
        private readonly string? _directoryPath;

        /// <summary>
        /// Initializes the dialog list from last operation plus disk logs (newest-first).
        /// </summary>
        /// <param name="directoryPath">
        /// Override rename-log directory. When blank, <see cref="RenameLogStore.DefaultDirectoryPath"/>.
        /// </param>
        public RenameLogDialogViewModel(string? directoryPath = null)
        {
            _directoryPath = directoryPath;
            Items = [];
            _ReloadItems(selectFirst: true);
        }

        /// <summary>
        /// Gets the log list (newest first; last operation at top when present).
        /// </summary>
        public ObservableCollection<RenameLogListItem> Items { get; }

        /// <summary>
        /// Gets or sets the selected list row.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
        [NotifyCanExecuteChangedFor(nameof(EraseCommand))]
        private RenameLogListItem? _selectedItem;

        /// <summary>
        /// Gets the details pane text for the selection.
        /// </summary>
        [ObservableProperty]
        private string _detailsText = string.Empty;

        /// <summary>
        /// Log chosen for Undo when the dialog closes with accept; otherwise <see langword="null"/>.
        /// </summary>
        public RenameLog? LogToUndo { get; private set; }

        /// <summary>
        /// Invoked to close the dialog; argument is <see langword="true"/> when Undo was accepted.
        /// </summary>
        public Action<bool>? CloseRequested { get; set; }

        /// <summary>
        /// Optional error UI when Erase fails to delete a disk file (title, message).
        /// </summary>
        public Func<string, string, Task>? ShowErrorAsync { get; set; }

        partial void OnSelectedItemChanged(RenameLogListItem? value)
        {
            _RefreshDetails(value);
        }

        /// <summary>
        /// Requests Undo for the selection (closes dialog; host runs rename-list undo).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanUndo))]
        public void Undo()
        {
            if (SelectedItem is null)
            {
                return;
            }

            var log = SelectedItem.TryGetLog(out _);
            if (log is null || !log.HasUndoableEntries)
            {
                return;
            }

            LogToUndo = log;
            CloseRequested?.Invoke(true);
        }

        /// <summary>
        /// Erases the selection: deletes the disk file when present; last-op erase only removes the list row.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanErase))]
        public async Task EraseAsync()
        {
            if (SelectedItem is null)
            {
                return;
            }

            var item = SelectedItem;
            var selectedIndex = Items.IndexOf(item);

            if (!item.IsLastOperation && !item.FilePath.IsBlank())
            {
                if (!RenameLogStore.TryDeleteFile(item.FilePath))
                {
                    var showError = ShowErrorAsync;
                    if (showError is not null)
                    {
                        await showError("Failed to erase log file", item.Title).ConfigureAwait(true);
                    }

                    return;
                }
            }

            Items.Remove(item);

            if (Items.Count == 0)
            {
                SelectedItem = null;
                return;
            }

            SelectedItem = selectedIndex < Items.Count ? Items[selectedIndex] : Items[^1];
        }

        /// <summary>
        /// Closes the dialog without undoing.
        /// </summary>
        [RelayCommand]
        public void Close()
        {
            LogToUndo = null;
            CloseRequested?.Invoke(false);
        }

        private bool _CanUndo()
        {
            if (SelectedItem is null)
            {
                return false;
            }

            var log = SelectedItem.TryGetLog(out _);
            return log is { HasUndoableEntries: true };
        }

        private bool _CanErase()
        {
            return SelectedItem is not null;
        }

        private void _ReloadItems(bool selectFirst)
        {
            Items.Clear();

            var diskPaths = RenameLogStore.ListDiskFilePaths(_directoryPath);
            var last = RenameLogStore.LastOperation;
            var lastWrittenPath = RenameLogStore.LastWrittenFilePath;
            var lastWrittenOnDisk =
                !lastWrittenPath.IsBlank()
                && diskPaths.Any(path => string.Equals(path, lastWrittenPath, StringComparison.OrdinalIgnoreCase));

            // Prefer the disk row when last op was written (Erase deletes the file), but seed its cache
            // with the in-memory log so opening the dialog after a large GO/Undo does not re-deserialize.
            if (last is not null && !lastWrittenOnDisk)
            {
                Items.Add(new RenameLogListItem(last));
            }

            foreach (var path in diskPaths)
            {
                var seedLastOp =
                    lastWrittenOnDisk
                    && last is not null
                    && string.Equals(path, lastWrittenPath, StringComparison.OrdinalIgnoreCase);
                Items.Add(seedLastOp ? new RenameLogListItem(path, cachedLog: last) : new RenameLogListItem(path));
            }

            SelectedItem = selectFirst && Items.Count > 0 ? Items[0] : null;
        }

        private void _RefreshDetails(RenameLogListItem? item)
        {
            if (item is null)
            {
                DetailsText = string.Empty;
                return;
            }

            var log = item.TryGetLog(out var errorMessage);
            if (log is null)
            {
                DetailsText = errorMessage ?? "Failed to load log.";
                return;
            }

            DetailsText = log.FormatDetails();
        }
    }
}

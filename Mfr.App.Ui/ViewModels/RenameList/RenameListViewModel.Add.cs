using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.RenameList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Engine.RenameList;
using Mfr.Models.Config;
using Serilog;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Add-from-File-List / drop pipeline for <see cref="RenameListViewModel"/>.
    /// </summary>
    public sealed partial class RenameListViewModel
    {
        /// <summary>
        /// Adds the File List selection to the Rename List.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelected))]
        public async Task AddSelectedAsync()
        {
            var sources = RenameListAddSourceResolver.ResolveSourcesFromSelection(
                _ToSourceItems(_fileListViewModel.SelectedEntries),
                _fileListViewModel.Mask,
                _SessionAddMode()
            );
            await _AddSourcesAsync(sources).ConfigureAwait(true);
        }

        /// <summary>
        /// Adds every listed File List row to the Rename List (same rules as Add Selected).
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddAll))]
        public async Task AddAllAsync()
        {
            var sources = RenameListAddSourceResolver.ResolveSourcesFromSelection(
                _ToSourceItems(_fileListViewModel.Entries),
                _fileListViewModel.Mask,
                _SessionAddMode()
            );
            await _AddSourcesAsync(sources).ConfigureAwait(true);
        }

        /// <summary>
        /// Adds dropped filesystem paths to the Rename List using the same rules as Add Selected.
        /// </summary>
        /// <param name="paths">Full file or folder paths from File List or Explorer drag-drop.</param>
        public async Task AddPathsAsync(IReadOnlyList<string> paths)
        {
            ArgumentNullException.ThrowIfNull(paths);

            var sources = RenameListAddSourceResolver.ResolveSourcesFromPaths(
                paths,
                _fileListViewModel.Mask,
                _SessionAddMode()
            );
            await _AddSourcesAsync(sources).ConfigureAwait(true);
        }

        /// <summary>
        /// Resolves sources into the engine, then mirrors a successful insert into <see cref="Entries"/>.
        /// </summary>
        private async Task _AddSourcesAsync(IReadOnlyList<string> sources)
        {
            if (sources.Count == 0 || IsAdding)
            {
                return;
            }

            var autoSort = IsAutoSort;
            var usedDropMark = !autoSort && DropMarkIndex is not null;
            var selectFirstAdded = !autoSort && (usedDropMark || _selectedEntries.Count > 0);
            var insertAt = _ResolveInsertAt();
            SetDropMarkIndex(null);
            var oldCount = _renameList.RenameItems.Count;
            LastAddError = string.Empty;

            var addMode = _SessionAddMode();
            var excludeMasks = _fileListViewModel.ExcludeMasksEnabled ? _fileListViewModel.ExcludeMasks : null;
            var metadataRequirement = _CurrentMetadataRequirement();
            var addSummary = new RenameListAddSummary(0);
            var completed = false;
            try
            {
                completed = await AddProgress
                    .RunAsync(
                        (token, progress) =>
                            addSummary = _renameList.AddSources(
                                sources: sources,
                                includeFiles: addMode.IncludesFiles(),
                                includeFolders: addMode.IncludesFolders(),
                                includeSubdirs: _SessionAddFolderContents(),
                                excludeMasks: excludeMasks,
                                cancellationToken: token,
                                progress: progress,
                                insertAtIndex: insertAt,
                                metadataRequirement: metadataRequirement
                            )
                    )
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                LastAddError = ex.Message;
                Log.Error(ex, "Unexpected failure while adding rename sources.");
            }

            if (!completed)
            {
                _RollbackAddedItems(insertAt, oldCount);
                _NotifyListChanged();
                return;
            }

            _SyncEntriesAfterAdd(insertAt, oldCount);
            if (autoSort)
            {
                _renameList.Sort(_sortKeys);
                _SyncEntriesToEngineOrder();
            }

            var addedCount = _renameList.RenameItems.Count - oldCount;
            if (selectFirstAdded && addedCount > 0)
            {
                SetSelectedEntries([Entries[insertAt]]);
            }

            LastAddError = _FormatAddOutcome(addedCount: addedCount, skippedSourceCount: addSummary.SkippedSourceCount);
            _LogAddOutcome(
                addedCount: addedCount,
                skippedSourceCount: addSummary.SkippedSourceCount,
                sourceCount: sources.Count
            );
            _NotifyListChanged();
        }

        /// <summary>
        /// Index to insert new rows: append when Auto-Sort; else before drop mark / after first selected / append.
        /// </summary>
        private int _ResolveInsertAt()
        {
            if (IsAutoSort)
            {
                return Entries.Count;
            }

            if (DropMarkIndex is { } markIndex)
            {
                return markIndex;
            }

            if (_selectedEntries.Count == 0)
            {
                return Entries.Count;
            }

            return _FindFirstSelectedIndex(_selectedEntries) + 1;
        }

        /// <summary>
        /// Builds the status-bar message after a completed add.
        /// </summary>
        private static string _FormatAddOutcome(int addedCount, int skippedSourceCount)
        {
            if (skippedSourceCount > 0)
            {
                return $"Added {addedCount} item(s). Skipped {skippedSourceCount} inaccessible source(s).";
            }

            if (addedCount == 0)
            {
                return "No items were added.";
            }

            return string.Empty;
        }

        /// <summary>
        /// Writes a batch summary when the add had skips or added nothing.
        /// </summary>
        private static void _LogAddOutcome(int addedCount, int skippedSourceCount, int sourceCount)
        {
            if (skippedSourceCount > 0)
            {
                Log.Warning(
                    "Rename list add finished. Added {AddedCount} item(s) from {SourceCount} source(s). Skipped {SkippedSourceCount} inaccessible source(s).",
                    addedCount,
                    sourceCount,
                    skippedSourceCount
                );
                return;
            }

            if (addedCount == 0)
            {
                Log.Warning("Rename list add finished with no items added from {SourceCount} source(s).", sourceCount);
            }
        }

        /// <summary>
        /// Mirrors an inserted engine add into <see cref="Entries"/>.
        /// </summary>
        /// <param name="insertAt">Index where the engine inserted the new items.</param>
        /// <param name="oldCount">Engine list size before this add; used to count new rows.</param>
        /// <remarks>
        /// <para>
        /// Called only after a successful <c>AddSources</c> that inserted its staging batch. Creates
        /// UI rows for the newly inserted engine items at the same index.
        /// </para>
        /// </remarks>
        private void _SyncEntriesAfterAdd(int insertAt, int oldCount)
        {
            var renameItems = _renameList.RenameItems;
            var addedCount = renameItems.Count - oldCount;
            if (addedCount <= 0)
            {
                return;
            }

            var newEntries = new List<RenameListEntry>(addedCount);
            for (var i = insertAt; i < insertAt + addedCount; i++)
            {
                newEntries.Add(RenameListEntry.ToEntry(renameItems[i]));
            }

            Entries.InsertRange(insertAt, newEntries);
        }

        /// <summary>
        /// Undoes an engine commit if cancel raced after the staging batch was inserted.
        /// </summary>
        /// <param name="insertAt">Index where the engine inserted the new items.</param>
        /// <param name="oldCount">Engine list size before this add; used to count new rows.</param>
        /// <remarks>
        /// <para>
        /// Usually a no-op: cancel discards the staging batch before it reaches <c>RenameItems</c>.
        /// This only runs when the walk finished and inserted, but the UI still saw cancel/failure.
        /// </para>
        /// </remarks>
        private void _RollbackAddedItems(int insertAt, int oldCount)
        {
            var renameItems = _renameList.RenameItems;
            var addedCount = renameItems.Count - oldCount;
            if (addedCount <= 0)
            {
                return;
            }

            _renameList.Remove([.. renameItems.Skip(insertAt).Take(addedCount)]);
        }

        private bool _CanAddSelected()
        {
            if (IsAdding)
            {
                return false;
            }

            return RenameListAddSourceResolver.CanResolveFromSelection(
                _ToSourceItems(_fileListViewModel.SelectedEntries),
                _fileListViewModel.Mask,
                _SessionAddMode()
            );
        }

        private bool _CanAddAll()
        {
            if (IsAdding)
            {
                return false;
            }

            // Sentinel gate: This PC / Network list Known Places and volumes; Add All would mass-add those.
            // Drive roots are fine — Add All walks listed children, not the root path itself.
            if (!RenameListAddSourceResolver.CanAddAllFrom(_fileListViewModel.CurrentPath))
            {
                return false;
            }

            return RenameListAddSourceResolver.CanResolveFromSelection(
                _ToSourceItems(_fileListViewModel.Entries),
                _fileListViewModel.Mask,
                _SessionAddMode()
            );
        }

        /// <summary>
        /// Add mode from session, or files when the Rename List section is absent.
        /// </summary>
        /// <returns>The current add mode without creating a session section.</returns>
        private static RenameListAddMode _SessionAddMode()
        {
            return SessionStore.Current.RenameList?.AddMode ?? RenameListAddMode.Files;
        }

        /// <summary>
        /// Folder-contents flag from session, or true when the Rename List section is absent.
        /// </summary>
        /// <returns>Whether folder sources recurse.</returns>
        private static bool _SessionAddFolderContents()
        {
            return SessionStore.Current.RenameList?.AddFolderContents ?? true;
        }

        private static IReadOnlyList<FileListSourceItem> _ToSourceItems(IEnumerable<FileListEntry> entries)
        {
            return [.. entries.Select(entry => new FileListSourceItem(entry.FullPath, entry.IsDirectory))];
        }
    }
}

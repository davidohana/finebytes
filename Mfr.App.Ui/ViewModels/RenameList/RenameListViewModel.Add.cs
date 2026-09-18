using CommunityToolkit.Mvvm.Input;
using Mfr.App.Ui.Services.FileList;
using Mfr.App.Ui.Services.RenameList;
using Mfr.App.Ui.ViewModels.FileList;
using Mfr.Engine.Config;
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
        /// Refreshes Add Selected / Add All can-execute after Options commits add policy to
        /// <see cref="ConfigStore"/>.
        /// </summary>
        internal void NotifyAddPolicyChanged()
        {
            AddSelectedCommand.NotifyCanExecuteChanged();
            AddAllCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Adds the File List selection to the Rename List.
        /// </summary>
        [RelayCommand(CanExecute = nameof(_CanAddSelected))]
        public async Task AddSelectedAsync()
        {
            var sources = RenameListAddSourceResolver.ResolveSourcesFromSelection(
                _ToSourceItems(_fileListViewModel.SelectedEntries),
                _fileListViewModel.Mask,
                _AddMode()
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
                _AddMode()
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
                _AddMode()
            );
            await _AddSourcesAsync(sources).ConfigureAwait(true);
        }

        /// <summary>
        /// Adds raw engine sources (paths or wildcards) for desktop startup seeding.
        /// </summary>
        /// <param name="sources">Positional source paths or wildcards (same shape as console <c>SOURCES</c>).</param>
        /// <param name="includeFiles">
        /// Add-files override; <see langword="null"/> uses Options <see cref="ConfigStore.Options"/> AddMode.
        /// </param>
        /// <param name="includeFolders">
        /// Add-folders override; <see langword="null"/> uses Options AddMode.
        /// </param>
        /// <param name="includeSubdirs">
        /// Recursive add override; <see langword="null"/> uses Options AddFolderContents.
        /// </param>
        /// <param name="includeHidden">
        /// Include-hidden override; <see langword="null"/> uses Options IncludeHidden.
        /// </param>
        /// <returns>Full path of the first added item, or <see langword="null"/> when nothing was added.</returns>
        /// <remarks>
        /// <para>
        /// Does not apply File List exclude masks (console-shaped argv seeding). Interactive Add Selected /
        /// Add Paths still honor exclude masks when enabled.
        /// </para>
        /// </remarks>
        internal async Task<string?> AddStartupSourcesAsync(
            IReadOnlyList<string> sources,
            bool? includeFiles = null,
            bool? includeFolders = null,
            bool? includeSubdirs = null,
            bool? includeHidden = null
        )
        {
            ArgumentNullException.ThrowIfNull(sources);

            return await _AddSourcesAsync(
                    sources,
                    includeFilesOverride: includeFiles,
                    includeFoldersOverride: includeFolders,
                    includeSubdirsOverride: includeSubdirs,
                    includeHiddenOverride: includeHidden,
                    applyFileListExcludeMasks: false
                )
                .ConfigureAwait(true);
        }

        /// <summary>
        /// Resolves sources into the engine, then mirrors a successful insert into <see cref="Entries"/>.
        /// </summary>
        /// <param name="sources">Engine add sources (paths, folder+mask, or wildcards).</param>
        /// <param name="includeFilesOverride">
        /// Optional files include; <see langword="null"/> uses Options AddMode.
        /// </param>
        /// <param name="includeFoldersOverride">
        /// Optional folders include; <see langword="null"/> uses Options AddMode.
        /// </param>
        /// <param name="includeSubdirsOverride">
        /// Optional recursive flag; <see langword="null"/> uses Options AddFolderContents.
        /// </param>
        /// <param name="includeHiddenOverride">
        /// Optional hidden include; <see langword="null"/> uses Options IncludeHidden.
        /// </param>
        /// <param name="applyFileListExcludeMasks">
        /// When <see langword="true"/>, uses File List exclude masks if enabled; when
        /// <see langword="false"/>, never excludes (startup / console-shaped seeding).
        /// </param>
        /// <returns>Full path of the first added item, or <see langword="null"/> when nothing was added.</returns>
        private async Task<string?> _AddSourcesAsync(
            IReadOnlyList<string> sources,
            bool? includeFilesOverride = null,
            bool? includeFoldersOverride = null,
            bool? includeSubdirsOverride = null,
            bool? includeHiddenOverride = null,
            bool applyFileListExcludeMasks = true
        )
        {
            if (IsBusy)
            {
                LastStatusMessage = StatusBarText.Warning("Rename List is busy.");
                return null;
            }

            if (sources.Count == 0)
            {
                LastStatusMessage = StatusBarText.Warning("No items were added.");
                return null;
            }

            var autoSort = IsAutoSort;
            var usedDropMark = !autoSort && DropMarkIndex is not null;
            var selectFirstAdded = !autoSort && (usedDropMark || _selectedEntries.Count > 0);
            var insertAt = _ResolveInsertAt();
            SetDropMarkIndex(null);
            var oldCount = _renameList.RenameItems.Count;

            var addMode = _AddMode();
            var includeFiles = includeFilesOverride ?? addMode.IncludesFiles();
            var includeFolders = includeFoldersOverride ?? addMode.IncludesFolders();
            var includeSubdirs = includeSubdirsOverride ?? _AddFolderContents();
            var includeHidden = includeHiddenOverride ?? _IncludeHidden();
            var excludeMasks =
                applyFileListExcludeMasks && _fileListViewModel.ExcludeMasksEnabled
                    ? _fileListViewModel.ExcludeMasks
                    : null;
            var metadataRequirement = _CurrentMetadataRequirement();
            var addSummary = new RenameListAddSummary(0);
            void OnAddCanceled()
            {
                _RollbackAddedItems(insertAt, oldCount);
                _NotifyListChangedAfterAdd(oldCount);
            }

            var completed = false;
            try
            {
                completed = await _RunProgressAsync(
                        RenameListProgressOperation.Add,
                        (token, progress) =>
                            addSummary = _renameList.AddSources(
                                sources: sources,
                                includeFiles: includeFiles,
                                includeFolders: includeFolders,
                                includeSubdirs: includeSubdirs,
                                includeHidden: includeHidden,
                                excludeMasks: excludeMasks,
                                cancellationToken: token,
                                progress: progress,
                                insertAtIndex: insertAt,
                                metadataRequirement: metadataRequirement
                            ),
                        onCancel: OnAddCanceled
                    )
                    .ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                LastStatusMessage = StatusBarText.Error(ex.Message);
                Log.Error(ex, "Unexpected failure while adding rename sources.");
                OnAddCanceled();
                return null;
            }

            if (!completed)
            {
                return null;
            }

            _SyncEntriesAfterAdd(insertAt, oldCount);
            var addedCount = _renameList.RenameItems.Count - oldCount;
            string? firstAddedPath = null;
            if (addedCount > 0)
            {
                firstAddedPath = Entries[insertAt].FullPath;
            }

            if (autoSort)
            {
                _renameList.Sort(_sortKeys);
                _SyncEntriesToEngineOrder();
            }

            if (selectFirstAdded && addedCount > 0)
            {
                SetSelectedEntries([Entries[insertAt]]);
            }

            LastStatusMessage = _FormatAddOutcome(
                addedCount: addedCount,
                skippedSourceCount: addSummary.SkippedSourceCount
            );
            _LogAddOutcome(
                addedCount: addedCount,
                skippedSourceCount: addSummary.SkippedSourceCount,
                sourceCount: sources.Count
            );
            _NotifyListChangedAfterAdd(oldCount);

            return firstAddedPath;
        }

        /// <summary>
        /// Updates list chrome after add or cancel, and raises membership only when the engine count changed.
        /// </summary>
        private void _NotifyListChangedAfterAdd(int oldCount)
        {
            if (_renameList.RenameItems.Count == oldCount)
            {
                _NotifyListChanged();
                return;
            }

            _NotifyMembershipChanged();
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
        private static StyledTextDisplay _FormatAddOutcome(int addedCount, int skippedSourceCount)
        {
            if (skippedSourceCount > 0)
            {
                return StatusBarText.Warning(
                    $"Added {addedCount} item(s). Skipped {skippedSourceCount} inaccessible source(s)."
                );
            }

            if (addedCount == 0)
            {
                return StatusBarText.Warning("No items were added.");
            }

            return StatusBarText.Neutral($"Added {addedCount} item(s).");
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
            if (IsBusy)
            {
                return false;
            }

            return RenameListAddSourceResolver.CanResolveFromSelection(
                _ToSourceItems(_fileListViewModel.SelectedEntries),
                _fileListViewModel.Mask,
                _AddMode()
            );
        }

        private bool _CanAddAll()
        {
            if (IsBusy || _fileListViewModel.IsListing)
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
                _AddMode()
            );
        }

        /// <summary>
        /// Options-owned add mode from <see cref="ConfigStore.Options"/>.
        /// </summary>
        private static RenameListAddMode _AddMode()
        {
            return ConfigStore.Options.AddMode;
        }

        /// <summary>
        /// Options-owned folder-contents flag from <see cref="ConfigStore.Options"/>.
        /// </summary>
        private static bool _AddFolderContents()
        {
            return ConfigStore.Options.AddFolderContents;
        }

        /// <summary>
        /// Options-owned Hidden|System include flag from <see cref="ConfigStore.Options"/>.
        /// </summary>
        private static bool _IncludeHidden()
        {
            return ConfigStore.Options.IncludeHidden;
        }

        private static IReadOnlyList<FileListSourceItem> _ToSourceItems(IEnumerable<FileListEntry> entries)
        {
            return [.. entries.Select(entry => new FileListSourceItem(entry.FullPath, entry.IsDirectory))];
        }
    }
}

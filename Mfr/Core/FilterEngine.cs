using Mfr.Models;

namespace Mfr.Core
{
    public enum RenameStatus
    {
        Ok,
        Skipped,
        ConflictSkipped,
        Error
    }

    public sealed record RenameResultItem(
        string OriginalPath,
        string ResultPath,
        RenameStatus Status,
        string? Error);

    public sealed record RenameBatchResult(
        string PresetName,
        int TotalFiles,
        int Renamed,
        int Skipped,
        int Conflicts,
        int Errors,
        IReadOnlyList<RenameResultItem> Results);

    /// <summary>
    /// Applies configured filters to file names and commits non-conflicting rename operations.
    /// </summary>
    public static partial class FilterEngine
    {
        /// <summary>
        /// Previews rename outcomes for a batch without touching the filesystem.
        /// </summary>
        /// <param name="preset">The rename preset (sequence of enabled filters).</param>
        /// <param name="renameItems">Candidate files to rename.</param>
        /// <param name="failFast">If <c>true</c>, stop previewing after the first per-item error.</param>
        /// <returns>Preview summary with computed destination paths and preview errors.</returns>
        public static RenameBatchResult Preview(
            FilterPreset preset,
            IReadOnlyList<RenameItem> renameItems,
            bool failFast)
        {
            var previewResults = new List<RenameResultItem>(renameItems.Count);
            foreach (var renameItem in renameItems)
            {
                try
                {
                    renameItem.ApplyFilters(preset.Filters);
                    var preview = renameItem.Preview ?? throw new InvalidOperationException("Preview not generated");
                    var destPath = preview.FullPath;

                    if (string.Equals(destPath, renameItem.Original.FullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        previewResults.Add(new RenameResultItem(
                            OriginalPath: renameItem.Original.FullPath,
                            ResultPath: destPath,
                            Status: RenameStatus.Skipped,
                            Error: null));
                        continue;
                    }

                    previewResults.Add(new RenameResultItem(
                        OriginalPath: renameItem.Original.FullPath,
                        ResultPath: destPath,
                        Status: RenameStatus.Skipped,
                        Error: null));
                }
                catch (Exception ex)
                {
                    previewResults.Add(new RenameResultItem(
                        OriginalPath: renameItem.Original.FullPath,
                        ResultPath: renameItem.Original.FullPath,
                        Status: RenameStatus.Error,
                        Error: ex.Message));
                    if (failFast)
                    {
                        return _Summarize(preset.Name, renameItems.Count, previewResults);
                    }
                }
            }

            return _Summarize(preset.Name, renameItems.Count, previewResults);
        }

        /// <summary>
        /// Commits previously previewed rename operations, skipping conflicts.
        /// </summary>
        /// <param name="presetName">Preset name used for summary output.</param>
        /// <param name="renameItem">Candidate files with preview paths already computed.</param>
        /// <param name="failFast">If <c>true</c>, stop committing after the first per-item error.</param>
        /// <returns>Commit summary including renamed, skipped, conflict, and error counts.</returns>
        public static RenameBatchResult Commit(
            string presetName,
            IReadOnlyList<RenameItem> renameItem,
            bool failFast)
        {
            var commitResults = new List<RenameResultItem>(renameItem.Count);
            var pending = new List<RenameItem>(renameItem.Count);
            foreach (var item in renameItem)
            {
                var sourcePath = item.Original.FullPath;
                var preview = item.Preview;
                if (preview is null)
                {
                    commitResults.Add(new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: sourcePath,
                        Status: RenameStatus.Skipped,
                        Error: null));
                    continue;
                }

                var destPath = preview.FullPath;
                if (string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
                {
                    commitResults.Add(new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: destPath,
                        Status: RenameStatus.Skipped,
                        Error: null));
                    continue;
                }

                commitResults.Add(new RenameResultItem(
                    OriginalPath: sourcePath,
                    ResultPath: destPath,
                    Status: RenameStatus.Skipped,
                    Error: null));
                pending.Add(item);
            }

            // 2) Resolve conflicts among pending destinations and against disk.
            var destToFiles = pending.Where(p => p.Preview is not null)
                .GroupBy(p => p.Preview!.FullPath, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var conflictDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in pending)
            {
                var preview = item.Preview;
                if (preview is null)
                {
                    continue;
                }

                var destPath = preview.FullPath;
                if (File.Exists(destPath))
                {
                    _ = conflictDestinations.Add(destPath);
                }

                if (destToFiles.ContainsKey(destPath))
                {
                    _ = conflictDestinations.Add(destPath);
                }
            }

            // 3) Commit non-conflicting renames.
            var renamedCount = 0;
            var resultIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < commitResults.Count; i++)
            {
                resultIndex[commitResults[i].OriginalPath] = i;
            }

            foreach (var item in pending)
            {
                var sourcePath = item.Original.FullPath;
                var preview = item.Preview;
                if (preview is null)
                {
                    continue;
                }

                var destPath = preview.FullPath;
                var idx = resultIndex[sourcePath];
                if (conflictDestinations.Contains(destPath))
                {
                    commitResults[idx] = new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: destPath,
                        Status: RenameStatus.ConflictSkipped,
                        Error: null);
                    continue;
                }

                try
                {
                    item.Apply();
                    commitResults[idx] = new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: destPath,
                        Status: RenameStatus.Ok,
                        Error: null);
                    renamedCount++;
                }
                catch (Exception ex)
                {
                    commitResults[idx] = new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: destPath,
                        Status: RenameStatus.Error,
                        Error: ex.Message);
                    if (failFast)
                    {
                        break;
                    }
                }
            }

            foreach (var item in renameItem)
            {
                item.ResetPreview();
            }

            return _Summarize(presetName, renameItem.Count, commitResults, renamedCount);
        }

        private static RenameBatchResult _Summarize(
            string presetName,
            int totalFiles,
            IReadOnlyList<RenameResultItem> results,
            int renamedCount = 0)
        {
            var renamed = results.Count(r => r.Status == RenameStatus.Ok);
            var skipped = results.Count(r => r.Status == RenameStatus.Skipped);
            var conflicts = results.Count(r => r.Status == RenameStatus.ConflictSkipped);
            var errors = results.Count(r => r.Status == RenameStatus.Error);
            return new RenameBatchResult(presetName, totalFiles, renamed, skipped, conflicts, errors, results);
        }

    }

}

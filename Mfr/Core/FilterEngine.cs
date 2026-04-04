using Mfr.Models;

namespace Mfr.Core
{
    public sealed record RenameResultItem(
        string OriginalPath,
        string ResultPath,
        RenameStatus Status,
        string? Error);

    /// <summary>
    /// Applies configured filters to file names and commits non-conflicting rename operations.
    /// </summary>
    public static partial class FilterEngine
    {
        /// <summary>
        /// Commits previously previewed rename operations, skipping conflicts.
        /// </summary>
        /// <param name="renameItem">Candidate files with preview paths already computed.</param>
        /// <param name="failFast">If <c>true</c>, stop committing after the first per-item error.</param>
        /// <returns>Per-item commit outcomes including success, skipped, conflict, and errors.</returns>
        public static IReadOnlyList<RenameResultItem> Commit(
            IReadOnlyList<RenameItem> renameItem,
            bool failFast)
        {
            var commitResults = new List<RenameResultItem>(renameItem.Count);
            var pending = new List<RenameItem>(renameItem.Count);
            foreach (var item in renameItem)
            {
                item.ResetCommitError();
                var sourcePath = item.Original.FullPath;
                var preview = item.Preview;
                if (preview is null)
                {
                    item.Status = RenameStatus.CommitSkipped;
                    commitResults.Add(new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: sourcePath,
                        Status: RenameStatus.CommitSkipped,
                        Error: null));
                    continue;
                }

                var destPath = preview.FullPath;
                if (string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
                {
                    item.Status = RenameStatus.CommitSkipped;
                    commitResults.Add(new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: destPath,
                        Status: RenameStatus.CommitSkipped,
                        Error: null));
                    continue;
                }

                item.Status = RenameStatus.CommitSkipped;
                commitResults.Add(new RenameResultItem(
                    OriginalPath: sourcePath,
                    ResultPath: destPath,
                    Status: RenameStatus.CommitSkipped,
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
                    item.Status = RenameStatus.CommitConflictSkipped;
                    commitResults[idx] = new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: destPath,
                        Status: RenameStatus.CommitConflictSkipped,
                        Error: null);
                    continue;
                }

                try
                {
                    item.CommitPreview();
                    item.Status = RenameStatus.CommitOk;
                    commitResults[idx] = new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: destPath,
                        Status: RenameStatus.CommitOk,
                        Error: null);
                }
                catch (Exception ex)
                {
                    item.CommitError = new RenameItemError(Message: ex.Message, Cause: ex);
                    item.Status = RenameStatus.CommitError;
                    commitResults[idx] = new RenameResultItem(
                        OriginalPath: sourcePath,
                        ResultPath: destPath,
                        Status: RenameStatus.CommitError,
                        Error: item.CommitError.Message);
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

            return commitResults;
        }

    }

}

using Mfr.Models;
using Mfr.Models.Filters;

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
        /// <param name="files">Candidate files to rename.</param>
        /// <param name="continueOnErrors">If <c>true</c>, continue previewing after per-item errors.</param>
        /// <returns>Preview summary with computed destination paths and preview errors.</returns>
        public static RenameBatchResult Preview(
            FilterPreset preset,
            IReadOnlyList<RenameItem> files,
            bool continueOnErrors)
        {
            var previewResults = new List<RenameResultItem>(files.Count);
            foreach (var item in files)
            {
                try
                {
                    _ApplyFiltersToName(preset.Filters, item);
                    var destPath = item.Preview.FullPath;

                    if (string.Equals(destPath, item.Original.FullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        previewResults.Add(new RenameResultItem(item.Original.FullPath, destPath, RenameStatus.Skipped, null));
                        continue;
                    }

                    previewResults.Add(new RenameResultItem(item.Original.FullPath, destPath, RenameStatus.Skipped, null));
                }
                catch (Exception ex)
                {
                    previewResults.Add(new RenameResultItem(item.Original.FullPath, item.Original.FullPath, RenameStatus.Error, ex.Message));
                    if (!continueOnErrors)
                    {
                        return _Summarize(preset.Name, files.Count, previewResults);
                    }
                }
            }

            return _Summarize(preset.Name, files.Count, previewResults);
        }

        /// <summary>
        /// Commits previously previewed rename operations, skipping conflicts.
        /// </summary>
        /// <param name="presetName">Preset name used for summary output.</param>
        /// <param name="files">Candidate files with preview paths already computed.</param>
        /// <param name="continueOnErrors">If <c>true</c>, continue committing after per-item errors.</param>
        /// <returns>Commit summary including renamed, skipped, conflict, and error counts.</returns>
        public static RenameBatchResult Commit(
            string presetName,
            IReadOnlyList<RenameItem> files,
            bool continueOnErrors)
        {
            var commitResults = new List<RenameResultItem>(files.Count);
            var pending = new List<RenameItem>(files.Count);
            foreach (var item in files)
            {
                var sourcePath = item.Original.FullPath;
                var destPath = item.Preview.FullPath;
                if (string.Equals(sourcePath, destPath, StringComparison.OrdinalIgnoreCase))
                {
                    commitResults.Add(new RenameResultItem(sourcePath, destPath, RenameStatus.Skipped, null));
                    continue;
                }

                commitResults.Add(new RenameResultItem(sourcePath, destPath, RenameStatus.Skipped, null));
                pending.Add(item);
            }

            // 2) Resolve conflicts among pending destinations and against disk.
            var destToFiles = pending.GroupBy(p => p.Preview.FullPath, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var conflictDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in pending)
            {
                var destPath = item.Preview.FullPath;
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
                var destPath = item.Preview.FullPath;
                var idx = resultIndex[sourcePath];
                if (conflictDestinations.Contains(destPath))
                {
                    commitResults[idx] = new RenameResultItem(sourcePath, destPath, RenameStatus.ConflictSkipped, null);
                    continue;
                }

                try
                {
                    item.Apply();
                    commitResults[idx] = new RenameResultItem(sourcePath, destPath, RenameStatus.Ok, null);
                    renamedCount++;
                }
                catch (Exception ex)
                {
                    commitResults[idx] = new RenameResultItem(sourcePath, destPath, RenameStatus.Error, ex.Message);
                    if (!continueOnErrors)
                    {
                        break;
                    }
                }
            }

            return _Summarize(presetName, files.Count, commitResults, renamedCount);
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

        private static void _ApplyFiltersToName(IReadOnlyList<Filter> filters, RenameItem item)
        {
            item.ResetPreview();
            var prefix = item.Original.Prefix;
            var extension = item.Original.Extension;

            foreach (var filter in filters)
            {
                if (!filter.Enabled)
                {
                    continue;
                }

                if (filter.Target is not FileNameTarget fileTarget)
                {
                    throw new NotSupportedException($"Phase 1 only supports target.family='FileName'. Filter '{filter.Type}' got '{filter.Target.Family}'.");
                }

                var mode = fileTarget.FileNameMode;
                var segment = mode switch
                {
                    FileNameTargetMode.Prefix => prefix,
                    FileNameTargetMode.Extension => extension,
                    FileNameTargetMode.Full => prefix + extension,
                    _ => throw new InvalidOperationException($"Unknown fileNameMode '{mode}'.")
                };

                var transformed = filter.Apply(segment, item);

                switch (mode)
                {
                    case FileNameTargetMode.Prefix:
                        prefix = transformed;
                        break;
                    case FileNameTargetMode.Extension:
                        extension = transformed;
                        break;
                    case FileNameTargetMode.Full:
                        var fullName = Path.GetFileName(transformed);
                        extension = Path.GetExtension(fullName);
                        prefix = Path.GetFileNameWithoutExtension(fullName);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown fileNameMode '{mode}'.");
                }
            }

            item.SetPreviewName(prefix, extension);
        }

    }

}

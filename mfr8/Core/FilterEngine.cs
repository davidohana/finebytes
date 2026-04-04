using Mfr8.Models;
using Mfr8.Models.Filters;

namespace Mfr8.Core
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
        /// Previews rename outcomes for a batch and then commits non-conflicting moves.
        /// </summary>
        /// <param name="preset">The rename preset (sequence of enabled filters).</param>
        /// <param name="files">Candidate files to rename.</param>
        /// <param name="continueOnErrors">If <c>true</c>, continue when preview/commit errors occur.</param>
        /// <returns>Summary of rename outcomes (renamed, skipped, conflicts, errors).</returns>
        public static RenameBatchResult PreviewAndCommit(
            FilterPreset preset,
            IReadOnlyList<FileEntryLite> files,
            bool continueOnErrors)
        {
            // Phase 1: Conflict strategy is `Skip` (no auto-number, no overwrite).
            var previewResults = new List<RenameResultItem>(files.Count);
            var pending = new List<(FileEntryLite file, string destPath)>(files.Count);

            // 1) Preview and compute destinations (or preview errors).
            foreach (var file in files)
            {
                try
                {
                    (var prefix, var extension) = _ApplyFiltersToName(preset.Filters, file);
                    var finalFileName = prefix + extension;
                    var destPath = Path.Combine(file.DirectoryPath, finalFileName);

                    if (string.Equals(destPath, file.FullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        previewResults.Add(new RenameResultItem(file.FullPath, destPath, RenameStatus.Skipped, null));
                        continue;
                    }

                    previewResults.Add(new RenameResultItem(file.FullPath, destPath, RenameStatus.Skipped, null));
                    pending.Add((file, destPath));
                }
                catch (Exception ex)
                {
                    previewResults.Add(new RenameResultItem(file.FullPath, file.FullPath, RenameStatus.Error, ex.Message));
                    if (!continueOnErrors)
                    {
                        return _Summarize(preset.Name, files.Count, previewResults);
                    }
                }
            }

            // If there were preview errors and /COPE is not enabled, do not commit.
            if (previewResults.Any(r => r.Status == RenameStatus.Error))
            {
                return _Summarize(preset.Name, files.Count, previewResults);
            }

            // 2) Resolve conflicts among pending destinations and against disk.
            var destToFiles = pending.GroupBy(p => p.destPath, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var conflictDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach ((var file, var destPath) in pending)
            {
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
            for (var i = 0; i < previewResults.Count; i++)
            {
                resultIndex[previewResults[i].OriginalPath] = i;
            }

            foreach ((var file, var destPath) in pending)
            {
                var idx = resultIndex[file.FullPath];
                if (conflictDestinations.Contains(destPath))
                {
                    previewResults[idx] = new RenameResultItem(file.FullPath, destPath, RenameStatus.ConflictSkipped, null);
                    continue;
                }

                try
                {
                    _CommitMove(file.FullPath, destPath);
                    previewResults[idx] = new RenameResultItem(file.FullPath, destPath, RenameStatus.Ok, null);
                    renamedCount++;
                }
                catch (Exception ex)
                {
                    previewResults[idx] = new RenameResultItem(file.FullPath, destPath, RenameStatus.Error, ex.Message);
                    if (!continueOnErrors)
                    {
                        break;
                    }
                }
            }

            return _Summarize(preset.Name, files.Count, previewResults, renamedCount);
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

        private static (string prefix, string extension) _ApplyFiltersToName(IReadOnlyList<Filter> filters, FileEntryLite file)
        {
            var prefix = file.Prefix;
            var extension = file.Extension;

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

                var transformed = filter.Apply(segment, file);

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

            return (prefix, extension);
        }

        private static void _CommitMove(string sourcePath, string destPath)
        {
            // Keep it simple for phase 1: try Move first, fallback to Copy+Delete.
            try
            {
                File.Move(sourcePath, destPath, overwrite: false);
            }
            catch (IOException)
            {
                // Cross-volume or other move failure -> fallback.
                File.Copy(sourcePath, destPath, overwrite: false);
                File.Delete(sourcePath);
            }
        }

    }

}

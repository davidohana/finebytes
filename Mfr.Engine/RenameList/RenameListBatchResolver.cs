using Mfr.Utils;
using Serilog;

namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Include flags and masks for one <see cref="RenameList.AddSources"/> call.
    /// </summary>
    internal readonly record struct SourceResolveOptions(
        bool IncludeFiles,
        bool IncludeFolders,
        bool IncludeSubdirs,
        bool IncludeHidden,
        IReadOnlyList<string>? ExcludeMasks
    );

    /// <summary>
    /// Orchestrates the resolution of input paths and masks into a staging batch of <see cref="RenameItem"/>s.
    /// </summary>
    internal static class RenameListBatchResolver
    {
        /// <summary>
        /// Resolves and adds a list of string sources into a staging batch of <see cref="RenameItem"/>s.
        /// </summary>
        /// <param name="sourceList">The sources to resolve (files, directories, masks).</param>
        /// <param name="resolveOptions">The inclusion/exclusion options to apply during resolution.</param>
        /// <param name="tracker">The tracker for progress and cancellation.</param>
        /// <param name="batch">The target list to fill with resolved items.</param>
        /// <param name="includedResolvedPaths">The set of existing normalized paths to avoid duplicates.</param>
        /// <returns>The number of sources that were skipped due to errors.</returns>
        public static int FillBatch(
            List<string> sourceList,
            SourceResolveOptions resolveOptions,
            RenameListProgressTracker tracker,
            List<RenameItem> batch,
            HashSet<string> includedResolvedPaths
        )
        {
            var skippedSourceCount = 0;
            foreach (var source in sourceList)
            {
                if (tracker.IsCanceled)
                {
                    break;
                }

                if (!_TryAddSource(source, resolveOptions, tracker, batch, includedResolvedPaths))
                {
                    skippedSourceCount++;
                }
            }

            return skippedSourceCount;
        }

        private static bool _TryAddSource(
            string source,
            SourceResolveOptions resolveOptions,
            RenameListProgressTracker tracker,
            List<RenameItem> batch,
            HashSet<string> includedResolvedPaths
        )
        {
            try
            {
                _AddSource(source, resolveOptions, tracker, batch, includedResolvedPaths);
                return true;
            }
            catch (Exception ex)
                when (ex is UserException or IOException or UnauthorizedAccessException or ArgumentException)
            {
                Log.Warning(ex, "Skipped rename source '{Source}'.", source);
                return false;
            }
        }

        private static void _AddSource(
            string source,
            SourceResolveOptions resolveOptions,
            RenameListProgressTracker tracker,
            List<RenameItem> batch,
            HashSet<string> includedResolvedPaths
        )
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new UserException("Source cannot be empty.");
            }

            var trimmedSource = source.Trim();
            var fullSource = Path.GetFullPath(trimmedSource);
            var isRootPath = string.Equals(Path.GetPathRoot(fullSource), fullSource, PathComparers.OsComparison);
            if (isRootPath)
            {
                throw new UserException($"Root paths cannot be added as rename sources: '{trimmedSource}'.");
            }

            var resolvedPaths = AddedSourceResolver.ResolveToPaths(
                source: trimmedSource,
                includeFiles: resolveOptions.IncludeFiles,
                includeFolders: resolveOptions.IncludeFolders,
                includeSubdirs: resolveOptions.IncludeSubdirs,
                excludeMasks: resolveOptions.ExcludeMasks,
                cancellationToken: tracker.Token
            );
            var addedCount = _CollectResolvedItems(
                resolvedPaths: resolvedPaths,
                resolveOptions: resolveOptions,
                tracker: tracker,
                batch: batch,
                includedResolvedPaths: includedResolvedPaths
            );
            Log.Information(
                "Resolved source '{Source}', added {AddedCount} new item(s) (scanned {ScannedCount}).",
                trimmedSource,
                addedCount,
                tracker.ScannedCount
            );
        }

        private static int _CollectResolvedItems(
            IEnumerable<string> resolvedPaths,
            SourceResolveOptions resolveOptions,
            RenameListProgressTracker tracker,
            List<RenameItem> batch,
            HashSet<string> includedResolvedPaths
        )
        {
            var addedCount = 0;
            foreach (var fullPath in resolvedPaths)
            {
                if (tracker.IsCanceled)
                {
                    return addedCount;
                }

                tracker.OnScanned(fullPath);

                var renameItem = _TryCreateResolvedItem(
                    fullPath: fullPath,
                    resolveOptions: resolveOptions,
                    includedResolvedPaths: includedResolvedPaths
                );
                if (renameItem is null)
                {
                    continue;
                }

                batch.Add(renameItem);
                tracker.OnAdded(fullPath);
                addedCount++;
            }

            return addedCount;
        }

        private static RenameItem? _TryCreateResolvedItem(
            string fullPath,
            SourceResolveOptions resolveOptions,
            HashSet<string> includedResolvedPaths
        )
        {
            var normalizedResolvedPath = RenameList.NormalizePathKey(fullPath);
            if (includedResolvedPaths.Contains(normalizedResolvedPath))
            {
                return null;
            }

            var attrs = File.GetAttributes(fullPath);
            if (
                !_ShouldIncludeResolvedPath(
                    fullPath: fullPath,
                    normalizedResolvedPath: normalizedResolvedPath,
                    attrs: attrs,
                    resolveOptions: resolveOptions
                )
            )
            {
                return null;
            }

            includedResolvedPaths.Add(normalizedResolvedPath);
            return RenameItemSnapshotBuilder.CreateRenameItem(fullPath, attrs);
        }

        private static bool _ShouldIncludeResolvedPath(
            string fullPath,
            string normalizedResolvedPath,
            FileAttributes attrs,
            SourceResolveOptions resolveOptions
        )
        {
            var isHiddenOrSystem = attrs.HasFlag(FileAttributes.Hidden) || attrs.HasFlag(FileAttributes.System);
            if (!resolveOptions.IncludeHidden && isHiddenOrSystem)
            {
                return false;
            }

            var isDirectory = attrs.IsDirectory();
            if (isDirectory && !resolveOptions.IncludeFolders)
            {
                return false;
            }

            if (!isDirectory && !resolveOptions.IncludeFiles)
            {
                return false;
            }

            if (!isDirectory)
            {
                return true;
            }

            var resolvedRoot = Path.GetPathRoot(fullPath) ?? string.Empty;
            var isResolvedRootPath = string.Equals(
                RenameList.NormalizePathKey(resolvedRoot),
                normalizedResolvedPath,
                StringComparison.Ordinal
            );
            if (!isResolvedRootPath)
            {
                return true;
            }

            Log.Warning("Skipping root path '{Path}': root paths cannot be renamed.", fullPath);
            return false;
        }
    }
}

namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Coordinates re-stating on-disk properties for existing <see cref="RenameItem"/>s,
    /// resolving casing differences, and maintaining the deduplication path set.
    /// </summary>
    internal static class RenameListOriginalsRefresher
    {
        /// <summary>
        /// Refreshes a single <see cref="RenameItem"/> with updated original properties from the filesystem,
        /// managing path deduplication and metadata caches.
        /// </summary>
        /// <param name="item">The item to refresh.</param>
        /// <param name="casingCache">The shared casing cache for resolving on-disk casing.</param>
        /// <param name="includedResolvedPaths">The set of active paths to update if casing changes.</param>
        public static void RefreshItemOriginal(
            RenameItem item,
            OnDiskCasingCache casingCache,
            HashSet<string> includedResolvedPaths
        )
        {
            var priorPath = item.Original.FullPath;
            item.ClearMetadataCaches();

            var resolvedPath = _ResolveExistingPath(priorPath, casingCache);
            if (resolvedPath is null)
            {
                item.SetMissingFromDisk(true);
                return;
            }

            item.SetMissingFromDisk(false);

            var priorKey = RenameList.NormalizePathKey(priorPath);
            var resolvedKey = RenameList.NormalizePathKey(resolvedPath);
            if (!string.Equals(priorKey, resolvedKey, StringComparison.Ordinal))
            {
                includedResolvedPaths.Remove(priorKey);
                includedResolvedPaths.Add(resolvedKey);
            }

            var refreshedOriginal = RenameItemSnapshotBuilder.CreateOriginalSnapshot(
                resolvedPath,
                File.GetAttributes(resolvedPath)
            );
            var original = item.Original;
            refreshedOriginal.RenameListIndex = original.RenameListIndex;
            refreshedOriginal.InFolderIndex = original.InFolderIndex;
            refreshedOriginal.RenameListTotalCount = original.RenameListTotalCount;
            refreshedOriginal.RenameListFolderSiblingCount = original.RenameListFolderSiblingCount;
            item.Original = refreshedOriginal;
        }

        private static string? _ResolveExistingPath(string path, OnDiskCasingCache casingCache)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                return casingCache.Resolve(path);
            }

            return null;
        }
    }
}

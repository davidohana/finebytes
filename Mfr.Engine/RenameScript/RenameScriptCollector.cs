namespace Mfr.Engine.RenameScript
{
    /// <summary>
    /// Builds rename-script IR from Rename List rows (path + RAHS only; skips errors and tag/date-only rows).
    /// </summary>
    public static class RenameScriptCollector
    {
        /// <summary>
        /// Collects script operations for each scriptable row, preserving list order.
        /// </summary>
        /// <param name="items">Rename items in list order.</param>
        /// <returns>
        /// One entry per scriptable item; each entry lists path op (when any) then attrs op (when any).
        /// </returns>
        public static IReadOnlyList<IReadOnlyList<RenameScriptOp>> Collect(IEnumerable<RenameItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            var groups = new List<IReadOnlyList<RenameScriptOp>>();
            foreach (var item in items)
            {
                var ops = _TryCollectItem(item);
                if (ops is null)
                {
                    continue;
                }

                groups.Add(ops);
            }

            return groups;
        }

        private static List<RenameScriptOp>? _TryCollectItem(RenameItem item)
        {
            if (item.PreviewError is not null || item.Status == RenameStatus.PreviewError)
            {
                return null;
            }

            var pathChanged = !item.IsPreviewPathUnchanged();
            var (setFlags, clearFlags) = _RahsDelta(item.Original.Attributes, item.Preview.Attributes);
            var attrsChanged = setFlags != 0 || clearFlags != 0;
            if (!pathChanged && !attrsChanged)
            {
                return null;
            }

            var ops = new List<RenameScriptOp>(2);
            if (pathChanged)
            {
                ops.Add(_CreatePathOp(item));
            }

            if (attrsChanged)
            {
                ops.Add(new SetRahsAttributes(item.Preview.FullPath, setFlags, clearFlags));
            }

            return ops;
        }

        /// <summary>
        /// Same-parent ordinal path → <see cref="RenameSameFolder"/> (includes case-only); otherwise <see cref="MoveWithParent"/>.
        /// </summary>
        private static RenameScriptOp _CreatePathOp(RenameItem item)
        {
            var originalDir = item.Original.DirectoryPath;
            var previewDir = item.Preview.DirectoryPath;
            var sameFolder = string.Equals(originalDir, previewDir, StringComparison.Ordinal);
            if (sameFolder)
            {
                return new RenameSameFolder(item.Original.FullPath, item.Preview.FullFileName);
            }

            return new MoveWithParent(
                SourceFullPath: item.Original.FullPath,
                DestinationDirectory: previewDir,
                DestinationFullPath: item.Preview.FullPath
            );
        }

        /// <summary>
        /// RAHS bits to turn on/off so preview matches original for the MFR7 attribute set only.
        /// </summary>
        private static (FileAttributes SetFlags, FileAttributes ClearFlags) _RahsDelta(
            FileAttributes original,
            FileAttributes preview
        )
        {
            var originalRahs = original & FileAttributesRahs.Mask;
            var previewRahs = preview & FileAttributesRahs.Mask;
            var setFlags = previewRahs & ~originalRahs;
            var clearFlags = originalRahs & ~previewRahs;
            return (setFlags, clearFlags);
        }
    }
}

using Mfr.Models;
using Serilog;

namespace Mfr.Core
{
    /// <summary>
    /// Provides rename-item helpers for filter-based preview name transformation and preview formatting.
    /// </summary>
    public static class RenameItemExtensions
    {
        /// <summary>
        /// Logs debug details for one item when preview produced path or attribute changes.
        /// </summary>
        /// <param name="renameItem">The previewed item to inspect.</param>
        internal static void LogPreviewChangeDetail(this RenameItem renameItem)
        {
            if (!renameItem.HasPreviewChanges())
            {
                return;
            }

            var originalFullPath = renameItem.Original.FullPath;
            Log.Debug("Preview changes for {OriginalFullPath}:", originalFullPath);

            var previewChanges = renameItem.GetPreviewPropertyChanges();
            foreach (var change in previewChanges)
            {
                // Property on its own line; old then new below with fixed indent (avoids console-prefix alignment math).
                var changeBlock = change.FormatPreviewChangeBlock();
                Log.Debug("{PreviewChangeBlock}", changeBlock);
            }

            if (renameItem.Status == RenameStatus.PreviewError)
            {
                var previewErrorMessage = renameItem.PreviewError?.Message ?? "Unknown preview error.";
                Log.Debug(
                    "  Error: '{PreviewErrorMessage}'",
                    previewErrorMessage);
            }
        }

        /// <summary>
        /// Formats preview property deltas for this item as plain text suitable for console output.
        /// </summary>
        /// <param name="renameItem">The previewed item to describe.</param>
        /// <returns>
        /// Non-empty text when the preview differs from the original (path, attributes, or timestamps); otherwise an empty string.
        /// When there are no per-property deltas but paths still differ, the full source and destination paths are shown.
        /// </returns>
        public static string FormatPreviewChangesForDisplay(this RenameItem renameItem)
        {
            if (!renameItem.HasPreviewChanges())
            {
                return string.Empty;
            }

            var previewChanges = renameItem.GetPreviewPropertyChanges();
            if (previewChanges.Count == 0)
            {
                return $"{renameItem.Original.FullPath} --> {renameItem.Preview.FullPath}";
            }

            return string.Join(Environment.NewLine, previewChanges.Select(change => change.FormatPreviewChangeBlock()));
        }

        /// <summary>
        /// Returns prefix, extension, and directory deltas between <see cref="RenameItem.Original"/> and <see cref="RenameItem.Preview"/>.
        /// </summary>
        /// <param name="renameItem">The item to inspect.</param>
        /// <returns>Property-level changes; may be empty when paths differ only in ways not modeled here.</returns>
        internal static IReadOnlyList<RenamePropertyChange> GetPreviewPropertyChanges(this RenameItem renameItem)
        {
            var changes = new List<RenamePropertyChange>();
            var prefixChanged = !string.Equals(renameItem.Original.Prefix, renameItem.Preview.Prefix, StringComparison.Ordinal);
            if (prefixChanged)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "Prefix",
                    OldValue: renameItem.Original.Prefix,
                    NewValue: renameItem.Preview.Prefix));
            }

            var extensionChanged = !string.Equals(renameItem.Original.Extension, renameItem.Preview.Extension, StringComparison.Ordinal);
            if (extensionChanged)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "Extension",
                    OldValue: renameItem.Original.Extension,
                    NewValue: renameItem.Preview.Extension));
            }

            var directoryChanged = !string.Equals(renameItem.Original.DirectoryPath, renameItem.Preview.DirectoryPath, StringComparison.OrdinalIgnoreCase);
            if (directoryChanged)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "DirectoryPath",
                    OldValue: renameItem.Original.DirectoryPath,
                    NewValue: renameItem.Preview.DirectoryPath));
            }

            var attributesChanged = renameItem.Original.Attributes != renameItem.Preview.Attributes;
            if (attributesChanged)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "Attributes",
                    OldValue: renameItem.Original.Attributes.ToString(),
                    NewValue: renameItem.Preview.Attributes.ToString()));
            }

            if (renameItem.Original.CreationTime != renameItem.Preview.CreationTime)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "CreationTime",
                    OldValue: renameItem.Original.CreationTime.ToString("O"),
                    NewValue: renameItem.Preview.CreationTime.ToString("O")));
            }

            if (renameItem.Original.LastWriteTime != renameItem.Preview.LastWriteTime)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "LastWriteTime",
                    OldValue: renameItem.Original.LastWriteTime.ToString("O"),
                    NewValue: renameItem.Preview.LastWriteTime.ToString("O")));
            }

            if (renameItem.Original.LastAccessTime != renameItem.Preview.LastAccessTime)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "LastAccessTime",
                    OldValue: renameItem.Original.LastAccessTime.ToString("O"),
                    NewValue: renameItem.Preview.LastAccessTime.ToString("O")));
            }

            return changes;
        }
    }
}

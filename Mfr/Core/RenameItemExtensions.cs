using Mfr.Models;
using Mfr.Models.Filters;
using Serilog;

namespace Mfr.Core
{
    /// <summary>
    /// Provides rename-item helpers for filter-based preview name transformation and preview formatting.
    /// </summary>
    public static class RenameItemExtensions
    {
        /// <summary>
        /// Applies enabled filters to update the item's preview file name.
        /// </summary>
        /// <param name="item">The rename item receiving transformed preview metadata.</param>
        /// <param name="filters">The configured filters to apply in order.</param>
        public static void ApplyFilters(this RenameItem item, IReadOnlyList<Filter> filters)
        {
            item.ClearPreview();

            foreach (var filter in filters)
            {
                filter.Apply(item);
            }
        }

        /// <summary>
        /// Logs debug details for one item when preview produced a destination path change.
        /// </summary>
        /// <param name="renameItem">The previewed item to inspect.</param>
        internal static void LogPreviewChangeDetail(this RenameItem renameItem)
        {
            if (renameItem.IsPreviewPathSameAsOriginal())
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
        /// Non-empty text when the preview path differs from the original; otherwise an empty string.
        /// When there are no per-property deltas but paths still differ, the full source and destination paths are shown.
        /// </returns>
        public static string FormatPreviewChangesForDisplay(this RenameItem renameItem)
        {
            if (renameItem.IsPreviewPathSameAsOriginal())
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

            return changes;
        }
    }
}

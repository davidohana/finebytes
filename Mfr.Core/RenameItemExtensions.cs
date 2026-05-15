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
                return;

            var originalFullPath = renameItem.Original.FullPath;
            Log.Debug("Preview changes for {OriginalFullPath}:", originalFullPath);

            var previewChanges = RenamePropertyChangeBuilder.GetPreviewPropertyChanges(renameItem);
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
        /// Non-empty text when the preview differs from the original (path, attributes, timestamps, or embedded audio tags); otherwise an empty string.
        /// When there are no per-property deltas but paths still differ, the full source and destination paths are shown.
        /// </returns>
        public static string FormatPreviewChangesForDisplay(this RenameItem renameItem)
        {
            if (!renameItem.HasPreviewChanges())
                return string.Empty;

            var previewChanges = RenamePropertyChangeBuilder.GetPreviewPropertyChanges(renameItem);
            if (previewChanges.Count == 0)
                return $"{renameItem.Original.FullPath} --> {renameItem.Preview.FullPath}";

            return string.Join(Environment.NewLine, previewChanges.Select(change => change.FormatPreviewChangeBlock()));
        }
    }
}

using Mfr.Utils;

namespace Mfr.Models
{
    /// <summary>
    /// Reads and writes preview string fields addressed by polymorphic filter targets.
    /// </summary>
    internal static class FilterTargetStringResolver
    {
        /// <summary>
        /// Returns the preview string for <paramref name="target"/> on <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The rename item.</param>
        /// <param name="target">The filter target.</param>
        /// <returns>The current preview value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an ancestor-folder level argument is invalid; see <see cref="DirectoryPathAncestor"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an ancestor-folder segment cannot be resolved; see <see cref="DirectoryPathAncestor"/>.</exception>
        /// <exception cref="NotSupportedException">Thrown when no handler exists for <paramref name="target"/>.</exception>
        internal static string GetPreviewString(RenameItem item, FilterTarget target)
        {
            return target switch
            {
                FileNameTarget fileNameTarget => _GetFileNamePreviewString(
                    item,
                    fileNamePart: fileNameTarget.FileNamePart),
                AncestorFolderTarget ancestorFolderTarget =>
                    DirectoryPathAncestor.GetSegmentName(
                        containingDirectoryPath: item.Preview.DirectoryPath,
                        level: ancestorFolderTarget.Level),
                _ => throw new NotSupportedException($"Unsupported filter target '{target.GetType().Name}'.")
            };
        }

        /// <summary>
        /// Writes the transformed preview string for <paramref name="target"/> on <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The rename item.</param>
        /// <param name="target">The filter target.</param>
        /// <param name="value">The transformed value.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an ancestor-folder level argument is invalid; see <see cref="DirectoryPathAncestor"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not a valid ancestor-folder segment replacement; see <see cref="DirectoryPathAncestor"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an ancestor-folder segment cannot be resolved; see <see cref="DirectoryPathAncestor"/>.</exception>
        /// <exception cref="NotSupportedException">Thrown when no handler exists for <paramref name="target"/>.</exception>
        internal static void SetPreviewString(RenameItem item, FilterTarget target, string value)
        {
            switch (target)
            {
                case FileNameTarget fileNameTarget:
                    item.SetPreviewValue(fileNameTarget.FileNamePart, value);
                    return;
                case AncestorFolderTarget ancestorFolderTarget:
                    item.Preview.DirectoryPath = DirectoryPathAncestor.ReplaceSegment(
                        containingDirectoryPath: item.Preview.DirectoryPath,
                        level: ancestorFolderTarget.Level,
                        newSegmentName: value);
                    return;
                default:
                    throw new NotSupportedException($"Unsupported filter target '{target.GetType().Name}'.");
            }
        }

        private static string _GetFileNamePreviewString(RenameItem item, FileNamePart fileNamePart)
        {
            var preview = item.Preview;
            return fileNamePart switch
            {
                FileNamePart.Prefix => preview.Prefix,
                FileNamePart.Extension => preview.Extension,
                FileNamePart.Full => preview.Prefix + preview.Extension,
                _ => throw new InvalidOperationException($"Unknown FileNamePart '{fileNamePart}'.")
            };
        }
    }
}

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
        /// <exception cref="NotSupportedException">Thrown when no handler exists for <paramref name="target"/>.</exception>
        internal static string GetPreviewString(RenameItem item, FilterTarget target)
        {
            return target switch
            {
                FileNameTarget fileNameTarget => _GetFileNamePreviewString(
                    item,
                    fileNamePart: fileNameTarget.FileNamePart),
                _ => throw new NotSupportedException($"Unsupported filter target '{target.GetType().Name}'.")
            };
        }

        /// <summary>
        /// Writes the transformed preview string for <paramref name="target"/> on <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The rename item.</param>
        /// <param name="target">The filter target.</param>
        /// <param name="value">The transformed value.</param>
        /// <exception cref="NotSupportedException">Thrown when no handler exists for <paramref name="target"/>.</exception>
        internal static void SetPreviewString(RenameItem item, FilterTarget target, string value)
        {
            switch (target)
            {
                case FileNameTarget fileNameTarget:
                    item.SetPreviewValue(fileNameTarget.FileNamePart, value);
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

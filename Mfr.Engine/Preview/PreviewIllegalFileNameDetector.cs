using Mfr.Utils;

namespace Mfr.Engine.Preview
{
    /// <summary>
    /// Marks preview rows whose final file name is illegal on Windows (MFR7 <c>VerifyCurrentValues</c> name check).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Runs after filters so <see cref="RenameItem.Preview"/> keeps the attempted name for the grid; rows already in
    /// <see cref="RenameStatus.PreviewError"/> are left alone.
    /// </para>
    /// </remarks>
    internal static class PreviewIllegalFileNameDetector
    {
        /// <summary>
        /// Marks each PreviewOk item whose <see cref="FileMeta.FullFileName"/> contains Windows-illegal characters.
        /// </summary>
        /// <param name="items">All rename items participating in the current preview pass.</param>
        internal static void MarkIllegalNames(IReadOnlyList<RenameItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            foreach (var item in items)
            {
                if (item.Status != RenameStatus.PreviewOk)
                {
                    continue;
                }

                var fullFileName = item.Preview.FullFileName;
                var invalidChars = WindowsFileNameChars.FindInvalid(fullFileName);
                if (invalidChars.Count == 0)
                {
                    continue;
                }

                var formattedChars = WindowsFileNameChars.FormatInvalidForMessage(invalidChars);
                item.SetPreviewError(
                    message: $"Target name '{fullFileName}' contains illegal characters: {formattedChars}.",
                    cause: null
                );
            }
        }
    }
}

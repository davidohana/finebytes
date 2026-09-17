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
        /// Marks each PreviewOk item whose <see cref="FileMeta.FullFileName"/> is empty, has illegal characters,
        /// or ends with a space or period.
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

                var message = WindowsFileNameChars.DescribeIllegality(item.Preview.FullFileName);
                if (message is null)
                {
                    continue;
                }

                item.SetPreviewError(message: message, cause: null);
            }
        }
    }
}

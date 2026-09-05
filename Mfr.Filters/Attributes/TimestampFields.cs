using System.Diagnostics;

namespace Mfr.Filters.Attributes
{
    /// <summary>
    /// Applies a transform to one filesystem timestamp on <see cref="FileMeta"/>.
    /// </summary>
    internal static class TimestampFields
    {
        /// <summary>
        /// Replaces the selected timestamp on <paramref name="preview"/> with <paramref name="update"/>(current).
        /// </summary>
        /// <param name="preview">Preview metadata to mutate.</param>
        /// <param name="field">Which timestamp to update.</param>
        /// <param name="update">Maps the current value to the new value.</param>
        internal static void Update(FileMeta preview, TimestampField field, Func<DateTime, DateTime> update)
        {
            ArgumentNullException.ThrowIfNull(preview);
            ArgumentNullException.ThrowIfNull(update);

            switch (field)
            {
                case TimestampField.Creation:
                    preview.CreationTime = update(preview.CreationTime);
                    break;
                case TimestampField.LastWrite:
                    preview.LastWriteTime = update(preview.LastWriteTime);
                    break;
                case TimestampField.LastAccess:
                    preview.LastAccessTime = update(preview.LastAccessTime);
                    break;
                default:
                    throw new UnreachableException();
            }
        }
    }
}

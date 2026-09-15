using System.Diagnostics;
using Mfr.Models.Media;

namespace Mfr.Models.Rename
{
    /// <summary>
    /// Reads and writes <see cref="FileMeta"/> filesystem timestamps by <see cref="TimestampField"/>.
    /// </summary>
    internal static class FileMetaTimestampExtensions
    {
        /// <summary>
        /// Returns the timestamp property addressed by <paramref name="field"/>.
        /// </summary>
        /// <param name="meta">Metadata snapshot.</param>
        /// <param name="field">Which filesystem timestamp to read.</param>
        /// <returns>The current value.</returns>
        internal static DateTime GetTimestamp(this FileMeta meta, TimestampField field)
        {
            ArgumentNullException.ThrowIfNull(meta);

            return field switch
            {
                TimestampField.Creation => meta.CreationTime,
                TimestampField.LastWrite => meta.LastWriteTime,
                TimestampField.LastAccess => meta.LastAccessTime,
                _ => throw new UnreachableException(),
            };
        }

        /// <summary>
        /// Assigns the timestamp property addressed by <paramref name="field"/>.
        /// </summary>
        /// <param name="meta">Metadata snapshot to mutate.</param>
        /// <param name="field">Which filesystem timestamp to write.</param>
        /// <param name="value">New timestamp value.</param>
        internal static void SetTimestamp(this FileMeta meta, TimestampField field, DateTime value)
        {
            ArgumentNullException.ThrowIfNull(meta);

            switch (field)
            {
                case TimestampField.Creation:
                    meta.CreationTime = value;
                    return;
                case TimestampField.LastWrite:
                    meta.LastWriteTime = value;
                    return;
                case TimestampField.LastAccess:
                    meta.LastAccessTime = value;
                    return;
                default:
                    throw new UnreachableException();
            }
        }

        /// <summary>
        /// Replaces the selected timestamp with <paramref name="update"/>(current).
        /// </summary>
        /// <param name="meta">Metadata snapshot to mutate.</param>
        /// <param name="field">Which filesystem timestamp to update.</param>
        /// <param name="update">Maps the current value to the new value.</param>
        internal static void UpdateTimestamp(this FileMeta meta, TimestampField field, Func<DateTime, DateTime> update)
        {
            ArgumentNullException.ThrowIfNull(meta);
            ArgumentNullException.ThrowIfNull(update);

            meta.SetTimestamp(field, update(meta.GetTimestamp(field)));
        }
    }
}

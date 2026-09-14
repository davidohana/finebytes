using System.Text;
using RenameLogModel = Mfr.Models.Rename.RenameLog;

namespace Mfr.App.Ui.Services.RenameLog
{
    /// <summary>
    /// User-facing Rename Log list titles and details-pane text.
    /// </summary>
    internal static class RenameLogDisplay
    {
        /// <summary>
        /// Max per-item blocks shown in <see cref="FormatDetails"/> before a truncation note.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Large GO/Undo logs (thousands of rows) must not build multi-megabyte strings for the
        /// details TextBox — that freezes the Rename Log dialog on open/select.
        /// </para>
        /// </remarks>
        public const int MaxDetailsEntries = 100;

        /// <summary>
        /// Builds the Rename Log list title for a commit time (MFR7 <c>dd/MM/yyyy HH:mm:ss</c> local).
        /// </summary>
        /// <param name="committedAt">
        /// Commit timestamp (typically UTC from <see cref="RenameLogModel.CommittedAt"/>).
        /// </param>
        /// <returns>Local-time list title matching disk rows and the details pane date line.</returns>
        public static string FormatListTitle(DateTimeOffset committedAt)
        {
            return committedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
        }

        /// <summary>
        /// Builds the Rename Log list subtitle (GO/Undo and entry count) for a loaded log.
        /// </summary>
        /// <param name="log">Log whose operation kind and entry count are shown.</param>
        /// <returns>
        /// Compact summary such as <c>GO · 3 items</c> or <c>Undo · 1 item</c>, matching the
        /// details-pane Operation / Processed lines.
        /// </returns>
        public static string FormatListSummary(RenameLogModel log)
        {
            ArgumentNullException.ThrowIfNull(log);

            var operation = log.IsUndo ? "Undo" : "GO";
            var count = log.Entries.Count;
            var itemsLabel = count == 1 ? "item" : "items";
            return $"{operation} · {count} {itemsLabel}";
        }

        /// <summary>
        /// Builds the Rename Log list title for a disk file (MFR7 <c>dd/MM/yyyy HH:mm:ss</c> from stamp).
        /// </summary>
        /// <param name="filePath">Absolute or relative <c>.mfrlog</c> path.</param>
        /// <returns>Formatted stamp when the stem is <c>yyyyMMddHHmmss</c> (optional <c>-N</c>); otherwise the stem.</returns>
        public static string FormatDiskListTitle(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            var stem = Path.GetFileNameWithoutExtension(filePath);
            var stamp = stem;
            var dashIndex = stem.IndexOf('-');
            if (dashIndex > 0)
            {
                stamp = stem[..dashIndex];
            }

            if (stamp.Length != 14 || !stamp.All(char.IsDigit))
            {
                return stem;
            }

            return string.Create(
                19,
                stamp,
                static (span, value) =>
                {
                    span[0] = value[6];
                    span[1] = value[7];
                    span[2] = '/';
                    span[3] = value[4];
                    span[4] = value[5];
                    span[5] = '/';
                    span[6] = value[0];
                    span[7] = value[1];
                    span[8] = value[2];
                    span[9] = value[3];
                    span[10] = ' ';
                    span[11] = value[8];
                    span[12] = value[9];
                    span[13] = ':';
                    span[14] = value[10];
                    span[15] = value[11];
                    span[16] = ':';
                    span[17] = value[12];
                    span[18] = value[13];
                }
            );
        }

        /// <summary>
        /// Formats a log for the Rename Log details pane (date, GO/Undo, item count, per-item changes/errors).
        /// </summary>
        /// <param name="log">Log to describe.</param>
        /// <param name="maxEntries">
        /// Max item blocks to include. When fewer than <paramref name="log"/>.<see cref="RenameLogModel.Entries"/>.Count,
        /// appends a truncation note. Defaults to <see cref="MaxDetailsEntries"/>.
        /// </param>
        /// <returns>Multi-line plain text suitable for a read-only details box.</returns>
        public static string FormatDetails(RenameLogModel log, int maxEntries = MaxDetailsEntries)
        {
            ArgumentNullException.ThrowIfNull(log);

            if (maxEntries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxEntries));
            }

            var builder = new StringBuilder();
            // Match Rename Log list titles (MFR7 dd/MM/yyyy HH:mm:ss), not culture "G".
            builder
                .Append("Operation Date: ")
                .Append(log.CommittedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"))
                .AppendLine();
            builder.Append("Operation: ").Append(log.IsUndo ? "Undo" : "GO").AppendLine();
            builder.AppendLine();
            builder.Append("Processed ").Append(log.Entries.Count).Append(" Items").AppendLine();
            builder.AppendLine();

            var shownCount = Math.Min(maxEntries, log.Entries.Count);
            for (var i = 0; i < shownCount; i++)
            {
                var entry = log.Entries[i];
                builder.Append("Item: ").Append(entry.DetailsItemPath).AppendLine();
                foreach (var change in entry.Changes)
                {
                    builder
                        .Append("Changed '")
                        .Append(change.Property)
                        .Append("' from '")
                        .Append(change.OldValue)
                        .Append("' to '")
                        .Append(change.NewValue)
                        .Append('\'')
                        .AppendLine();
                }

                if (entry.Error is not null)
                {
                    builder.Append("Error: ").Append(entry.Error).AppendLine();
                }

                builder.AppendLine();
            }

            var omittedCount = log.Entries.Count - shownCount;
            if (omittedCount > 0)
            {
                builder
                    .Append("… and ")
                    .Append(omittedCount)
                    .Append(" more item(s). Full history is in the saved .mfrlog file.")
                    .AppendLine();
            }

            return builder.ToString();
        }
    }
}

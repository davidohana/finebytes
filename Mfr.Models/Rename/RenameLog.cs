using System.Text;

namespace Mfr.Models.Rename
{
    /// <summary>
    /// One rename-commit operation recorded for Undo Last / Rename Log (JSON <c>.mfrlog</c>).
    /// </summary>
    /// <param name="CommittedAt">When the commit finished (UTC preferred).</param>
    /// <param name="Entries">
    /// Per-item outcomes included in this log (<see cref="RenameStatus.CommitOk"/> and
    /// <see cref="RenameStatus.CommitError"/> rows).
    /// </param>
    /// <param name="IsUndo">
    /// Whether this log was produced by Undo (undo-of-undo) rather than GO. Missing in older files → GO.
    /// </param>
    public sealed record RenameLog(
        DateTimeOffset CommittedAt,
        IReadOnlyList<RenameLogEntry> Entries,
        bool IsUndo = false
    )
    {
        /// <summary>
        /// Whether Undo can reverse at least one row (non-error entry with a restorable property delta).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>StripAllEmbeddedTagsOnCommit</c> alone is not restorable (Tag Remover).
        /// </para>
        /// </remarks>
        public bool HasUndoableEntries => Entries.Any(static entry => entry.IsUndoable);

        /// <summary>
        /// Formats this log for the Rename Log details pane (date, GO/Undo, item count, per-item changes/errors).
        /// </summary>
        /// <returns>Multi-line plain text suitable for a read-only details box.</returns>
        public string FormatDetails()
        {
            var builder = new StringBuilder();
            // Match Rename Log list titles (MFR7 dd/MM/yyyy HH:mm:ss), not culture "G".
            builder
                .Append("Operation Date: ")
                .Append(CommittedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"))
                .AppendLine();
            builder.Append("Operation: ").Append(IsUndo ? "Undo" : "GO").AppendLine();
            builder.AppendLine();
            builder.Append("Processed ").Append(Entries.Count).Append(" Items").AppendLine();
            builder.AppendLine();

            foreach (var entry in Entries)
            {
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

            return builder.ToString();
        }
    }

    /// <summary>
    /// One file or folder row in a <see cref="RenameLog"/>.
    /// </summary>
    /// <param name="DestinationPath">
    /// Post-commit path when successful (undo opens this path). For commit-error rows, the attempted
    /// destination or <paramref name="OriginalPath"/> when destination was blank.
    /// </param>
    /// <param name="OriginalPath">Pre-commit path when known (audit / display).</param>
    /// <param name="IsFolder">Whether the entry is a directory.</param>
    /// <param name="Changes">Property-level Old→New deltas applied at commit.</param>
    /// <param name="Error">Optional commit-error message; undo skips rows with errors.</param>
    public sealed record RenameLogEntry(
        string DestinationPath,
        string OriginalPath,
        bool IsFolder,
        IReadOnlyList<RenamePropertyChange> Changes,
        string? Error = null
    )
    {
        /// <summary>
        /// Path shown as details <c>Item:</c> — <see cref="OriginalPath"/> when this row has an
        /// <see cref="Error"/> (file still at source); otherwise <see cref="DestinationPath"/>.
        /// </summary>
        public string DetailsItemPath => Error is not null ? OriginalPath : DestinationPath;

        /// <summary>
        /// Whether Undo can reverse this row (no error, and at least one restorable change).
        /// </summary>
        public bool IsUndoable =>
            Error is null
            && Changes.Any(static change =>
                !string.Equals(change.Property, "StripAllEmbeddedTagsOnCommit", StringComparison.Ordinal)
            );
    }
}

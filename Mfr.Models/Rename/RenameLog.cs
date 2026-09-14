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
        /// <see cref="RenamePropertyNames.StripAllEmbeddedTagsOnCommit"/> alone is not restorable (Tag Remover).
        /// </para>
        /// </remarks>
        public bool HasUndoableEntries => Entries.Any(static entry => entry.IsUndoable);
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
                !string.Equals(
                    change.Property,
                    RenamePropertyNames.StripAllEmbeddedTagsOnCommit,
                    StringComparison.Ordinal
                )
            );
    }
}

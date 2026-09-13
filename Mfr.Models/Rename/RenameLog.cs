namespace Mfr.Models.Rename
{
    /// <summary>
    /// One rename-commit operation recorded for Undo Last / Rename Log (JSON <c>.mfrlog</c>).
    /// </summary>
    /// <param name="CommittedAt">When the commit finished (UTC preferred).</param>
    /// <param name="Entries">Per-item outcomes included in this log (typically <see cref="RenameStatus.CommitOk"/> rows).</param>
    public sealed record RenameLog(DateTimeOffset CommittedAt, IReadOnlyList<RenameLogEntry> Entries);

    /// <summary>
    /// One file or folder row in a <see cref="RenameLog"/>.
    /// </summary>
    /// <param name="DestinationPath">Post-commit path on disk (undo opens this path).</param>
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
    );
}

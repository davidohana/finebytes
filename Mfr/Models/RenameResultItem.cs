namespace Mfr.Models
{
    /// <summary>
    /// Represents the commit outcome for one rename item.
    /// </summary>
    /// <param name="OriginalPath">Original source path before commit.</param>
    /// <param name="ResultPath">Resulting destination path or original path when skipped.</param>
    /// <param name="Status">Final preview/commit status for this row.</param>
    /// <param name="Error">Optional error message when commit fails.</param>
    public sealed record RenameResultItem(
        string OriginalPath,
        string ResultPath,
        RenameStatus Status,
        string? Error);
}

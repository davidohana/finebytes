namespace Mfr.Engine.RenameScript
{
    /// <summary>
    /// One filesystem operation emitted into a rename script (path rename/move or RAHS attrs).
    /// </summary>
    public abstract record RenameScriptOp;

    /// <summary>
    /// Same-parent rename (<c>ren</c> / <c>Rename-Item</c>), including case-only name changes.
    /// </summary>
    /// <param name="SourceFullPath">Original full path.</param>
    /// <param name="DestinationFileName">New file name including extension (not a full path).</param>
    public sealed record RenameSameFolder(string SourceFullPath, string DestinationFileName) : RenameScriptOp;

    /// <summary>
    /// Cross-folder move with destination-parent creation (<c>mkdir</c>+<c>move</c> / <c>New-Item</c>+<c>Move-Item</c>).
    /// </summary>
    /// <param name="SourceFullPath">Original full path.</param>
    /// <param name="DestinationDirectory">Preview parent directory to create when missing.</param>
    /// <param name="DestinationFullPath">Preview full destination path.</param>
    public sealed record MoveWithParent(string SourceFullPath, string DestinationDirectory, string DestinationFullPath)
        : RenameScriptOp;

    /// <summary>
    /// RAHS attribute deltas applied to <paramref name="TargetFullPath"/> (preview path after rename/move).
    /// </summary>
    /// <param name="TargetFullPath">Path to attribute after any path op for the same row.</param>
    /// <param name="SetFlags">RAHS bits to turn on.</param>
    /// <param name="ClearFlags">RAHS bits to turn off.</param>
    public sealed record SetRahsAttributes(string TargetFullPath, FileAttributes SetFlags, FileAttributes ClearFlags)
        : RenameScriptOp;
}

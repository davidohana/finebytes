namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Outcome of <see cref="RenameList.Undo"/> including rows that never loaded from disk.
    /// </summary>
    /// <param name="Results">Per-item commit outcomes from the undo re-commit.</param>
    /// <param name="NotLoadedCount">
    /// Undoable log rows whose <c>DestinationPath</c> was missing or otherwise failed to load into the list.
    /// </param>
    public sealed record RenameListUndoResult(IReadOnlyList<RenameResultItem> Results, int NotLoadedCount);
}

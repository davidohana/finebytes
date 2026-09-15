namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Outcome of <see cref="RenameList.PrepareUndo"/> including rows that never loaded from disk.
    /// </summary>
    /// <param name="Plan">Preview commit plan for the prepared undo session (pass to <see cref="RenameList.Commit"/> on GO).</param>
    /// <param name="PreparedCount">Undoable log rows successfully loaded and seeded.</param>
    /// <param name="NotLoadedCount">
    /// Undoable log rows whose <c>DestinationPath</c> was missing or otherwise failed to load into the list.
    /// </param>
    public sealed record RenameListPrepareUndoResult(CommitPlan Plan, int PreparedCount, int NotLoadedCount);
}

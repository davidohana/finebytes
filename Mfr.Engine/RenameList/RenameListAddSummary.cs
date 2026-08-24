namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Outcome summary for one <see cref="RenameList.AddSources"/> call.
    /// </summary>
    /// <param name="SkippedSourceCount">Sources that could not be resolved (for example access denied).</param>
    public sealed record RenameListAddSummary(int SkippedSourceCount);
}

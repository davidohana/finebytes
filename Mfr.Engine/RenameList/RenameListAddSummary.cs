namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Outcome summary for one <see cref="RenameList.AddSources"/> call.
    /// </summary>
    /// <param name="SkippedSourceCount">Sources that could not be resolved (for example access denied).</param>
    /// <param name="WasCanceled">Whether the add stopped because cancel was requested.</param>
    /// <param name="KeptPartial">
    /// Whether a canceled add inserted the staging batch via <see cref="RenameListAddCancelDisposition.KeepPartial"/>.
    /// </param>
    public sealed record RenameListAddSummary(
        int SkippedSourceCount,
        bool WasCanceled = false,
        bool KeptPartial = false
    );
}

namespace Mfr.Engine.RenameList
{
    /// <summary>
    /// Mutable cancel disposition for one <see cref="RenameList.AddSources"/> call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The UI may set <see cref="KeepPartial"/> at cancel time so the engine inserts the staging
    /// batch instead of discarding it. Default is discard (same as cancel without a holder).
    /// </para>
    /// </remarks>
    public sealed class RenameListAddCancelDisposition
    {
        /// <summary>
        /// Gets or sets whether a canceled add should insert the staging batch instead of discarding it.
        /// </summary>
        public bool KeepPartial { get; set; }
    }
}

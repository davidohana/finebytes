namespace Mfr.App.Ui.ViewModels.Options
{
    /// <summary>
    /// Options draft mode for <c>renameLog.limit</c> (Disabled / Limited / Unlimited).
    /// </summary>
    public enum RenameLogRetentionMode
    {
        /// <summary>
        /// Disk logging off (<c>limit = 0</c>); in-memory last operation still available for Undo Last.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// Keep the newest N on-disk rename logs (<c>limit = N</c>).
        /// </summary>
        Limited = 1,

        /// <summary>
        /// Keep all on-disk rename logs (<c>limit = <see cref="int.MaxValue"/></c>).
        /// </summary>
        Unlimited = 2,
    }
}

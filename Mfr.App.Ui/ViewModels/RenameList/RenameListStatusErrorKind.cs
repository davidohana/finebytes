namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Status-column error kind for the Rename List <c>!</c> glyph tip (highest priority wins).
    /// </summary>
    public enum RenameListStatusErrorKind
    {
        /// <summary>No status error.</summary>
        None,

        /// <summary>Metadata load failure or missing from disk.</summary>
        LoadOrMissing,

        /// <summary>Last preview failed for this row.</summary>
        Preview,

        /// <summary>Last rename/commit failed for this row.</summary>
        Commit,
    }
}

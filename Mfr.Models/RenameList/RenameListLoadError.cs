namespace Mfr.Models.RenameList
{
    /// <summary>
    /// One Rename List row issue shown in Show Error Details (metadata reader or missing path).
    /// </summary>
    /// <param name="UserExplanation">Plain-language explanation for the issue.</param>
    /// <param name="TechnicalDetails">Stored reader exception message, or the missing path.</param>
    /// <param name="IsMissingFromDisk">
    /// <see langword="true"/> when this entry is a missing path rather than a metadata-reader failure.
    /// </param>
    public sealed record RenameListLoadError(
        string UserExplanation,
        string TechnicalDetails,
        bool IsMissingFromDisk = false
    );
}

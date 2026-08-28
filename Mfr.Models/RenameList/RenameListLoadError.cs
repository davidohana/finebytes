namespace Mfr.Models.RenameList
{
    /// <summary>
    /// One original metadata-reader failure on a rename row.
    /// </summary>
    /// <param name="UserExplanation">Plain-language explanation for the failed reader.</param>
    /// <param name="TechnicalDetails">Stored reader exception message for support and copy/paste.</param>
    public sealed record RenameListLoadError(string UserExplanation, string TechnicalDetails);
}

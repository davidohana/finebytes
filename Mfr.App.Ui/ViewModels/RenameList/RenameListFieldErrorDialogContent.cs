namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Content for the Rename List Show Field Error dialog.
    /// </summary>
    /// <param name="FieldDisplayName">Grid column label for the failed field.</param>
    /// <param name="UserExplanation">Plain-language explanation for the failure.</param>
    /// <param name="TechnicalDetails">Stored reader exception message for support and copy/paste.</param>
    public sealed record RenameListFieldErrorDialogContent(
        string FieldDisplayName,
        string UserExplanation,
        string TechnicalDetails
    );
}

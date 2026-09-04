namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Payload for the Rename List Show Preview Error dialog.
    /// </summary>
    /// <param name="FilePath">Absolute path of the errored row.</param>
    /// <param name="Message">User-facing preview error message.</param>
    /// <param name="TechnicalDetails">Optional exception text for the details box.</param>
    public sealed record RenameListPreviewErrorDialogContent(string FilePath, string Message, string? TechnicalDetails);
}

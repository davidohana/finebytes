namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Payload for the shared Rename List row-error dialog (load, preview, and later apply).
    /// </summary>
    /// <param name="Title">Window title.</param>
    /// <param name="Summary">Headline shown at the top of the dialog.</param>
    /// <param name="FilePath">Absolute path of the errored row.</param>
    /// <param name="DetailsText">Folded details for the read-only box.</param>
    public sealed record RenameListRowErrorDialogContent(
        string Title,
        string Summary,
        string FilePath,
        string DetailsText
    );
}

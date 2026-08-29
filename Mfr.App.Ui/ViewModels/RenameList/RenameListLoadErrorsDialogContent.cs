using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Content for the Rename List Show Load Errors dialog (all reader failures on the row).
    /// </summary>
    /// <param name="FilePath">Original absolute path for the selected row.</param>
    /// <param name="Errors">Distinct TagLib and/or image load failures.</param>
    public sealed record RenameListLoadErrorsDialogContent(string FilePath, IReadOnlyList<RenameListLoadError> Errors);
}

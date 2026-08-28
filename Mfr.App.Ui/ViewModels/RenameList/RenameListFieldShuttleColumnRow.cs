using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// One selected column row in the field shuttle Columns tab.
    /// </summary>
    /// <param name="Index">Zero-based index in the draft visible-column list.</param>
    /// <param name="Column">Draft visible column.</param>
    public sealed record RenameListFieldShuttleColumnRow(int Index, RenameListVisibleColumn Column)
    {
        /// <summary>
        /// Gets the catalog field display name without preview decoration.
        /// </summary>
        public string DisplayName => RenameListFieldCatalog.GetField(Column.Key).DisplayName;

        /// <summary>
        /// Gets whether the selected column shows preview values.
        /// </summary>
        public bool IsPreview => Column.Key.IsPreview;
    }
}

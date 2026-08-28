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
        /// Gets the user-visible label, including the preview suffix when applicable.
        /// </summary>
        public string Label
        {
            get
            {
                var catalogField = RenameListFieldCatalog.GetField(Column.Key);
                return RenameListFieldDisplay.GetColumnHeaderText(catalogField, Column.Key.IsPreview);
            }
        }
    }
}

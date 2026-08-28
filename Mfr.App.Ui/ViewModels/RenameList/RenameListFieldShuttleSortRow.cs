using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// One selected sort key row in the field shuttle Sort tab.
    /// </summary>
    /// <param name="Index">Zero-based index in the draft sort-key list.</param>
    /// <param name="Key">Draft sort key.</param>
    public sealed record RenameListFieldShuttleSortRow(int Index, RenameListSortKey Key)
    {
        /// <summary>
        /// Gets the 1-based Auto-Sort priority shown beside the row.
        /// </summary>
        public int Priority => Index + 1;

        /// <summary>
        /// Gets the user-visible field label.
        /// </summary>
        public string Label => RenameListSortDisplay.GetFieldLabel(Key.FieldKey);

        /// <summary>
        /// Gets the direction glyph for the row toggle.
        /// </summary>
        public string DirectionGlyph => Key.Descending ? "↓" : "↑";
    }
}

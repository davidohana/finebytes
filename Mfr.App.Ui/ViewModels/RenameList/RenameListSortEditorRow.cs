using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// One row in the Rename List sort editor flyout.
    /// </summary>
    /// <param name="Index">Zero-based index in <see cref="RenameListViewModel.SortKeys"/>.</param>
    /// <param name="Key">Sort key at that index.</param>
    public sealed record RenameListSortEditorRow(int Index, RenameListSortKey Key)
    {
        /// <summary>
        /// Gets the 1-based priority shown beside the row.
        /// </summary>
        public int DisplayPriority => Index + 1;

        /// <summary>
        /// Gets the direction glyph for the row toggle.
        /// </summary>
        public string DirectionGlyph => Key.Descending ? "↓" : "↑";
    }
}

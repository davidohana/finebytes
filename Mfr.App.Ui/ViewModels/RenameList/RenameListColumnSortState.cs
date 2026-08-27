namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Sort priority and direction for one Rename List grid column header.
    /// </summary>
    /// <param name="Priority">1-based sort level when active; otherwise <see langword="null"/>.</param>
    /// <param name="IsDescending">Whether the active key sorts descending.</param>
    public readonly record struct RenameListColumnSortState(int? Priority, bool IsDescending)
    {
        /// <summary>
        /// Gets whether this column participates in the active sort.
        /// </summary>
        public bool IsActive => Priority.HasValue;

        /// <summary>
        /// Gets the direction glyph shown beside the priority number.
        /// </summary>
        public string DirectionGlyph => IsDescending ? "↓" : "↑";
    }
}

using Mfr.Models.RenameList;

namespace Mfr.App.Ui.ViewModels.RenameList
{
    /// <summary>
    /// Sort header state keyed by column. Columns absent from the sort list are inactive.
    /// </summary>
    public sealed class RenameListColumnSortStates
    {
        private readonly IReadOnlyDictionary<RenameListSortColumn, RenameListColumnSortState> _columnToState;

        /// <summary>
        /// Initializes a lookup from sort column to header glyph state.
        /// </summary>
        /// <param name="columnToState">Active columns and their glyph state.</param>
        public RenameListColumnSortStates(
            IReadOnlyDictionary<RenameListSortColumn, RenameListColumnSortState> columnToState
        )
        {
            ArgumentNullException.ThrowIfNull(columnToState);
            _columnToState = columnToState;
        }

        /// <summary>
        /// Empty lookup (Auto-Sort off).
        /// </summary>
        public static RenameListColumnSortStates Inactive { get; } =
            new(new Dictionary<RenameListSortColumn, RenameListColumnSortState>());

        /// <summary>
        /// Gets header glyph state for a sort column, or inactive when that column is not a sort key.
        /// </summary>
        /// <param name="column">Sort column.</param>
        /// <returns>Priority and direction, or default when inactive.</returns>
        public RenameListColumnSortState this[RenameListSortColumn column] => _columnToState.GetValueOrDefault(column);
    }
}
